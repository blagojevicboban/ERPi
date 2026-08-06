# Script for building and packaging ERPi using Velopack (vpk) for 64-bit and 32-bit architectures
$ErrorActionPreference = "Stop"

Write-Host "================================================="
Write-Host "ERPi -- Build and Packaging (Velopack 32/64-bit)"
Write-Host "================================================="

$version = (Get-Content "version.txt").Trim()
Write-Host "Target Version: $version"

# Clean previous release packages
if (Test-Path "ReleasePackage") {
    Remove-Item -Recurse -Force "ReleasePackage"
}
New-Item -ItemType Directory -Path "ReleasePackage" -Force | Out-Null

# ---------------------------------------------------------
# 1. BUILD & PACKAGE 64-BIT (win-x64)
# ---------------------------------------------------------
Write-Host "`n1. Building 64-bit (win-x64) self-contained binaries..."
if (Test-Path "publish_output_x64") { Remove-Item -Recurse -Force "publish_output_x64" }

dotnet publish ERPiApp/ERPiApp.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:Version=$version -o publish_output_x64

if (Test-Path "ERPiApp\Resources\Help") {
    New-Item -ItemType Directory -Path "publish_output_x64\Resources\Help" -Force | Out-Null
    Copy-Item -Path "ERPiApp\Resources\Help\*" -Destination "publish_output_x64\Resources\Help" -Recurse -Force
}

Write-Host "2. Packaging 64-bit with Velopack..."
vpk pack --packId "ERPi" --channel "win-x64" --packVersion "$version" --packDir "publish_output_x64" --mainExe "ERPiApp.exe" --outputDir "ReleasePackage" --packTitle "ERPi Poslovni Sistem" --packAuthors "Blagojevic Boban" --icon "ERPiApp\app.ico"

# ---------------------------------------------------------
# 2. BUILD & PACKAGE 32-BIT (win-x86)
# ---------------------------------------------------------
Write-Host "`n3. Building 32-bit (win-x86) self-contained binaries..."
if (Test-Path "publish_output_x86") { Remove-Item -Recurse -Force "publish_output_x86" }

dotnet publish ERPiApp/ERPiApp.csproj -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:Version=$version -o publish_output_x86

if (Test-Path "ERPiApp\Resources\Help") {
    New-Item -ItemType Directory -Path "publish_output_x86\Resources\Help" -Force | Out-Null
    Copy-Item -Path "ERPiApp\Resources\Help\*" -Destination "publish_output_x86\Resources\Help" -Recurse -Force
}

Write-Host "4. Packaging 32-bit with Velopack..."
vpk pack --packId "ERPi32" --channel "win-x86" --packVersion "$version" --packDir "publish_output_x86" --mainExe "ERPiApp.exe" --outputDir "ReleasePackage" --packTitle "ERPi Poslovni Sistem (32-bit)" --packAuthors "Blagojevic Boban" --icon "ERPiApp\app.ico"

Write-Host "================================================="
Write-Host "SUCCESS! Velopack packages created in ReleasePackage\"
Write-Host "64-bit Installer: ReleasePackage\ERPi-$version-win-x64-Setup.exe"
Write-Host "32-bit Installer: ReleasePackage\ERPi32-$version-win-x86-Setup.exe"
Write-Host "================================================="
