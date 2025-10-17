# Xtreamium Proxy - Packaging & Distribution

This document provides an overview of all packaging and distribution methods for Xtreamium Proxy.

## Supported Platforms

### Windows
- **Velopack Installer** - Modern auto-updating installer
- **Features**: Auto-updates, silent installation, no admin required
- **Format**: `.exe`
- **Documentation**: [WINDOWS_INSTALLER.md](WINDOWS_INSTALLER.md)

### Linux

#### Debian/Ubuntu
- **Format**: `.deb` package
- **Install**: `sudo dpkg -i xtreamium-proxy_*.deb`
- **Features**: systemd service, auto-start on boot

#### RHEL/Fedora/CentOS
- **Format**: `.rpm` package
- **Install**: `sudo dnf install xtreamium-proxy-*.rpm`
- **Features**: systemd service, auto-start on boot

#### Arch Linux
- **Format**: `.pkg.tar.zst` package
- **Install**: `sudo pacman -U xtreamium-proxy-*.pkg.tar.zst`
- **Features**: systemd service, auto-start on boot

#### Generic Linux
- **Format**: `.tar.gz` archive
- **Install**: Manual installation with install script
- **Features**: User or system-wide installation options

## Quick Start

### For Users

#### Windows
1. Download `XtreamiumProxy-Setup-{version}.exe` from releases
2. Run the installer
3. Application starts automatically and checks for updates

#### Debian/Ubuntu
```bash
wget https://github.com/xtreamium/xtreamium-proxy/releases/download/v1.3.1/xtreamium-proxy_1.3.1_amd64.deb
sudo dpkg -i xtreamium-proxy_1.3.1_amd64.deb
sudo systemctl enable --now xtreamium-proxy
```

#### RHEL/Fedora/CentOS
```bash
wget https://github.com/xtreamium/xtreamium-proxy/releases/download/v1.3.1/xtreamium-proxy-1.3.1-1.x86_64.rpm
sudo dnf install xtreamium-proxy-1.3.1-1.x86_64.rpm
sudo systemctl enable --now xtreamium-proxy
```

#### Arch Linux
```bash
wget https://github.com/xtreamium/xtreamium-proxy/releases/download/v1.3.1/xtreamium-proxy-1.3.1-1-x86_64.pkg.tar.zst
sudo pacman -U xtreamium-proxy-1.3.1-1-x86_64.pkg.tar.zst
sudo systemctl enable --now xtreamium-proxy
```

### For Developers

#### Build All Packages Locally

**Windows Installer:**
```powershell
.\scripts\packaging\build-windows-installer.ps1
```

**Linux Packages:**
```bash
./scripts/build.sh  # Build application first
./scripts/packaging/build-packages.sh  # Build all Linux packages
```

#### Build via GitHub Actions

1. Tag a version: `git tag v1.3.1 && git push origin v1.3.1`
2. Create a GitHub release
3. All packages are automatically built and attached

## Package Contents

### Windows (Velopack)
- Application binary
- Auto-update support
- Configuration files
- Desktop shortcuts

### Linux (DEB/RPM/Arch)
- Application binary in `/opt/xtreamium-proxy/`
- systemd service unit
- Configuration in `/etc/xtreamium-proxy/`
- Data directory in `/var/lib/xtreamium-proxy/`
- Logs in `/var/log/xtreamium-proxy/`

## Configuration

### Windows
Configuration file location:
- `%AppData%\XtreamiumProxy\appsettings.json`

### Linux
Configuration file location:
- `/etc/xtreamium-proxy/appsettings.json`

### Common Settings

```json
{
  "App": {
    "VideoPlayer": {
      "Executable": "/usr/bin/mpv",
      "DefaultArguments": "--no-border --ontop {{URL}}"
    },
    "Recordings": {
      "Path": "/path/to/recordings",
      "MinDurationMinutes": 1,
      "MaxDurationMinutes": 600
    }
  },
  "UpdateUrl": "https://github.com/xtreamium/xtreamium-proxy"
}
```

## Auto-Updates

### Windows
- **Method**: Velopack
- **Frequency**: Checks 1 minute after startup
- **Type**: Delta updates (only changed files)
- **Action**: Automatic download and restart

### Linux
Updates via package manager:
```bash
# Debian/Ubuntu
sudo apt update && sudo apt upgrade xtreamium-proxy

# RHEL/Fedora
sudo dnf update xtreamium-proxy

# Arch Linux
sudo pacman -Syu xtreamium-proxy
```

## Service Management

### Windows
```powershell
# Application runs automatically
# Check logs in %AppData%\XtreamiumProxy\logs\
```

### Linux
```bash
# Start service
sudo systemctl start xtreamium-proxy

# Stop service
sudo systemctl stop xtreamium-proxy

# Restart service
sudo systemctl restart xtreamium-proxy

# Check status
sudo systemctl status xtreamium-proxy

# View logs
sudo journalctl -u xtreamium-proxy -f
```

## Uninstallation

### Windows
```powershell
# Via Windows Settings
Settings → Apps → Xtreamium Proxy → Uninstall

# Or via command line
%LocalAppData%\XtreamiumProxy\Update.exe --uninstall
```

### Linux
```bash
# Debian/Ubuntu
sudo apt remove xtreamium-proxy

# RHEL/Fedora
sudo dnf remove xtreamium-proxy

# Arch Linux
sudo pacman -R xtreamium-proxy
```

## Troubleshooting

### Windows
- **Logs**: `%AppData%\XtreamiumProxy\logs\`
- **Updates failing**: Check internet connection and GitHub releases
- **Won't start**: Check Windows Event Viewer

### Linux
- **Logs**: `sudo journalctl -u xtreamium-proxy`
- **Service won't start**: Check configuration and permissions
- **Dependencies missing**: Install ffmpeg and optionally mpv

## Build Requirements

### Windows
- .NET 9 SDK
- PowerShell 7+
- Velopack CLI (auto-installed)

### Linux
- .NET 9 SDK
- nfpm (for package building)
- bash

## Documentation

- [Windows Installer](WINDOWS_INSTALLER.md) - Velopack auto-updating installer
- [Linux Packages](README.md) - nfpm DEB/RPM/Arch packages
- [GitHub Actions](.github/workflows/build-preview.yaml) - CI/CD pipeline

## Release Process

1. **Update version** in `xtreamium-proxy.csproj`
2. **Commit changes**: `git commit -am "Release v1.3.1"`
3. **Tag release**: `git tag v1.3.1`
4. **Push**: `git push origin develop --tags`
5. **Create GitHub Release** from the tag
6. **Packages are automatically built** and attached to the release

## Support

- **Issues**: https://github.com/xtreamium/xtreamium-proxy/issues
- **Documentation**: https://github.com/xtreamium/xtreamium-proxy
