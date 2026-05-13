@echo off
setlocal

echo ============================================
echo   VIVE SiteOwl XR Capture - Build Helper
echo ============================================
echo.

REM Check if Unity is installed
where unity >nul 2>nul
if %errorlevel% neq 0 (
    echo [ERROR] Unity not found in PATH.
    echo Please ensure Unity is installed and in your PATH.
    pause
    exit /b 1
)

REM Set paths
set PROJECT_PATH=%~dp0UnityProject
set BUILD_PATH=%~dp0Builds
set APK_NAME=SiteOwl_XR_Capture.apk

echo Project: %PROJECT_PATH%
echo Build: %BUILD_PATH%
echo APK: %APK_NAME%
echo.

REM Create build directory if needed
if not exist "%BUILD_PATH%" (
    mkdir "%BUILD_PATH%"
    echo Created build directory.
)

echo.
echo Starting Android build...
echo This may take several minutes...
echo.

REM Build the APK (headless mode)
unity -batchmode -quit -projectPath "%PROJECT_PATH%" -buildTarget Android -executeMethod BuildScript.BuildAndroid -logFile "%BUILD_PATH%\build.log"

if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Build failed! Check build.log for details.
    pause
    exit /b 1
)

echo.
echo ============================================
echo   Build Complete!
echo ============================================
echo.
echo APK location: %BUILD_PATH%\%APK_NAME%
echo.
echo To install on headset:
echo   1. Connect VIVE XR Elite via USB
echo   2. Run: adb install -r "%BUILD_PATH%\%APK_NAME%"
echo   3. Or use: Build and Run in Unity
echo.
pause
