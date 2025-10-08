using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using ZLFileRelay.Core.Interfaces;
using ZLFileRelay.Core.Models;
using ZLFileRelay.Core.Services;
using ZLFileRelay.Service.Services;

// Configure Serilog for early initialization
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/zlfilerelay-.log", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting ZL File Relay Service");

    var builder = Host.CreateApplicationBuilder(args);

    // Load configuration FIRST
    var configuration = builder.Configuration
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
        .Build();

    var appConfig = new ZLFileRelayConfiguration();
    configuration.GetSection("ZLFileRelay").Bind(appConfig);
    builder.Services.AddSingleton(appConfig);

    // Configure Serilog AFTER config is loaded
    builder.Services.AddSerilog((services, loggerConfiguration) =>
    {
        var config = services.GetRequiredService<ZLFileRelayConfiguration>();
        
        loggerConfiguration
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(config.Paths.LogDirectory, "zlfilerelay-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: config.Logging.RetentionDays,
                fileSizeLimitBytes: config.Logging.MaxFileSizeMB * 1024 * 1024);

        if (config.Logging.EnableEventLog && OperatingSystem.IsWindows())
        {
            loggerConfiguration.WriteTo.EventLog(
                "ZL File Relay",
                manageEventSource: true);
        }
    });

    // Register core services
    builder.Services.AddSingleton<ICredentialProvider>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<CredentialProvider>>();
        var credentialPath = Path.Combine(appConfig.Paths.ConfigDirectory, "credentials.dat");
        return new CredentialProvider(logger, credentialPath);
    });

    builder.Services.AddSingleton<IDiskSpaceChecker, DiskSpaceChecker>();

    // Register service-specific services
    builder.Services.AddSingleton<IFileWatcher, FileWatcher>();
    builder.Services.AddSingleton<IFileQueue, FileQueue>();
    
    builder.Services.AddSingleton<RetryPolicy>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<RetryPolicy>>();
        return new RetryPolicy(
            logger,
            appConfig.Service.RetryAttempts,
            TimeSpan.FromSeconds(appConfig.Service.RetryDelaySeconds),
            appConfig.Service.RetryBackoffMultiplier);
    });

    // Register transfer services
    builder.Services.AddTransient<ScpFileTransferService>();
    builder.Services.AddTransient<SmbFileTransferService>();
    builder.Services.AddSingleton<IFileTransferServiceFactory, FileTransferServiceFactory>();

    // Register background worker
    builder.Services.AddHostedService<TransferWorker>();

    // Configure Windows Service
    if (OperatingSystem.IsWindows())
    {
        builder.Services.AddWindowsService(options =>
        {
            options.ServiceName = appConfig.Service.ServiceName;
        });
    }

    var host = builder.Build();

    // Ensure directories exist
    Directory.CreateDirectory(appConfig.Paths.LogDirectory);
    Directory.CreateDirectory(appConfig.Paths.ConfigDirectory);
    Directory.CreateDirectory(appConfig.Service.WatchDirectory);

    if (appConfig.Service.ArchiveAfterTransfer)
    {
        Directory.CreateDirectory(appConfig.Service.ArchiveDirectory);
    }

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ZL File Relay Service terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;
