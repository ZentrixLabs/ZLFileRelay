using System.Diagnostics;
using System.DirectoryServices;
using System.IO;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ZLFileRelay.ConfigTool.Services;

public class ServiceAccountManager
{
    private readonly ILogger<ServiceAccountManager> _logger;
    private const string ServiceName = "ZLFileRelay";

    public ServiceAccountManager(ILogger<ServiceAccountManager> logger)
    {
        _logger = logger;
    }

    public async Task<string?> GetCurrentServiceAccountAsync()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"qc {ServiceName}",
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
            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"config {ServiceName} obj= \"{username}\" password= \"{password}\"",
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
            // Use ntrights.exe or PowerShell to grant logon as service right
            var psScript = $@"
                $tempPath = '{Path.GetTempPath()}GrantLogonAsService.ps1'
                @'
                param($UserName)
                $sidstr = $null
                try {{
                    $ntprincipal = new-object System.Security.Principal.NTAccount ""$UserName""
                    $sid = $ntprincipal.Translate([System.Security.Principal.SecurityIdentifier])
                    $sidstr = $sid.Value.ToString()
                }} catch {{
                    $sidstr = $null
                }}
                Write-Host ""Account: "" + $UserName
                Write-Host ""Account SID: "" + $sidstr
                $tmp = [System.IO.Path]::GetTempFileName()
                secedit.exe /export /cfg ""$tmp""
                $c = Get-Content -Path $tmp
                $currentSetting = """"
                foreach($s in $c) {{
                    if( $s -like ""SeServiceLogonRight*"") {{
                        $x = $s.split(""="",  [System.StringSplitOptions]::RemoveEmptyEntries)
                        $currentSetting = $x[1].Trim()
                    }}
                }}
                if( $currentSetting -notlike ""*$sidstr*"" ) {{
                    Write-Host ""Modify Setting ""SeServiceLogonRight""""
                    if( [string]::IsNullOrEmpty($currentSetting) ) {{
                        $currentSetting = ""*$sidstr""
                    }} else {{
                        $currentSetting = ""*$sidstr,$currentSetting""
                    }}
                    $outfile = @""
                [Unicode]
                Unicode=yes
                [Version]
                signature=""`$CHICAGO`$""
                Revision=1
                [Privilege Rights]
                SeServiceLogonRight = $currentSetting
                ""@
                    $tmp2 = [System.IO.Path]::GetTempFileName()
                    $outfile | Set-Content -Path $tmp2 -Encoding Unicode -Force
                    secedit.exe /configure /db ""secedit.sdb"" /cfg ""$tmp2"" /areas USER_RIGHTS
                    Remove-Item -Path $tmp2 -Force
                }} else {{
                    Write-Host ""Account already has SeServiceLogonRight""
                }}
                Remove-Item -Path $tmp -Force
'@ | Out-File -FilePath $tempPath -Encoding UTF8
                powershell.exe -ExecutionPolicy Bypass -File $tempPath -UserName '{username}'
                Remove-Item $tempPath -Force
            ";

            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psScript}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Verb = "runas" // Run as admin
            };

            using var process = Process.Start(startInfo);
            if (process == null) return false;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            _logger.LogInformation("Grant logon as service output: {Output}", output);
            return process.ExitCode == 0;
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

