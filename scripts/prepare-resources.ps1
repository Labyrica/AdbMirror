# prepare-resources.ps1
# Downloads and prepares platform-tools and scrcpy for embedding

param(
    [string]$ResourcesPath = "$PSScriptRoot/../src/PhoneMirror.Core/Resources"
)

$ErrorActionPreference = "Stop"

# Ensure Resources directory exists
New-Item -ItemType Directory -Force -Path $ResourcesPath | Out-Null

Write-Host "Resource preparation script"
Write-Host "==========================="
Write-Host ""
Write-Host "This script prepares platform-tools and scrcpy for embedding."
Write-Host "You need to manually download the files first:"
Write-Host ""
Write-Host "1. platform-tools:"
Write-Host "   - Windows: https://dl.google.com/android/repository/platform-tools-latest-windows.zip"
Write-Host "   - macOS:   https://dl.google.com/android/repository/platform-tools-latest-darwin.zip"
Write-Host "   - Linux:   https://dl.google.com/android/repository/platform-tools-latest-linux.zip"
Write-Host ""
Write-Host "2. scrcpy:"
Write-Host "   - Visit: https://github.com/Genymobile/scrcpy/releases/latest"
Write-Host "   - Download appropriate version for your platform"
Write-Host ""

# Check for existing extracted folders and create ZIPs
$platformToolsDir = "$ResourcesPath/platform-tools"
$scrcpyDir = "$ResourcesPath/scrcpy"

if (Test-Path $platformToolsDir) {
    Write-Host "Creating platform-tools.zip from $platformToolsDir..."
    $zipPath = "$ResourcesPath/platform-tools.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Compress-Archive -Path "$platformToolsDir/*" -DestinationPath $zipPath
    Write-Host "Created: platform-tools.zip"
} else {
    Write-Host "WARNING: platform-tools folder not found at $platformToolsDir"
    Write-Host "         Extract platform-tools there before running this script."
}

if (Test-Path $scrcpyDir) {
    Write-Host "Creating scrcpy.zip from $scrcpyDir..."
    $zipPath = "$ResourcesPath/scrcpy.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Compress-Archive -Path "$scrcpyDir/*" -DestinationPath $zipPath
    Write-Host "Created: scrcpy.zip"
} else {
    Write-Host "WARNING: scrcpy folder not found at $scrcpyDir"
    Write-Host "         Extract scrcpy there before running this script."
}

Write-Host ""
Write-Host "Done. Run 'dotnet build' to embed resources."
