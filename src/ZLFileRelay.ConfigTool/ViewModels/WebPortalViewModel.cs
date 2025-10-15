using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using ZLFileRelay.ConfigTool.Services;
using ZLFileRelay.Core.Models;

namespace ZLFileRelay.ConfigTool.ViewModels;

public partial class WebPortalViewModel : ObservableObject
{
    private readonly ConfigurationService _configurationService;

    // Kestrel Server Settings
    [ObservableProperty] private int _httpPort = 8080;
    [ObservableProperty] private int _httpsPort = 8443;
    [ObservableProperty] private bool _enableHttps = false;
    [ObservableProperty] private string _certificatePath = string.Empty;
    [ObservableProperty] private string _certificateStatus = "No certificate configured";

    // Authentication
    [ObservableProperty] private bool _requireAuthentication = true;
    [ObservableProperty] private string _allowedGroups = string.Empty;


    // Branding Settings (editable)
    [ObservableProperty] private string _companyName = "Your Company";
    [ObservableProperty] private string _siteName = "Main Site";
    [ObservableProperty] private string _supportEmail = "support@example.com";
    [ObservableProperty] private string _logoPath = "";

    // Status
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isValid = true;

    public WebPortalViewModel(ConfigurationService configurationService)
    {
        _configurationService = configurationService;
        LoadFromConfiguration();
    }

    private void LoadFromConfiguration()
    {
        var config = _configurationService.CurrentConfiguration;
        if (config == null) return;

        HttpPort = config.WebPortal.Kestrel.HttpPort;
        HttpsPort = config.WebPortal.Kestrel.HttpsPort;
        EnableHttps = config.WebPortal.Kestrel.EnableHttps;
        CertificatePath = config.WebPortal.Kestrel.CertificatePath ?? string.Empty;
        RequireAuthentication = config.WebPortal.RequireAuthentication;
        AllowedGroups = config.WebPortal.AllowedGroups != null ? string.Join("\n", config.WebPortal.AllowedGroups) : string.Empty;
        
        // Branding (editable)
        CompanyName = config.Branding.CompanyName;
        SiteName = config.Branding.SiteName;
        SupportEmail = config.Branding.SupportEmail;
        LogoPath = config.Branding.LogoPath ?? string.Empty;
        
        ValidateConfiguration();
    }

    [RelayCommand]
    private void BrowseCertificate()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select SSL Certificate",
            Filter = "Certificate Files (*.pfx;*.p12)|*.pfx;*.p12|All Files (*.*)|*.*",
            DefaultExt = ".pfx"
        };

        if (dialog.ShowDialog() == true)
        {
            CertificatePath = dialog.FileName;
            ValidateConfiguration();
        }
    }

    [RelayCommand]
    private void BrowseLogo()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Company Logo",
            Filter = "Image Files (*.png;*.jpg;*.jpeg;*.gif;*.svg)|*.png;*.jpg;*.jpeg;*.gif;*.svg|All Files (*.*)|*.*",
            DefaultExt = ".png"
        };

        if (dialog.ShowDialog() == true)
        {
            LogoPath = dialog.FileName;
        }
    }

    [RelayCommand]
    private async Task TestCertificateAsync(PasswordBox? passwordBox)
    {
        if (string.IsNullOrWhiteSpace(CertificatePath))
        {
            CertificateStatus = "❌ No certificate path specified";
            return;
        }

        if (!File.Exists(CertificatePath))
        {
            CertificateStatus = $"❌ Certificate file not found";
            return;
        }

        try
        {
            var password = passwordBox?.Password ?? string.Empty;
            
            // Try to load the certificate
            var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                CertificatePath,
                password);

            CertificateStatus = $"✅ Valid: {cert.Subject} (Expires: {cert.NotAfter:yyyy-MM-dd})";
            IsValid = true;
        }
        catch (Exception ex)
        {
            CertificateStatus = $"❌ Invalid: {ex.Message}";
            IsValid = false;
        }

        await Task.CompletedTask;
    }


    [RelayCommand]
    private async Task RestartWebServiceAsync()
    {
        try
        {
            StatusMessage = "⚠️ Restarting web service... (Feature not yet implemented)";
            // TODO: Implement web service restart logic
            await Task.Delay(1000);
            StatusMessage = "Web service restart would occur here. For now, manually restart the service.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveConfigurationAsync()
    {
        try
        {
            var config = _configurationService.CurrentConfiguration;
            if (config == null)
            {
                StatusMessage = "❌ Configuration not loaded";
                return;
            }

            // Update configuration from UI
            config.WebPortal.Kestrel.HttpPort = HttpPort;
            config.WebPortal.Kestrel.HttpsPort = HttpsPort;
            config.WebPortal.Kestrel.EnableHttps = EnableHttps;
            config.WebPortal.Kestrel.CertificatePath = CertificatePath;
            config.WebPortal.RequireAuthentication = RequireAuthentication;
            
            // Parse allowed groups
            var groups = AllowedGroups
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(g => g.Trim())
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .ToList();
            config.WebPortal.AllowedGroups = groups.Count > 0 ? groups : null;
            
            
            // Save branding settings
            config.Branding.CompanyName = CompanyName;
            config.Branding.SiteName = SiteName;
            config.Branding.SupportEmail = SupportEmail;
            config.Branding.LogoPath = string.IsNullOrWhiteSpace(LogoPath) ? null : LogoPath;

            var success = await _configurationService.SaveAsync(config);
            StatusMessage = success 
                ? "✅ Configuration saved. Restart Web Portal service to apply changes."
                : "❌ Failed to save configuration";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error: {ex.Message}";
        }
    }

    private void ValidateConfiguration()
    {
        var errors = new List<string>();

        if (HttpPort < 1 || HttpPort > 65535)
            errors.Add("HTTP port must be between 1 and 65535");

        if (HttpsPort < 1 || HttpsPort > 65535)
            errors.Add("HTTPS port must be between 1 and 65535");

        if (HttpPort == HttpsPort)
            errors.Add("HTTP and HTTPS ports must be different");

        if (EnableHttps && string.IsNullOrWhiteSpace(CertificatePath))
            errors.Add("Certificate path required when HTTPS is enabled");

        if (EnableHttps && !string.IsNullOrWhiteSpace(CertificatePath) && !File.Exists(CertificatePath))
            errors.Add("Certificate file not found");

        IsValid = errors.Count == 0;
        
        if (!IsValid)
        {
            StatusMessage = "⚠️ " + string.Join(", ", errors);
        }
    }

    partial void OnHttpPortChanged(int value) => ValidateConfiguration();
    partial void OnHttpsPortChanged(int value) => ValidateConfiguration();
    partial void OnEnableHttpsChanged(bool value) => ValidateConfiguration();
    partial void OnCertificatePathChanged(string value) => ValidateConfiguration();
}

