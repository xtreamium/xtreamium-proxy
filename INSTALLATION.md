# Xtreamium Proxy Installation Guide

This guide covers the installation of Xtreamium Proxy on both Windows and Linux systems using the pre-built packages
from GitHub releases.

## System Requirements

- **Windows**: Windows 10/11 or Windows Server 2016+
- **Linux**: Any modern Linux distribution with systemd support
- **Hardware**: Minimum 512MB RAM, 100MB disk space
- **Network**: Internet connection for streaming proxy functionality

## Download

Download the latest release packages from the [GitHub Releases](https://github.com/your-repo/xtreamium-proxy/releases)
page:

- **Windows**: `xtreamium-proxy-windows.zip`
- **Linux**: `xtreamium-proxy-linux.tar.gz`

---

## Windows Installation

### Prerequisites

- Administrator privileges required for service installation
- Windows Defender or antivirus may need to whitelist the executable

### Installation Steps

1. **Download and Extract**
   ```
   Download xtreamium-proxy-windows.zip
   Extract to a folder (e.g., C:\xtreamium-proxy)
   ```

2. **Install as Windows Service**
  - Right-click on `install-service.bat`
  - Select "Run as administrator"
  - Wait for installation to complete

3. **Verify Installation**
  - Open Services (services.msc)
  - Look for "Xtreamium Proxy Service"
  - Status should show "Running"

### Alternative: Manual Installation

If you prefer not to use the automated installer:

```batch
# Open Command Prompt as Administrator
cd C:\path\to\extracted\files

# Create the service manually
sc create XtreamiumProxy binPath= "C:\path\to\xtreamium-proxy.exe" DisplayName= "Xtreamium Proxy Service" start= auto

# Start the service
sc start XtreamiumProxy
```

### Windows Service Management

```batch
# Check service status
sc query XtreamiumProxy

# Start service
sc start XtreamiumProxy

# Stop service
sc stop XtreamiumProxy

# View logs in Event Viewer
eventvwr.msc → Applications and Services Logs
```

### Uninstallation

1. Right-click on `uninstall-service.bat`
2. Select "Run as administrator"
3. Delete the extracted folder

---

## Linux Installation

### Prerequisites

- Modern Linux distribution with systemd
- Root/sudo access for system-wide installation (optional for user installation)

### Installation Steps

1. **Download and Extract**
   ```bash
   wget https://github.com/your-repo/xtreamium-proxy/releases/latest/download/xtreamium-proxy-linux.tar.gz
   tar -xzf xtreamium-proxy-linux.tar.gz
   cd xtreamium-proxy-linux
   ```

2. **Run the Installer**
   ```bash
   chmod +x install.sh
   ./install.sh
   ```

3. **Choose Installation Type**
  - **Option 1**: System-wide service (requires sudo)
    - Installs to `/opt/xtreamium-proxy`
    - Runs as dedicated `xtreamium` user
    - Starts automatically on boot
    - Recommended for production servers

  - **Option 2**: User service (no sudo required)
    - Installs to `~/.local/opt/xtreamium-proxy`
    - Runs as current user
    - Starts when user logs in
    - Good for desktop environments

### Alternative: Manual Installation

#### System-wide Installation

```bash
# Create system user
sudo useradd -r -s /bin/false xtreamium

# Create installation directory
sudo mkdir -p /opt/xtreamium-proxy
sudo cp xtreamium-proxy /opt/xtreamium-proxy/
sudo cp *.json /opt/xtreamium-proxy/
sudo chown -R xtreamium:xtreamium /opt/xtreamium-proxy
sudo chmod +x /opt/xtreamium-proxy/xtreamium-proxy

# Install systemd service
sudo cp xtreamium-proxy.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable xtreamium-proxy
sudo systemctl start xtreamium-proxy
```

#### User Installation

```bash
# Create user directory
mkdir -p ~/.local/opt/xtreamium-proxy
cp xtreamium-proxy ~/.local/opt/xtreamium-proxy/
cp *.json ~/.local/opt/xtreamium-proxy/
chmod +x ~/.local/opt/xtreamium-proxy/xtreamium-proxy

# Install user service
mkdir -p ~/.config/systemd/user
cp xtreamium-proxy-user.service ~/.config/systemd/user/
systemctl --user daemon-reload
systemctl --user enable xtreamium-proxy-user
systemctl --user start xtreamium-proxy-user

# Enable lingering (optional, for auto-start on boot)
sudo loginctl enable-linger $USER
```

### Linux Service Management

#### System Service

```bash
# Check status
sudo systemctl status xtreamium-proxy

# Start/stop/restart
sudo systemctl start xtreamium-proxy
sudo systemctl stop xtreamium-proxy
sudo systemctl restart xtreamium-proxy

# View logs
sudo journalctl -u xtreamium-proxy -f
sudo journalctl -u xtreamium-proxy --since today
```

#### User Service

```bash
# Check status
systemctl --user status xtreamium-proxy-user

# Start/stop/restart
systemctl --user start xtreamium-proxy-user
systemctl --user stop xtreamium-proxy-user
systemctl --user restart xtreamium-proxy-user

# View logs
journalctl --user -u xtreamium-proxy-user -f
journalctl --user -u xtreamium-proxy-user --since today
```

### Uninstallation

Run the uninstaller script:

```bash
chmod +x uninstall.sh
./uninstall.sh
```

Or manually:

```bash
# System service
sudo systemctl stop xtreamium-proxy
sudo systemctl disable xtreamium-proxy
sudo rm /etc/systemd/system/xtreamium-proxy.service
sudo systemctl daemon-reload
sudo rm -rf /opt/xtreamium-proxy
sudo userdel xtreamium

# User service
systemctl --user stop xtreamium-proxy-user
systemctl --user disable xtreamium-proxy-user
rm ~/.config/systemd/user/xtreamium-proxy-user.service
systemctl --user daemon-reload
rm -rf ~/.local/opt/xtreamium-proxy
```

---

## Configuration

### Default Settings

The application uses `appsettings.json` for configuration. Key settings include:

- **Logging**: Configure log levels and output
- **Network**: Binding addresses and ports
- **Database**: SQLite database settings
- **Security**: Authentication and authorization settings

### Configuration File Location

- **Windows**: Same directory as executable
- **Linux System**: `/opt/xtreamium-proxy/appsettings.json`
- **Linux User**: `~/.local/opt/xtreamium-proxy/appsettings.json`

### Applying Configuration Changes

After editing configuration:

**Windows:**

```batch
sc stop XtreamiumProxy
sc start XtreamiumProxy
```

**Linux System:**

```bash
sudo systemctl restart xtreamium-proxy
```

**Linux User:**

```bash
systemctl --user restart xtreamium-proxy-user
```

---

## Accessing the Application

Once installed and running, the application will be available at:

- Default URL: `http://localhost:8963` (check appsettings.json for actual port)
- Web interface for configuration and monitoring

---

## Troubleshooting

### Windows Issues

1. **Service won't start**
  - Check Windows Event Viewer for errors
  - Verify executable permissions
  - Run executable manually to see error messages

2. **Access denied errors**
  - Ensure installer was run as Administrator
  - Check Windows Defender exclusions

### Linux Issues

1. **Permission denied**
  - Ensure scripts are executable: `chmod +x install.sh`
  - For system installation, ensure you have sudo access

2. **Service fails to start**
  - Check logs: `journalctl -u xtreamium-proxy`
  - Verify executable permissions: `chmod +x xtreamium-proxy`
  - Check systemd service file syntax

3. **Port binding issues**
  - Check if port is already in use: `netstat -tulpn | grep :8963`
  - Modify port in appsettings.json
  - For ports < 1024, system service may be required

### General Issues

1. **Configuration problems**
  - Validate JSON syntax in appsettings.json
  - Check file permissions
  - Restore default configuration if needed

2. **Network connectivity**
  - Verify firewall settings
  - Check binding addresses in configuration
  - Ensure required ports are open

---

## Security Considerations

### Windows

- Service runs under Local System account by default
- Consider creating a dedicated service account
- Configure Windows Firewall rules as needed

### Linux

- System service runs under dedicated `xtreamium` user with limited privileges
- Security settings include `NoNewPrivileges`, `ProtectSystem`, `ProtectHome`
- User service runs with user's privileges
- Configure iptables/firewall rules as needed

### General

- Regularly update to latest version
- Monitor logs for suspicious activity
- Use strong authentication if enabled
- Consider running behind reverse proxy for additional security

---

## Support

For issues and support:

- Check the troubleshooting section above
- Review application logs
- Create an issue on the GitHub repository
- Include system information and relevant log entries
