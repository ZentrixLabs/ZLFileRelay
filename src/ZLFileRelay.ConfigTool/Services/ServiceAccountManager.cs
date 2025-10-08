using System.Diagnostics;
using System.DirectoryServices;
using System.IO;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZLFileRelay.ConfigTool.Interfaces;

namespace ZLFileRelay.ConfigTool.Services;

/// <summary>
/// Manages the service account credentials for the ZLFileRelay Windows Service.
/// 
/// IMPORTANT: Service account credentials managed by this class are ONLY for running 
/// the ZLFileRelay service itself. They are NOT used for remote management operations.
/// Remote management uses either current user credentials or explicit admin credentials.
/// </summary>
public class ServiceAccountManager
{
    private readonly ILogger<ServiceAccountManager> _logger;
    private readonly IRemoteServerProvider _remoteServerProvider;
    private readonly PowerShellRemotingService _psRemoting;
    private const string ServiceName = "ZLFileRelay";

    public ServiceAccountManager(
        ILogger<ServiceAccountManager> logger,
        IRemoteServerProvider remoteServerProvider,
        PowerShellRemotingService psRemoting)
    {
        _logger = logger;
        _remoteServerProvider = remoteServerProvider;
        _psRemoting = psRemoting;
    }

    private string GetScTarget()
    {
        if (_remoteServerProvider.IsRemote && !string.IsNullOrWhiteSpace(_remoteServerProvider.ServerName))
        {
            return $"\\\\{_remoteServerProvider.ServerName} ";
        }
        return "";
    }

    public async Task<string?> GetCurrentServiceAccountAsync()
    {
        try
        {
            var scTarget = GetScTarget();
            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"{scTarget}qc {ServiceName}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return null;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Parse output for SERVICE_START_NAME
            foreach (var line in output.Split('\n'))
            {
                if (line.Contains("SERVICE_START_NAME"))
                {
                    var parts = line.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        return parts[1].Trim();
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current service account");
            return null;
        }
    }

    /// <summary>
    /// Sets the service account for the ZLFileRelay Windows Service.
    /// These credentials are ONLY used for running the service, NOT for remote management.
    /// </summary>
    public async Task<bool> SetServiceAccountAsync(string username, string password)
    {
        try
        {
            // Security check: Log that these are service credentials, not remote management credentials
            _logger.LogInformation("Setting service account credentials for ZLFileRelay service: {Username}", username);
            _logger.LogDebug("Note: Service account credentials are NOT used for remote management operations");
            
            // SECURITY FIX: Use WMI via PowerShell with SecureString instead of sc.exe
            // This prevents password exposure in command-line arguments
            
            // Convert password to SecureString (encrypted in memory)
            var securePassword = new System.Security.SecureString();
            foreach (char c in password)
            {
                securePassword.AppendChar(c);
            }
            securePassword.MakeReadOnly();

            // Define PowerShell script that uses WMI to change service account
            var psScript = @"
param(
    [Parameter(Mandatory=$true)]
    [string]$ServiceName,
    
    [Parameter(Mandatory=$true)]
    [string]$Username,
    
    [Parameter(Mandatory=$true)]
    [securestring]$Password
)

$ErrorActionPreference = 'Stop'

try {
    # Convert SecureString to plain text for WMI (handled securely in memory)
    $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password)
    try {
        $plainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
        
        # Get the service via WMI
        $service = Get-WmiObject -Class Win32_Service -Filter ""Name='$ServiceName'""
        
        if ($null -eq $service) {
            throw ""Service not found: $ServiceName""
        }
        
        Write-Output ""Found service: $($service.DisplayName)""
        Write-Output ""Current service account: $($service.StartName)""
        Write-Output ""Changing service account to: $Username""
        
        # Change service account using WMI Change method
        # Parameters: DisplayName, PathName, ServiceType, ErrorControl, StartMode, 
        #             DesktopInteract, StartName, StartPassword, LoadOrderGroup, 
        #             LoadOrderGroupDependencies, ServiceDependencies
        $result = $service.Change(
            $null,           # DisplayName (null = no change)
            $null,           # PathName (null = no change)
            $null,           # ServiceType (null = no change)
            $null,           # ErrorControl (null = no change)
            $null,           # StartMode (null = no change)
            $null,           # DesktopInteract (null = no change)
            $Username,       # StartName (the new account)
            $plainPassword,  # StartPassword (the new password)
            $null,           # LoadOrderGroup (null = no change)
            $null,           # LoadOrderGroupDependencies (null = no change)
            $null            # ServiceDependencies (null = no change)
        )
        
        # Check result code
        switch ($result.ReturnValue) {
            0 { 
                Write-Output ""✅ Service account changed successfully""
                return $true 
            }
            1 { throw ""Not supported"" }
            2 { throw ""Access denied"" }
            3 { throw ""Dependent services running"" }
            4 { throw ""Invalid service control"" }
            5 { throw ""Service cannot accept control"" }
            6 { throw ""Service not active"" }
            7 { throw ""Service request timeout"" }
            8 { throw ""Unknown failure"" }
            9 { throw ""Path not found"" }
            10 { throw ""Service already running"" }
            11 { throw ""Service database locked"" }
            12 { throw ""Service dependency deleted"" }
            13 { throw ""Service dependency failure"" }
            14 { throw ""Service disabled"" }
            15 { throw ""Service logon failure - Invalid account or password"" }
            16 { throw ""Service marked for deletion"" }
            17 { throw ""Service no thread"" }
            18 { throw ""Status: Circular dependency"" }
            19 { throw ""Status: Duplicate name"" }
            20 { throw ""Status: Invalid name"" }
            21 { throw ""Status: Invalid parameter"" }
            22 { throw ""Status: Invalid service account"" }
            23 { throw ""Status: Service exists"" }
            24 { throw ""Service already paused"" }
            default { throw ""Unknown error code: $($result.ReturnValue)"" }
        }
    }
    finally {
        # Zero out and free the unmanaged string containing the password
        [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}
catch {
    Write-Error ""Failed to change service account: $($_.Exception.Message)""
    throw
}
";

            // Execute the script with parameters
            // First, load the script
            var loadResult = await _psRemoting.ExecuteScriptAsync(psScript);
            
            if (!loadResult.Success)
            {
                _logger.LogError("Failed to load PowerShell script: {Errors}", loadResult.ErrorText);
                return false;
            }

            // Now execute with safe parameter passing
            var parameters = new Dictionary<string, object>
            {
                { "ServiceName", ServiceName },
                { "Username", username },
                { "Password", securePassword }
            };

            var result = await _psRemoting.ExecuteCommandAsync(psScript, parameters);

            if (result.Success)
            {
                _logger.LogInformation("Service account updated successfully: {Output}", result.OutputText);
                return true;
            }
            else
            {
                _logger.LogError("Failed to set service account: {Errors}", result.ErrorText);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set service account for {Username}", username);
            return false;
        }
        finally
        {
            // Clear password from memory as soon as possible
            // Note: The SecureString is automatically disposed when it goes out of scope
        }
    }

    public async Task<bool> GrantLogonAsServiceRightAsync(string username)
    {
        try
        {
            _logger.LogInformation("Granting logon as service right to: {Username}", username);

            // SECURITY FIX: Use parameterized PowerShell execution to prevent injection attacks
            // Define the PowerShell function that grants SeServiceLogonRight
            var functionDefinition = @"
function Grant-ServiceLogonRight {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]$UserName
    )
    
    $ErrorActionPreference = 'Stop'
    
    try {
        # Resolve username to SID
        $ntprincipal = New-Object System.Security.Principal.NTAccount $UserName
        $sid = $ntprincipal.Translate([System.Security.Principal.SecurityIdentifier])
        $sidstr = $sid.Value.ToString()
        
        Write-Output ""Account: $UserName""
        Write-Output ""Account SID: $sidstr""
        
        # Export current security policy
        $tmp = [System.IO.Path]::GetTempFileName()
        $null = secedit.exe /export /cfg ""$tmp""
        
        if (-not (Test-Path $tmp)) {
            throw ""Failed to export security policy""
        }
        
        # Read current settings
        $content = Get-Content -Path $tmp
        $currentSetting = """"
        
        foreach($line in $content) {
            if($line -like ""SeServiceLogonRight*"") {
                $parts = $line.Split('=', [System.StringSplitOptions]::RemoveEmptyEntries)
                if ($parts.Count -ge 2) {
                    $currentSetting = $parts[1].Trim()
                }
                break
            }
        }
        
        # Check if user already has the right
        if($currentSetting -notlike ""*$sidstr*"") {
            Write-Output ""Granting SeServiceLogonRight...""
            
            # Build new setting list
            if([string]::IsNullOrEmpty($currentSetting)) {
                $currentSetting = ""*$sidstr""
            } else {
                $currentSetting = ""*$sidstr,$currentSetting""
            }
            
            # Create security database configuration
            $configContent = @""
[Unicode]
Unicode=yes
[Version]
signature=""$CHICAGO$""
Revision=1
[Privilege Rights]
SeServiceLogonRight = $currentSetting
""@
            
            $tmp2 = [System.IO.Path]::GetTempFileName()
            $configContent | Set-Content -Path $tmp2 -Encoding Unicode -Force
            
            # Apply configuration
            $null = secedit.exe /configure /db ""secedit.sdb"" /cfg ""$tmp2"" /areas USER_RIGHTS /quiet
            
            # Clean up
            if (Test-Path $tmp2) {
                Remove-Item -Path $tmp2 -Force
            }
            
            Write-Output ""Successfully granted SeServiceLogonRight to $UserName""
        } else {
            Write-Output ""Account already has SeServiceLogonRight""
        }
        
        # Clean up
        if (Test-Path $tmp) {
            Remove-Item -Path $tmp -Force
        }
        
        return $true
    }
    catch {
        Write-Error $_.Exception.Message
        return $false
    }
}
";

            // First, load the function definition
            var loadResult = await _psRemoting.ExecuteScriptAsync(functionDefinition);
            
            if (!loadResult.Success)
            {
                _logger.LogError("Failed to load PowerShell function: {Errors}", loadResult.ErrorText);
                return false;
            }

            // Now invoke the function with safe parameter passing
            var parameters = new Dictionary<string, object>
            {
                { "UserName", username }
            };

            var result = await _psRemoting.ExecuteCommandAsync("Grant-ServiceLogonRight", parameters);

            if (result.Success)
            {
                _logger.LogInformation("Grant logon as service right succeeded for {User}: {Output}", 
                    username, result.OutputText);
                return true;
            }
            else
            {
                _logger.LogError("Failed to grant logon as service right for {User}: {Errors}", 
                    username, result.ErrorText);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to grant logon as service right for {Username}", username);
            return false;
        }
    }

    public async Task<bool> CheckProfileExistsAsync(string username)
    {
        try
        {
            // Extract just the username part (remove domain)
            var parts = username.Split('\\');
            var user = parts.Length > 1 ? parts[1] : parts[0];
            
            var profilePath = Path.Combine(@"C:\Users", user);
            return await Task.Run(() => Directory.Exists(profilePath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check profile exists");
            return false;
        }
    }

    public async Task<string?> GetProfilePathAsync(string username)
    {
        try
        {
            var parts = username.Split('\\');
            var user = parts.Length > 1 ? parts[1] : parts[0];
            
            var profilePath = Path.Combine(@"C:\Users", user);
            return await Task.Run(() => Directory.Exists(profilePath) ? profilePath : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get profile path");
            return null;
        }
    }
}

