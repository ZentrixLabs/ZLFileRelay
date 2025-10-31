using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZLFileRelay.WebPortal.Data;

namespace ZLFileRelay.WebPortal.Services
{
    /// <summary>
    /// Service to initialize the database and seed roles
    /// </summary>
    public class DatabaseInitializationService
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<DatabaseInitializationService> _logger;

        public DatabaseInitializationService(
            ApplicationDbContext context,
            RoleManager<IdentityRole> roleManager,
            ILogger<DatabaseInitializationService> logger)
        {
            _context = context;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            try
            {
                // Ensure database is created
                _logger.LogInformation("🔧 Ensuring database exists...");
                await _context.Database.EnsureCreatedAsync();
                
                // Apply any pending migrations
                if (_context.Database.GetPendingMigrations().Any())
                {
                    _logger.LogInformation("📦 Applying pending database migrations...");
                    await _context.Database.MigrateAsync();
                }
                
                // Seed roles
                await SeedRolesAsync();
                
                _logger.LogInformation("✅ Database initialization complete");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error initializing database");
                throw;
            }
        }

        private async Task SeedRolesAsync()
        {
            string[] roleNames = { "Admin", "Uploader" };
            
            foreach (var roleName in roleNames)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    _logger.LogInformation("🔧 Creating role: {Role}", roleName);
                    var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
                    
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("✅ Role created: {Role}", roleName);
                    }
                    else
                    {
                        _logger.LogError("❌ Failed to create role {Role}: {Errors}", 
                            roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }
        }
    }
}

