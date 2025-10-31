using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using System.Windows;
using ZLFileRelay.ConfigTool.Services;
using ZLFileRelay.Core.Models;
using ZLFileRelay.Core.Constants;
using ZLFileRelay.Core.Interfaces;

namespace ZLFileRelay.ConfigTool.ViewModels;

public partial class WebPortalViewModel : ObservableObject
{
    private readonly ConfigurationService _configurationService;
    private readonly ServiceManager _serviceManager;
    private readonly ICredentialProvider _credentialProvider;

    // Kestrel Server Settings
    [ObservableProperty] private int _httpPort = 8080;
    [ObservableProperty] private int _httpsPort = 8443;
    [ObservableProperty] private bool _enableHttps = false;
    
    // Certificate Configuration - File Path (fallback)
    [ObservableProperty] private string _certificatePath = string.Empty;
    [ObservableProperty] private string _certificatePassword = string.Empty;
    
    // Certificate Configuration - Certificate Store (preferred)
    [ObservableProperty] private string _certificateThumbprint = string.Empty;
    [ObservableProperty] private string _certificateStoreLocation = "LocalMachine";
    [ObservableProperty] private string _certificateStoreName = "My";
    [ObservableProperty] private bool _useCertificateStore = false; // Toggle between store and file path
    
    [ObservableProperty] private string _certificateStatus = "No certificate configured";

    // Authentication Settings
    [ObservableProperty] private bool _enableEntraId = false;
    [ObservableProperty] private bool _enableLocalAccounts = true;
    [ObservableProperty] private string _entraIdTenantId = string.Empty;
    [ObservableProperty] private string _entraIdClientId = string.Empty;
    [ObservableProperty] private string _entraIdClientSecret = string.Empty;
    
    // Note: When Entra ID is enabled, authorization is handled by the Enterprise App assignment
    // When Local Accounts is enabled, all authenticated users are allowed


        // Branding Settings (editable)
        [ObservableProperty] private string _companyName = "Your Company";
        [ObservableProperty] private string _siteName = "Main Site";
        [ObservableProperty] private string _supportEmail = "support@example.com";
        [ObservableProperty] private string _logoPath = "Path to your company logo (relative to web portal root, e.g., 'images/logo.png') - Optional";

    // Status
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isValid = true;
    
    // Reference to PasswordBox for bidirectional updates
    private System.Windows.Controls.PasswordBox? _clientSecretPasswordBox;

    public WebPortalViewModel(ConfigurationService configurationService, ServiceManager serviceManager, ICredentialProvider credentialProvider)
    {
        _configurationService = configurationService;
        _serviceManager = serviceManager;
        _credentialProvider = credentialProvider;
        LoadFromConfiguration();
    }
    
    public void SetPasswordBoxReference(System.Windows.Controls.PasswordBox passwordBox)
    {
        _clientSecretPasswordBox = passwordBox;
        
        // Load initial value from credential store if available
        if (_credentialProvider.HasCredential("entraid.clientsecret"))
        {
            var secret = _credentialProvider.GetCredential("entraid.clientsecret");
            if (!string.IsNullOrWhiteSpace(secret))
            {
                _clientSecretPasswordBox.Password = secret;
                EntraIdClientSecret = secret;
            }
        }
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
        
        // Certificate Store settings
        CertificateThumbprint = config.WebPortal.Kestrel.CertificateThumbprint ?? string.Empty;
        CertificateStoreLocation = config.WebPortal.Kestrel.CertificateStoreLocation ?? "LocalMachine";
        CertificateStoreName = config.WebPortal.Kestrel.CertificateStoreName ?? "My";
        
        // Determine which method is being used
        UseCertificateStore = !string.IsNullOrWhiteSpace(CertificateThumbprint);
        
        // Authentication settings
        EnableEntraId = config.WebPortal.Authentication.EnableEntraId;
        EnableLocalAccounts = config.WebPortal.Authentication.EnableLocalAccounts;
        
        // Backward compatibility: If both are disabled, force Local Accounts on
        if (!EnableEntraId && !EnableLocalAccounts)
        {
            EnableLocalAccounts = true;
        }
        
        EntraIdTenantId = config.WebPortal.Authentication.EntraIdTenantId ?? string.Empty;
        EntraIdClientId = config.WebPortal.Authentication.EntraIdClientId ?? string.Empty;
        
        // Load client secret from encrypted credential store (or fallback to plaintext for backwards compatibility)
        if (_credentialProvider.HasCredential("entraid.clientsecret"))
        {
            EntraIdClientSecret = _credentialProvider.GetCredential("entraid.clientsecret") ?? string.Empty;
        }
        else
        {
            // Fallback to plaintext for backwards compatibility during migration
            EntraIdClientSecret = config.WebPortal.Authentication.EntraIdClientSecret ?? string.Empty;
        }
        
        
        // Branding (editable)
        CompanyName = config.Branding.CompanyName;
        SiteName = config.Branding.SiteName;
        SupportEmail = config.Branding.SupportEmail;
        LogoPath = string.IsNullOrWhiteSpace(config.Branding.LogoPath) 
            ? "Path to your company logo (relative to web portal root, e.g., 'images/logo.png') - Optional"
            : config.Branding.LogoPath;
        
        ValidateConfiguration();
    }

    [RelayCommand]
    private void BrowseCertificate()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select SSL Certificate File (.pfx or .p12)",
            Filter = "Certificate Files (*.pfx;*.p12)|*.pfx;*.p12|All Files (*.*)|*.*",
            DefaultExt = ".pfx"
        };

        if (dialog.ShowDialog() == true)
        {
            CertificatePath = dialog.FileName;
            UseCertificateStore = false; // Switch to file path mode
            ValidateConfiguration();
        }
    }
    
    [RelayCommand]
    private void BrowseCertificateStore()
    {
        try
        {
            var storeLocation = CertificateStoreLocation == "CurrentUser" 
                ? StoreLocation.CurrentUser 
                : StoreLocation.LocalMachine;
            
            var store = new X509Store(CertificateStoreName, storeLocation);
            store.Open(OpenFlags.ReadOnly);
            
            try
            {
                // Get certificates that have private keys
                var certs = store.Certificates
                    .OfType<X509Certificate2>()
                    .Where(c => c.HasPrivateKey)
                    .OrderBy(c => c.NotAfter)
                    .ToList();
                
                if (certs.Count == 0)
                {
                    MessageBox.Show(
                        $"No certificates with private keys found in {CertificateStoreLocation}\\{CertificateStoreName}",
                        "No Certificates Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }
                
                // Show a simple selection dialog
                var dialog = new Views.CertificateStoreBrowser(certs, CertificateThumbprint)
                {
                    Owner = System.Windows.Application.Current.MainWindow
                };
                
                if (dialog.ShowDialog() == true && dialog.SelectedCertificate != null)
                {
                    CertificateThumbprint = dialog.SelectedCertificate.Thumbprint;
                    CertificateStoreLocation = storeLocation == StoreLocation.CurrentUser ? "CurrentUser" : "LocalMachine";
                    CertificateStoreName = CertificateStoreName; // Keep current store name
                    UseCertificateStore = true; // Switch to certificate store mode
                    CertificateStatus = $"✅ Selected: {dialog.SelectedCertificate.Subject} (Expires: {dialog.SelectedCertificate.NotAfter:yyyy-MM-dd})";
                    ValidateConfiguration();
                }
            }
            finally
            {
                store.Close();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error accessing certificate store: {ex.Message}",
                "Certificate Store Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    
    [RelayCommand]
    private async Task ImportCertificateAsync(PasswordBox? passwordBox)
    {
        if (string.IsNullOrWhiteSpace(CertificatePath))
        {
            MessageBox.Show("Please select a certificate file first.", "No Certificate Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (!File.Exists(CertificatePath))
        {
            MessageBox.Show("Certificate file not found.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        
        try
        {
            StatusMessage = "📦 Importing certificate to certificate store...";
            
            var password = passwordBox?.Password ?? string.Empty;
            var scriptPath = Path.Combine(ApplicationConstants.Paths.DefaultInstallPath, "scripts", "Import-SslCertificate.ps1");
            
            // Check if script exists
            if (!File.Exists(scriptPath))
            {
                // Try to find it in the current application directory
                var altScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", "Import-SslCertificate.ps1");
                if (File.Exists(altScriptPath))
                {
                    scriptPath = altScriptPath;
                }
                else
                {
                    // Manual import using .NET
                    await ImportCertificateManuallyAsync(CertificatePath, password);
                    return;
                }
            }
            
            // Use PowerShell script to import
            var processInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\" -CertificatePath \"{CertificatePath}\" -Password \"{password}\" -StoreLocation \"{CertificateStoreLocation}\" -StoreName \"{CertificateStoreName}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Verb = "runas" // Run as administrator
            };
            
            using var process = Process.Start(processInfo);
            if (process != null)
            {
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                
                if (process.ExitCode == 0)
                {
                    // Parse thumbprint from output
                    var thumbprintMatch = System.Text.RegularExpressions.Regex.Match(output, @"Thumbprint:\s*([A-Fa-f0-9]+)");
                    if (thumbprintMatch.Success)
                    {
                        CertificateThumbprint = thumbprintMatch.Groups[1].Value;
                        UseCertificateStore = true;
                        CertificateStatus = $"✅ Imported to certificate store: {CertificateThumbprint}";
                        StatusMessage = "✅ Certificate imported successfully and configured to use certificate store.";
                        ValidateConfiguration();
                    }
                    else
                    {
                        StatusMessage = $"✅ Certificate imported. Output:\n{output}";
                    }
                }
                else
                {
                    StatusMessage = $"❌ Import failed:\n{error}\n{output}";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error importing certificate: {ex.Message}";
            MessageBox.Show($"Error importing certificate: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async Task ImportCertificateManuallyAsync(string certPath, string password)
    {
        await Task.Run(() =>
        {
            try
            {
                var storeLocation = CertificateStoreLocation == "CurrentUser"
                    ? StoreLocation.CurrentUser
                    : StoreLocation.LocalMachine;
                
                var store = new X509Store(CertificateStoreName, storeLocation);
                store.Open(OpenFlags.ReadWrite);
                
                try
                {
                    var cert = new X509Certificate2(certPath, password, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
                    store.Add(cert);
                    
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        CertificateThumbprint = cert.Thumbprint;
                        UseCertificateStore = true;
                        CertificateStatus = $"✅ Imported: {cert.Subject} (Expires: {cert.NotAfter:yyyy-MM-dd})";
                        StatusMessage = "✅ Certificate imported successfully to certificate store.";
                        ValidateConfiguration();
                    });
                }
                finally
                {
                    store.Close();
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = $"❌ Manual import assistant failed: {ex.Message}";
                });
            }
        });
    }

    [RelayCommand]
    private void OpenEntraIdSetup()
    {
        try
        {
            // Open Azure Portal to app registrations
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationsListBlade",
                UseShellExecute = true
            });

            // Show setup instructions dialog
            var dialog = new Views.EntraIdSetupWizard
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                // User filled in the credentials in the wizard
                EntraIdTenantId = dialog.TenantId ?? string.Empty;
                EntraIdClientId = dialog.ClientId ?? string.Empty;
                EntraIdClientSecret = dialog.ClientSecret ?? string.Empty;
                
                // Also update the PasswordBox if reference is available
                if (_clientSecretPasswordBox != null)
                {
                    _clientSecretPasswordBox.Password = EntraIdClientSecret;
                }
                
                EnableEntraId = true;
                
                StatusMessage = "✅ Entra ID credentials configured. Click 'Save Configuration' to persist.";
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Could not open Azure Portal: {ex.Message}\n\nPlease visit https://portal.azure.com manually and go to:\nAzure Active Directory > App registrations > New registration",
                "Error Opening Azure Portal",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
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
            try
            {
                var sourceFile = dialog.FileName;
                if (!File.Exists(sourceFile))
                {
                    StatusMessage = "❌ Selected file does not exist";
                    return;
                }

                // Determine web portal wwwroot/images directory
                // Default install path: C:\Program Files\ZLFileRelay\WebPortal\wwwroot\images
                var webPortalImagesDir = Path.Combine(ApplicationConstants.Paths.DefaultInstallPath, "WebPortal", "wwwroot", "images");
                
                // Ensure directory exists
                Directory.CreateDirectory(webPortalImagesDir);

                // Get file extension and create destination filename
                var extension = Path.GetExtension(sourceFile);
                var destinationFile = Path.Combine(webPortalImagesDir, $"logo{extension}");

                // Backup existing logo if it exists
                if (File.Exists(destinationFile))
                {
                    var backupFile = Path.Combine(webPortalImagesDir, $"logo.{DateTime.Now:yyyyMMddHHmmss}.bak");
                    File.Copy(destinationFile, backupFile, true);
                }

                // Copy the logo file
                File.Copy(sourceFile, destinationFile, true);

                // Store relative path (images/logo.png) in LogoPath
                var relativePath = Path.Combine("images", $"logo{extension}").Replace('\\', '/');
                LogoPath = relativePath;
                
                StatusMessage = $"✅ Logo imported successfully to: {destinationFile}\nRelative path: {relativePath}";
            }
            catch (UnauthorizedAccessException ex)
            {
                StatusMessage = $"❌ Permission denied copying logo file. Error: {ex.Message}\n\nTry running as Administrator.";
                LogoPath = "Path to your company logo (relative to web portal root, e.g., 'images/logo.png') - Optional";
            }
            catch (DirectoryNotFoundException ex)
            {
                StatusMessage = $"❌ Web portal directory not found. Error: {ex.Message}\n\nEnsure the web portal is installed at: {ApplicationConstants.Paths.DefaultInstallPath}\\WebPortal";
                LogoPath = "Path to your company logo (relative to web portal root, e.g., 'images/logo.png') - Optional";
            }
            catch (IOException ex)
            {
                StatusMessage = $"❌ Error copying logo file: {ex.Message}\n\nThe file may be in use or the disk may be full.";
                LogoPath = "Path to your company logo (relative to web portal root, e.g., 'images/logo.png') - Optional";
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Unexpected error importing logo: {ex.Message}";
                LogoPath = "Path to your company logo (relative to web portal root, e.g., 'images/logo.png') - Optional";
            }
        }
    }

    [RelayCommand]
    private async Task TestCertificateAsync(PasswordBox? passwordBox)
    {
        // Test certificate based on current mode
        if (UseCertificateStore && !string.IsNullOrWhiteSpace(CertificateThumbprint))
        {
            // Test certificate from store
            await TestCertificateFromStoreAsync();
        }
        else if (!string.IsNullOrWhiteSpace(CertificatePath))
        {
            // Test certificate from file
            await TestCertificateFromFileAsync(passwordBox);
        }
        else
        {
            CertificateStatus = "❌ No certificate configured";
            IsValid = false;
        }
    }
    
    private async Task TestCertificateFromStoreAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                var storeLocation = CertificateStoreLocation == "CurrentUser"
                    ? StoreLocation.CurrentUser
                    : StoreLocation.LocalMachine;
                
                var store = new X509Store(CertificateStoreName, storeLocation);
                store.Open(OpenFlags.ReadOnly);
                
                try
                {
                    var thumbprintClean = CertificateThumbprint.Replace(" ", "").Replace("-", "");
                    var certs = store.Certificates.Find(
                        X509FindType.FindByThumbprint,
                        thumbprintClean,
                        false);
                    
                    if (certs.Count == 0)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            CertificateStatus = $"❌ Certificate not found in {CertificateStoreLocation}\\{CertificateStoreName}";
                            IsValid = false;
                        });
                        return;
                    }
                    
                    var cert = certs[0];
                    
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var isValid = cert.NotAfter > DateTime.Now;
                        var statusIcon = isValid ? "✅" : "⚠️";
                        var expiryInfo = cert.NotAfter < DateTime.Now
                            ? "EXPIRED"
                            : cert.NotAfter < DateTime.Now.AddDays(30)
                            ? $"Expires soon: {cert.NotAfter:yyyy-MM-dd}"
                            : $"Expires: {cert.NotAfter:yyyy-MM-dd}";
                        
                        CertificateStatus = $"{statusIcon} {cert.Subject} ({expiryInfo})";
                        IsValid = isValid;
                    });
                }
                finally
                {
                    store.Close();
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CertificateStatus = $"❌ Error: {ex.Message}";
                    IsValid = false;
                });
            }
        });
    }
    
    private async Task TestCertificateFromFileAsync(PasswordBox? passwordBox)
    {
        await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(CertificatePath))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        CertificateStatus = $"❌ Certificate file not found";
                        IsValid = false;
                    });
                    return;
                }
                
                var password = Application.Current.Dispatcher.Invoke(() => passwordBox?.Password ?? string.Empty);
                
                // Try to load the certificate
                var cert = new X509Certificate2(CertificatePath, password);
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var isValid = cert.NotAfter > DateTime.Now;
                    var statusIcon = isValid ? "✅" : "⚠️";
                    var expiryInfo = cert.NotAfter < DateTime.Now
                        ? "EXPIRED"
                        : cert.NotAfter < DateTime.Now.AddDays(30)
                        ? $"Expires soon: {cert.NotAfter:yyyy-MM-dd}"
                        : $"Expires: {cert.NotAfter:yyyy-MM-dd}";
                    
                    CertificateStatus = $"{statusIcon} Valid: {cert.Subject} ({expiryInfo})";
                    IsValid = isValid;
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CertificateStatus = $"❌ Invalid: {ex.Message}";
                    IsValid = false;
                });
            }
        });
    }


    [RelayCommand]
    private async Task RestartWebServiceAsync()
    {
        try
        {
            StatusMessage = "🔄 Restarting web service...";
            var success = await _serviceManager.RestartAsync();
            
            if (success)
            {
                StatusMessage = "✅ Web service restarted successfully";
            }
            else
            {
                StatusMessage = "❌ Failed to restart web service. Check permissions and try again.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error restarting service: {ex.Message}";
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
            // Certificate configuration - save based on which method is being used
            // IMPORTANT: If UseCertificateStore is explicitly true (from radio button), always use store mode
            // This ensures that when user selects "Certificate Store" option, we clear file path even if thumbprint not yet selected
            if (UseCertificateStore)
            {
                // Using certificate store (preferred)
                config.WebPortal.Kestrel.CertificateThumbprint = string.IsNullOrWhiteSpace(CertificateThumbprint) ? null : CertificateThumbprint;
                config.WebPortal.Kestrel.CertificateStoreLocation = CertificateStoreLocation;
                config.WebPortal.Kestrel.CertificateStoreName = CertificateStoreName;
                // ALWAYS clear file path settings when using store mode (important!)
                config.WebPortal.Kestrel.CertificatePath = null;
                config.WebPortal.Kestrel.CertificatePassword = null;
            }
            else if (!string.IsNullOrWhiteSpace(CertificateThumbprint))
            {
                // Fallback: If thumbprint is set but UseCertificateStore is false, still use store (legacy support)
                config.WebPortal.Kestrel.CertificateThumbprint = CertificateThumbprint;
                config.WebPortal.Kestrel.CertificateStoreLocation = CertificateStoreLocation;
                config.WebPortal.Kestrel.CertificateStoreName = CertificateStoreName;
                // Clear file path settings
                config.WebPortal.Kestrel.CertificatePath = null;
                config.WebPortal.Kestrel.CertificatePassword = null;
            }
            else
            {
                // Using file path (fallback)
                config.WebPortal.Kestrel.CertificatePath = string.IsNullOrWhiteSpace(CertificatePath) ? null : CertificatePath;
                config.WebPortal.Kestrel.CertificatePassword = string.IsNullOrWhiteSpace(CertificatePassword) ? null : CertificatePassword;
                // Clear store settings when using file path
                config.WebPortal.Kestrel.CertificateThumbprint = null;
            }
            // Authentication settings
            config.WebPortal.Authentication.EnableEntraId = EnableEntraId;
            config.WebPortal.Authentication.EnableLocalAccounts = EnableLocalAccounts;
            config.WebPortal.Authentication.EntraIdTenantId = string.IsNullOrWhiteSpace(EntraIdTenantId) ? null : EntraIdTenantId;
            config.WebPortal.Authentication.EntraIdClientId = string.IsNullOrWhiteSpace(EntraIdClientId) ? null : EntraIdClientId;
            
            // Store client secret in encrypted credential store (NOT in config JSON)
            if (!string.IsNullOrWhiteSpace(EntraIdClientSecret))
            {
                _credentialProvider.StoreCredential("entraid.clientsecret", EntraIdClientSecret);
            }
            else
            {
                // If cleared, also remove from credential store
                _credentialProvider.DeleteCredential("entraid.clientsecret");
            }
            
            // Clear the field from JSON config to avoid storing plaintext
            config.WebPortal.Authentication.EntraIdClientSecret = null;
            
            // Save branding settings
            config.Branding.CompanyName = CompanyName;
            config.Branding.SiteName = SiteName;
            config.Branding.SupportEmail = SupportEmail;
            // Only save logo path if it's not the placeholder text
            var logoPlaceholder = "Path to your company logo (relative to web portal root, e.g., 'images/logo.png') - Optional";
            config.Branding.LogoPath = string.IsNullOrWhiteSpace(LogoPath) || LogoPath == logoPlaceholder ? null : LogoPath;

            // Verify logo file exists if path is provided (and not placeholder)
            if (!string.IsNullOrWhiteSpace(config.Branding.LogoPath) && config.Branding.LogoPath != logoPlaceholder)
            {
                // Check if it's an absolute path (old format) or relative path
                var logoFilePath = config.Branding.LogoPath;
                if (!Path.IsPathRooted(logoFilePath))
                {
                    // Relative path - check in wwwroot/images
                    logoFilePath = Path.Combine(ApplicationConstants.Paths.DefaultInstallPath, "WebPortal", "wwwroot", config.Branding.LogoPath);
                }
                
                if (!File.Exists(logoFilePath))
                {
                    StatusMessage = $"⚠️ Warning: Logo file not found at: {logoFilePath}\n\nContinuing with save, but logo may not display correctly in web portal.";
                }
            }

            // Attempt to save with thorough error reporting
            // Use webPortalOnly flag to only validate Web Portal-specific settings (SSH/SMB validation skipped)
            var configPath = _configurationService.GetConfigurationPath();
            var success = await _configurationService.SaveAsync(config, webPortalOnly: true);
            
            if (success)
            {
                StatusMessage = $"✅ Configuration saved successfully to:\n{configPath}";
                StatusMessage += "\n\nRestart Web Portal service to apply changes.";
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

        if (EnableHttps)
        {
            // Determine which mode we're in - prefer certificate store if thumbprint is set
            // This ensures that if user selected cert store, we validate that path even if 
            // UseCertificateStore toggle hasn't updated yet
            bool usingCertStore = UseCertificateStore || !string.IsNullOrWhiteSpace(CertificateThumbprint);
            
            // Check if certificate is configured (either method)
            if (usingCertStore)
            {
                if (string.IsNullOrWhiteSpace(CertificateThumbprint))
                    errors.Add("Certificate thumbprint required when using certificate store");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(CertificatePath))
                    errors.Add("Certificate path required when HTTPS is enabled");
                else if (!File.Exists(CertificatePath))
                    errors.Add("Certificate file not found");
            }
        }
        
        // Validate authentication configuration
        if (!EnableEntraId && !EnableLocalAccounts)
        {
            errors.Add("At least one authentication method must be enabled (either Entra ID or Local Accounts)");
        }
        
        if (EnableEntraId)
        {
            if (string.IsNullOrWhiteSpace(EntraIdTenantId))
                errors.Add("Entra ID Tenant ID is required when Entra ID authentication is enabled");
            
            if (string.IsNullOrWhiteSpace(EntraIdClientId))
                errors.Add("Entra ID Client ID is required when Entra ID authentication is enabled");
        }

        IsValid = errors.Count == 0;
        
        if (!IsValid)
        {
            StatusMessage = "⚠️ " + string.Join(", ", errors);
        }
        else if (EnableEntraId && EnableLocalAccounts)
        {
            // Show warning when both are enabled (not an error, but not recommended)
            StatusMessage = "⚠️ Warning: Both Entra ID and Local Accounts are enabled. Recommended: disable Local Accounts when using Entra ID.";
        }
        else
        {
            StatusMessage = string.Empty;
        }
    }

    partial void OnHttpPortChanged(int value) => ValidateConfiguration();
    partial void OnHttpsPortChanged(int value) => ValidateConfiguration();
    partial void OnEnableHttpsChanged(bool value) => ValidateConfiguration();
    partial void OnCertificatePathChanged(string value) => ValidateConfiguration();
    partial void OnUseCertificateStoreChanged(bool value) => ValidateConfiguration();
    partial void OnCertificateThumbprintChanged(string value) => ValidateConfiguration();
    partial void OnCertificateStoreLocationChanged(string value) => ValidateConfiguration();
    partial void OnCertificateStoreNameChanged(string value) => ValidateConfiguration();
    partial void OnEnableEntraIdChanged(bool value)
    {
        // When Entra ID is enabled, automatically disable Local Accounts (mutual exclusivity)
        if (value && EnableLocalAccounts)
        {
            EnableLocalAccounts = false;
        }
        ValidateConfiguration();
    }
    partial void OnEnableLocalAccountsChanged(bool value)
    {
        // When Local Accounts is disabled and Entra ID is also disabled, force Local Accounts back on
        if (!value && !EnableEntraId)
        {
            EnableLocalAccounts = true;
            StatusMessage = "⚠️ Warning: At least one authentication method must be enabled. Local Accounts re-enabled.";
        }
        else
        {
            ValidateConfiguration();
        }
    }
}

