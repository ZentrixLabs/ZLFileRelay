using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Linq;
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
        [ObservableProperty] private string _logoPath = "Path to your company logo (relative to web portal root, e.g., 'Assets/logo.png') - Optional";

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
        LogoPath = string.IsNullOrWhiteSpace(config.Branding.LogoPath) 
            ? "Path to your company logo (relative to web portal root, e.g., 'Assets/logo.png') - Optional"
            : config.Branding.LogoPath;
        
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
    private void BrowseGroups()
    {
        var dialog = new Views.ActiveDirectoryGroupBrowser
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.SelectedGroupName))
        {
            // Add the selected group to the list
            if (string.IsNullOrWhiteSpace(AllowedGroups))
            {
                AllowedGroups = dialog.SelectedGroupName;
            }
            else
            {
                // Check if already in the list
                var groups = AllowedGroups.Split('\n').Select(g => g.Trim()).ToList();
                if (!groups.Contains(dialog.SelectedGroupName))
                {
                    AllowedGroups += "\n" + dialog.SelectedGroupName;
                }
            }
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
            StatusMessage = "💾 Saving configuration...";
            
            var config = _configurationService.CurrentConfiguration;
            if (config == null)
            {
                StatusMessage = "❌ Configuration not loaded - please restart the application";
                return;
            }

            // Validate current settings first
            ValidateConfiguration();
            if (!IsValid)
            {
                StatusMessage = "❌ Please fix validation errors before saving";
                return;
            }

            // Update configuration from UI
            config.WebPortal.Kestrel.HttpPort = HttpPort;
            config.WebPortal.Kestrel.HttpsPort = HttpsPort;
            config.WebPortal.Kestrel.EnableHttps = EnableHttps;
            config.WebPortal.Kestrel.CertificatePath = string.IsNullOrWhiteSpace(CertificatePath) ? null : CertificatePath;
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
            // Only save logo path if it's not the placeholder text
            var logoPlaceholder = "Path to your company logo (relative to web portal root, e.g., 'Assets/logo.png') - Optional";
            config.Branding.LogoPath = string.IsNullOrWhiteSpace(LogoPath) || LogoPath == logoPlaceholder ? null : LogoPath;

            // Check if logo file exists if path is provided (and not placeholder)
            if (!string.IsNullOrWhiteSpace(LogoPath) && LogoPath != logoPlaceholder && !File.Exists(LogoPath))
            {
                StatusMessage = $"❌ Logo file not found: {LogoPath}";
                return;
            }

            // Attempt to save with thorough error reporting
            var configPath = _configurationService.GetConfigurationPath();
            var success = await _configurationService.SaveAsync(config);
            
            if (success)
            {
                StatusMessage = $"✅ Configuration saved successfully to:\n{configPath}\n\nRestart Web Portal service to apply changes.";
            }
            else
            {
                StatusMessage = $"❌ Failed to save configuration to:\n{configPath}\n\nCheck the following:\n" +
                              "• Ensure you have write permissions to the config file\n" +
                              "• Check that the config file is not locked by another process\n" +
                              "• Verify all required fields are filled\n" +
                              "• Check application logs for detailed error information";
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            var configPath = _configurationService.GetConfigurationPath();
            StatusMessage = $"❌ Permission denied saving to:\n{configPath}\n\nError: {ex.Message}\n\n" +
                          "Try running the configuration tool as Administrator or check file permissions.";
        }
        catch (DirectoryNotFoundException ex)
        {
            var configPath = _configurationService.GetConfigurationPath();
            StatusMessage = $"❌ Directory not found for:\n{configPath}\n\nError: {ex.Message}\n\n" +
                          "The configuration directory may have been moved or deleted.";
        }
        catch (IOException ex)
        {
            var configPath = _configurationService.GetConfigurationPath();
            StatusMessage = $"❌ File access error for:\n{configPath}\n\nError: {ex.Message}\n\n" +
                          "The configuration file may be locked by another process or the disk may be full.";
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = $"❌ Configuration validation error: {ex.Message}\n\n" +
                          "Please check your settings and try again.";
        }
        catch (Exception ex)
        {
            var configPath = _configurationService.GetConfigurationPath();
            StatusMessage = $"❌ Unexpected error saving to:\n{configPath}\n\nError: {ex.Message}\n\n" +
                          "Please check the application logs for more details.";
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

