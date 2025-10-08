using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Server.IISIntegration;
using Serilog;
using ZLFileRelay.Core.Interfaces;
using ZLFileRelay.Core.Models;
using ZLFileRelay.WebPortal.Services;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/zlfilerelay-web-.log", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting ZL File Relay Web Portal");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine("C:\\FileRelay\\logs", "zlfilerelay-web-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30);
    });

    // Load configuration
    var appConfig = new ZLFileRelayConfiguration();
    builder.Configuration.GetSection("ZLFileRelay").Bind(appConfig);
    builder.Services.AddSingleton(appConfig);

    // Add authentication (always add middleware even if not required)
    if (OperatingSystem.IsWindows())
    {
        builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
            .AddNegotiate();
    }

    // Add authorization (always needed for [Authorize] attribute)
    builder.Services.AddAuthorization();

    // Configure IIS integration for Windows Authentication
    if (OperatingSystem.IsWindows())
    {
        builder.Services.Configure<IISServerOptions>(options =>
        {
            options.AutomaticAuthentication = appConfig.WebPortal.RequireAuthentication;
        });
    }

    // Register services
    builder.Services.AddScoped<IFileUploadService, FileUploadService>();
    builder.Services.AddScoped<AuthorizationService>();

    // Add Razor Pages
    builder.Services.AddRazorPages();

    var app = builder.Build();

    // Ensure directories exist
    Directory.CreateDirectory(appConfig.Paths.UploadDirectory);
    Directory.CreateDirectory(appConfig.Paths.LogDirectory);

    if (appConfig.WebPortal.EnableUploadToTransfer)
    {
        Directory.CreateDirectory(appConfig.Service.WatchDirectory);
    }

    // Configure the HTTP request pipeline
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    // Always add auth middleware (pages have [Authorize] attributes)
    if (OperatingSystem.IsWindows())
    {
        app.UseAuthentication();
    }
    app.UseAuthorization();

    app.MapRazorPages();

    Log.Information("ZL File Relay Web Portal starting on {Urls}", 
        string.Join(", ", app.Urls));

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ZL File Relay Web Portal terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;
