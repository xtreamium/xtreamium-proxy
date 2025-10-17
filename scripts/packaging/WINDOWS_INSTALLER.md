# Windows Installer with Auto-Updates (Velopack)

## Overview

Xtreamium Proxy uses **Velopack** - the modern successor to Squirrel.Windows - to provide automatic updates on Windows.

## Features

- ✅ **Auto-updates from GitHub releases** - Users get updates automatically
- ✅ **Delta updates** - Only downloads changed files (saves bandwidth)
- ✅ **Silent installation** - `Setup.exe --silent`
- ✅ **No admin rights required** - User-level installation
- ✅ **Automatic restart** - App restarts after update installation
- ✅ **Modern & maintained** - Velopack is actively developed

## Building the Installer

### Prerequisites

On Windows:
```powershell
# Velopack CLI is installed automatically by the build script
# Or install globally:
dotnet tool install --global vpk
```

### Local Build

```powershell
# Build with version from .csproj
.\scripts\packaging\build-windows-installer.ps1

# Build with specific version
.\scripts\packaging\build-windows-installer.ps1 -Version "1.3.1"
```

The installer will be created in `publish/packages/XtreamiumProxy-Setup-{version}.exe`

### GitHub Actions

The installer is automatically built when you:
1. Push a tag (e.g., `v1.3.1`)
2. Create a GitHub release

## Installation

### End Users

```powershell
# Standard installation (double-click or run)
XtreamiumProxy-Setup-1.3.1.exe

# Silent installation
XtreamiumProxy-Setup-1.3.1.exe --silent

# Install to custom location
XtreamiumProxy-Setup-1.3.1.exe --installPath "C:\CustomPath"
```

### Install Locations

- **Application**: `%LocalAppData%\XtreamiumProxy\`
- **Executable**: `%LocalAppData%\XtreamiumProxy\current\xtreamium-proxy.exe`
- **User Data**: `%AppData%\XtreamiumProxy\`
- **Logs**: Application logs directory
- **Shortcuts**: Desktop + Start Menu

## How Auto-Updates Work

1. **Application checks for updates** 1 minute after startup
2. **Downloads from GitHub releases** using Velopack's GitHub source
3. **Applies delta updates** (only changed files downloaded)
4. **Restarts application automatically** with new version

### Configuration

The update URL is configured in `appsettings.json`:

```json
{
  "UpdateUrl": "https://github.com/xtreamium/xtreamium-proxy"
}
```

**Note**: Use the repository URL only (not `/releases` path).

### Disable Auto-Updates

To disable automatic updates, set an empty `UpdateUrl`:

```json
{
  "UpdateUrl": ""
}
```

## Uninstallation

```powershell
# Via Windows Settings
# Settings → Apps → Xtreamium Proxy → Uninstall

# Via command line
%LocalAppData%\XtreamiumProxy\Update.exe --uninstall
```

## Release Process

1. **Tag and push version**:
   ```bash
   git tag v1.3.1
   git push origin v1.3.1
   ```

2. **Create GitHub Release**:
   - Go to GitHub → Releases → Draft a new release
   - Choose the tag
   - Publish

3. **Installer is automatically attached**:
   - `XtreamiumProxy-Setup-1.3.1.exe` is built and attached

4. **Users get auto-updates**:
   - Installed applications automatically detect and install updates

## Advanced Configuration

### Update Check Frequency

In `Program.cs`, modify the delay:

```csharp
// Check 5 minutes after startup instead of 1
await Task.Delay(TimeSpan.FromMinutes(5));
```

### Pre-release Updates

To enable beta/pre-release updates, modify `UpdateManager.cs`:

```csharp
var source = new GithubSource(
    repoUrl: _updateUrl,
    accessToken: null,
    prerelease: true  // Enable pre-releases
);
```

### Custom Update Source

You can use custom update sources (not just GitHub):

```csharp
// HTTP source
var source = new SimpleWebSource("https://myserver.com/releases/");

// Local network source
var source = new SimpleWebSource("\\\\server\\updates\\");
```

## Troubleshooting

### Updates not working?

1. Check application logs for update errors
2. Verify GitHub release has the Setup.exe attached
3. Ensure `UpdateUrl` is correct (repo URL, not releases URL)
4. Check internet connectivity

### Build fails?

```powershell
# Clean build
Remove-Item -Recurse -Force publish, tools
dotnet clean
.\scripts\packaging\build-windows-installer.ps1
```

### Installer won't run?

1. Check Windows Defender/antivirus isn't blocking it
2. Try running as administrator
3. Download the certificate if it's a private release

### Need to force reinstall?

1. Uninstall via Windows Settings
2. Delete `%LocalAppData%\XtreamiumProxy\`
3. Delete `%AppData%\XtreamiumProxy\`
4. Reinstall

## Why Velopack?

| Feature | Squirrel (Old) | Velopack (New) |
|---------|----------------|----------------|
| Maintenance | ❌ Deprecated | ✅ Active |
| Performance | Moderate | ✅ Faster |
| Delta Updates | ✅ Yes | ✅ Improved |
| GitHub Integration | ✅ Yes | ✅ Better |
| Cross-platform Build | ❌ Windows only | ✅ Any OS |
| Documentation | ❌ Limited | ✅ Comprehensive |

## Development Notes

- Velopack only initializes on Windows (cross-platform safe)
- Update checks run in background (non-blocking)
- Updates apply on next restart (or immediate with `ApplyUpdatesAndRestart`)
- Delta updates significantly reduce download size
- No admin rights needed for installation or updates

## Resources

- [Velopack Documentation](https://velopack.io/)
- [Velopack GitHub](https://github.com/velopack/velopack)
- [Migration from Squirrel](https://docs.velopack.io/migrating/from-squirrel)
