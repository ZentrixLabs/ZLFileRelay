using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ZLFileRelay.WebPortal.Data;

/// <summary>
/// Database context for ASP.NET Core Identity
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Customize Identity schema if needed
        // Example: builder.Entity<ApplicationUser>().ToTable("Users");
    }
}

/// <summary>
/// Extended Identity user with approval workflow support
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Whether this user has been approved by an admin
    /// New registrations start with IsApproved = false if RequireApproval is enabled
    /// </summary>
    public bool IsApproved { get; set; }
    
    /// <summary>
    /// Date when the user was approved
    /// </summary>
    public DateTime? ApprovedDate { get; set; }
    
    /// <summary>
    /// Email/username of admin who approved this user
    /// </summary>
    public string? ApprovedBy { get; set; }
    
    /// <summary>
    /// Date when the user registered
    /// </summary>
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
}

