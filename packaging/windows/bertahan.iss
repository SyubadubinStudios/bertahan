; Installer Windows untuk Bertahan (Inno Setup 6).
; Biasanya dipanggil oleh packaging/release.ps1 atau release.sh:
;   iscc /DAppVersion=1.0.0 /DSourceDir=<hasil publish win-x64> /DOutputDir=<dist> packaging\windows\bertahan.iss

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\..\dist\stage\win-x64"
#endif
#ifndef OutputDir
  #define OutputDir "..\..\dist"
#endif

[Setup]
AppId={{8C1F4E2A-5B7D-4E61-9A3C-B3A7E1D2F0B9}
AppName=Bertahan
AppVersion={#AppVersion}
AppVerName=Bertahan {#AppVersion}
AppPublisher=Subadubin Studios
AppCopyright=Subadubin Studios
DefaultDirName={autopf}\Bertahan
DefaultGroupName=Bertahan
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir={#OutputDir}
OutputBaseFilename=Bertahan-{#AppVersion}-windows-x64-setup
SetupIconFile=..\icons\bertahan.ico
UninstallDisplayIcon={app}\Bertahan.exe
LicenseFile=..\..\LICENSE
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern

[Tasks]
Name: "desktopicon"; Description: "Buat ikon di Desktop"; GroupDescription: "Ikon tambahan:"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Bertahan"; Filename: "{app}\Bertahan.exe"
Name: "{group}\Hapus Bertahan"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Bertahan"; Filename: "{app}\Bertahan.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Bertahan.exe"; Description: "Mainkan Bertahan sekarang"; Flags: nowait postinstall skipifsilent
