; Inno Setup script for OpenCV Master.
; Build the app first (self-contained, so target PCs need no .NET runtime):
;   dotnet publish -c Release -r win-x64 --self-contained true -o publish
; then compile this script with ISCC.exe. Output: installer\Output\OpenCVMaster-Setup-<version>.exe

#define AppName "OpenCV Master"
#define AppVersion "1.0.0"
#define AppExe "OpenCVMaster.exe"

[Setup]
AppId={{6E1B7A52-3C4D-4E8F-9A21-0C5D8B7F4A13}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Manggdy
AppPublisherURL=https://github.com/mrkim0118/OpenCVMaster
DefaultDirName={autopf}\OpenCVMaster
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
; installs for all users by default; the user may choose a per-user install without admin rights
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
OutputDir=Output
OutputBaseFilename=OpenCVMaster-Setup-{#AppVersion}
SetupIconFile=..\Assets\app.ico
UninstallDisplayIcon={app}\{#AppExe}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent
