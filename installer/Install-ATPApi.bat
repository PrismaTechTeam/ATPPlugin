@echo off
setlocal EnableExtensions
title ATPApi Service Installer

REM ============================================================================
REM  ATPApi service installer (customer PC)
REM  Double-click this file. It will request Administrator rights, then run
REM  Install-ATPApi.ps1 (which must sit in the SAME folder as this .bat).
REM
REM  By default it downloads the latest ATPApi release from S3 and verifies it.
REM  For an OFFLINE install, put the extracted release files (ATPApi.exe, the
REM  DLLs, appsettings.sample.json, ATPApiUpdater.exe) either next to this .bat
REM  or in a ".\payload" subfolder - the installer auto-detects them.
REM
REM  To preset config on a first install, edit the CONFIG block below.
REM ============================================================================

REM ---- CONFIG (optional - fill these for an unattended first install) --------
set "API_KEY=109izjiwjr14m"
set "DB_SERVER="
set "DB_NAME="
set "DB_USER=sa"
set "DB_PASSWORD="
set "AC_LOGIN_USER=ADMIN"
set "AC_LOGIN_PASSWORD=ADMIN"
set "AUTOCOUNT_PATH="
REM Set CONFIGURE_ONLY=1 to install files + register the service but NOT start it.
REM The installer then prints a checklist of what to set in appsettings.json and how to run it.
set "CONFIGURE_ONLY=0"
REM ---------------------------------------------------------------------------

REM ---- Self-elevate to Administrator if needed -------------------------------
net session >nul 2>&1
if %errorlevel% NEQ 0 (
  echo Requesting administrator privileges...
  powershell -NoProfile -Command "Start-Process -FilePath '%ComSpec%' -ArgumentList '/c','\"%~f0\"' -Verb RunAs"
  goto :eof
)

set "PS1=%~dp0Install-ATPApi.ps1"
if not exist "%PS1%" (
  echo.
  echo ERROR: Install-ATPApi.ps1 was not found next to this file:
  echo   %PS1%
  echo Keep the .bat and the .ps1 together in the same folder.
  echo.
  pause
  goto :eof
)

REM ---- Build optional arguments from the CONFIG block ------------------------
set "ARGS="
if not "%API_KEY%"==""           set "ARGS=%ARGS% -ApiKey "%API_KEY%""
if not "%DB_SERVER%"==""          set "ARGS=%ARGS% -DbServer "%DB_SERVER%""
if not "%DB_NAME%"==""            set "ARGS=%ARGS% -Database "%DB_NAME%""
if not "%DB_USER%"==""            set "ARGS=%ARGS% -SqlUser "%DB_USER%""
if not "%DB_PASSWORD%"==""        set "ARGS=%ARGS% -SqlPassword "%DB_PASSWORD%""
if not "%AC_LOGIN_USER%"==""      set "ARGS=%ARGS% -LoginUser "%AC_LOGIN_USER%""
if not "%AC_LOGIN_PASSWORD%"==""  set "ARGS=%ARGS% -LoginPassword "%AC_LOGIN_PASSWORD%""
if not "%AUTOCOUNT_PATH%"==""     set "ARGS=%ARGS% -AutoCountPath "%AUTOCOUNT_PATH%""
if "%CONFIGURE_ONLY%"=="1"        set "ARGS=%ARGS% -ConfigureOnly"

echo.
echo Running installer...
powershell -NoProfile -ExecutionPolicy Bypass -File "%PS1%" %ARGS%
set "RC=%errorlevel%"

echo.
echo ===============================================
if "%RC%"=="0" (
  echo  Install finished successfully.
) else (
  echo  Install finished with warnings/errors ^(code %RC%^).
  echo  Read the messages above for the next step.
)
echo ===============================================
pause
endlocal
