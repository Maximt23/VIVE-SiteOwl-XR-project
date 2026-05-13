@echo off
setlocal
cd /d "%~dp0"

echo =====================================
echo   VIVE SiteOwl XR Capture - Auto Push
echo =====================================
echo.

echo Checking git status...
git status --short
if %errorlevel% neq 0 (
    echo Not a git repository!
    pause
    exit /b 1
)

echo.
echo Adding changes...
git add .

echo.
echo Committing...
set msg=Auto development update - %date% %time%
git commit -m "%msg%"
if %errorlevel% neq 0 (
    echo Nothing to commit or commit failed.
    pause
    exit /b 0
)

echo.
echo Pushing to GitHub...
git push origin main
if %errorlevel% neq 0 (
    echo Push failed!
    pause
    exit /b 1
)

echo.
echo =====================================
echo   Changes pushed successfully!
echo =====================================
echo.
echo Latest commits:
git log --oneline -3

echo.
pause
