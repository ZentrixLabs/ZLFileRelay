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
    [ObservableProperty] private string _certificatePassword = string.Empty;

    // Authentication
    [ObservableProperty] private bool _requireAuthentication = true;
    [ObservableProperty] private bool _useWindowsAuth = true;

    // File Upload Settings
    [ObservableProperty] private long _maxFileSizeMB = 4096;
    [ObservableProperty] private bool _enableUploadToTransfer = true;

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
        CertificatePassword = config.WebPortal.Kestrel.CertificatePassword ?? string.Empty;
        UseWindowsAuth = config.WebPortal.Kestrel.UseWindowsAuth;
        RequireAuthentication = config.WebPortal.RequireAuthentication;
        MaxFileSizeMB = config.WebPortal.MaxFileSizeBytes / (1024 * 1024);
        EnableUploadToTransfer = config.WebPortal.EnableUploadToTransfer;
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
    private async Task TestCertificateAsync()
    {
        if (string.IsNullOrWhiteSpace(CertificatePath))
        {
            StatusMessage = "❌ No certificate path specified";
            return;
        }

        if (!File.Exists(CertificatePath))
        {
            StatusMessage = $"❌ Certificate file not found: {CertificatePath}";
            return;
        }

        try
        {
            // Try to load the certificate
            var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                CertificatePath,
                CertificatePassword);

            StatusMessage = $"✅ Certificate valid: {cert.Subject} (Expires: {cert.NotAfter:yyyy-MM-dd})";
            IsValid = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Invalid certificate: {ex.Message}";
            IsValid = false;
        }

        await Task.CompletedTask;
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
            config.WebPortal.Kestrel.CertificatePassword = CertificatePassword;
            config.WebPortal.Kestrel.UseWindowsAuth = UseWindowsAuth;
            config.WebPortal.RequireAuthentication = RequireAuthentication;
            config.WebPortal.MaxFileSizeBytes = MaxFileSizeMB * 1024 * 1024;
            config.WebPortal.EnableUploadToTransfer = EnableUploadToTransfer;

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

