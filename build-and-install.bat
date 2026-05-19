@echo off
REM ============================================================
REM  ATP Plugin: Build -> Package -> Install (one-click)
REM ============================================================
REM  1) MSBuild the plugin csproj  -> bin\Debug\<DLL>
REM  2) AppBuilderCmd packs .appp  -> <OUTPUT>.app
REM  3) Launches the .app via file association so AutoCount's
REM     Plug-in Manager opens the install dialog.
REM ============================================================

setlocal ENABLEDELAYEDEXPANSION
cd /d "%~dp0"

echo.
echo  Reminder: if you changed PLUGIN_CONFIG.md, ask Claude "regenerate appp"
echo            BEFORE running this script, otherwise the .appp will be stale.
echo.

REM ---- CONFIG (edit these once the csproj/appp exist) ---------
set "CSPROJ=ServiceContractPhotocopier\ServiceContractPhotocopier.csproj"
set "APPP=ServiceContractPhotocopier\ServiceContractPhotocopier.appp"
set "OUTPUT_APP=ServiceContractPhotocopier\ServiceContractPhotocopier.app"
set "CONFIG=Debug"
set "PLATFORM=AnyCPU"

REM ---- TOOLS --------------------------------------------------
set "MSBUILD=C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"
set "APPBUILDERCMD=C:\Program Files (x86)\AutoCount\Development\AppBuilder 2.1\AppBuilderCmd.exe"

if not exist "%MSBUILD%"        echo [ERROR] MSBuild not found        && exit /b 1
if not exist "%APPBUILDERCMD%"  echo [ERROR] AppBuilderCmd not found  && exit /b 1
if not exist "%CSPROJ%"         echo [ERROR] csproj not found         && exit /b 1
if not exist "%APPP%"           echo [ERROR] appp not found           && exit /b 1

echo.
echo === [1/3] Building %CSPROJ% (%CONFIG%^|%PLATFORM%) ===
"%MSBUILD%" "%CSPROJ%" /t:Rebuild /p:Configuration=%CONFIG% /p:Platform=%PLATFORM% /v:minimal /nologo
if errorlevel 1 ( echo [ERROR] MSBuild failed. & exit /b 1 )

echo.
echo === [2/3] Packaging %APPP% -^> %OUTPUT_APP% ===
"%APPBUILDERCMD%" "%~dp0%APPP%" "%~dp0%OUTPUT_APP%"
if errorlevel 1 ( echo [ERROR] AppBuilderCmd failed. & exit /b 1 )

echo.
echo === [3/4] Launching installer for %OUTPUT_APP% ===
start "" "%OUTPUT_APP%"
echo Approve the install dialog in AutoCount Plug-in Manager, then press any key
echo to continue (this script will then restart AutoCount and auto-login).
pause >nul

echo.
echo === [4/4] Restart AutoCount with auto-login ===
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\auto-login.ps1"

echo.
echo Done.
endlocal
