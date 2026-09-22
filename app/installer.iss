; ============================================================
;  경남수학문화관 SW체험 타이머 - 설치 파일 (Inno Setup 6)
;  CI(build-exe.yml) 가 빌드 결과(pkg\GNMC-Timer)를 넣어 GNMC-Timer-Setup.exe 를 만든다.
;  - 관리자 권한 없이 사용자 폴더(%LOCALAPPDATA%\GNMC-Timer) 에 설치
;  - 바탕 화면 바로 가기, Windows 시작 시 자동 실행(선택) 을 설치 중에 체크
; ============================================================

#define AppName "경남수학문화관 타이머"
#define AppExe  "GNMC-Timer.exe"
#ifndef SrcDir
  #define SrcDir "..\pkg\GNMC-Timer"
#endif
#ifndef OutDir
  #define OutDir ".."
#endif
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

[Setup]
AppId={{9C4D6F1E-2B7A-4E8C-9A3D-5F1E2C7B8A90}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=경남수학문화관
AppPublisherURL=https://github.com/i20091119-ai/2026.time
DefaultDirName={localappdata}\GNMC-Timer
DisableDirPage=yes
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir={#OutDir}
OutputBaseFilename=GNMC-Timer-Setup
SetupIconFile=app.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
; 타이머가 켜져 있으면 닫고 설치하도록 안내
AppMutex=GNMC-Timer-SingleInstance
CloseApplications=yes

[Languages]
#if FileExists(AddBackslash(CompilerPath) + "Languages\Korean.isl")
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"
#else
Name: "english"; MessagesFile: "compiler:Default.isl"
#endif

[Tasks]
Name: "desktopicon"; Description: "바탕 화면에 바로 가기 만들기"; GroupDescription: "추가 작업:"
Name: "autostart";   Description: "Windows 시작 시 타이머 자동 실행 (키오스크용)"; GroupDescription: "추가 작업:"

[Files]
Source: "{#SrcDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}";        Filename: "{app}\{#AppExe}"
Name: "{group}\{#AppName} 제거";   Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}";  Filename: "{app}\{#AppExe}"; Tasks: desktopicon
Name: "{userstartup}\{#AppName}";  Filename: "{app}\{#AppExe}"; Tasks: autostart

[Run]
Filename: "{app}\{#AppExe}"; Description: "지금 타이머 실행"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: files;          Name: "{app}\update.log"
Type: files;          Name: "{app}\timer.html.new"
Type: filesandordirs; Name: "{app}\WebView2"
