@echo off
setlocal
echo =============================================
echo   SiteOwl XR - Transfer Survey CSV to VIVE
echo =============================================
echo.

REM Check adb
where adb >nul 2>nul
if %errorlevel% neq 0 (
    echo [ERROR] adb not found. Install Android SDK Platform Tools.
    echo Download: https://developer.android.com/studio/releases/platform-tools
    pause
    exit /b 1
)

REM Detect connected device
adb devices | findstr /i "device$" >nul 2>nul
if %errorlevel% neq 0 (
    echo [ERROR] No VIVE headset detected via USB.
    echo Make sure: USB Debugging is ON, cable is data-capable.
    pause
    exit /b 1
)

set APP_DATA=/sdcard/Android/data/com.siteowl.xr.capture/files

REM ── PUSH: Send CSV to headset ───────────────────────────────────────────────
if "%1"=="" (
    echo Usage: TRANSFER_DATA.bat [push^|pull] [optional: csv_file_path]
    echo.
    echo Examples:
    echo   TRANSFER_DATA.bat push "Store_1234_CCTV.csv"
    echo   TRANSFER_DATA.bat pull
    pause
    exit /b 0
)

if /i "%1"=="push" (
    if "%2"=="" (
        echo [ERROR] Provide path to CSV file: TRANSFER_DATA.bat push "path\to\file.csv"
        pause
        exit /b 1
    )
    echo Pushing "%2" to headset...
    adb shell mkdir -p %APP_DATA%
    adb push "%~2" "%APP_DATA%/"
    if %errorlevel% equ 0 (
        echo [OK] CSV transferred. Launch the app - it will auto-load the file.
    ) else (
        echo [ERROR] Transfer failed. Check cable and USB debugging.
    )
)

REM ── PULL: Get captured data back ────────────────────────────────────────────
if /i "%1"=="pull" (
    set OUTPUT=.\CapturedData_%date:~10,4%%date:~4,2%%date:~7,2%
    mkdir "%OUTPUT%" 2>nul
    echo Pulling captured data to %OUTPUT%...
    adb pull "%APP_DATA%/" "%OUTPUT%\"
    if %errorlevel% equ 0 (
        echo [OK] Data pulled to: %OUTPUT%
        echo Contents:
        dir /b "%OUTPUT%"
    ) else (
        echo [ERROR] Pull failed.
    )
)

echo.
pause
