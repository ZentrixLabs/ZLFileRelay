using System.Runtime.Versioning;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.Extensions.Logging;
using ZLFileRelay.Core.Models;

namespace ZLFileRelay.WebPortal.Services
{
    /// <summary>
    /// Service for handling Windows AD authorization
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class AuthorizationService
    {
        private readonly ZLFileRelayConfiguration _config;
        private readonly ILogger<AuthorizationService> _logger;

        public AuthorizationService(
            ZLFileRelayConfiguration config, 
            ILogger<AuthorizationService> logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool IsUserInAllowedGroups(ClaimsPrincipal user)
        {
            var allowedGroups = _config.WebPortal.AllowedGroups;

            if (allowedGroups == null || !allowedGroups.Any())
            {
                _logger.LogWarning("No allowed groups configured - denying access");
                return false;
            }

            var identity = user.Identity as WindowsIdentity;
            if (identity == null)
            {
                _logger.LogWarning("User identity is not WindowsIdentity");
                return false;
            }

            // Get user groups
            var userGroups = new List<string>();
            if (identity.Groups != null)
            {
                foreach (var group in identity.Groups)
                {
                    try
                    {
                        var fullGroupName = new SecurityIdentifier(group.Value)
                            .Translate(typeof(NTAccount)).Value;
                        userGroups.Add(fullGroupName);

                        // Add short name without domain
                        if (fullGroupName.Contains("\\"))
                        {
                            var shortGroupName = fullGroupName.Split('\\')[1];
                            userGroups.Add(shortGroupName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not translate group SID {Sid}", group.Value);
                        userGroups.Add(group.Value);
                    }
                }
            }

            _logger.LogDebug("User {User} groups: {Groups}", user.Identity?.Name, string.Join(", ", userGroups));
            _logger.LogDebug("Allowed groups: {Groups}", string.Join(", ", allowedGroups));

            // Compare case-insensitively
            var matches = userGroups
                .Select(g => g.ToLowerInvariant())
                .Intersect(allowedGroups.Select(g => g.ToLowerInvariant()))
                .ToList();

            if (matches.Any())
            {
                _logger.LogInformation("User {User} authorized via groups: {Groups}", 
                    user.Identity?.Name, string.Join(", ", matches));
                return true;
            }

            _logger.LogWarning("User {User} not in any allowed groups", user.Identity?.Name);
            return false;
        }

        public bool IsUserAllowed(ClaimsPrincipal user)
        {
            // Check if user is in allowed users list
            if (_config.WebPortal.AllowedUsers != null && _config.WebPortal.AllowedUsers.Any())
            {
                var userName = user.Identity?.Name?.ToLowerInvariant();
                if (userName != null && _config.WebPortal.AllowedUsers
                    .Any(u => u.ToLowerInvariant() == userName))
                {
                    _logger.LogInformation("User {User} authorized via user list", user.Identity?.Name);
                    return true;
                }
            }

            // Check group membership
            return IsUserInAllowedGroups(user);
        }
    }
}

