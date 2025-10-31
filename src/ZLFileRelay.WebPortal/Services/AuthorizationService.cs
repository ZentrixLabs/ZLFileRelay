using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZLFileRelay.Core.Models;

namespace ZLFileRelay.WebPortal.Services
{
    /// <summary>
    /// Service for handling simplified authorization (Entra ID + Local Accounts)
    /// </summary>
    public class AuthorizationService
    {
        private readonly IOptionsMonitor<ZLFileRelayConfiguration> _configMonitor;
        private readonly ILogger<AuthorizationService> _logger;

        public AuthorizationService(
            IOptionsMonitor<ZLFileRelayConfiguration> configMonitor, 
            ILogger<AuthorizationService> logger)
        {
            _configMonitor = configMonitor ?? throw new ArgumentNullException(nameof(configMonitor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Check if user is allowed to upload files
        /// Simplified authorization: All authenticated users are allowed
        /// - Entra ID: Enterprise App assignment controls access
        /// - Local Accounts: All authenticated users can upload
        /// </summary>
        public Task<bool> IsUserAllowedAsync(ClaimsPrincipal user)
        {
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                _logger.LogWarning("User is not authenticated");
                return Task.FromResult(false);
            }

            // All authenticated users are allowed to upload
            var userName = user.Identity?.Name ?? "Unknown";
            var email = user.FindFirst(ClaimTypes.Email)?.Value ?? 
                        user.FindFirst("preferred_username")?.Value ?? // Entra ID email claim
                        user.FindFirst("emails")?.Value; // Alternate Entra ID email claim

            _logger.LogInformation("✅ User {User} ({Email}) authorized - all authenticated users can upload", userName, email);
            return Task.FromResult(true);
        }

        /// <summary>
        /// Check if user has admin privileges
        /// Note: No separate admin concept in simplified model
        /// Enterprise App admins control Entra ID access via Azure Portal
        /// </summary>
        public bool IsUserAdmin(ClaimsPrincipal user)
        {
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                return false;
            }

            // For now, all authenticated users have the same privileges
            // Admin functionality is controlled via Enterprise App assignment in Azure Portal
            return true;
        }

        /// <summary>
        /// Get user's display name from claims
        /// </summary>
        public string GetUserDisplayName(ClaimsPrincipal user)
        {
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                return "Anonymous";
            }

            // Try various claim types for name
            return user.FindFirst(ClaimTypes.Name)?.Value ??
                   user.FindFirst("name")?.Value ?? // Entra ID name claim
                   user.FindFirst(ClaimTypes.Email)?.Value ??
                   user.FindFirst("preferred_username")?.Value ??
                   user.Identity?.Name ??
                   "Unknown User";
        }

        /// <summary>
        /// Get user's email from claims
        /// </summary>
        public string? GetUserEmail(ClaimsPrincipal user)
        {
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                return null;
            }

            return user.FindFirst(ClaimTypes.Email)?.Value ??
                   user.FindFirst("preferred_username")?.Value ??
                   user.FindFirst("emails")?.Value;
        }
    }
}
