#!/usr/bin/env bash
set -e

# Script to push package updates to the AUR
# Requires:
# - SSH key configured for AUR access
# - git installed
# - Package already built

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

print_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Get version from environment or parameter
VERSION="${VERSION:-$1}"
if [ -z "$VERSION" ]; then
    print_error "VERSION not set. Usage: VERSION=1.0.0 $0 or $0 1.0.0"
    exit 1
fi

# Remove 'v' prefix if present
VERSION="${VERSION#v}"

print_info "Pushing version $VERSION to AUR..."

# Temporary directory for AUR repo
AUR_DIR=$(mktemp -d)
trap "rm -rf $AUR_DIR" EXIT

# Clone the AUR repository
print_info "Cloning AUR repository..."
cd "$AUR_DIR"

# Use SSH to clone (requires SSH key configured)
if ! git clone ssh://aur@aur.archlinux.org/xtreamium-proxy.git; then
    print_error "Failed to clone AUR repository"
    print_error "Make sure your SSH key is configured for AUR access"
    exit 1
fi

cd xtreamium-proxy

# Update PKGBUILD with new version
print_info "Updating PKGBUILD..."
sed "s/VERSION_PLACEHOLDER/$VERSION/g" "$SCRIPT_DIR/PKGBUILD.template" > PKGBUILD

# Update .SRCINFO
print_info "Generating .SRCINFO..."
if ! command -v makepkg &> /dev/null; then
    print_warning "makepkg not found, attempting to generate .SRCINFO manually"
    # Generate basic .SRCINFO
    cat > .SRCINFO << EOF
pkgbase = xtreamium-proxy
	pkgdesc = Xtreamium Proxy Service
	pkgver = $VERSION
	pkgrel = 1
	url = https://github.com/yourusername/xtreamium-proxy
	arch = x86_64
	license = MIT
	depends = glibc
	provides = xtreamium-proxy
	conflicts = xtreamium-proxy
	backup = etc/xtreamium-proxy/appsettings.json
	source = https://github.com/yourusername/xtreamium-proxy/releases/download/v$VERSION/xtreamium-proxy-linux.tar.gz
	sha256sums = SKIP

pkgname = xtreamium-proxy
EOF
else
    makepkg --printsrcinfo > .SRCINFO
fi

# Configure git
print_info "Configuring git..."
git config user.name "GitHub Actions"
git config user.email "actions@github.com"

# Check if there are changes
if git diff --quiet && git diff --cached --quiet; then
    print_info "No changes to commit, AUR is already up to date"
    exit 0
fi

# Commit and push
print_info "Committing changes..."
git add PKGBUILD .SRCINFO
git commit -m "Update to version $VERSION"

print_info "Pushing to AUR..."
if git push; then
    print_info "Successfully pushed version $VERSION to AUR!"
else
    print_error "Failed to push to AUR"
    exit 1
fi

print_info "AUR package updated successfully!"
# Maintainer: Your Name <your.email@example.com>
pkgname=xtreamium-proxy
pkgver=VERSION_PLACEHOLDER
pkgrel=1
pkgdesc="Xtreamium Proxy Service"
arch=('x86_64')
url="https://github.com/yourusername/xtreamium-proxy"
license=('MIT')
depends=('glibc')
provides=('xtreamium-proxy')
conflicts=('xtreamium-proxy')
backup=('etc/xtreamium-proxy/appsettings.json')
source=("https://github.com/yourusername/xtreamium-proxy/releases/download/v${pkgver}/xtreamium-proxy-linux.tar.gz")
sha256sums=('SKIP')

package() {
    # Install binary
    install -Dm755 "${srcdir}/xtreamium-proxy" "${pkgdir}/usr/bin/xtreamium-proxy"
    
    # Install configuration
    install -Dm644 "${srcdir}/appsettings.json" "${pkgdir}/etc/xtreamium-proxy/appsettings.json"
    
    # Install systemd service
    install -Dm644 "${srcdir}/xtreamium-proxy.service" "${pkgdir}/usr/lib/systemd/system/xtreamium-proxy.service"
    install -Dm644 "${srcdir}/xtreamium-proxy-user.service" "${pkgdir}/usr/lib/systemd/user/xtreamium-proxy-user.service"
    
    # Install documentation
    install -Dm644 "${srcdir}/README.md" "${pkgdir}/usr/share/doc/${pkgname}/README.md"
}

