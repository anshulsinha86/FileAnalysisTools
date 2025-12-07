# File Analysis Tools - Build Script
# This script builds a standalone executable for Windows

Write-Host "╔════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   File Analysis Tools - Build Script      ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
Remove-Item -Path "bin\Publish" -Recurse -ErrorAction SilentlyContinue
Remove-Item -Path "bin\Release" -Recurse -ErrorAction SilentlyContinue
Remove-Item -Path "obj" -Recurse -ErrorAction SilentlyContinue

Write-Host "Clean complete" -ForegroundColor Green
Write-Host ""

# Restore packages
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "Package restore failed!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "Packages restored" -ForegroundColor Green
Write-Host ""

# Build
Write-Host "Building project..." -ForegroundColor Yellow
dotnet build -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "Build successful" -ForegroundColor Green
Write-Host ""

# Publish standalone executable
Write-Host "Publishing standalone executable..." -ForegroundColor Yellow
Write-Host "   Target: Windows x64" -ForegroundColor Gray
Write-Host "   Mode: Self-contained" -ForegroundColor Gray
Write-Host "   Output: Single file" -ForegroundColor Gray
Write-Host ""

dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishReadyToRun=true /p:IncludeNativeLibrariesForSelfExtract=true /p:EnableCompressionInSingleFile=true -o "bin\Publish"

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "BUILD SUCCESSFUL!" -ForegroundColor Green
    Write-Host ""
    
    $exePath = "bin\Publish\FileAnalysisTools.exe"
    
    if (Test-Path $exePath) {
        $size = (Get-Item $exePath).Length / 1MB
        
        Write-Host "Executable location:" -ForegroundColor Cyan
        Write-Host "   $exePath" -ForegroundColor White
        Write-Host ""
        Write-Host "File size: $([math]::Round($size, 2)) MB" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Your standalone application is ready!" -ForegroundColor Green
        Write-Host "No installation required - just copy and run!" -ForegroundColor Gray
        Write-Host ""
        
        # Offer to open the folder
        $openFolder = Read-Host "Would you like to open the output folder? (Y/N)"
        if ($openFolder -eq "Y" -or $openFolder -eq "y") {
            Start-Process explorer.exe -ArgumentList "bin\Publish"
        }
    } else {
        Write-Host "Warning: Executable not found at expected location" -ForegroundColor Yellow
    }
} else {
    Write-Host ""
    Write-Host "BUILD FAILED!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please check the error messages above." -ForegroundColor Yellow
}

Write-Host ""
Read-Host "Press Enter to exit"