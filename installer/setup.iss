; Inno Setup script for Urban Dry Clean CRM.
; Built by .github/workflows/build.yml (ISCC.exe, installed via chocolatey on windows-latest)
; after `dotnet publish` + scripts/prepare-backend.cjs have populated publish/win-x64/ with the
; self-contained app AND the bundled backend (publish/win-x64/backend/...). This script just
; packages that whole folder - see README.md's "Building" section for the full pipeline.
;
; Locally: run `iscc installer\setup.iss` from the repo root once publish/win-x64/ exists
; (Inno Setup - https://jrsoftware.org/isinfo.php - installed, ISCC.exe on PATH or full path).

#define MyAppName "Urban Dry Clean CRM"
#define MyAppVersion "0.1.0"
#define MyAppPublisher "Urban Dry Clean"
#define MyAppExeName "Udc.App.exe"
#define MyPublishDir "..\publish\win-x64"

[Setup]
AppId={{236D2F16-9E0F-44C9-8282-299A5B2C6093}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir=Output
OutputBaseFilename=UrbanDryCleanCRM-Setup
Compression=lzma2
SolidCompression=yes
; The published app is win-x64 self-contained - no point offering a 32-bit install.
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes
; Default to a per-user install (no admin prompt) - a shop-counter PC may only have a standard
; account - but let an admin choose an all-users install instead if they run it elevated.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Files]
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
