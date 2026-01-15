# publish-windows.ps1
# Builds and publishes Phone Mirror for Windows

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputPath = "$PSScriptRoot/../publish/windows"
)

$ErrorActionPreference = "Stop"
$ProjectPath = "$PSScriptRoot/../src/PhoneMirror/PhoneMirror.csproj"

Write-Host "Publishing Phone Mirror for Windows..."
Write-Host "Configuration: $Configuration"
Write-Host "Runtime: $Runtime"
Write-Host ""

# Clean output directory
if (Test-Path $OutputPath) {
    Remove-Item -Recurse -Force $OutputPath
}
New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null

# Publish
dotnet publish $ProjectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -o $OutputPath

if ($LASTEXITCODE -ne 0) {
    Write-Error "Publish failed!"
    exit 1
}

Write-Host ""
Write-Host "Publish complete!"
Write-Host "Output: $OutputPath"
Write-Host ""

# List output files
Get-ChildItem $OutputPath | Format-Table Name, Length
