@echo off
setlocal EnableDelayedExpansion

echo ============================================
echo   VIVE SiteOwl XR - Automated Setup
echo ============================================
echo.
echo This will help set up everything automatically
echo.

:: Check if running as admin (not needed, but helpful to know)
net session >nul 2>&1
if %errorLevel% == 0 (
    echo [OK] Running with administrator privileges
) else (
    echo [INFO] Running without admin (normal - this is fine)
)

echo.
echo Step 1/5: Checking Git installation...
echo ----------------------------------------

where git >nul 2>&1
if %errorlevel% neq 0 (
    echo [MISSING] Git not found!
    echo.
    echo Would you like me to open the Git download page?
    choice /C YN /M "Open browser to download Git"
    if !errorlevel! equ 1 (
        start https://git-scm.com/download/win
        echo.
        echo Please:
        echo 1. Download Git from the page that opened
        echo 2. Install it (accept all defaults)
        echo 3. Restart this script
        echo.
        pause
        exit /b 1
    ) else (
        echo You need Git installed. Cannot continue.
        pause
        exit /b 1
    )
) else (
    for /f "tokens=*" %%a in ('git --version') do set GIT_VERSION=%%a
    echo [OK] Found: !GIT_VERSION!
)

echo.
echo Step 2/5: Cloning repository...
echo ----------------------------------------

set REPO_URL=https://github.com/Maximt23/VIVE-SiteOwl-XR-project.git
set PROJECT_DIR=%USERPROFILE%\Documents\VIVE-SiteOwl-XR

if exist "%PROJECT_DIR%" (
    echo [INFO] Project folder already exists at:
    echo %PROJECT_DIR%
    echo.
    choice /C YN /M "Delete and re-clone (Y) or use existing (N)"
    if !errorlevel! equ 1 (
        echo Removing existing folder...
        rmdir /S /Q "%PROJECT_DIR%"
    ) else (
        echo Using existing folder.
        goto :skip_clone
    )
)

echo Cloning from GitHub...
echo (This may take 1-2 minutes)
git clone %REPO_URL% "%PROJECT_DIR%"

if %errorlevel% neq 0 (
    echo [ERROR] Failed to clone repository!
    echo.
    echo Possible issues:
    echo - No internet connection
    echo - GitHub is blocked
    echo - Repository URL changed
    echo.
    pause
    exit /b 1
)

echo [OK] Repository cloned successfully!

:skip_clone
echo.
echo Step 3/5: Checking Unity Hub...
echo ----------------------------------------

set UNITY_HUB_PATH=%ProgramFiles%\Unity Hub\Unity Hub.exe
set UNITY_HUB_PATH_2=%LOCALAPPDATA%\Programs\Unity Hub\Unity Hub.exe

if exist "%UNITY_HUB_PATH%" (
    echo [OK] Unity Hub found: %UNITY_HUB_PATH%
    set UNITY_HUB=%UNITY_HUB_PATH%
) else if exist "%UNITY_HUB_PATH_2%" (
    echo [OK] Unity Hub found: %UNITY_HUB_PATH_2%
    set UNITY_HUB=%UNITY_HUB_PATH_2%
) else (
    echo [MISSING] Unity Hub not found!
    echo.
    echo You need to:
    echo 1. Download Unity Hub from: https://unity.com/download
    echo 2. Install it
    echo 3. Run Unity Hub and sign in
    echo 4. Run this script again
    echo.
    choice /C YN /M "Open Unity download page now"
    if !errorlevel! equ 1 (
        start https://unity.com/download
    )
    pause
    exit /b 1
)

echo.
echo Step 4/5: Adding project to Unity Hub...
echo ----------------------------------------

echo Opening Unity Hub and adding project...
echo (Unity Hub should open automatically)

"%UNITY_HUB%" -- --projectPath "%PROJECT_DIR%\UnityProject"

echo.
echo [OK] Project added to Unity Hub!

echo.
echo Step 5/5: Setup Summary
echo ----------------------------------------
echo.
echo ✓ Git installed: %GIT_VERSION%
echo ✓ Project cloned to: %PROJECT_DIR%
echo ✓ Unity Hub opened with project
echo.
echo NEXT STEPS:
echo ----------------------------------------
echo 1. Unity Hub should be open now
echo 2. Look for "VIVE-SiteOwl-XR" in the projects list
echo 3. If Unity version is missing, click "Install Unity 2022.3 LTS"
echo 4. Click on the project name to open it
echo 5. Once Unity opens, go to: Tools -^> CCTV Survey -^> Build Working Scene
echo.
echo Need help? Check SETUP_FOR_BEGINNERS.md
echo.

choice /C YN /M "Open Unity Hub now (should already be open)"
if !errorlevel! equ 1 (
    start "" "%UNITY_HUB%"
)

echo.
echo Setup complete! 🎉
echo.
pause
