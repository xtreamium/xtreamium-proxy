# Xtreamium Proxy Packaging

This directory contains the packaging configuration and scripts for building distribution packages for Xtreamium Proxy using [nfpm](https://nfpm.goreleaser.com/).

## Supported Package Formats

- **DEB** - Debian/Ubuntu packages
- **RPM** - Red Hat/Fedora/CentOS packages  
- **Arch Linux** - Arch Linux packages (.pkg.tar.zst)

## Prerequisites

### Install nfpm

You need to install nfpm to build the packages. Choose one of the following methods:

#### Using Go
```bash
go install github.com/goreleaser/nfpm/v2/cmd/nfpm@latest
```

#### Using Homebrew (macOS/Linux)
```bash
brew install nfpm
```

#### Using apt (Debian/Ubuntu)
```bash
echo 'deb [trusted=yes] https://repo.goreleaser.com/apt/ /' | sudo tee /etc/apt/sources.list.d/goreleaser.list
sudo apt update
sudo apt install nfpm
```

#### Using yum/dnf (RHEL/Fedora/CentOS)
```bash
echo '[goreleaser]
name=GoReleaser
baseurl=https://repo.goreleaser.com/yum/
enabled=1
gpgcheck=0' | sudo tee /etc/yum.repos.d/goreleaser.repo
sudo yum install nfpm
```

For more installation options, see: https://nfpm.goreleaser.com/install/

## Building Packages

### 1. Build the Application

First, ensure the application is built and published:

```bash
cd /srv/dev/xtreamium/xtreamium-proxy
./scripts/build.sh
```

This will create the Linux build in `./publish/linux/`

### 2. Build All Packages

Run the packaging script:

```bash
./scripts/packaging/build-packages.sh
```

This will create packages for all supported formats in `./publish/packages/`:
- `xtreamium-proxy_<version>_amd64.deb`
- `xtreamium-proxy-<version>-1.x86_64.rpm`
- `xtreamium-proxy-<version>-1-x86_64.pkg.tar.zst`

### 3. Build Individual Package Formats

You can also build specific package formats:

```bash
# Build only DEB package
nfpm package --config nfpm.yaml --packager deb --target ../publish/packages/

# Build only RPM package
nfpm package --config nfpm.yaml --packager rpm --target ../publish/packages/

# Build only Arch Linux package
nfpm package --config nfpm.yaml --packager archlinux --target ../publish/packages/
```

## Package Structure

### Installation Directories

- `/opt/xtreamium-proxy/` - Application binaries
- `/etc/xtreamium-proxy/` - Configuration files
- `/var/lib/xtreamium-proxy/` - Working directory and data
- `/var/log/xtreamium-proxy/` - Log files (symlinked from /opt/xtreamium-proxy/logs)

### Configuration

The main configuration file is installed at `/etc/xtreamium-proxy/appsettings.json` and symlinked to `/opt/xtreamium-proxy/appsettings.json`.

**Important:** Edit `/etc/xtreamium-proxy/appsettings.json` before starting the service to configure:
- Video player executable path
- Recording paths
- CORS allowed origins
- Logging settings

### Service Management

The package includes a systemd service unit. After installation:

```bash
# Edit configuration
sudo nano /etc/xtreamium-proxy/appsettings.json

# Enable and start the service
sudo systemctl enable --now xtreamium-proxy

# Check service status
sudo systemctl status xtreamium-proxy

# View logs
sudo journalctl -u xtreamium-proxy -f
```

## Package Installation

### Debian/Ubuntu

```bash
sudo dpkg -i xtreamium-proxy_<version>_amd64.deb
sudo apt-get install -f  # Install dependencies if needed
```

### RHEL/Fedora/CentOS

```bash
sudo rpm -i xtreamium-proxy-<version>-1.x86_64.rpm
# or
sudo dnf install xtreamium-proxy-<version>-1.x86_64.rpm
```

### Arch Linux

```bash
sudo pacman -U xtreamium-proxy-<version>-1-x86_64.pkg.tar.zst
```

## Package Removal

The package can be removed using the standard package manager commands:

```bash
# Debian/Ubuntu
sudo apt remove xtreamium-proxy

# RHEL/Fedora/CentOS
sudo dnf remove xtreamium-proxy
# or
sudo rpm -e xtreamium-proxy

# Arch Linux
sudo pacman -R xtreamium-proxy
```

**Note:** User data in `/var/lib/xtreamium-proxy`, `/var/log/xtreamium-proxy`, and `/etc/xtreamium-proxy` is preserved during uninstallation. To completely remove all data:

```bash
sudo rm -rf /var/lib/xtreamium-proxy /var/log/xtreamium-proxy /etc/xtreamium-proxy
sudo userdel xtreamium-proxy
sudo groupdel xtreamium-proxy
```

## Dependencies

### Required
- `ffmpeg` - Required for video processing

### Recommended
- `mpv` - Default video player (can be changed in configuration)

## Customization

### Modifying Package Configuration

Edit `nfpm.yaml` to customize:
- Package metadata (name, version, description, etc.)
- Dependencies
- File locations and permissions
- Install/uninstall scripts

### Modifying Systemd Service

Edit `systemd/xtreamium-proxy.service` to customize:
- Service user/group
- Environment variables
- Start command and arguments
- Resource limits
- Security settings

### Modifying Install Scripts

The following scripts can be customized:
- `scripts/preinstall.sh` - Runs before package installation
- `scripts/postinstall.sh` - Runs after package installation
- `scripts/preremove.sh` - Runs before package removal
- `scripts/postremove.sh` - Runs after package removal

## Troubleshooting

### Package Build Fails

1. Ensure the application is built: `./scripts/build.sh`
2. Verify nfpm is installed: `nfpm --version`
3. Check that all referenced files exist in the nfpm.yaml

### Service Won't Start

1. Check logs: `sudo journalctl -u xtreamium-proxy -f`
2. Verify configuration: `sudo nano /etc/xtreamium-proxy/appsettings.json`
3. Check permissions: `ls -la /var/lib/xtreamium-proxy`
4. Ensure dependencies are installed: `ffmpeg --version`

## References

- [nfpm Documentation](https://nfpm.goreleaser.com/)
- [nfpm Configuration Reference](https://nfpm.goreleaser.com/configuration/)
- [GoReleaser](https://goreleaser.com/)
