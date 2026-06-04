@echo off
echo Uninstalling Azure DB Login Helper Startup Application...

set STARTUP_FOLDER=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup
set SHORTCUT_PATH=%STARTUP_FOLDER%\Azure DB Login Helper.lnk

if exist "%SHORTCUT_PATH%" (
    echo Removing startup shortcut...
    del "%SHORTCUT_PATH%"
    if %ERRORLEVEL% neq 0 (
        echo Failed to remove startup shortcut!
        pause
        exit /b 1
    )
    echo Startup shortcut removed successfully!
) else (
    echo No startup shortcut found.
)

echo.
echo Application uninstalled from startup!
echo.
pause
