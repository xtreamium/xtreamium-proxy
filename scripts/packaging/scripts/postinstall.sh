#!/bin/sh
set -e

# Ensure correct ownership
chown -R xtreamium-proxy:xtreamium-proxy /var/lib/xtreamium-proxy
chown -R xtreamium-proxy:xtreamium-proxy /var/log/xtreamium-proxy
chown xtreamium-proxy:xtreamium-proxy /etc/xtreamium-proxy/appsettings.json

# Create symlink for configuration
if [ ! -L /opt/xtreamium-proxy/appsettings.json ]; then
    ln -sf /etc/xtreamium-proxy/appsettings.json /opt/xtreamium-proxy/appsettings.json
fi

# Create symlink for logs
if [ ! -L /opt/xtreamium-proxy/logs ]; then
    ln -sf /var/log/xtreamium-proxy /opt/xtreamium-proxy/logs
fi

# Reload systemd daemon
if command -v systemctl >/dev/null 2>&1; then
    systemctl daemon-reload
    
    # Enable but don't start the service (let user configure first)
    echo "Xtreamium Proxy has been installed."
    echo ""
    echo "To configure the service, edit: /etc/xtreamium-proxy/appsettings.json"
    echo ""
    echo "To start the service, run:"
    echo "  sudo systemctl enable --now xtreamium-proxy"
    echo ""
    echo "To view logs, run:"
    echo "  sudo journalctl -u xtreamium-proxy -f"
fi

exit 0
