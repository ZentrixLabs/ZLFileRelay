; ZL File Relay - Inno Setup Installer Script
; Self-Contained Deployment with .NET 8 Runtime Included
; Compatible with DMZ/OT/Air-Gapped Environments
; Features: Hybrid Authentication (Entra ID + Local Accounts), Air-Gapped Support

#define MyAppName "ZL File Relay"
#define MyAppVersion "1.1.1"
#define MyAppPublisher "ZentrixLabs"
#define MyAppURL "https://zentrixlabs.com"
#define MyAppExeName "ZLFileRelay.ConfigTool.exe"

[Setup]
; App Information
AppId={{8F9C5D2E-1B3A-4F7C-9E8D-6A5B4C3D2E1F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
AppCopyright=Copyright (C) 2025 {#MyAppPublisher}
LicenseFile=..\LICENSE

; Installation Directories
DefaultDirName={autopf}\ZLFileRelay
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes

; Output
OutputDir=..\artifacts
OutputBaseFilename=ZLFileRelaySetup
SetupIconFile=assets\icon.ico
UninstallDisplayIcon={app}\ConfigTool\{#MyAppExeName}

; Compression (Ultra compression for large .NET runtime)
Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes
LZMADictionarySize=1048576
LZMANumFastBytes=273

; Architecture
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; Privileges
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog

; Wizard
DisableWelcomePage=no
WizardStyle=modern
; TODO: Create branded wizard images for professional installer appearance
; WizardImageFile=assets\WizardImage.bmp (164x314)
; WizardSmallImageFile=assets\WizardSmallImage.bmp (55x58)
; Using Inno Setup defaults until custom images are created

; Uninstall
UninstallDisplayName={#MyAppName}
UninstallFilesDir={app}\Uninstall

; Misc
CloseApplications=force
RestartApplications=no
; Allow upgrades - when using same AppId, Inno Setup will handle upgrades automatically
DisableDirPage=no
DisableReadyPage=no

; Code Signing
; Uncomment and configure the line below in Inno Setup IDE:
; Tools → Configure Sign Tools... → Add SignTool named "signtool"
; Then uncomment this line to enable automatic installer signing:
SignTool=SignTool
; 
; SignTool command example:
; "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe" sign /fd SHA256 /td SHA256 /tr http://timestamp.digicert.com /sha1 YOUR_CERT_THUMBPRINT /d "ZL File Relay Setup" /du "https://github.com/ZentrixLabs/ZLFileRelay" $f

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Types]
Name: "full"; Description: "Full Installation (Service + Web Portal + ConfigTool)"
Name: "servercore"; Description: "Server Core Installation (Service + Web Portal, no ConfigTool)"
Name: "configtoolonly"; Description: "ConfigTool Only (for remote management)"
Name: "service"; Description: "Service Only (no Web Portal, no ConfigTool)"
Name: "custom"; Description: "Custom Installation"; Flags: iscustom

[Components]
Name: "service"; Description: "File Transfer Service"; Types: full servercore service custom
Name: "webportal"; Description: "Web Upload Portal (Windows Service - No IIS Required)"; Types: full servercore custom
Name: "configtool"; Description: "Configuration Tool"; Types: full configtoolonly custom

[Tasks]
Name: "desktopicon"; Description: "Create desktop shortcut for Configuration Tool"; GroupDescription: "Additional icons:"
Name: "installservice"; Description: "Install and start File Transfer Service"; GroupDescription: "Services:"; Components: service; Flags: checkedonce
Name: "installwebservice"; Description: "Install and start Web Portal Service"; GroupDescription: "Services:"; Components: webportal; Flags: checkedonce
Name: "configurefirewall"; Description: "Configure Windows Firewall for Web Portal (ports 8080 & 8443)"; GroupDescription: "Network:"; Components: webportal; Flags: checkedonce
Name: "resetconfig"; Description: "Reset configuration files to defaults during upgrade (WARNING: Overwrites existing config!)"; Check: IsUpgrade(); GroupDescription: "Upgrade Options:"; Flags: unchecked

[Files]
; ═══════════════════════════════════════════════════════════════
; SELF-CONTAINED DEPLOYMENTS - INCLUDES .NET 8 RUNTIME
; ═══════════════════════════════════════════════════════════════

; File Transfer Service (with .NET 8, ~70MB)
Source: "..\publish\Service\*"; DestDir: "{app}\Service"; Components: service; Flags: ignoreversion recursesubdirs createallsubdirs

; Web Upload Portal (with ASP.NET Core 8, ~75MB)
Source: "..\publish\WebPortal\*"; DestDir: "{app}\WebPortal"; Components: webportal; Flags: ignoreversion recursesubdirs createallsubdirs

; Configuration Tool (single .exe with .NET 8, ~300MB)
Source: "..\publish\ConfigTool\ZLFileRelay.ConfigTool.exe"; DestDir: "{app}\ConfigTool"; Components: configtool; Flags: ignoreversion
; ConfigTool needs appsettings.json for fallback defaults
Source: "..\appsettings.json"; DestDir: "{app}\ConfigTool"; Components: configtool; Flags: ignoreversion onlyifdoesntexist

; Configuration Files (respect existing during upgrades unless resetconfig task is selected)
Source: "..\appsettings.json"; DestDir: "{commonappdata}\ZLFileRelay"; Flags: uninsneveruninstall; Tasks: resetconfig
Source: "..\appsettings.json"; DestDir: "{commonappdata}\ZLFileRelay"; Flags: onlyifdoesntexist uninsneveruninstall

; Documentation (selective copy - excludes development & archive folders)
Source: "..\publish\docs\*"; DestDir: "{app}\docs"; Flags: ignoreversion recursesubdirs createallsubdirs

; SSL Certificate Import Script (optional helper - certificate import is handled by ConfigTool GUI)
Source: "scripts\Import-SslCertificate.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion; Components: webportal
Source: "scripts\Setup-HttpSysUrlReservations.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion; Components: webportal

[Dirs]
; Application directories
Name: "{app}\Service"
Name: "{app}\WebPortal"
Name: "{app}\ConfigTool"
Name: "{app}\docs"
Name: "{app}\scripts"
Name: "{app}\Uninstall"

; Data directories
Name: "{commonappdata}\ZLFileRelay"; Permissions: users-modify
Name: "C:\FileRelay"; Permissions: users-modify
Name: "C:\FileRelay\uploads"; Permissions: users-modify
Name: "C:\FileRelay\uploads\transfer"; Permissions: users-modify
Name: "C:\FileRelay\logs"; Permissions: users-modify
Name: "C:\FileRelay\archive"; Permissions: users-modify
Name: "C:\FileRelay\temp"; Permissions: users-modify

[Icons]
Name: "{group}\ZL File Relay Configuration"; Filename: "{app}\ConfigTool\{#MyAppExeName}"; IconFilename: "{app}\ConfigTool\{#MyAppExeName}"
Name: "{group}\Documentation"; Filename: "{app}\docs\README.md"
Name: "{group}\Open Upload Folder"; Filename: "C:\FileRelay\uploads"
Name: "{group}\Open Logs Folder"; Filename: "C:\FileRelay\logs"
Name: "{group}\Edit Configuration"; Filename: "notepad.exe"; Parameters: "{commonappdata}\ZLFileRelay\appsettings.json"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\ZL File Relay"; Filename: "{app}\ConfigTool\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; Register application
Root: HKLM; Subkey: "Software\ZLFileRelay"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\ZLFileRelay"; ValueType: string; ValueName: "Version"; ValueData: "{#MyAppVersion}"
Root: HKLM; Subkey: "Software\ZLFileRelay"; ValueType: string; ValueName: "ConfigPath"; ValueData: "{commonappdata}\ZLFileRelay"

[Run]
; Install File Transfer Windows Service
; Note: Existing services are stopped in InitializeSetup() for upgrade scenarios
Filename: "sc.exe"; Parameters: "create ZLFileRelay binPath=""{app}\Service\ZLFileRelay.Service.exe"" start=auto DisplayName=""ZL File Relay - File Transfer"""; StatusMsg: "Installing File Transfer Service..."; Flags: runhidden; Components: service; Tasks: installservice
Filename: "sc.exe"; Parameters: "description ZLFileRelay ""Automated file transfer from DMZ to SCADA networks"""; Flags: runhidden; Components: service; Tasks: installservice
Filename: "sc.exe"; Parameters: "start ZLFileRelay"; StatusMsg: "Starting File Transfer Service..."; Flags: runhidden; Components: service; Tasks: installservice

; Install Web Portal Windows Service (Kestrel - NO IIS NEEDED!)
; Note: Existing services are stopped in InitializeSetup() for upgrade scenarios
Filename: "sc.exe"; Parameters: "create ZLFileRelay.WebPortal binPath=""{app}\WebPortal\ZLFileRelay.WebPortal.exe"" start=auto DisplayName=""ZL File Relay - Web Portal"""; StatusMsg: "Installing Web Portal Service..."; Flags: runhidden; Components: webportal; Tasks: installwebservice
Filename: "sc.exe"; Parameters: "description ZLFileRelay.WebPortal ""Web-based file upload interface with hybrid authentication (Entra ID + Local Accounts) on port 8080/8443"""; Flags: runhidden; Components: webportal; Tasks: installwebservice
Filename: "sc.exe"; Parameters: "start ZLFileRelay.WebPortal"; StatusMsg: "Starting Web Portal Service..."; Flags: runhidden; Components: webportal; Tasks: installwebservice

; Configure Windows Firewall for Web Portal
Filename: "netsh.exe"; Parameters: "advfirewall firewall add rule name=""ZL File Relay Web Portal (HTTP)"" dir=in action=allow protocol=TCP localport=8080"; StatusMsg: "Configuring firewall..."; Flags: runhidden; Components: webportal; Tasks: configurefirewall
Filename: "netsh.exe"; Parameters: "advfirewall firewall add rule name=""ZL File Relay Web Portal (HTTPS)"" dir=in action=allow protocol=TCP localport=8443"; StatusMsg: "Configuring firewall..."; Flags: runhidden; Components: webportal; Tasks: configurefirewall

; Config Tool launch removed - user can launch manually from shortcuts

[UninstallRun]
; Stop and remove Windows Services
Filename: "sc.exe"; Parameters: "stop ZLFileRelay"; Flags: runhidden; RunOnceId: "StopService"
Filename: "sc.exe"; Parameters: "delete ZLFileRelay"; Flags: runhidden; RunOnceId: "DeleteService"
Filename: "sc.exe"; Parameters: "stop ZLFileRelay.WebPortal"; Flags: runhidden; RunOnceId: "StopWebPortal"
Filename: "sc.exe"; Parameters: "delete ZLFileRelay.WebPortal"; Flags: runhidden; RunOnceId: "DeleteWebPortal"

; Remove firewall rules
Filename: "netsh.exe"; Parameters: "advfirewall firewall delete rule name=""ZL File Relay Web Portal (HTTP)"""; Flags: runhidden; RunOnceId: "RemoveFirewallHTTP"
Filename: "netsh.exe"; Parameters: "advfirewall firewall delete rule name=""ZL File Relay Web Portal (HTTPS)"""; Flags: runhidden; RunOnceId: "RemoveFirewallHTTPS"

[UninstallDelete]
; Clean up log files (optional - user can choose to keep)
Type: filesandordirs; Name: "C:\FileRelay\logs"

[Code]
function StopService(const ServiceName: String): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  // Try to stop the service if it's running
  if Exec('sc.exe', 'stop ' + ServiceName, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    // Service was stopped successfully (or wasn't running)
    Log('Service ' + ServiceName + ' stopped or not running');
  end
  else
  begin
    Log('Could not stop service ' + ServiceName + ' (may not exist)');
  end;
  // Wait a moment for the service to fully stop
  Sleep(1000);
end;

function IsUpgrade(): Boolean;
begin
  // Check if appsettings.json already exists (indicating an upgrade)
  Result := FileExists(ExpandConstant('{commonappdata}\ZLFileRelay\appsettings.json'));
end;

function InitializeSetup(): Boolean;
var
  WindowsVersion: TWindowsVersion;
begin
  Result := True;
  
  // Stop existing services if they exist (for upgrade scenarios)
  StopService('ZLFileRelay');
  StopService('ZLFileRelay.WebPortal');
  
  // Check for 64-bit Windows
  if not IsWin64 then
  begin
    MsgBox('This application requires 64-bit Windows.' + #13#10 + 
           'Please install on a 64-bit version of Windows.', 
           mbError, MB_OK);
    Result := False;
    Exit;
  end;
  
  // Check Windows version (2016+)
  GetWindowsVersionEx(WindowsVersion);
  if (WindowsVersion.Major < 10) then
  begin
    MsgBox('This application requires Windows Server 2016 or later.' + #13#10 +
           'Current version: ' + IntToStr(WindowsVersion.Major) + '.' + IntToStr(WindowsVersion.Minor),
           mbError, MB_OK);
    Result := False;
    Exit;
  end;
  
  // Show self-contained notice
  MsgBox('✅ SELF-CONTAINED INSTALLER' + #13#10 + #13#10 +
         'This installer includes the .NET 8 runtime.' + #13#10 +
         'No additional framework installation required!' + #13#10 + #13#10 +
         '✓ Works on air-gapped networks' + #13#10 +
         '✓ No internet connection needed' + #13#10 +
         '✓ DMZ/OT environment compatible', 
         mbInformation, MB_OK);
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  
  // No IIS check needed - web portal runs as Windows Service!
  // This is perfect for DMZ/OT environments
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Show completion message
    MsgBox('Installation complete!' + #13#10 + #13#10 +
           '✅ All files deployed (includes .NET 8 runtime)' + #13#10 +
           '✅ Windows Services installed' + #13#10 +
           '✅ Directories created' + #13#10 +
           '✅ Firewall configured' + #13#10 + #13#10 +
           'Access Web Portal at:' + #13#10 +
           '  http://localhost:8080 (HTTP)' + #13#10 +
           '  https://localhost:8443 (HTTPS)' + #13#10 +
           '  http://<server-name>:8080 (HTTP)' + #13#10 +
           '  https://<server-name>:8443 (HTTPS)' + #13#10 + #13#10 +
           'Next steps:' + #13#10 +
           '1. Launch Configuration Tool' + #13#10 +
           '2. Configure SSH keys or SMB credentials' + #13#10 +
           '3. (Optional) Configure SSL certificate for HTTPS via ConfigTool → Web Portal tab → Certificate' + #13#10 +
           '4. Test file upload via web portal' + #13#10 +
           '5. Verify file transfer' + #13#10 + #13#10 +
           'Note: SSL certificate import and HttpSys URL reservations are automatically handled by the Config Tool when saving Web Portal settings (requires Administrator privileges).', 
           mbInformation, MB_OK);
  end;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
end;

[Messages]
WelcomeLabel2=This will install [name/ver] on your computer.%n%nThis is a SELF-CONTAINED installation that includes the .NET 8 runtime. No additional framework installation is required.%n%nThis installer is designed for DMZ and OT environments and will work on air-gapped networks.

[CustomMessages]
AppIsRunning=The application is currently running. Please close it before continuing.

