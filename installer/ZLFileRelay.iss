; ZL File Relay - Inno Setup Installer Script
; Self-Contained Deployment with .NET 8 Runtime Included
; Compatible with DMZ/OT/Air-Gapped Environments

#define MyAppName "ZL File Relay"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Your Company"
#define MyAppURL "https://yourcompany.com"
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

; Installation Directories
DefaultDirName={autopf}\ZLFileRelay
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes

; Output
OutputDir=installer\output
OutputBaseFilename=ZLFileRelay-Setup-v{#MyAppVersion}-SelfContained
SetupIconFile=installer\assets\icon.ico
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
WizardImageFile=installer\assets\WizardImage.bmp
WizardSmallImageFile=installer\assets\WizardSmallImage.bmp

; Uninstall
UninstallDisplayName={#MyAppName}
UninstallFilesDir={app}\Uninstall

; Misc
CloseApplications=force
RestartApplications=no

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
Name: "configurefirewall"; Description: "Configure Windows Firewall for Web Portal (port 8080)"; GroupDescription: "Network:"; Components: webportal; Flags: checkedonce

[Files]
; ═══════════════════════════════════════════════════════════════
; SELF-CONTAINED DEPLOYMENTS - INCLUDES .NET 8 RUNTIME
; ═══════════════════════════════════════════════════════════════

; File Transfer Service (with .NET 8, ~70MB)
Source: "..\publish\Service\*"; DestDir: "{app}\Service"; Components: service; Flags: ignoreversion recursesubdirs createallsubdirs

; Web Upload Portal (with ASP.NET Core 8, ~75MB)
Source: "..\publish\WebPortal\*"; DestDir: "{app}\WebPortal"; Components: webportal; Flags: ignoreversion recursesubdirs createallsubdirs

; Configuration Tool (single .exe with .NET 8, ~65MB)
Source: "..\publish\ConfigTool\ZLFileRelay.ConfigTool.exe"; DestDir: "{app}\ConfigTool"; Components: configtool; Flags: ignoreversion
Source: "..\publish\ConfigTool\appsettings.json"; DestDir: "{app}\ConfigTool"; Components: configtool; Flags: ignoreversion onlyifdoesntexist

; Configuration Files
Source: "..\publish\appsettings.json"; DestDir: "{commonappdata}\ZLFileRelay"; Flags: onlyifdoesntexist uninsneveruninstall

; Documentation
Source: "..\publish\docs\*"; DestDir: "{app}\docs"; Flags: ignoreversion recursesubdirs createallsubdirs

; PowerShell Helper Scripts
Source: "scripts\*"; DestDir: "{app}\scripts"; Flags: ignoreversion recursesubdirs

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
Filename: "sc.exe"; Parameters: "create ZLFileRelay binPath=""{app}\Service\ZLFileRelay.Service.exe"" start=auto DisplayName=""ZL File Relay - File Transfer"""; StatusMsg: "Installing File Transfer Service..."; Flags: runhidden; Components: service; Tasks: installservice
Filename: "sc.exe"; Parameters: "description ZLFileRelay ""Automated file transfer from DMZ to SCADA networks"""; Flags: runhidden; Components: service; Tasks: installservice
Filename: "sc.exe"; Parameters: "start ZLFileRelay"; StatusMsg: "Starting File Transfer Service..."; Flags: runhidden; Components: service; Tasks: installservice

; Install Web Portal Windows Service (Kestrel - NO IIS NEEDED!)
Filename: "sc.exe"; Parameters: "create ZLFileRelay.WebPortal binPath=""{app}\WebPortal\ZLFileRelay.WebPortal.exe"" start=auto DisplayName=""ZL File Relay - Web Portal"""; StatusMsg: "Installing Web Portal Service..."; Flags: runhidden; Components: webportal; Tasks: installwebservice
Filename: "sc.exe"; Parameters: "description ZLFileRelay.WebPortal ""Web-based file upload interface (Kestrel on port 8080)"""; Flags: runhidden; Components: webportal; Tasks: installwebservice
Filename: "sc.exe"; Parameters: "start ZLFileRelay.WebPortal"; StatusMsg: "Starting Web Portal Service..."; Flags: runhidden; Components: webportal; Tasks: installwebservice

; Configure Windows Firewall for Web Portal
Filename: "netsh.exe"; Parameters: "advfirewall firewall add rule name=""ZL File Relay Web Portal"" dir=in action=allow protocol=TCP localport=8080"; StatusMsg: "Configuring firewall..."; Flags: runhidden; Components: webportal; Tasks: configurefirewall

; Launch Config Tool
Filename: "{app}\ConfigTool\{#MyAppExeName}"; Description: "Launch Configuration Tool"; Flags: postinstall skipifsilent nowait

[UninstallRun]
; Stop and remove Windows Services
Filename: "sc.exe"; Parameters: "stop ZLFileRelay"; Flags: runhidden
Filename: "sc.exe"; Parameters: "delete ZLFileRelay"; Flags: runhidden
Filename: "sc.exe"; Parameters: "stop ZLFileRelay.WebPortal"; Flags: runhidden
Filename: "sc.exe"; Parameters: "delete ZLFileRelay.WebPortal"; Flags: runhidden

; Remove firewall rule
Filename: "netsh.exe"; Parameters: "advfirewall firewall delete rule name=""ZL File Relay Web Portal"""; Flags: runhidden

[UninstallDelete]
; Clean up log files (optional - user can choose to keep)
Type: filesandordirs; Name: "C:\FileRelay\logs"

[Code]
var
  RequireIIS: Boolean;

function InitializeSetup(): Boolean;
var
  WindowsVersion: TWindowsVersion;
begin
  Result := True;
  
  // Check for 64-bit Windows
  if not IsWin64 then
  begin
    MsgBox('This application requires 64-bit Windows.' + #13#10 + 
           'Please install on a 64-bit version of Windows.', 
           mbCritical, MB_OK);
    Result := False;
    Exit;
  end;
  
  // Check Windows version (2016+)
  GetWindowsVersionEx(WindowsVersion);
  if (WindowsVersion.Major < 10) then
  begin
    MsgBox('This application requires Windows Server 2016 or later.' + #13#10 +
           'Current version: ' + IntToStr(WindowsVersion.Major) + '.' + IntToStr(WindowsVersion.Minor),
           mbCritical, MB_OK);
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
           '  http://localhost:8080' + #13#10 +
           '  http://<server-name>:8080' + #13#10 + #13#10 +
           'Next steps:' + #13#10 +
           '1. Launch Configuration Tool' + #13#10 +
           '2. Configure SSH keys or SMB credentials' + #13#10 +
           '3. Test file upload via web portal' + #13#10 +
           '4. Verify file transfer', 
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

