using System.Diagnostics;
using System.DirectoryServices;
using System.IO;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZLFileRelay.ConfigTool.Interfaces;

namespace ZLFileRelay.ConfigTool.Services;

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

    public async Task<bool> SetServiceAccountAsync(string username, string password)
    {
        try
        {
            var scTarget = GetScTarget();
            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"{scTarget}config {ServiceName} obj= \"{username}\" password= \"{password}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return false;

            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                _logger.LogInformation("Service account updated successfully");
                return true;
            }

            var error = await process.StandardError.ReadToEndAsync();
            _logger.LogError("Failed to set service account: {Error}", error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set service account");
            return false;
        }
    }

    public async Task<bool> GrantLogonAsServiceRightAsync(string username)
    {
        try
        {
            // PowerShell script to grant "Log on as a service" right
            var psScript = $@"
param($UserName)

$sidstr = $null
try {{
    $ntprincipal = new-object System.Security.Principal.NTAccount $UserName
    $sid = $ntprincipal.Translate([System.Security.Principal.SecurityIdentifier])
    $sidstr = $sid.Value.ToString()
}} catch {{
    Write-Error ""Failed to resolve SID for user: $UserName""
    exit 1
}}

Write-Output ""Account: $UserName""
Write-Output ""Account SID: $sidstr""

# Export current security policy
$tmp = [System.IO.Path]::GetTempFileName()
secedit.exe /export /cfg ""$tmp"" | Out-Null

# Read current settings
$c = Get-Content -Path $tmp
$currentSetting = """"
foreach($s in $c) {{
    if($s -like ""SeServiceLogonRight*"") {{
        $x = $s.split(""="", [System.StringSplitOptions]::RemoveEmptyEntries)
        $currentSetting = $x[1].Trim()
    }}
}}

# Check if user already has the right
if($currentSetting -notlike ""*$sidstr*"") {{
    Write-Output ""Granting SeServiceLogonRight...""
    
    if([string]::IsNullOrEmpty($currentSetting)) {{
        $currentSetting = ""*$sidstr""
    }} else {{
        $currentSetting = ""*$sidstr,$currentSetting""
    }}
    
    # Create security database configuration
    $outfile = @""
[Unicode]
Unicode=yes
[Version]
signature=""$$CHICAGO$$""
Revision=1
[Privilege Rights]
SeServiceLogonRight = $currentSetting
""@
    
    $tmp2 = [System.IO.Path]::GetTempFileName()
    $outfile | Set-Content -Path $tmp2 -Encoding Unicode -Force
    
    # Apply configuration
    secedit.exe /configure /db ""secedit.sdb"" /cfg ""$tmp2"" /areas USER_RIGHTS /quiet
    
    Remove-Item -Path $tmp2 -Force
    Write-Output ""Successfully granted SeServiceLogonRight to $UserName""
}} else {{
    Write-Output ""Account already has SeServiceLogonRight""
}}

Remove-Item -Path $tmp -Force
exit 0
";

            // Execute via PowerShell Remoting if remote, otherwise locally
            var result = await _psRemoting.ExecuteScriptAsync(
                psScript.Replace("$UserName", $"'{username}'"));

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
            _logger.LogError(ex, "Failed to grant logon as service right");
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

