using System;
using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;

namespace ZLFileRelay.Service.Services
{
    /// <summary>
    /// Handles network connection impersonation for SMB/UNC access
    /// </summary>
    public class NetworkConnection : IDisposable
    {
        private string? _networkName;

        [DllImport("mpr.dll")]
        private static extern int WNetAddConnection2(NetResource netResource,
            string password, string username, int flags);

        [DllImport("mpr.dll")]
        private static extern int WNetCancelConnection2(string name, int flags,
            bool force);

        [StructLayout(LayoutKind.Sequential)]
        private class NetResource
        {
            public int Scope = 0;
            public int Type = 1; // RESOURCETYPE_DISK
            public int DisplayType = 0;
            public int Usage = 0;
            public string? LocalName = null;
            public string? RemoteName = null;
            public string? Comment = null;
            public string? Provider = null;
        }

        public NetworkConnection(string networkName, NetworkCredential credentials)
        {
            _networkName = networkName;

            var netResource = new NetResource
            {
                RemoteName = networkName
            };

            var userName = string.IsNullOrEmpty(credentials.Domain)
                ? credentials.UserName
                : credentials.Domain + "\\" + credentials.UserName;

            int result = WNetAddConnection2(
                netResource,
                credentials.Password,
                userName,
                0);

            if (result != 0)
            {
                string errorMessage = GetNetworkErrorMessage(result, networkName, userName);
                throw new Win32Exception(result, errorMessage);
            }
        }

        private static string GetNetworkErrorMessage(int errorCode, string networkName, string userName)
        {
            return errorCode switch
            {
                53 => $"Network path not found: {networkName}",
                67 => $"Network name not found: {networkName}",
                86 => $"Invalid password for user {userName}",
                1203 => $"Session limit exceeded when connecting to {networkName}",
                1219 => $"Multiple connections to {networkName} using different credentials not allowed",
                1326 => $"Invalid username or password: {userName}",
                1396 => $"Logon failure for user {userName}",
                1909 => $"Account locked out: {userName}",
                5 => $"Access denied to {networkName} for user {userName}",
                1231 => $"Network location {networkName} is not available",
                64 => $"Network name is in use when connecting to {networkName}",
                1331 => $"Account {userName} is disabled",
                1907 => $"Password for account {userName} has expired",
                1311 => $"No logon servers available for {userName}",
                _ => $"Error {errorCode} connecting to {networkName} as {userName}"
            };
        }

        ~NetworkConnection()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!string.IsNullOrEmpty(_networkName))
            {
                WNetCancelConnection2(_networkName!, 0, true);
                _networkName = null;
            }
        }
    }
}

