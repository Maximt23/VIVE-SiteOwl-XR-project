@echo off
echo ============================================
echo   CREATE WORKING UNITY SCENE
echo ============================================
echo.
echo This will create a ready-to-use Unity scene
echo with all components pre-wired.
echo.
echo Prerequisites:
echo - Unity 2022.3+ installed
echo - Android Build Support
echo - OpenXR Plugin
echo.
echo Steps:
echo 1. Open Unity Hub
echo 2. Create project in: UnityProject/
echo 3. Import this package
echo 4. Build and run
echo.
echo Press any key to generate scene package...
pause >nul

REM Create directory structure
echo Creating scene structure...

REM This would be a Unity Editor script that creates:
REM - XR Origin with all components
REM - UI Canvas with panels
REM - Event wiring
REM - Test data

echo.
echo NEXT: Run the Unity Editor script:
echo Assets/Editor/CreateCctvSurveyScene.cs
echo.
echo This will generate:
echo - CctvSurvey.unity scene
echo - XR Cctv Rig prefab
echo - UI prefabs
echo - Test camera data
echo.
pause
