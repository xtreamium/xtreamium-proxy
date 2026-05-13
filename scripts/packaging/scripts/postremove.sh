#!/bin/bash
# Post-remove for xtreamium-proxy (.deb / .rpm).
# Per-user state in ~/.config/xtreamium-proxy is intentionally left in place.

cat <<'EOF'

==> Xtreamium Proxy removed.

Per-user state was left untouched. To remove it manually:
  rm -rf ~/.config/xtreamium-proxy

EOF
