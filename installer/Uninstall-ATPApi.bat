@echo off
setlocal EnableExtensions
title ATPApi Service Uninstaller

REM ============================================================================
REM  ATPApi service uninstaller (customer PC)
REM  Double-click to remove the ATPApi Windows Service.
REM  By default the install folder (appsettings.json + logs) is kept.
REM  To also delete C:\Program Files\ATPApi, set PURGE=1 below.
REM ============================================================================

set "PURGE=0"

net session >nul 2>&1
if %errorlevel% NEQ 0 (
  echo Requesting administrator privileges...
  powershell -NoProfile -Command "Start-Process -FilePath '%ComSpec%' -ArgumentList '/c','\"%~f0\"' -Verb RunAs"
  goto :eof
)

set "PS1=%~dp0Uninstall-ATPApi.ps1"
if not exist "%PS1%" (
  echo.
  echo ERROR: Uninstall-ATPApi.ps1 not found next to this file:
  echo   %PS1%
  echo.
  pause
  goto :eof
)

set "ARGS="
if "%PURGE%"=="1" set "ARGS=-Purge"

powershell -NoProfile -ExecutionPolicy Bypass -File "%PS1%" %ARGS%

echo.
pause
endlocal
