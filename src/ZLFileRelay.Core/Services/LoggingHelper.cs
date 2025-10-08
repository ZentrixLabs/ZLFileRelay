using System;
using System.IO;
using System.Text.RegularExpressions;

namespace ZLFileRelay.Core.Services
{
    /// <summary>
    /// Helper class for sanitizing sensitive information in logs.
    /// Prevents exposure of private keys, credentials, internal paths, and AD structure.
    /// </summary>
    public static class LoggingHelper
    {
        /// <summary>
        /// Sanitizes a file path by showing only the filename, hiding directory structure.
        /// </summary>
        /// <param name="path">Full file path</param>
        /// <returns>Just the filename</returns>
        public static string SanitizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "[empty]";

            try
            {
                return Path.GetFileName(path);
            }
            catch
            {
                return "[invalid-path]";
            }
        }

        /// <summary>
        /// Sanitizes SSH/SCP command lines by redacting private key paths and other sensitive info.
        /// </summary>
        /// <param name="command">Full command line</param>
        /// <returns>Sanitized command with sensitive parts redacted</returns>
        public static string SanitizeCommandLine(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return "[empty]";

            try
            {
                // Redact private key paths (-i "path")
                command = Regex.Replace(command, @"-i\s+""[^""]+""", "-i \"[REDACTED]\"", RegexOptions.IgnoreCase);
                
                // Redact password parameters if any (shouldn't exist but be safe)
                command = Regex.Replace(command, @"password[=:\s]+[^\s]+", "password=[REDACTED]", RegexOptions.IgnoreCase);
                
                // Redact any obvious credential patterns
                command = Regex.Replace(command, @"(pwd|pass|secret|token)[=:\s]+[^\s]+", "$1=[REDACTED]", RegexOptions.IgnoreCase);

                return command;
            }
            catch
            {
                return "[sanitization-failed]";
            }
        }

        /// <summary>
        /// Sanitizes AD group names by showing only the short name without domain.
        /// </summary>
        /// <param name="groupName">Full group name (DOMAIN\GroupName)</param>
        /// <returns>Short group name without domain</returns>
        public static string SanitizeGroupName(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                return "[empty]";

            try
            {
                // If it contains backslash, take only the part after it
                if (groupName.Contains("\\"))
                {
                    return groupName.Split('\\')[^1];
                }

                // If it contains @ (email format), take only the part before it
                if (groupName.Contains("@"))
                {
                    return groupName.Split('@')[0];
                }

                return groupName;
            }
            catch
            {
                return "[invalid-group]";
            }
        }

        /// <summary>
        /// Sanitizes a list of group names.
        /// </summary>
        /// <param name="groupNames">List of group names</param>
        /// <returns>Comma-separated short names</returns>
        public static string SanitizeGroupList(IEnumerable<string> groupNames)
        {
            if (groupNames == null || !groupNames.Any())
                return "[none]";

            try
            {
                var sanitized = groupNames.Select(SanitizeGroupName);
                return string.Join(", ", sanitized);
            }
            catch
            {
                return "[sanitization-failed]";
            }
        }

        /// <summary>
        /// Sanitizes a username by removing domain prefix but keeping the username.
        /// </summary>
        /// <param name="username">Full username (DOMAIN\user or user@domain)</param>
        /// <returns>Username without domain</returns>
        public static string SanitizeUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return "[empty]";

            try
            {
                // Remove domain prefix (DOMAIN\user)
                if (username.Contains("\\"))
                {
                    return username.Split('\\')[^1];
                }

                // Remove email domain (user@domain.com)
                if (username.Contains("@"))
                {
                    return username.Split('@')[0];
                }

                return username;
            }
            catch
            {
                return "[invalid-username]";
            }
        }

        /// <summary>
        /// Masks part of a sensitive string, showing only first and last few characters.
        /// Useful for API keys, tokens, etc.
        /// </summary>
        /// <param name="sensitiveValue">Sensitive string to mask</param>
        /// <param name="visibleChars">Number of characters to show at start and end (default: 4)</param>
        /// <returns>Masked string like "abcd***xyz"</returns>
        public static string MaskSensitiveValue(string sensitiveValue, int visibleChars = 4)
        {
            if (string.IsNullOrWhiteSpace(sensitiveValue))
                return "[empty]";

            if (sensitiveValue.Length <= visibleChars * 2)
                return new string('*', sensitiveValue.Length);

            try
            {
                var start = sensitiveValue.Substring(0, visibleChars);
                var end = sensitiveValue.Substring(sensitiveValue.Length - visibleChars);
                var maskedLength = sensitiveValue.Length - (visibleChars * 2);
                return $"{start}{new string('*', Math.Min(maskedLength, 8))}{end}";
            }
            catch
            {
                return "[masked]";
            }
        }
    }
}
