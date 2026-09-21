<#
.SYNOPSIS
    Builds the ArcGIS Pro Geometry QC Add-In Package (.esriAddinX) only without installing.
#>

$ErrorActionPreference = "Stop"

Write-Host "=== Building Geometry QC Add-In Package ===" -ForegroundColor Cyan
dotnet build "$PSScriptRoot\GeometryQCAddIn.csproj" -c Release

$outputPackage = "$PSScriptRoot\bin\Release\win-x64\GeometryQCAddIn.esriAddinX"

if (Test-Path $outputPackage) {
    Write-Host ""
    Write-Host "============================================================" -ForegroundColor Green
    Write-Host " [SUCCESS] Add-In Package created successfully at:" -ForegroundColor Green
    Write-Host " $outputPackage" -ForegroundColor Yellow
    Write-Host "============================================================" -ForegroundColor Green

    # Clear stale AssemblyCache to ensure ArcGIS Pro loads the newest build
    $cacheDir = Join-Path $env:LOCALAPPDATA "ESRI\ArcGISPro\AssemblyCache\{8a7f921d-44a3-4b92-95f2-953e5e6080dc}"
    if (Test-Path $cacheDir) {
        Remove-Item $cacheDir -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host " [CLEARED] Cleared stale ArcGIS Pro AssemblyCache." -ForegroundColor Cyan
    }
} else {
    # Check Debug folder if Release wasn't default
    $dbgPackage = "$PSScriptRoot\bin\x64\Debug\win-x64\GeometryQCAddIn.esriAddinX"
    if (Test-Path $dbgPackage) {
        Write-Host " [SUCCESS] Add-In Package created at: $dbgPackage" -ForegroundColor Yellow
    }
}
