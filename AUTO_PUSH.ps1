# Auto Push Script for VIVE SiteOwl XR Capture
# Run this to quickly commit and push changes

Set-Location $PSScriptRoot

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "  VIVE SiteOwl XR Capture - Auto Push" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Check git status
Write-Host "Checking git status..." -ForegroundColor Yellow
$status = git status --short
if ($status) {
    Write-Host "Modified files:" -ForegroundColor Yellow
    Write-Host $status
} else {
    Write-Host "No changes to commit." -ForegroundColor Green
    exit 0
}

Write-Host ""

# Add all changes
Write-Host "Adding changes..." -ForegroundColor Yellow
git add .

# Commit with timestamp
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$commitMsg = "Auto development update - $timestamp"

Write-Host "Committing: $commitMsg" -ForegroundColor Yellow
git commit -m "$commitMsg"

Write-Host ""

# Push to origin
Write-Host "Pushing to GitHub..." -ForegroundColor Yellow
git push origin main

Write-Host ""
Write-Host "=====================================" -ForegroundColor Green
Write-Host "  Changes pushed successfully!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green

# Show latest log
Write-Host ""
Write-Host "Latest commits:" -ForegroundColor Cyan
git log --oneline -3
