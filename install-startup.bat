@echo off
echo Installing Azure DB Login Helper as Startup Application...

REM Build the application
echo Building application...
dotnet build --configuration Release
if %ERRORLEVEL% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)

REM Get the current directory
set CURRENT_DIR=%CD%
set APP_PATH=%CURRENT_DIR%\bin\Release\net8.0-windows\AzureDbLoginHelper.exe

REM Check if the executable exists
if not exist "%APP_PATH%" (
    echo Executable not found at: %APP_PATH%
    pause
    exit /b 1
)

REM Create startup folder shortcut
echo Creating startup application...
set STARTUP_FOLDER=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup
set SHORTCUT_PATH=%STARTUP_FOLDER%\Azure DB Login Helper.lnk

REM Create VBScript to create shortcut
echo Set oWS = WScript.CreateObject("WScript.Shell") > CreateShortcut.vbs
echo sLinkFile = "%SHORTCUT_PATH%" >> CreateShortcut.vbs
echo Set oLink = oWS.CreateShortcut(sLinkFile) >> CreateShortcut.vbs
echo oLink.TargetPath = "%APP_PATH%" >> CreateShortcut.vbs
echo oLink.WorkingDirectory = "%CURRENT_DIR%\bin\Release\net8.0-windows" >> CreateShortcut.vbs
echo oLink.Description = "Azure DB Login Helper" >> CreateShortcut.vbs
echo oLink.Save >> CreateShortcut.vbs

REM Execute VBScript
cscript CreateShortcut.vbs
if %ERRORLEVEL% neq 0 (
    echo Failed to create startup shortcut!
    pause
    exit /b 1
)

REM Clean up VBScript
del CreateShortcut.vbs

echo.
echo Application installed as startup application!
echo Azure DB Login Helper will start automatically when you log in.
echo You should see the tray icon after your next login or restart.
echo.
echo To uninstall, run: uninstall-startup.bat
echo.
pause
