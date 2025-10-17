#!/bin/sh
set -e

# Create system user and group if they don't exist
if ! getent group xtreamium-proxy >/dev/null 2>&1; then
    groupadd --system xtreamium-proxy
fi

if ! getent passwd xtreamium-proxy >/dev/null 2>&1; then
    useradd --system \
        --gid xtreamium-proxy \
        --home-dir /var/lib/xtreamium-proxy \
        --no-create-home \
        --shell /usr/sbin/nologin \
        --comment "Xtreamium Proxy Service" \
        xtreamium-proxy
fi

exit 0
