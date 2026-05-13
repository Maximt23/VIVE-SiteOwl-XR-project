@echo off
setlocal EnableDelayedExpansion

echo ============================================
echo   FILE ORGANIZER FOR VIVE SITEOWL XR
echo ============================================
echo.
echo This will help you find and organize your files
echo.

:: Try to find the project
set FOUND=0
set PROJECT_PATH=

:: Check common locations
echo [1/5] Searching for project in common locations...

if exist "%USERPROFILE%\Documents\VIVE-SiteOwl-XR-Capture" (
    set PROJECT_PATH=%USERPROFILE%\Documents\VIVE-SiteOwl-XR-Capture
    set FOUND=1
    echo [FOUND] Project at: Documents\VIVE-SiteOwl-XR-Capture
) else if exist "%USERPROFILE%\Documents\VIVE-SiteOwl-XR" (
    set PROJECT_PATH=%USERPROFILE%\Documents\VIVE-SiteOwl-XR
    set FOUND=1
    echo [FOUND] Project at: Documents\VIVE-SiteOwl-XR
) else if exist "%USERPROFILE%\Unity Projects\VIVE-SiteOwl-XR" (
    set PROJECT_PATH=%USERPROFILE%\Unity Projects\VIVE-SiteOwl-XR
    set FOUND=1
    echo [FOUND] Project at: Unity Projects\VIVE-SiteOwl-XR
) else if exist "%USERPROFILE%\Downloads\VIVE-SiteOwl-XR-Capture" (
    set PROJECT_PATH=%USERPROFILE%\Downloads\VIVE-SiteOwl-XR-Capture
    set FOUND=1
    echo [FOUND] Project at: Downloads\VIVE-SiteOwl-XR-Capture
) else (
    echo [NOT FOUND] Project not in common locations
    echo.
    echo Let's search your entire Documents folder...
    echo (This may take 30-60 seconds)
    echo.
    
    :: Search for key file
    for /f "delims=" %%a in ('dir "%USERPROFILE%\Documents" /s /b /c:0 "CctvCaptureController.cs" 2^>nul ^| findstr "CctvCaptureController"') do (
        if !FOUND! equ 0 (
            for %%F in ("%%a") do set PROJECT_PATH=%%~dpF
            for %%F in (!PROJECT_PATH!) do set PROJECT_PATH=%%~dpF
            set FOUND=1
            echo [FOUND] Project via file search!
        )
    )
)

if !FOUND! equ 0 (
    echo.
    echo [ERROR] Could not find your project automatically.
    echo.
    echo Please tell me where you saved it, or:
    echo 1. Open File Explorer
    echo 2. Navigate to where you saved the project
    echo 3. Click in the address bar
    echo 4. Copy the path
    echo 5. Paste it here:
    echo.
    set /p MANUAL_PATH="Enter full path: "
    
    if exist "!MANUAL_PATH!\UnityProject" (
        set PROJECT_PATH=!MANUAL_PATH!
        set FOUND=1
        echo [OK] Using provided path
    ) else if exist "!MANUAL_PATH!\Assets" (
        set PROJECT_PATH=!MANUAL_PATH!
        set FOUND=1
        echo [OK] Using provided path
    )
)

if !FOUND! equ 0 (
    echo.
    echo [FATAL] Could not find project. 
    echo Please run AutoSetup.bat first to clone the project.
    pause
    exit /b 1
)

echo.
echo [2/5] Project found at:
echo !PROJECT_PATH!
echo.

:: Show file statistics
echo [3/5] Analyzing project structure...
cd /d "!PROJECT_PATH!"

set FILE_COUNT=0
for /f %%a in ('dir /s /b *.cs 2^>nul ^| find /c /v ""') do set FILE_COUNT=%%a

set SCENE_COUNT=0
for /f %%a in ('dir /s /b *.unity 2^>nul ^| find /c /v ""') do set SCENE_COUNT=%%a

set SCRIPT_FOLDER_COUNT=0
for /f %%a in ('dir /s /b /ad "Scripts" 2^>nul ^| find /c /v ""') do set SCRIPT_FOLDER_COUNT=%%a

echo [STATS] Found:
echo         - %FILE_COUNT% C# script files
echo         - %SCENE_COUNT% Unity scenes
echo         - %SCRIPT_FOLDER_COUNT% Scripts folders
echo.

:: Check if organized
echo [4/5] Checking organization...

if exist "!PROJECT_PATH!\UnityProject\Assets\Scripts\00_Models" (
    echo [OK] Files already organized with numbered prefixes!
    set ORGANIZED=1
) else if exist "!PROJECT_PATH!\UnityProject\Assets\Scripts\Models" (
    echo [OK] Files organized in standard structure
    set ORGANIZED=1
) else (
    echo [INFO] Files not in numbered folders
    echo [INFO] (This is OK - code still works!)
    set ORGANIZED=0
)

:: Create helpful shortcuts
echo.
echo [5/5] Creating helpful shortcuts...

:: Create a "GO HERE" shortcut on desktop
echo Set oWS = WScript.CreateObject("WScript.Shell") > "%TEMP%\CreateShortcut.vbs"
echo sLinkFile = "%USERPROFILE%\Desktop\VIVE SiteOwl XR Project.lnk" >> "%TEMP%\CreateShortcut.vbs"
echo Set oLink = oWS.CreateShortcut(sLinkFile) >> "%TEMP%\CreateShortcut.vbs"
echo oLink.TargetPath = "!PROJECT_PATH!" >> "%TEMP%\CreateShortcut.vbs"
echo oLink.Description = "VIVE SiteOwl XR Capture Project" >> "%TEMP%\CreateShortcut.vbs"
echo oLink.IconLocation = "%SystemRoot%\System32\SHELL32.dll,4" >> "%TEMP%\CreateShortcut.vbs"
echo oLink.Save >> "%TEMP%\CreateShortcut.vbs"
cscript /nologo "%TEMP%\CreateShortcut.vbs"
if exist "%TEMP%\CreateShortcut.vbs" del "%TEMP%\CreateShortcut.vbs"

echo [OK] Created desktop shortcut!
echo.

:: Create File Organization Report
echo Creating organization report...
set REPORT_FILE=!PROJECT_PATH!\FILE_REPORT.txt
(
echo ============================================
echo   FILE ORGANIZATION REPORT
echo   Generated: %date% %time%
echo ============================================
echo.
echo PROJECT LOCATION:
echo !PROJECT_PATH!
echo.
echo FOLDER STRUCTURE:
echo.
dir /s /b /ad 2^>nul | findstr /i "Scripts\|Models\|Events\|Managers\|UI\|AI\|Controller"
echo.
echo C# SCRIPT FILES:
echo.
if exist "!PROJECT_PATH!\UnityProject\Assets\Scripts" (
    dir /s /b "!PROJECT_PATH!\UnityProject\Assets\Scripts\*.cs" 2^>nul
) else (
    dir /s /b "!PROJECT_PATH!\*.cs" 2^>nul | findstr /i /v "Library\|Temp\|obj\"
)
echo.
echo KEY FILES:
echo.
echo Controller (The Brain):
if exist "*CctvCaptureController.cs" (
    echo   ✓ CctvCaptureController.cs - FOUND
) else (
    echo   ✗ CctvCaptureController.cs - NOT FOUND
)

echo Event System:
if exist "*CctvSurveyEvents.cs" (
    echo   ✓ CctvSurveyEvents.cs - FOUND
) else (
    echo   ✗ CctvSurveyEvents.cs - NOT FOUND
)

echo GPS Manager:
if exist "*RealGpsManager.cs" (
    echo   ✓ RealGpsManager.cs - FOUND
) else (
    echo   ✗ RealGpsManager.cs - NOT FOUND
)

echo CSV Manager:
if exist "*SiteOwlCsvManager.cs" (
    echo   ✓ SiteOwlCsvManager.cs - FOUND
) else (
    echo   ✗ SiteOwlCsvManager.cs - NOT FOUND
)

echo.
echo SCENES:
echo.
if exist "!PROJECT_PATH!\UnityProject\Assets\Scenes\*.unity" (
    for %%a in ("!PROJECT_PATH!\UnityProject\Assets\Scenes\*.unity") do echo   ✓ %%~na.unity
) else (
    echo   (No scenes found in standard location)
)
echo.
echo DATA FILES:
echo.
if exist "!PROJECT_PATH!\Data\Input\cameras.csv" (
    echo   ✓ cameras.csv - INPUT FILE FOUND
    for %%F in ("!PROJECT_PATH!\Data\Input\cameras.csv") do (
        echo     Size: %%~zF bytes
    )
) else (
    echo   ✗ cameras.csv - INPUT FILE NOT FOUND (Work needs to push this!)
)

echo.
echo ============================================
echo   WHAT TO DO NEXT:
echo ============================================
echo.
echo 1. Open Unity Hub
echo 2. Click "Open Project"
echo 3. Select this folder: 
echo    !PROJECT_PATH!\UnityProject
echo.
echo 4. Once Unity opens:
echo    - Go to: Tools ^> CCTV Survey ^> Build Working Scene
echo    - OR: Open existing scene in Assets/Scenes/
echo.
echo 5. Press Play to test in Editor
echo 6. Build APK for headset deployment
echo.
echo ============================================
) > "!REPORT_FILE!"

echo [OK] Report saved to: !REPORT_FILE!
echo.

:: Final Summary
echo ============================================
echo   ✅ ORGANIZATION COMPLETE!
echo ============================================
echo.
echo YOUR PROJECT:
echo   Location: !PROJECT_PATH!
echo   Scripts:  %FILE_COUNT% files
echo   Scenes:   %SCENE_COUNT% scenes
echo.
echo QUICK ACCESS:
echo   - Desktop shortcut created!
echo   - Double-click "VIVE SiteOwl XR Project" on desktop
echo.
echo NEXT STEPS:
echo   1. Open Unity Hub
echo   2. Add project from: !PROJECT_PATH!\UnityProject
echo   3. Open the scene
echo   4. Press Play to test
echo.
echo View detailed report:
echo   !REPORT_FILE!
echo.
echo Need help? Open SETUP_FOR_BEGINNERS.md
echo.

choice /C YN /M "Open Unity Hub now"
if %errorlevel% equ 1 (
    where Unity Hub >nul 2>&1
    if %errorlevel% equ 0 (
        start "Unity Hub" "Unity Hub.exe"
    ) else (
        echo Unity Hub not found in PATH
echo Please open it manually from Start menu
    )
)

echo.
pause
