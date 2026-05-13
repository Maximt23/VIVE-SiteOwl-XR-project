@echo off
setlocal EnableDelayedExpansion

echo ============================================
echo   SYNC WITH WORK COMPUTER
echo ============================================
echo.
echo This will get the latest camera data from
echo your work colleague's CSV export.
echo.

:: Find project
echo [1/4] Finding your project...
set PROJECT_PATH=

if exist "%USERPROFILE%\Documents\VIVE-SiteOwl-XR-Capture" (
    set PROJECT_PATH=%USERPROFILE%\Documents\VIVE-SiteOwl-XR-Capture
) else if exist "%USERPROFILE%\Documents\VIVE-SiteOwl-XR" (
    set PROJECT_PATH=%USERPROFILE%\Documents\VIVE-SiteOwl-XR
) else (
    echo [INFO] Searching for project...
    for /f "delims=" %%a in ('dir "%USERPROFILE%\Documents" /s /b /c:0 "README.md" 2^>nul ^| findstr "VIVE.*SiteOwl"') do (
        for %%F in ("%%a") do set PROJECT_PATH=%%~dpF
    )
)

if not defined PROJECT_PATH (
    echo [ERROR] Cannot find project!
    echo.
    echo Please run this script from inside your project folder.
    pause
    exit /b 1
)

echo [OK] Project found: %PROJECT_PATH%
cd /d "%PROJECT_PATH%"

echo.
echo [2/4] Fetching latest data from GitHub...
echo.

:: Check git
git --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Git not installed!
    echo Please install Git first.
    pause
    exit /b 1
)

:: Stash any local changes (to avoid conflicts)
git stash

:: Pull latest
git pull origin main

if %errorlevel% neq 0 (
    echo.
    echo [WARNING] Git pull had issues.
    echo This might be because:
    echo   - No internet connection
    echo   - You have local changes
    echo   - Work hasn't pushed yet
    echo.
    echo Trying to resolve...
    echo.
    
    git fetch origin
    git reset --hard origin/main
    
    if %errorlevel% neq 0 (
        echo [ERROR] Could not sync!
        pause
        exit /b 1
    )
)

echo.
echo [3/4] Checking for data files...
echo.

set DATA_FOUND=0

if exist "Data\Input\cameras.csv" (
    echo ✓ Data/Input/cameras.csv - FOUND
    for %%F in ("Data\Input\cameras.csv") do (
        echo   Size: %%~zF bytes
        echo   Modified: %%~tF
    )
    set DATA_FOUND=1
) else (
    echo ✗ Data/Input/cameras.csv - NOT FOUND
)

if exist "UnityProject\Assets\StreamingAssets\cameras.csv" (
    echo ✓ Unity/StreamingAssets/cameras.csv - FOUND
    for %%F in ("UnityProject\Assets\StreamingAssets\cameras.csv") do (
        echo   Size: %%~zF bytes
    )
) else (
    echo ✗ Unity/StreamingAssets/cameras.csv - NOT FOUND
)

echo.
echo [4/4] Syncing to Unity StreamingAssets...
echo.

:: Copy to Unity if exists in Data
if exist "Data\Input\cameras.csv" (
    if not exist "UnityProject\Assets\StreamingAssets" (
        mkdir "UnityProject\Assets\StreamingAssets"
    )
    
    copy /Y "Data\Input\cameras.csv" "UnityProject\Assets\StreamingAssets\cameras.csv"
    echo ✓ Copied to Unity StreamingAssets
) else (
    echo [INFO] No cameras.csv to copy (work hasn't pushed it yet?)
)

echo.
echo ============================================
echo   ✅ SYNC COMPLETE!
echo ============================================
echo.

if %DATA_FOUND% equ 1 (
    echo READY TO BUILD:
echo   1. Open Unity
echo   2. Check that cameras.csv is in StreamingAssets
echo   3. Build Working Scene
echo   4. Deploy to headset
echo.
) else (
    echo [NOTICE] No camera data yet.
echo.
echo Ask your work colleague to:
echo   1. Export CSV from SiteOwl
echo   2. Put it in: Data/Input/cameras.csv
echo   3. Run: git add Data/Input/cameras.csv
echo   4. Run: git commit -m "Add site data"
echo   5. Run: git push origin main
echo.
echo Then run this script again!
echo.
)

choice /C YN /M "Open Unity now"
if %errorlevel% equ 1 (
    where Unity >nul 2>&1
    if %errorlevel% equ 0 (
        start Unity.exe -projectPath "%PROJECT_PATH%\UnityProject"
    ) else (
        start "" "%PROGRAMFILES%\Unity\Hub\Editor\2022.3*\Editor\Unity.exe" -projectPath "%PROJECT_PATH%\UnityProject"
    )
)

echo.
pause
