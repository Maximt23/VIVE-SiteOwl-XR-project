@echo off
setlocal

echo ============================================
echo   VIVE SiteOwl XR Capture - Build Helper
echo ============================================
echo.

REM ── Paths ────────────────────────────────────────────────────────────────────
set SCRIPT_DIR=%~dp0
set PROJECT_PATH=%SCRIPT_DIR%UnityProject
set BUILD_PATH=%SCRIPT_DIR%Builds
set STREAMING_ASSETS=%SCRIPT_DIR%Assets\StreamingAssets\Data
set PYTHON_SCRIPTS=%SCRIPT_DIR%Scripts
set APK_NAME=SiteOwl_XR_Capture.apk

REM ── Step 1: Python — build device_recognition_library.json ───────────────────
echo [Step 1/3] Building device recognition library...
echo.

REM Use uv if available, else fall back to python
where uv >nul 2>nul
if %errorlevel% equ 0 (
    uv run "%PYTHON_SCRIPTS%\build_device_library.py" --out-dir "%STREAMING_ASSETS%"
) else (
    where python >nul 2>nul
    if %errorlevel% neq 0 (
        echo [ERROR] Neither 'uv' nor 'python' found in PATH.
        echo Install Python or uv and retry.
        pause
        exit /b 1
    )
    python "%PYTHON_SCRIPTS%\build_device_library.py" --out-dir "%STREAMING_ASSETS%"
)

if %errorlevel% neq 0 (
    echo [ERROR] Python build step failed. Check CSV paths.
    pause
    exit /b 1
)

echo.
echo [OK] device_recognition_library.json written to StreamingAssets\Data\
echo.

REM ── Step 2: Copy camera_model_library.json if it exists ──────────────────────
set CAM_LIB=C:\VIVE-SiteOwl-XR-Designs\Meta data\data\camera_model_library.json
if exist "%CAM_LIB%" (
    echo [Step 2/3] Copying camera_model_library.json to StreamingAssets...
    copy /Y "%CAM_LIB%" "%STREAMING_ASSETS%\camera_model_library.json" >nul
    echo [OK] camera_model_library.json updated.
) else (
    echo [Step 2/3] camera_model_library.json not found at expected path, skipping copy.
    echo            Expected: %CAM_LIB%
)
echo.

REM ── Step 3: Unity Android build ──────────────────────────────────────────────
echo [Step 3/3] Building Android APK...
echo.

where unity >nul 2>nul
if %errorlevel% neq 0 (
    echo [WARNING] Unity not found in PATH. Skipping Unity build.
    echo           Open the project in Unity Hub and build manually.
    echo           All assets are up to date in StreamingAssets\.
    pause
    exit /b 0
)

if not exist "%BUILD_PATH%" (
    mkdir "%BUILD_PATH%"
    echo Created build directory.
)

unity -batchmode -quit ^
      -projectPath "%PROJECT_PATH%" ^
      -buildTarget Android ^
      -executeMethod BuildScript.BuildAndroid ^
      -logFile "%BUILD_PATH%\build.log"

if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Unity build failed. Check %BUILD_PATH%\build.log
    pause
    exit /b 1
)

echo.
echo ============================================
echo   Build Complete!
echo ============================================
echo.
echo APK: %BUILD_PATH%\%APK_NAME%
echo.
echo To install on headset:
echo   1. Connect VIVE XR Elite via USB
echo   2. adb install -r "%BUILD_PATH%\%APK_NAME%"
echo.
pause
