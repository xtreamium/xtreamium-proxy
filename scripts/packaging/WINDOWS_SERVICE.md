# Windows Service Configuration

## How It Works

The Velopack installer now automatically configures Xtreamium Proxy as a Windows Service:

### On Installation (First Run)
- Creates a Windows Service named "XtreamiumProxy"
- Configures it to start automatically on boot
- Starts the service immediately

### Service Details
- **Service Name**: `XtreamiumProxy`
- **Display Name**: `Xtreamium Proxy Service`
- **Start Type**: Automatic
- **Executable**: The installed application binary

### On Uninstallation
- Stops the running service
- Removes the Windows Service registration

## Manual Service Management

If you need to manage the service manually:

```powershell
# Check service status
sc query XtreamiumProxy

# Start the service
sc start XtreamiumProxy

# Stop the service
sc stop XtreamiumProxy

# Restart the service
sc stop XtreamiumProxy
timeout /t 5
sc start XtreamiumProxy

# Remove the service (manual uninstall)
sc delete XtreamiumProxy

# View service configuration
sc qc XtreamiumProxy
```

## Using Windows Services Manager

1. Press `Win + R`
2. Type `services.msc` and press Enter
3. Find "Xtreamium Proxy Service"
4. Right-click for options (Start, Stop, Restart, Properties)

## Troubleshooting

### Service won't start
1. Check Event Viewer: `eventvwr.msc` → Windows Logs → Application
2. Look for errors from "XtreamiumProxy"
3. Verify configuration file exists and is valid
4. Check that required dependencies (ffmpeg, etc.) are available

### Service starts but stops immediately
1. The application may be configured to run in console mode
2. Check that `DOTNET_ENVIRONMENT` is set correctly
3. Verify log files in the application directory

### Manual Service Creation

If automatic installation fails, you can create the service manually:

```powershell
# Get the installation path
$installPath = "$env:LOCALAPPDATA\XtreamiumProxy\current\xtreamium-proxy.exe"

# Create the service
sc create XtreamiumProxy binPath= $installPath DisplayName= "Xtreamium Proxy Service" start= auto

# Start it
sc start XtreamiumProxy
```

## Important Notes

- The service runs under the LOCAL SYSTEM account by default
- To run under a different account, modify the service properties in `services.msc`
- The service requires .NET 9 runtime (included in self-contained build)
- Configuration file location: `%LOCALAPPDATA%\XtreamiumProxy\current\appsettings.json`
- Logs are written to the configured log directory (check appsettings.json)

## Permissions

The installer will attempt to create the service, which requires administrative privileges. If the user runs the installer without admin rights:
- The application will install normally
- The service creation may fail silently
- The user can manually create the service later with admin rights
- Or they can run the application normally (non-service mode)
