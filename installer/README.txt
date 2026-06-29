ATPApi — Service Installer (customer package)
=============================================

WHAT THIS IS
  ATPApi is the webhook/API Windows Service that receives Stock Issue / Stock
  Transfer requests from PUMS and feeds them into AutoCount. These scripts
  install / uninstall that service. No .NET SDK or source code is required.

PREREQUISITES on the target PC
  1. AutoCount Accounting 2.x installed (the service loads AutoCount at runtime).
  2. SQL Server reachable with the account-book credentials.
  3. Internet access (for the default "download latest from S3" mode) — OR ship
     the release files alongside these scripts for an offline install.

----------------------------------------------------------------------------
INSTALL
----------------------------------------------------------------------------
  1. (Optional) Open Install-ATPApi.bat in Notepad and fill the CONFIG block:
        API_KEY            the X-API-Key PUMS sends (already pre-filled)
        DB_SERVER          e.g. localhost,1433  or  SERVERPC\SQLEXPRESS
        DB_NAME            the AutoCount account-book database
        DB_USER / DB_PASSWORD
        AC_LOGIN_USER / AC_LOGIN_PASSWORD   AutoCount login (e.g. ADMIN / ADMIN)
        AUTOCOUNT_PATH     only if AutoCount is NOT in the default Program Files

  2. Right-click Install-ATPApi.bat -> "Run as administrator"
     (or just double-click - it will ask for admin rights).

  3. It installs to C:\Program Files\ATPApi and registers the "ATPApi" service,
     but DOES NOT start it. It then prints a checklist of what to configure.

  4. CONFIGURE FIRST: open and complete
        C:\Program Files\ATPApi\appsettings.json
     (ApiKey, Server, Database, SqlUser, SqlPassword, LoginUser, LoginPassword;
      AutoCountInstallPath only if AutoCount is in a non-default folder).

  5. THEN start the service (PowerShell as Administrator):
        Start-Service ATPApi
        Invoke-RestMethod http://localhost:5007/api/ping -Headers @{ 'X-Api-Key' = 'YOUR_KEY' }
     Restart after any later appsettings.json change:  Restart-Service ATPApi

  (Advanced: set START=1 in the .bat to start automatically right after install -
   only do that when appsettings.json is already fully configured.)

OFFLINE INSTALL (no internet)
  Put the extracted release files (ATPApi.exe, all DLLs, appsettings.sample.json,
  ATPApiUpdater.exe) in THIS folder (or a "payload" subfolder), then run the .bat.
  The installer uses the local files instead of downloading.

----------------------------------------------------------------------------
UPDATE (after first install)
----------------------------------------------------------------------------
  Normally from inside AutoCount: plugin menu -> "About / Check for Updates".
  Re-running Install-ATPApi.bat also updates and NEVER overwrites your
  appsettings.json.

----------------------------------------------------------------------------
UNINSTALL
----------------------------------------------------------------------------
  Run Uninstall-ATPApi.bat as administrator. The install folder (config + logs)
  is kept by default; set PURGE=1 in the .bat to delete it too.

----------------------------------------------------------------------------
TROUBLESHOOTING
----------------------------------------------------------------------------
  - Service won't start: check C:\Program Files\ATPApi\appsettings.json
    (DB reachable? ApiKey set? no <SET_ME> left?).
  - Logs: C:\Program Files\ATPApi\logs\  and Event Viewer > Windows Logs > Application.
  - Wrong AutoCount folder: set "AutoCountInstallPath" in appsettings.json.
