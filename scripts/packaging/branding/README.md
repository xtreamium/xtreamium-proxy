# Converting PNG to ICO for Windows Installer

## Quick Online Conversion

1. Go to https://convertio.co/png-ico/
2. Upload `app-icon.png`
3. Download as `app-icon.ico`
4. Place in `scripts/packaging/branding/app-icon.ico`

## Using ImageMagick (Command Line)

### Install ImageMagick

**Windows:**
```powershell
winget install ImageMagick.ImageMagick
# or
choco install imagemagick
```

**Linux/macOS:**
```bash
# Ubuntu/Debian
sudo apt install imagemagick

# macOS
brew install imagemagick
```

### Convert the Icon

```bash
cd scripts/packaging/branding

# Create ICO with multiple sizes
magick convert app-icon.png -define icon:auto-resize=256,128,96,64,48,32,16 app-icon.ico

# Or using older ImageMagick syntax
convert app-icon.png -define icon:auto-resize=256,128,96,64,48,32,16 app-icon.ico
```

## Notes

- ICO files should contain multiple sizes (16x16, 32x32, 48x48, 64x64, 96x96, 128x128, 256x256)
- The build script will automatically use the ICO file if it exists
- If ICO doesn't exist, it will attempt to convert PNG to ICO using ImageMagick
- The installer will use a default icon if neither ICO file exists nor ImageMagick is available
