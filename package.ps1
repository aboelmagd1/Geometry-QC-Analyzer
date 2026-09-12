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
} else {
    # Check Debug folder if Release wasn't default
    $dbgPackage = "$PSScriptRoot\bin\x64\Debug\win-x64\GeometryQCAddIn.esriAddinX"
    if (Test-Path $dbgPackage) {
        Write-Host " [SUCCESS] Add-In Package created at: $dbgPackage" -ForegroundColor Yellow
    }
}
