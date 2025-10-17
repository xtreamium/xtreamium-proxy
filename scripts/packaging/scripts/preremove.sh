#!/bin/sh
set -e

# Stop and disable the service if it's running
if command -v systemctl >/dev/null 2>&1; then
    if systemctl is-active --quiet xtreamium-proxy; then
        systemctl stop xtreamium-proxy
    fi
    
    if systemctl is-enabled --quiet xtreamium-proxy 2>/dev/null; then
        systemctl disable xtreamium-proxy
    fi
fi

exit 0
