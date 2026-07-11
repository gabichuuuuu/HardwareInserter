; HardwareInserter.iss
;
; Builds a single installer (HardwareInserterSetup.exe) for the Solid Edge COM add-in.
; No administrator privileges required: installs to a per-user folder
; (%LocalAppData%\HardwareInserter) and registers the add-in in HKCU, exactly like
; dev/register-addin.ps1 does.
;
; Requires a prior Release x64 build:
;   dotnet build ..\HardwareInserter.slnx -c Release -p:Platform=x64
;
; Compile the installer (with Inno Setup installed, ISCC.exe on PATH):
;   ISCC.exe installer\HardwareInserter.iss
;
; The resulting .exe is written to installer\Output\HardwareInserterSetup.exe: a single file,
; with no external dependencies, valid both for a GitHub Release download and for copying to a
; USB drive and running on another machine with no internet connection.

#define MyAppName "HardwareInserter"
#define MyAppVersion "0.1.0"
#define MyAppPublisher "Gabriel Spoleto"
#define AddInDll "HardwareInserter.AddIn.dll"
#define BuildOutputDir "..\src\HardwareInserter.AddIn\bin\x64\Release\net48"

[Setup]
AppId={{7B7C6B2E-9C7D-4B2E-9E2B-6F5F6F5A1B3B}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\{#MyAppName}
DisableProgramGroupPage=yes
DisableDirPage=yes
DisableReadyPage=yes
DisableWelcomePage=no
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=Output
OutputBaseFilename=HardwareInserterSetup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#AddInDll}
DisableFinishedPage=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[CustomMessages]
english.RegisteringAddIn=Registering the add-in with Solid Edge...
spanish.RegisteringAddIn=Registrando el add-in en Solid Edge...

[Files]
Source: "{#BuildOutputDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

; Registers the add-in in HKCU with the Framework64 RegAsm (same path used by dev/register-addin.ps1).
[Run]
Filename: "{win}\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe"; \
    Parameters: """{app}\{#AddInDll}"" /codebase"; \
    Flags: runhidden waituntilterminated; \
    StatusMsg: "{cm:RegisteringAddIn}"

; Unregisters the add-in before removing the files.
[UninstallRun]
Filename: "{win}\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe"; \
    Parameters: """{app}\{#AddInDll}"" /unregister"; \
    Flags: runhidden waituntilterminated; \
    RunOnceId: "UnregisterHardwareInserter"
