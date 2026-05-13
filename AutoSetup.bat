@echo off
setlocal EnableDelayedExpansion

echo ============================================
echo   VIVE SiteOwl XR - ROBUST AUTO SETUP
echo ============================================
echo.
echo This script finds Git (even if not in PATH),

echo locates your project, checks Unity Hub, and opens everything.
echo.

:: --- STEP 1/5: FIND GIT (Check common paths + PATH) ---
echo [1/5] Locating Git installation...
echo     Checking standard installation directories...

set "GIT_EXE="
:: Check 64-bit standard path
if exist "C:\Program Files\Git\cmd\git.exe" (
    set "GIT_EXE=C:\Program Files\Git\cmd\git.exe"
    echo     [OK] Found at: C:\Program Files\Git\cmd\
)
:: Check 32-bit standard path (fallback)
if not defined GIT_EXE (
    if exist "C:\Program Files (x86)\Git\cmd\git.exe" (
        set "GIT_EXE=C:\Program Files (x86)\Git\cmd\git.exe"
        echo     [OK] Found at: C:\Program Files (x86)\Git\cmd\
    )
)
:: Check user-local install path
if not defined GIT_EXE (
    if exist "%LocalAppData%\Programs\Git\cmd\git.exe" (
        set "GIT_EXE=%LocalAppData%\Programs\Git\cmd\git.exe"
        echo     [OK] Found at: %LocalAppData%\Programs\Git\cmd\
    )
)
:: Check via WHERE command
if not defined GIT_EXE (
    for /f "delims=" %%a in ('where git.exe 2^>nul') do (
        set "GIT_EXE=%%a"
        echo     [OK] Found in PATH: %%a
    )
)

if not defined GIT_EXE (
    echo.
    echo [ERROR] Could not find git.exe!
    echo.
    echo Git IS installed but not in standard locations.
    echo.
    echo PERMANENT FIX:
    echo 1. Reinstall Git: https://git-scm.com/download/win
    echo 2. CHECK this box during install: "Add to PATH"
    echo.
    echo TEMPORARY FIX - Choose one:
    echo A) Copy this .bat file to: C:\Program Files\Git\cmd\
    echo    Then run it from there
    echo B) Run this in Command Prompt first:
    echo    set PATH=%PATH%;C:\Program Files\Git\cmd
    echo    Then run this .bat
    pause
    exit /b 1
)

:: Verify Git works
for /f "tokens=*" %%a in ('"%GIT_EXE%" --version') do (
    echo     [OK] Git version: %%a
)

:: --- STEP 2/5: FIND PROJECT FOLDER ---
echo.
echo [2/5] Locating project files...

set "PROJECT_DIR=%~dp0"
if "%PROJECT_DIR:~-1%"=="\" set "PROJECT_DIR=%PROJECT_DIR:~0,-1%"

if exist "%PROJECT_DIR%\UnityProject\Assets" (
    echo     [OK] Project found at: %PROJECT_DIR%
    set "UNITY_PROJECT_PATH=%PROJECT_DIR%\UnityProject"
) else if exist "%PROJECT_DIR%\VIVE-SiteOwl-XR-Capture\UnityProject\Assets" (
    set "PROJECT_DIR=%PROJECT_DIR%\VIVE-SiteOwl-XR-Capture"
    echo     [OK] Project found at: %PROJECT_DIR%
    set "UNITY_PROJECT_PATH=%PROJECT_DIR%\UnityProject"
) else (
    echo [WARNING] UnityProject not found here: %PROJECT_DIR%
    pause
    exit /b 1
)

:: --- STEP 3/5: UPDATE FROM GITHUB ---
echo.
echo [3/5] Checking for updates...
cd /d "%PROJECT_DIR%"

"%GIT_EXE%" status >nul 2>&1
if %errorlevel% equ 0 (
    "%GIT_EXE%" pull origin main 2>nul
    if %errorlevel% equ 0 (
        echo     [OK] Updated from GitHub
    ) else (
        echo     [INFO] No updates (or ZIP download)
    )
) else (
    echo     [INFO] ZIP download detected (no git repo)
)

:: --- STEP 4/5: FIND UNITY HUB ---
echo.
echo [4/5] Locating Unity Hub...

set "UNITY_HUB="
if exist "C:\Program Files\Unity Hub\Unity Hub.exe" set "UNITY_HUB=C:\Program Files\Unity Hub\Unity Hub.exe"
if not defined UNITY_HUB if exist "%LOCALAPPDATA%\Programs\Unity Hub\Unity Hub.exe" set "UNITY_HUB=%LOCALAPPDATA%\Programs\Unity Hub\Unity Hub.exe"

if not defined UNITY_HUB (
    echo [WARNING] Unity Hub not found!
    echo Download: https://unity.com/download
    pause
    exit /b 1
)

echo     [OK] Found Unity Hub

:: --- STEP 5/5: OPEN PROJECT ---
echo.
echo [5/5] Opening project...
echo.

"%UNITY_HUB%" -- --projectPath "%UNITY_PROJECT_PATH%"

echo.
echo ============================================
echo   ✅ SETUP COMPLETE!
echo ============================================
echo.
echo Unity Hub is launching your project now.
echo.
echo NEXT:
echo 1. Wait for Unity to load (pink bar)
echo 2. Install packages: Window -^> Package Manager
    - XR Plugin Management
    - OpenXR Plugin
    - XR Interaction Toolkit
    - TextMeshPro
echo 3. Tools -^> CCTV Survey -^> Build Working Scene
echo.
pause
