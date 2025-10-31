using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using ZLFileRelay.Core.Models;
using ZLFileRelay.Core.Interfaces;
using ZLFileRelay.Core.Services;
using ZLFileRelay.Core.Constants;
using ZLFileRelay.ConfigTool.Services;
using ZLFileRelay.ConfigTool.ViewModels;

namespace ZLFileRelay.ConfigTool;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                // Load configuration from SINGLE shared location
                // ConfigTool READS from and WRITES to this location
                var sharedConfigPath = ApplicationConstants.Configuration.SharedConfigPath;
                
                // Ensure directory exists
                var configDir = Path.GetDirectoryName(sharedConfigPath);
                if (configDir != null && !Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }
                
                if (File.Exists(sharedConfigPath))
                {
                    config.AddJsonFile(sharedConfigPath, optional: false, reloadOnChange: true);
                }
                // If file doesn't exist, ConfigurationService will create it on first save
            })
            .ConfigureServices((context, services) =>
            {
                // Configuration
                var appConfig = new ZLFileRelayConfiguration();
                context.Configuration.GetSection("ZLFileRelay").Bind(appConfig);
                services.AddSingleton(appConfig);

                // Core Services
                services.AddSingleton<ICredentialProvider>(sp =>
                {
                    var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CredentialProvider>>();
                    var credentialPath = Path.Combine(
                        appConfig.Paths.ConfigDirectory, 
                        "credentials.dat");
                    return new CredentialProvider(logger, credentialPath);
                });

                // Remote Server Provider (singleton for shared state)
                services.AddSingleton<ZLFileRelay.ConfigTool.Interfaces.IRemoteServerProvider, ZLFileRelay.ConfigTool.Services.RemoteServerProvider>();

                // Config Tool Services
                services.AddSingleton<ConfigurationService>();
                services.AddSingleton<ServiceManager>();
                services.AddSingleton<SshKeyGenerator>();
                services.AddSingleton<ServiceAccountImpersonator>();
                services.AddSingleton<PowerShellRemotingService>();
                services.AddSingleton<ServiceAccountManager>();
                services.AddSingleton<PermissionManager>();
                services.AddSingleton<INotificationService, NotificationService>();
                services.AddSingleton<PreFlightCheckService>();
                
                // ConnectionTester with impersonator dependency
                services.AddSingleton<ConnectionTester>(sp =>
                {
                    var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ConnectionTester>>();
                    var impersonator = sp.GetRequiredService<ServiceAccountImpersonator>();
                    return new ConnectionTester(logger, impersonator);
                });

                // ViewModels
                services.AddTransient<MainViewModel>();
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<RemoteServerViewModel>();
                
                // New Unified ViewModels (post-UX simplification)
                services.AddTransient<ServiceViewModel>();
                services.AddTransient<FileTransferViewModel>();
                
                // Legacy ViewModels (kept for compatibility, may be removed later)
                services.AddTransient<ServiceManagementViewModel>();
                services.AddTransient<ConfigurationViewModel>();
                services.AddTransient<WebPortalViewModel>();
                services.AddTransient<SshSettingsViewModel>();
                services.AddTransient<ServiceAccountViewModel>();

                // Main Window - NOT registered as singleton, created manually after config loads
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        // CRITICAL: Load configuration FIRST before creating any ViewModels
        // ViewModels read from CurrentConfiguration in their constructors
        var configService = _host.Services.GetRequiredService<ConfigurationService>();
        await configService.LoadAsync();

        // Now create MainWindow (which creates ViewModels) after config is loaded
        // Create manually since we don't register it as singleton
        var mainWindow = new MainWindow(
            _host.Services.GetRequiredService<DashboardViewModel>(),
            _host.Services.GetRequiredService<ServiceViewModel>(),
            _host.Services.GetRequiredService<FileTransferViewModel>(),
            _host.Services.GetRequiredService<WebPortalViewModel>(),
            _host.Services.GetRequiredService<RemoteServerViewModel>(),
            _host.Services.GetRequiredService<INotificationService>());
        
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }

    public static T GetService<T>() where T : class
    {
        return ((App)Current)._host.Services.GetRequiredService<T>();
    }
}

