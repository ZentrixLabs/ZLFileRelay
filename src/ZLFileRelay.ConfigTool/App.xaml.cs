using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using ZLFileRelay.Core.Models;
using ZLFileRelay.Core.Interfaces;
using ZLFileRelay.Core.Services;
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
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
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
                services.AddSingleton<ConnectionTester>();
                services.AddSingleton<PowerShellRemotingService>();
                services.AddSingleton<ServiceAccountManager>();
                services.AddSingleton<PermissionManager>();
                services.AddSingleton<INotificationService, NotificationService>();
                services.AddSingleton<PreFlightCheckService>();

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

                // Main Window
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        // Load configuration on startup
        var configService = _host.Services.GetRequiredService<ConfigurationService>();
        await configService.LoadAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
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

