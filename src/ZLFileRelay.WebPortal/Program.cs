using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Serilog;
using System.Threading.RateLimiting;
using ZLFileRelay.Core.Interfaces;
using ZLFileRelay.Core.Models;
using ZLFileRelay.Core.Constants;
using ZLFileRelay.Core.Services;
using ZLFileRelay.WebPortal.Data;
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

    // Load configuration from SINGLE shared location
    // ConfigTool is the ONLY component that writes to this file
    // Service and WebPortal ONLY read from this location
    var sharedConfigPath = ApplicationConstants.Configuration.SharedConfigPath;
    
    // Ensure directory exists
    var configDir = Path.GetDirectoryName(sharedConfigPath);
    if (configDir != null && !Directory.Exists(configDir))
    {
        Directory.CreateDirectory(configDir);
    }
    
    if (File.Exists(sharedConfigPath))
    {
        Log.Information("Loading configuration from shared location: {Path}", sharedConfigPath);
        builder.Configuration.AddJsonFile(sharedConfigPath, optional: false, reloadOnChange: true);
    }
    else
    {
        Log.Warning("Configuration file not found at {Path}. WebPortal will use default values. Run ConfigTool to create configuration.", sharedConfigPath);
        // Make it optional so web portal can still start with defaults
        builder.Configuration.AddJsonFile(sharedConfigPath, optional: true, reloadOnChange: true);
    }
    
    // Configure options with automatic reload support
    // Using IOptionsMonitor allows configuration to reload when appsettings.json changes
    builder.Services.Configure<ZLFileRelayConfiguration>(
        builder.Configuration.GetSection("ZLFileRelay"));
    
    // Get initial config for logging and early initialization
    var initialConfig = new ZLFileRelayConfiguration();
    builder.Configuration.GetSection("ZLFileRelay").Bind(initialConfig);
    
    // Load client secret from encrypted credential store (if stored there)
    // This overrides any plaintext value in config JSON for security
    try
    {
        var credentialPath = Path.Combine(initialConfig.Paths.ConfigDirectory, "credentials.dat");
        var credentialProvider = new CredentialProvider(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<CredentialProvider>.Instance,
            credentialPath);
        
        if (credentialProvider.HasCredential("entraid.clientsecret"))
        {
            var encryptedSecret = credentialProvider.GetCredential("entraid.clientsecret");
            if (!string.IsNullOrWhiteSpace(encryptedSecret))
            {
                initialConfig.WebPortal.Authentication.EntraIdClientSecret = encryptedSecret;
                Log.Information("✅ Entra ID client secret loaded from encrypted credential store");
            }
        }
    }
    catch (Exception ex)
    {
        // Non-fatal - will fall back to plaintext in config if available
        Log.Warning(ex, "Could not load encrypted client secret, will use config value if available");
    }
    
    Log.Information("Configuration loaded. Company: {CompanyName}, Site: {SiteName}, Config: {ConfigPath}", 
        initialConfig.Branding?.CompanyName ?? "N/A", 
        initialConfig.Branding?.SiteName ?? "N/A",
        File.Exists(sharedConfigPath) ? sharedConfigPath : "embedded appsettings.json");
    
    // Register as singleton for backward compatibility (services can still inject directly)
    // But IOptionsMonitor<ZLFileRelayConfiguration> will reload automatically
    builder.Services.AddSingleton(initialConfig);

    // Configure as Windows Service (no IIS needed!)
    if (OperatingSystem.IsWindows())
    {
        builder.Services.AddWindowsService(options =>
        {
            options.ServiceName = "ZLFileRelay.WebPortal";
        });
    }

    // Configure Kestrel server for HTTP/HTTPS
    // Kestrel runs standalone without IIS, supporting both HTTP and HTTPS endpoints
    // Certificate handling is simpler with Kestrel (no netsh URL reservations needed)
    
    var kestrel = initialConfig.WebPortal.Kestrel;
    
    // Load certificate first (needed for Kestrel HTTPS if HTTPS enabled)
    System.Security.Cryptography.X509Certificates.X509Certificate2? cert = null;
    
    if (kestrel.EnableHttps)
    {
        // Configure HTTPS certificate - prefer certificate store (thumbprint) over file path
        
        // Log certificate configuration state for debugging
        Log.Information("HTTPS enabled. Certificate configuration: Thumbprint={Thumbprint}, Path={Path}, Store={Location}\\{Store}",
            string.IsNullOrWhiteSpace(kestrel.CertificateThumbprint) ? "Not set" : kestrel.CertificateThumbprint,
            string.IsNullOrWhiteSpace(kestrel.CertificatePath) ? "Not set" : kestrel.CertificatePath,
            kestrel.CertificateStoreLocation ?? "Not set",
            kestrel.CertificateStoreName ?? "Not set");
        
        bool hasCertificateConfig = !string.IsNullOrWhiteSpace(kestrel.CertificateThumbprint) || 
                                   !string.IsNullOrWhiteSpace(kestrel.CertificatePath);
        
        if (hasCertificateConfig)
        {
            try
            {
                // PREFERRED: Load from Windows certificate store by thumbprint
                if (!string.IsNullOrWhiteSpace(kestrel.CertificateThumbprint))
                {
                    var storeLocation = kestrel.CertificateStoreLocation == "CurrentUser" 
                        ? System.Security.Cryptography.X509Certificates.StoreLocation.CurrentUser
                        : System.Security.Cryptography.X509Certificates.StoreLocation.LocalMachine;
                    
                    using var store = new System.Security.Cryptography.X509Certificates.X509Store(
                        kestrel.CertificateStoreName ?? "My", storeLocation);
                    store.Open(System.Security.Cryptography.X509Certificates.OpenFlags.ReadOnly | 
                               System.Security.Cryptography.X509Certificates.OpenFlags.OpenExistingOnly);
                    
                    // Find certificate by thumbprint (case-insensitive, remove spaces/dashes)
                    var thumbprint = kestrel.CertificateThumbprint.Replace(" ", "").Replace("-", "");
                    var certificates = store.Certificates.Find(
                        System.Security.Cryptography.X509Certificates.X509FindType.FindByThumbprint,
                        thumbprint,
                        validOnly: false);
                    
                    if (certificates.Count > 0)
                    {
                        // Get the first valid certificate (not expired, or most recent if all expired)
                        cert = certificates
                            .OfType<System.Security.Cryptography.X509Certificates.X509Certificate2>()
                            .OrderByDescending(c => c.NotAfter)
                            .First();
                        
                        // Create a new instance that won't be disposed when store closes
                        cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(cert);
                        
                        Log.Information("✅ HTTPS certificate loaded from certificate store: {Thumbprint}", 
                            kestrel.CertificateThumbprint);
                        Log.Information("Certificate store: {Location}\\{Store}", 
                            kestrel.CertificateStoreLocation, kestrel.CertificateStoreName);
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Certificate with thumbprint '{kestrel.CertificateThumbprint}' not found in store " +
                            $"{kestrel.CertificateStoreLocation}\\{kestrel.CertificateStoreName}");
                    }
                }
                        // FALLBACK: Load from file path
                        else if (!string.IsNullOrWhiteSpace(kestrel.CertificatePath))
                        {
                            cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                                kestrel.CertificatePath,
                                kestrel.CertificatePassword ?? string.Empty);
                            
                            Log.Information("✅ HTTPS certificate loaded from file: {Path}", kestrel.CertificatePath);
                        }
                        
                if (cert != null)
                {
                    Log.Information("Certificate subject: {Subject}, valid until: {Expiry}", 
                        cert.Subject, cert.NotAfter);
                    
                    // Log certificate SANs (Subject Alternative Names) for troubleshooting hostname mismatches
                    // This is important when accessing via different hostnames (e.g., corp domain vs DMZ domain)
                    try
                    {
                        var sanExtension = cert.Extensions.OfType<System.Security.Cryptography.X509Certificates.X509Extension>()
                            .FirstOrDefault(e => e.Oid?.Value == "2.5.29.17"); // SAN OID
                        
                        if (sanExtension != null && sanExtension.Format(false).Length > 0)
                        {
                            // Parse SAN extension (format: DNS Name=hostname1, DNS Name=hostname2, etc.)
                            var sanText = sanExtension.Format(false);
                            Log.Information("   Certificate SANs (Subject Alternative Names): {SANs}", sanText);
                            
                            // Extract DNS names from SAN
                            var dnsNames = new System.Collections.Generic.List<string>();
                            var matches = System.Text.RegularExpressions.Regex.Matches(sanText, @"DNS Name=([^,]+)");
                            foreach (System.Text.RegularExpressions.Match match in matches)
                            {
                                if (match.Groups.Count > 1)
                                {
                                    dnsNames.Add(match.Groups[1].Value.Trim());
                                }
                            }
                            
                            if (dnsNames.Count > 0)
                            {
                                Log.Information("   Certificate valid for hostnames: {Hostnames}", string.Join(", ", dnsNames));
                            }
                        }
                        else
                        {
                            Log.Warning("⚠️ Certificate has no Subject Alternative Names (SANs) - only valid for CN/subject name: {Subject}", cert.Subject);
                            Log.Warning("   For multi-hostname scenarios (e.g., corp domain → DMZ), ensure certificate includes all hostnames in SAN list");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "⚠️ Could not parse certificate SAN extension");
                    }
                    
                    if (cert.NotAfter < DateTime.Now)
                    {
                        Log.Warning("⚠️ Certificate has EXPIRED (expired on {Expiry})", cert.NotAfter);
                    }
                    else if (cert.NotAfter < DateTime.Now.AddDays(30))
                    {
                        Log.Warning("⚠️ Certificate expires soon: {Expiry} (less than 30 days remaining)", cert.NotAfter);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrWhiteSpace(kestrel.CertificateThumbprint))
                {
                    Log.Error(ex, "❌ Failed to load HTTPS certificate from store. Thumbprint: {Thumbprint}, Store: {Location}\\{Store}", 
                        kestrel.CertificateThumbprint, kestrel.CertificateStoreLocation, kestrel.CertificateStoreName);
                    throw new InvalidOperationException(
                        $"Failed to load HTTPS certificate from certificate store: {ex.Message}", ex);
                }
                else
                {
                    Log.Error(ex, "❌ Failed to load HTTPS certificate from file: {Path}", kestrel.CertificatePath);
                    throw new InvalidOperationException($"Failed to load HTTPS certificate from file: {ex.Message}", ex);
                }
            }
        }
        else
        {
            Log.Warning("⚠️ HTTPS enabled but no certificate configuration provided. Only HTTP will be available.");
        }
    }
    
    // Configure Kestrel server for HTTP/HTTPS
    builder.WebHost.ConfigureKestrel(options =>
    {
        // SECURITY FIX (MEDIUM-1): Set request body size limit
        options.Limits.MaxRequestBodySize = initialConfig.Security.MaxUploadSizeBytes;
        
        // Configure HTTP endpoint
        options.ListenAnyIP(kestrel.HttpPort, listenOptions =>
        {
            Log.Information("✅ HTTP endpoint configured: http://*:{Port}/", kestrel.HttpPort);
        });
        
        // Configure HTTPS endpoint if enabled and certificate is available
        if (kestrel.EnableHttps && cert != null)
        {
            options.ListenAnyIP(kestrel.HttpsPort, listenOptions =>
            {
                listenOptions.UseHttps(cert);
                Log.Information("✅ HTTPS endpoint configured: https://*:{Port}/", kestrel.HttpsPort);
                Log.Information("   Certificate: {Subject} (valid until {Expiry})", cert.Subject, cert.NotAfter);
            });
        }
        else if (kestrel.EnableHttps && cert == null)
        {
            Log.Warning("⚠️ HTTPS enabled but no certificate configured - HTTPS will not be available");
        }
        
        // Kestrel fully supports SAN (Subject Alternative Names) certificates
        // Certificate binding is handled directly by Kestrel - no netsh commands needed
    });

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

    // Configure Authentication (Hybrid: Entra ID + Local Accounts)
    var authConfig = initialConfig.WebPortal.Authentication;
    
    // Configure Identity database - ALWAYS register for Login page support
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(authConfig.ConnectionString));
    
    // Add Authentication builder (required for both Identity and Entra ID)
    var authBuilder = builder.Services.AddAuthentication();
    
    // Configure ASP.NET Core Identity - ALWAYS register for Login page support
    // When Local Accounts is disabled, registration/login simply won't work
    builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        // Sign-in settings - no email confirmation required for simplicity
        options.SignIn.RequireConfirmedAccount = false;
        
        // Password settings
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        
        // Lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
        
        // User settings
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
    
    // Configure Identity UI to use our custom Login page instead of /Account/Login
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Login";
        options.AccessDeniedPath = "/NotAuthorized";
    });
    
    if (authConfig.EnableLocalAccounts)
    {
        Log.Information("✅ Local Accounts (ASP.NET Core Identity) enabled");
    }
    else
    {
        Log.Information("⚠️ Local Accounts disabled - only Entra ID authentication will work");
    }
    
    // Configure Entra ID (Azure AD) authentication if enabled
    if (authConfig.EnableEntraId && !string.IsNullOrEmpty(authConfig.EntraIdTenantId))
    {
        authBuilder.AddMicrosoftIdentityWebApp(options =>
        {
            options.Instance = "https://login.microsoftonline.com/";
            options.TenantId = authConfig.EntraIdTenantId;
            options.ClientId = authConfig.EntraIdClientId;
            options.ClientSecret = authConfig.EntraIdClientSecret;
            options.CallbackPath = "/signin-oidc";
            options.SignedOutCallbackPath = "/signout-callback-oidc";
            
            // Explicitly set response type to use authorization code flow (not implicit)
            options.ResponseType = "code";
            options.ResponseMode = "form_post"; // Use form_post instead of query string
            
            // Customize OpenID Connect events to handle post-authentication redirects
            options.Events.OnRemoteFailure = context =>
            {
                context.HandleResponse();
                context.Response.Redirect("/Login?error=" + Uri.EscapeDataString(context.Failure?.Message ?? "Authentication failed"));
                return Task.CompletedTask;
            };
            
            options.Events.OnAuthenticationFailed = context =>
            {
                context.HandleResponse();
                context.Response.Redirect("/Login?error=" + Uri.EscapeDataString(context.Exception?.Message ?? "Authentication error"));
                return Task.CompletedTask;
            };
            
            // Log successful authentication for debugging
            options.Events.OnTicketReceived = context =>
            {
                Log.Logger.Information("OnTicketReceived: Authentication ticket received successfully");
                return Task.CompletedTask;
            };
        }, cookieScheme: "AzureAD");
        
        // Configure Microsoft Identity cookie settings to match our custom Login page
        builder.Services.Configure<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>("AzureAD", options =>
        {
            options.LoginPath = "/Login";
            options.LogoutPath = "/Login";
            options.AccessDeniedPath = "/NotAuthorized";
        });
        
        Log.Information("✅ Entra ID (Azure AD) authentication enabled");
        Log.Information("   Tenant: {TenantId}", authConfig.EntraIdTenantId);
    }
    
    Log.Information("✅ Hybrid authentication configured:");
    Log.Information("   - Entra ID enabled: {EntraEnabled}", authConfig.EnableEntraId && !string.IsNullOrEmpty(authConfig.EntraIdTenantId));
    Log.Information("   - Local Accounts enabled: {LocalEnabled}", authConfig.EnableLocalAccounts);

    // Add authorization (always needed for [Authorize] attribute)
    builder.Services.AddAuthorization(options =>
    {
        // Configure authorization to require authentication for protected routes
        options.FallbackPolicy = null; // Don't require auth globally (landing page is public)
        
        // Default policy requires authenticated user from EITHER Identity.Application OR AzureAD scheme
        var defaultPolicyBuilder = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder(
            IdentityConstants.ApplicationScheme,
            "AzureAD");
        
        options.DefaultPolicy = defaultPolicyBuilder
            .RequireAuthenticatedUser()
            .Build();
    });

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
            limiterOptions.PermitLimit = initialConfig.WebPortal.MaxConcurrentUploads;
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

    // Add Razor Pages with Microsoft Identity UI (for Entra ID login pages)
    builder.Services.AddRazorPages()
        .AddMicrosoftIdentityUI();

    var app = builder.Build();

    // Initialize database and seed roles (for Identity)
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = services.GetRequiredService<ILogger<Program>>();
            
            var dbInit = new DatabaseInitializationService(context, roleManager, 
                services.GetRequiredService<ILogger<DatabaseInitializationService>>());
            await dbInit.InitializeAsync();
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "❌ An error occurred while initializing the database");
        }
    }

    // Ensure directories exist (using initial config for startup)
    Directory.CreateDirectory(initialConfig.Paths.UploadDirectory);
    Directory.CreateDirectory(initialConfig.Paths.LogDirectory);

    if (initialConfig.WebPortal.EnableUploadToTransfer)
    {
        Directory.CreateDirectory(initialConfig.Service.WatchDirectory);
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
        if (initialConfig.WebPortal.Kestrel.EnableHttps)
        {
            app.UseHsts();
        }
    }

    // SECURITY FIX (CRITICAL-1): Only use HTTPS redirection if HTTPS is enabled
    if (initialConfig.WebPortal.Kestrel.EnableHttps)
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
    // IMPORTANT: Authentication middleware MUST be before authorization middleware
    app.UseAuthentication();
    app.UseAuthorization();
    
    // Add diagnostic middleware to log authentication attempts
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/Upload") || 
            context.Request.Path.StartsWithSegments("/Index"))
        {
            var isAuthenticated = context.User?.Identity?.IsAuthenticated ?? false;
            var userName = context.User?.Identity?.Name ?? "Anonymous";
            var identityType = context.User?.Identity?.AuthenticationType ?? "None";
            
            if (isAuthenticated)
            {
                Log.Information("✅ Authenticated request to {Path} from {User} ({Ip}) via {AuthType}", 
                    context.Request.Path, userName, context.Connection.RemoteIpAddress, identityType);
            }
            else
            {
                Log.Information("🔐 Unauthenticated request to {Path} from {Ip}", 
                    context.Request.Path, context.Connection.RemoteIpAddress);
            }
        }
        
        await next();
    });

    app.MapRazorPages();
    
    // Map SignalR hub for real-time transfer status updates
    app.MapHub<ZLFileRelay.WebPortal.Hubs.TransferStatusHub>("/hubs/transferstatus");

    // Log registered authentication schemes for debugging
    var authSchemeProvider = app.Services.GetRequiredService<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
    var schemes = await authSchemeProvider.GetAllSchemesAsync();
    Log.Information("✅ Registered authentication schemes: {Schemes}", 
        string.Join(", ", schemes.Select(s => s.Name)));
    
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
