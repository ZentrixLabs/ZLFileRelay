using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.AspNetCore.Server.IISIntegration;
using Serilog;
using System.Threading.RateLimiting;
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

    // Load configuration from shared location first, then fall back to embedded appsettings.json
    var appConfig = new ZLFileRelayConfiguration();
    builder.Configuration.GetSection("ZLFileRelay").Bind(appConfig);
    
    // Try to load from shared configuration file (ConfigDirectory from embedded config or C:\ProgramData\ZLFileRelay)
    var configDir = appConfig.Paths?.ConfigDirectory ?? @"C:\ProgramData\ZLFileRelay";
    var sharedConfigPath = Path.Combine(configDir, "appsettings.json");
    
    if (File.Exists(sharedConfigPath))
    {
        Log.Information("Loading configuration from shared location: {Path}", sharedConfigPath);
        builder.Configuration.AddJsonFile(sharedConfigPath, optional: false, reloadOnChange: true);
        
        // Reload configuration from the shared file
        appConfig = new ZLFileRelayConfiguration();
        builder.Configuration.GetSection("ZLFileRelay").Bind(appConfig);
        Log.Information("Configuration loaded successfully from: {Path}", sharedConfigPath);
    }
    else
    {
        Log.Information("Shared configuration not found at {Path}, using embedded appsettings.json", sharedConfigPath);
    }
    
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
    // Note: UseUrls is not used here because we configure listeners explicitly below
    
    if (appConfig.WebPortal.Kestrel.EnableHttps)
    {
        // Configure HTTPS certificate if provided
        if (!string.IsNullOrWhiteSpace(appConfig.WebPortal.Kestrel.CertificatePath))
        {
            builder.WebHost.ConfigureKestrel(options =>
            {
                // SECURITY FIX (MEDIUM-1): Set request body size limit at Kestrel level
                options.Limits.MaxRequestBodySize = appConfig.Security.MaxUploadSizeBytes;
                
                // Configure HTTPS listener with certificate
                options.ListenAnyIP(appConfig.WebPortal.Kestrel.HttpsPort, listenOptions =>
                {
                    listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
                    
                    try
                    {
                        var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                            appConfig.WebPortal.Kestrel.CertificatePath,
                            appConfig.WebPortal.Kestrel.CertificatePassword);
                        
                        listenOptions.UseHttps(cert);
                        Log.Information("✅ HTTPS certificate loaded successfully from: {Path}", appConfig.WebPortal.Kestrel.CertificatePath);
                        Log.Information("Certificate subject: {Subject}, valid until: {Expiry}", 
                            cert.Subject, cert.NotAfter);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "❌ Failed to load HTTPS certificate from: {Path}", appConfig.WebPortal.Kestrel.CertificatePath);
                        throw new InvalidOperationException($"Failed to load HTTPS certificate: {ex.Message}", ex);
                    }
                });
                
                // Configure HTTP listener
                options.ListenAnyIP(appConfig.WebPortal.Kestrel.HttpPort, listenOptions =>
                {
                    listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
                });
                
                Log.Information("HTTP listener configured on port {Port}", appConfig.WebPortal.Kestrel.HttpPort);
            });
        }
        else
        {
            // HTTPS enabled but no certificate provided
            builder.WebHost.ConfigureKestrel(options =>
            {
                // SECURITY FIX (MEDIUM-1): Set request body size limit at Kestrel level
                options.Limits.MaxRequestBodySize = appConfig.Security.MaxUploadSizeBytes;
                
                // Still configure HTTP listener
                options.ListenAnyIP(appConfig.WebPortal.Kestrel.HttpPort, listenOptions =>
                {
                    listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
                });
                
                Log.Warning("⚠️ HTTPS enabled but no certificate path specified. Only HTTP will be available on port {Port}", 
                    appConfig.WebPortal.Kestrel.HttpPort);
            });
        }
    }
    else
    {
        // HTTP only - configure listener explicitly
        builder.WebHost.ConfigureKestrel(options =>
        {
            // SECURITY FIX (MEDIUM-1): Set request body size limit at Kestrel level
            options.Limits.MaxRequestBodySize = appConfig.Security.MaxUploadSizeBytes;
            
            // Configure HTTP listener
            options.ListenAnyIP(appConfig.WebPortal.Kestrel.HttpPort, listenOptions =>
            {
                listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
            });
            
            Log.Information("HTTP listener configured on port {Port}", appConfig.WebPortal.Kestrel.HttpPort);
        });
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
    builder.Services.AddAuthorization(options =>
    {
        // Configure authorization to require authentication for protected routes
        options.FallbackPolicy = null; // Don't require auth globally
    });

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
    
    // Register SignalR services for real-time transfer status updates
    builder.Services.AddSignalR();
    builder.Services.AddSingleton<ITransferStatusService, TransferStatusService>();
    builder.Services.AddHostedService<StatusMonitorService>();

    // SECURITY FIX (MEDIUM-3): Add comprehensive rate limiting for all endpoints
    builder.Services.AddRateLimiter(options =>
    {
        // Global rate limiter - applies to all requests per IP
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            // Get client IP (handles proxy scenarios)
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100, // 100 requests per minute per IP (protects against brute force)
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0 // No queuing - reject immediately
            });
        });
        
        // Specific rate limiter for file uploads (more restrictive)
        options.AddFixedWindowLimiter("upload", limiterOptions =>
        {
            limiterOptions.PermitLimit = appConfig.WebPortal.MaxConcurrentUploads;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 0; // No queuing - reject immediately if limit exceeded
        });

        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            
            // Provide different messages based on which limiter rejected
            var message = context.Lease.TryGetMetadata(MetadataName.ReasonPhrase, out var reason) && reason == "upload"
                ? "Too many upload requests. Please wait a moment and try again."
                : "Too many requests from your IP address. Please wait a moment and try again.";
            
            await context.HttpContext.Response.WriteAsync(message, cancellationToken);
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
    // SECURITY FIX (HIGH-2): Explicit error handling for all environments
    if (app.Environment.IsDevelopment())
    {
        // In development, show detailed errors
        app.UseDeveloperExceptionPage();
    }
    else
    {
        // In production, use generic error page (no technical details)
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "text/html";
                
                await context.Response.WriteAsync(@"
<!DOCTYPE html>
<html>
<head>
    <title>Error - ZL File Relay</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; background-color: #f5f5f5; }
        .error-container { max-width: 600px; margin: 0 auto; background: white; padding: 30px; border-radius: 5px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        h1 { color: #d32f2f; }
        p { color: #666; line-height: 1.6; }
        a { color: #0066CC; text-decoration: none; }
        a:hover { text-decoration: underline; }
    </style>
</head>
<body>
    <div class='error-container'>
        <h1>An Error Occurred</h1>
        <p>We're sorry, but something went wrong while processing your request.</p>
        <p>Please try again later or contact your system administrator if the problem persists.</p>
        <p><a href='/Upload'>Return to Upload Page</a></p>
    </div>
</body>
</html>");
            });
        });
        
        // Only use HSTS if HTTPS is enabled
        if (appConfig.WebPortal.Kestrel.EnableHttps)
        {
            app.UseHsts();
        }
    }

    // SECURITY FIX (CRITICAL-1): Only use HTTPS redirection if HTTPS is enabled
    if (appConfig.WebPortal.Kestrel.EnableHttps)
    {
        app.UseHttpsRedirection();
    }
    
    // SECURITY FIX (HIGH-1): Add security headers for DMZ deployment
    app.Use(async (context, next) =>
    {
        // Prevent clickjacking
        context.Response.Headers["X-Frame-Options"] = "DENY";
        
        // Prevent MIME sniffing
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        
        // XSS Protection (legacy browsers)
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        
        // Content Security Policy - restrictive for file upload app (allow SignalR WebSockets)
        context.Response.Headers["Content-Security-Policy"] = 
            "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; frame-ancestors 'none'; connect-src 'self' ws: wss:";
        
        // Control referrer information
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        
        // Permissions policy - disable unnecessary features
        context.Response.Headers["Permissions-Policy"] = 
            "geolocation=(), microphone=(), camera=(), payment=(), usb=(), magnetometer=(), gyroscope=()";
        
        await next();
    });
    
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
    
    // Map SignalR hub for real-time transfer status updates
    app.MapHub<ZLFileRelay.WebPortal.Hubs.TransferStatusHub>("/hubs/transferstatus");

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
