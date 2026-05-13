#!/bin/bash
# Post-install for xtreamium-proxy (.deb / .rpm).
# Nothing to do as root — the service is per-user. Just print guidance.

cat <<'EOF'

==> Xtreamium Proxy installed as a per-user systemd service.

To start it as your desktop user:
  systemctl --user enable --now xtreamium-proxy

To start it automatically on boot (without requiring login):
  loginctl enable-linger $USER

Config will be created at ~/.config/xtreamium-proxy/appsettings.json on first run.
Reference template: /usr/share/xtreamium-proxy/appsettings.json.example

EOF
