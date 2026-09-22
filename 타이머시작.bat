@echo off
chcp 65001 >nul
REM ============================================
REM  경남수학문화관 학교체험 SW프로그램 타이머
REM  더블클릭 → 업데이트 확인 → Edge 전체화면 실행
REM  실제 동작은 같은 폴더의 launcher.ps1 이 담당합니다.
REM ============================================
if not exist "%~dp0launcher.ps1" (
    echo [오류] launcher.ps1 파일을 찾을 수 없습니다.
    echo 이 bat 파일과 launcher.ps1, timer.html 을 같은 폴더에 두세요.
    pause
    exit /b
)
powershell -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File "%~dp0launcher.ps1"
exit
