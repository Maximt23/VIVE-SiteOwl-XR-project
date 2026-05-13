@echo off
REM ============================================================
REM  run_fetch_images.bat
REM  Double-click this from Windows Explorer to download
REM  reference images for every camera model in the library.
REM  Runs through Walmart sysproxy (NTLM auth auto-handled).
REM ============================================================
title Camera Image Library Fetcher

cd /d "%~dp0"

echo.
echo  ============================================================
echo   CAMERA IMAGE LIBRARY FETCHER
echo   Downloading reference images for AI recognition...
echo  ============================================================
echo.

REM -- Activate the project venv
call "%~dp0.venv\Scripts\activate.bat" 2>nul
IF ERRORLEVEL 1 (
    echo  [ERROR] Virtual environment not found at .venv\
    echo  Run this first:
    echo    uv venv
    echo    uv pip install requests requests-ntlm duckduckgo-search
    pause
    exit /b 1
)

REM -- Set Walmart proxy so requests can route through it
set HTTPS_PROXY=http://sysproxy.wal-mart.com:8080
set HTTP_PROXY=http://sysproxy.wal-mart.com:8080
set NO_PROXY=localhost,127.0.0.1

REM -- Run the fetcher
python -u Scripts\fetch_camera_images.py

echo.
echo  Done! Check C:\VIVE-SiteOwl-XR-Designs\Meta data\data\camera_images\
echo.
pause
