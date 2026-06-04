@echo off
cd /d "%~dp0\.."
echo Installing Azure DB Login Helper as Startup Application...

echo Building application...
dotnet build --configuration Release -f net8.0-windows
if %ERRORLEVEL% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)

set CURRENT_DIR=%CD%
set APP_PATH=%CURRENT_DIR%\bin\Release\net8.0-windows\AzureDbLoginHelper.exe

if not exist "%APP_PATH%" (
    echo Executable not found at: %APP_PATH%
    pause
    exit /b 1
)

echo Creating startup application...
set STARTUP_FOLDER=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup
set SHORTCUT_PATH=%STARTUP_FOLDER%\Azure DB Login Helper.lnk

echo Set oWS = WScript.CreateObject("WScript.Shell") > CreateShortcut.vbs
echo sLinkFile = "%SHORTCUT_PATH%" >> CreateShortcut.vbs
echo Set oLink = oWS.CreateShortcut(sLinkFile) >> CreateShortcut.vbs
echo oLink.TargetPath = "%APP_PATH%" >> CreateShortcut.vbs
echo oLink.WorkingDirectory = "%CURRENT_DIR%\bin\Release\net8.0-windows" >> CreateShortcut.vbs
echo oLink.Description = "Azure DB Login Helper" >> CreateShortcut.vbs
echo oLink.Save >> CreateShortcut.vbs

cscript CreateShortcut.vbs
if %ERRORLEVEL% neq 0 (
    echo Failed to create startup shortcut!
    del CreateShortcut.vbs 2>nul
    pause
    exit /b 1
)

del CreateShortcut.vbs

echo.
echo Application installed as startup application!
echo Azure DB Login Helper will start automatically when you log in.
echo.
echo To uninstall, run: scripts\uninstall-startup.bat
echo.
pause
