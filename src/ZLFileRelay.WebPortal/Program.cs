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

    // Load configuration first
    var appConfig = new ZLFileRelayConfiguration();
    builder.Configuration.GetSection("ZLFileRelay").Bind(appConfig);
    builder.Services.AddSingleton(appConfig);

    // Configure as Windows Service (no IIS needed!)
    if (OperatingSystem.IsWindows())
    {
        builder.Services.AddWindowsService(options =>
        {
            options.ServiceName = "ZLFileRelay.WebPortal";
        });
    }

    // Configure Kestrel to listen on ports from configuration
    var httpUrl = $"http://*:{appConfig.WebPortal.Kestrel.HttpPort}";
    var httpsUrl = $"https://*:{appConfig.WebPortal.Kestrel.HttpsPort}";
    
    if (appConfig.WebPortal.Kestrel.EnableHttps)
    {
        builder.WebHost.UseUrls(httpUrl, httpsUrl);
        
        // Configure HTTPS certificate if provided
        if (!string.IsNullOrWhiteSpace(appConfig.WebPortal.Kestrel.CertificatePath))
        {
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ConfigureHttpsDefaults(httpsOptions =>
                {
                    httpsOptions.ServerCertificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                        appConfig.WebPortal.Kestrel.CertificatePath,
                        appConfig.WebPortal.Kestrel.CertificatePassword);
                });
            });
        }
    }
    else
    {
        // HTTP only
        builder.WebHost.UseUrls(httpUrl);
    }

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

    // Configuration already loaded above for Kestrel setup

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

    // SECURITY FIX (MEDIUM-4): Add rate limiting for file uploads
    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("upload", limiterOptions =>
        {
            limiterOptions.PermitLimit = appConfig.WebPortal.MaxConcurrentUploads;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 0; // No queuing - reject immediately if limit exceeded
        });

        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.HttpContext.Response.WriteAsync(
                "Too many upload requests. Please wait a moment and try again.", 
                cancellationToken);
        };
    });

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

    // SECURITY FIX (MEDIUM-4): Enable rate limiting middleware
    app.UseRateLimiter();

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
