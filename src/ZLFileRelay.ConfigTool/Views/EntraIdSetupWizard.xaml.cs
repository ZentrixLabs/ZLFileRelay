using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Navigation;
using ZLFileRelay.Core.Models;

namespace ZLFileRelay.ConfigTool.Views
{
    public partial class EntraIdSetupWizard : Window
    {
        public string? TenantId { get; private set; }
        public string? ClientId { get; private set; }
        public string? ClientSecret { get; private set; }
        public string RedirectUri { get; private set; }
        public List<string> RedirectUris { get; private set; }

        public List<string> RegistrationInfo { get; private set; }

        public EntraIdSetupWizard()
        {
            InitializeComponent();

            // Load configuration to determine redirect URI
            var configPath = System.IO.Path.Combine(
                ZLFileRelay.Core.Constants.ApplicationConstants.Configuration.DefaultConfigDirectory,
                "appsettings.json");

            var kestrelPort = 8443; // Default
            try
            {
                if (System.IO.File.Exists(configPath))
                {
                    var json = System.IO.File.ReadAllText(configPath);
                    var config = System.Text.Json.JsonSerializer.Deserialize<ZLFileRelayConfiguration>(
                        json, 
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    kestrelPort = config?.WebPortal?.Kestrel?.HttpsPort ?? 8443;
                }
            }
            catch
            {
                // Use default if config can't be read
            }

            // Detect all possible hostnames for multi-NIC scenarios
            var hostnames = GetAvailableHostnames();
            RedirectUris = hostnames.Select(h => $"https://{h}:{kestrelPort}/signin-oidc").ToList();
            
            // Use primary URI (first one)
            RedirectUri = RedirectUris.First();

            // Setup registration info for step 1
            RegistrationInfo = new List<string>
            {
                "• Name: ZL File Relay (or your preferred name)",
                "• Supported account types: Accounts in this organizational directory only (Single tenant)",
                $"• Redirect URI(s): Add all URLs below to Azure Portal",
                "• Click Register (you'll add URIs after registration)"
            };

            DataContext = this;
            
            // Initial population of complete URI list
            Loaded += (s, e) => UpdateCompleteUriList();
        }
        
        private void UpdateCompleteUriList()
        {
            if (CompleteUriList == null) return;
            
            var allUris = new List<string>();
            
            // Add auto-detected URIs
            allUris.AddRange(RedirectUris);
            
            // Add custom URIs
            if (AdditionalUrisBox != null && !string.IsNullOrWhiteSpace(AdditionalUrisBox.Text))
            {
                var customHostnames = AdditionalUrisBox.Text
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(h => h.Trim())
                    .Where(h => !string.IsNullOrWhiteSpace(h));
                
                // Assume HTTPS port 8443 for custom hostnames (same as detected ones)
                var kestrelPort = 8443;
                try
                {
                    var configPath = System.IO.Path.Combine(
                        ZLFileRelay.Core.Constants.ApplicationConstants.Configuration.DefaultConfigDirectory,
                        "appsettings.json");
                    if (System.IO.File.Exists(configPath))
                    {
                        var json = System.IO.File.ReadAllText(configPath);
                        var config = System.Text.Json.JsonSerializer.Deserialize<ZLFileRelayConfiguration>(
                            json, 
                            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        kestrelPort = config?.WebPortal?.Kestrel?.HttpsPort ?? 8443;
                    }
                }
                catch
                {
                    // Use default
                }
                
                foreach (var hostname in customHostnames)
                {
                    if (!hostname.StartsWith("https://") && !hostname.StartsWith("http://"))
                    {
                        allUris.Add($"https://{hostname}:{kestrelPort}/signin-oidc");
                    }
                    else
                    {
                        allUris.Add($"{hostname}/signin-oidc");
                    }
                }
            }
            
            CompleteUriList.Text = string.Join(Environment.NewLine, allUris.Distinct());
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.ToString(),
                UseShellExecute = true
            });
            e.Handled = true;
        }

        private void CopyFromClipboard_TenantId(object sender, RoutedEventArgs e)
        {
            try
            {
                var text = Clipboard.GetText();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    TenantIdBox.Text = text.Trim();
                }
            }
            catch
            {
                MessageBox.Show("Could not paste from clipboard. Please paste manually.", 
                    "Clipboard Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
            }
        }

        private void CopyFromClipboard_ClientId(object sender, RoutedEventArgs e)
        {
            try
            {
                var text = Clipboard.GetText();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    ClientIdBox.Text = text.Trim();
                }
            }
            catch
            {
                MessageBox.Show("Could not paste from clipboard. Please paste manually.", 
                    "Clipboard Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
            }
        }

        private void CopyFromClipboard_ClientSecret(object sender, RoutedEventArgs e)
        {
            try
            {
                var text = Clipboard.GetText();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var secureString = new System.Security.SecureString();
                    foreach (char c in text.Trim())
                    {
                        secureString.AppendChar(c);
                    }
                    ClientSecretBox.Password = text.Trim();
                }
            }
            catch
            {
                MessageBox.Show("Could not paste from clipboard. Please paste manually.", 
                    "Clipboard Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
            }
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            // Validate all fields are filled
            if (string.IsNullOrWhiteSpace(TenantIdBox.Text))
            {
                MessageBox.Show("Please enter the Tenant ID.", 
                    "Missing Information", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
                TenantIdBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(ClientIdBox.Text))
            {
                MessageBox.Show("Please enter the Client ID.", 
                    "Missing Information", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
                ClientIdBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(ClientSecretBox.Password))
            {
                MessageBox.Show("Please enter the Client Secret.", 
                    "Missing Information", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
                ClientSecretBox.Focus();
                return;
            }

            // Set properties
            TenantId = TenantIdBox.Text.Trim();
            ClientId = ClientIdBox.Text.Trim();
            ClientSecret = ClientSecretBox.Password;

            // Return success
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void CopyCompleteList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CompleteUriList != null && !string.IsNullOrWhiteSpace(CompleteUriList.Text))
                {
                    Clipboard.SetText(CompleteUriList.Text);
                    MessageBox.Show(
                        "Complete redirect URI list copied to clipboard!\n\nPaste into Azure Portal or a text editor.",
                        "Copied",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not copy to clipboard: {ex.Message}\n\nPlease copy manually from the text box above.",
                    "Clipboard Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void AdditionalUrisBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateCompleteUriList();
        }

        private List<string> GetAvailableHostnames()
        {
            var hostnames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            // Add computer name (short name)
            hostnames.Add(System.Environment.MachineName.ToLower());
            
            try
            {
                // Get FQDN from DNS
                var fqdn = System.Net.Dns.GetHostEntry(string.Empty).HostName.ToLower();
                hostnames.Add(fqdn);
            }
            catch
            {
                // DNS lookup failed, skip
            }
            
            // Get all IP addresses and try to resolve hostnames
            try
            {
                var addresses = Dns.GetHostAddresses(string.Empty);
                foreach (var address in addresses)
                {
                    // Skip loopback and link-local
                    if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(address) &&
                        !IsLinkLocal(address))
                    {
                        try
                        {
                            var hostEntry = Dns.GetHostEntry(address);
                            hostnames.Add(hostEntry.HostName.ToLower());
                        }
                        catch
                        {
                            // Reverse lookup failed
                        }
                    }
                }
            }
            catch
            {
                // Failed to get addresses
            }
            
            // Get network interfaces and their DNS suffixes
            try
            {
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus == OperationalStatus.Up &&
                        nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        var ipProps = nic.GetIPProperties();
                        
                        // Get DNS suffix from network interface
                        foreach (var dns in ipProps.DnsAddresses)
                        {
                            if (IPAddress.IsLoopback(dns)) continue;
                        }
                        
                        // Get DNS suffixes
                        if (ipProps.DnsSuffix != null && !string.IsNullOrEmpty(ipProps.DnsSuffix))
                        {
                            var fqdnWithSuffix = $"{System.Environment.MachineName}.{ipProps.DnsSuffix}".ToLower();
                            hostnames.Add(fqdnWithSuffix);
                        }
                    }
                }
            }
            catch
            {
                // Failed to enumerate NICs
            }
            
            return hostnames.ToList();
        }

        private bool IsLinkLocal(IPAddress address)
        {
            // IPv4 link-local: 169.254.0.0/16
            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                var bytes = address.GetAddressBytes();
                return bytes[0] == 169 && bytes[1] == 254;
            }
            return false;
        }
    }
}

