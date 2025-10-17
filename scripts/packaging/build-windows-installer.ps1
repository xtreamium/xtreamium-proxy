#!/usr/bin/env pwsh
param(
    [string]$Version = "",
    [string]$OutputDir = "publish/packages"
)

# Get version from .csproj if not provided
if ([string]::IsNullOrEmpty($Version)) {
    [xml]$csproj = Get-Content "xtreamium-proxy.csproj"
    $Version = $csproj.Project.PropertyGroup.Version
}

# Remove 'v' prefix if present
$Version = $Version -replace '^v', ''

Write-Host "Building Windows installer for version $Version" -ForegroundColor Green

# Ensure output directory exists
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

# Build the application
Write-Host "Building application..." -ForegroundColor Yellow
dotnet publish xtreamium-proxy.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:Version=$Version `
    -p:DefineConstants=WINDOWS `
    -o publish/win-x64

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed!"
    exit 1
}

# Remove development configuration
Remove-Item -Path "publish/win-x64/appsettings.Development.json" -ErrorAction SilentlyContinue

# Install Velopack if not present
if (-not (Test-Path "tools/vpk")) {
    Write-Host "Installing Velopack..." -ForegroundColor Yellow
    dotnet tool install --tool-path tools vpk
}

Write-Host "Creating Velopack installer..." -ForegroundColor Yellow

# Convert PNG to ICO if needed (requires ImageMagick or use online converter)
$iconPath = "scripts/packaging/branding/app-icon.ico"
if (-not (Test-Path $iconPath)) {
    $pngPath = "scripts/packaging/branding/app-icon.png"
    if (Test-Path $pngPath) {
        Write-Host "Converting PNG icon to ICO format..." -ForegroundColor Yellow
        
        # Try to use magick (ImageMagick) if available
        if (Get-Command magick -ErrorAction SilentlyContinue) {
            magick convert $pngPath -define icon:auto-resize=256,128,96,64,48,32,16 $iconPath
        }
        # Try convert command (older ImageMagick)
        elseif (Get-Command convert -ErrorAction SilentlyContinue) {
            convert $pngPath -define icon:auto-resize=256,128,96,64,48,32,16 $iconPath
        }
        else {
            Write-Warning "ImageMagick not found. Please convert app-icon.png to app-icon.ico manually or install ImageMagick."
            Write-Warning "You can use: https://convertio.co/png-ico/ to convert the icon."
            $iconPath = ""
        }
    }
}

# Build installer with Velopack
$vpkArgs = @(
    "pack",
    "--packId", "XtreamiumProxy",
    "--packVersion", $Version,
    "--packDir", "publish/win-x64",
    "--mainExe", "xtreamium-proxy.exe",
    "--outputDir", "publish/releases",
    "--packTitle", "Xtreamium Proxy",
    "--packAuthors", "Xtreamium"
)

# Add icon if available
if ($iconPath -and (Test-Path $iconPath)) {
    $vpkArgs += "--icon"
    $vpkArgs += $iconPath
}

& "tools/vpk" @vpkArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "Velopack pack failed!"
    exit 1
}

# Copy installer to output directory
$installerName = "XtreamiumProxy-Setup-$Version.exe"
$sourceInstaller = Get-ChildItem -Path "publish/releases" -Filter "*Setup.exe" | Select-Object -First 1

if ($sourceInstaller) {
    Copy-Item $sourceInstaller.FullName "$OutputDir/$installerName" -Force
    Write-Host "`n✓ Windows installer created successfully!" -ForegroundColor Green
    Write-Host "  Location: $OutputDir/$installerName" -ForegroundColor Cyan
} else {
    Write-Error "Could not find generated installer in publish/releases"
    exit 1
}

Write-Host "`nInstaller features:" -ForegroundColor Yellow
Write-Host "  - Auto-updates from GitHub releases" -ForegroundColor White
Write-Host "  - Silent installation support" -ForegroundColor White
Write-Host "  - No admin rights required" -ForegroundColor White
Write-Host "  - Delta updates (minimal download size)" -ForegroundColor White
Write-Host "  - Modern, actively maintained (Velopack)" -ForegroundColor White
