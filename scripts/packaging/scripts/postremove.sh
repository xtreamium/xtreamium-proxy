#!/bin/sh
set -e

# Reload systemd daemon
if command -v systemctl >/dev/null 2>&1; then
    systemctl daemon-reload
fi

# Remove symlinks if they exist
rm -f /opt/xtreamium-proxy/appsettings.json
rm -f /opt/xtreamium-proxy/logs

# Note: We don't remove the user, group, or data directories
# to preserve data on uninstall. Users can manually remove them if needed.

echo "Xtreamium Proxy has been removed."
echo "User data in /var/lib/xtreamium-proxy and /var/log/xtreamium-proxy has been preserved."
echo "To completely remove all data, run:"
echo "  sudo rm -rf /var/lib/xtreamium-proxy /var/log/xtreamium-proxy /etc/xtreamium-proxy"
echo "  sudo userdel xtreamium-proxy"
echo "  sudo groupdel xtreamium-proxy"

exit 0
