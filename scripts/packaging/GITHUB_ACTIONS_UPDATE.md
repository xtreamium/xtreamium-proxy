# GitHub Actions Update Summary

## Changes Made

The GitHub Actions workflow (`.github/workflows/build-preview.yaml`) has been updated to build and release nfpm packages alongside the existing installers.

### New Steps Added to `build-linux` Job

1. **Install nfpm** - Installs nfpm from the GoReleaser APT repository
2. **Build Distribution Packages** - Runs the `build-packages.sh` script to create DEB, RPM, and Arch Linux packages
3. **Upload Package Artifacts** - Uploads three new artifacts:
   - DEB package (Debian/Ubuntu)
   - RPM package (RHEL/Fedora/CentOS)
   - Arch Linux package

### Updated `release` Job

The release job now:
- Downloads all package artifacts (Windows, Linux TAR, DEB, RPM, Arch)
- Uploads all packages as release assets

## What Gets Released

When you create a new release on GitHub, the workflow will now automatically build and attach:

1. **xtreamium-proxy-windows.zip** - Windows installer with service scripts
2. **xtreamium-proxy-linux.tar.gz** - Linux generic installer
3. **xtreamium-proxy_X.X.X_amd64.deb** - Debian/Ubuntu package
4. **xtreamium-proxy-X.X.X-1.x86_64.rpm** - RHEL/Fedora/CentOS package
5. **xtreamium-proxy-X.X.X-1-x86_64.pkg.tar.zst** - Arch Linux package

## Version Handling

The build script has been updated to use the GitHub release tag as the version number:
- Tag `v1.3.1` becomes version `1.3.1`
- Tag `1.3.1` stays as `1.3.1`
- If no VERSION environment variable is set, it falls back to extracting from `xtreamium-proxy.csproj`

## Testing the Workflow

To test the updated workflow:

1. **Create a new release:**
   ```bash
   git tag v1.3.2
   git push origin v1.3.2
   ```

2. **Go to GitHub:** Releases → Draft a new release → Choose the tag → Publish

3. **The workflow will:**
   - Build Windows and Linux binaries
   - Create DEB, RPM, and Arch Linux packages
   - Attach all 5 distribution packages to the release

## Manual Local Testing

You can still build packages manually:

```bash
# Build the application first
./scripts/build.sh

# Build packages
./scripts/packaging/build-packages.sh

# Packages will be in ./publish/packages/
```

## Installation Instructions for Users

### Debian/Ubuntu
```bash
wget https://github.com/xtreamium/xtreamium-proxy/releases/download/vX.X.X/xtreamium-proxy_X.X.X_amd64.deb
sudo dpkg -i xtreamium-proxy_X.X.X_amd64.deb
sudo apt-get install -f  # Install dependencies if needed
```

### RHEL/Fedora/CentOS
```bash
wget https://github.com/xtreamium/xtreamium-proxy/releases/download/vX.X.X/xtreamium-proxy-X.X.X-1.x86_64.rpm
sudo dnf install xtreamium-proxy-X.X.X-1.x86_64.rpm
```

### Arch Linux
```bash
wget https://github.com/xtreamium/xtreamium-proxy/releases/download/vX.X.X/xtreamium-proxy-X.X.X-1-x86_64.pkg.tar.zst
sudo pacman -U xtreamium-proxy-X.X.X-1-x86_64.pkg.tar.zst
```

## Next Steps

1. Commit and push the updated workflow
2. Create a test release to verify everything works
3. Update your project README to include installation instructions for the new package formats
