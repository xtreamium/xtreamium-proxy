#!/usr/bin/env bash

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CONTAINER_NAME="xtreamium-windows-tiny11"
COMPOSE_FILE="$SCRIPT_DIR/docker-compose.yaml"
STORAGE_DIR="$SCRIPT_DIR/storage"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

show_help() {
    cat << EOF
Usage: $(basename "$0") [OPTION]

Manage the Windows test container for Xtreamium Proxy

Options:
    -c, --connect    Start container and connect via RDP (default)
    -m, --make       Start the docker-compose services
    -s, --stop       Stop the docker-compose services and remove orphans
    -d, --delete     Stop, remove container and delete storage directory
    -h, --help       Show this help message

Examples:
    $(basename "$0")              # Connect to Windows (starts if needed)
    $(basename "$0") --make       # Start the container
    $(basename "$0") --stop       # Stop the container
    $(basename "$0") --delete     # Delete everything (requires confirmation)

EOF
}

start_container() {
    echo -e "${GREEN}Starting Windows container...${NC}"
    docker --context default compose -f "$COMPOSE_FILE" up -d
    echo -e "${GREEN}Container started successfully${NC}"
}

stop_container() {
    echo -e "${YELLOW}Stopping Windows container...${NC}"
    docker --context default compose -f "$COMPOSE_FILE" down --remove-orphans
    echo -e "${GREEN}Container stopped${NC}"
}

connect_rdp() {
    # Check if container is running
    if ! docker --context default ps --format '{{.Names}}' | grep -q "^${CONTAINER_NAME}$"; then
        echo -e "${YELLOW}Container is not running. Starting it now...${NC}"
        start_container
        echo -e "${YELLOW}Waiting 30 seconds for Windows to boot...${NC}"
        sleep 30
    fi

    echo -e "${GREEN}Connecting to Windows via RDP...${NC}"
    echo -e "${YELLOW}Username: admin${NC}"
    echo -e "${YELLOW}Password: password${NC}"
    
    # Check if xfreerdp is installed
    if ! command -v xfreerdp &> /dev/null; then
        echo -e "${RED}xfreerdp is not installed. Please install it:${NC}"
        echo -e "${YELLOW}  sudo pacman -S freerdp  # Arch Linux${NC}"
        echo -e "${YELLOW}  sudo apt install freerdp2-x11  # Ubuntu/Debian${NC}"
        exit 1
    fi

    # Connect via RDP with your preferred settings
    xfreerdp +clipboard +fonts /sound /mic /smart-sizing /f \
      /floatbar:sticky:off,default:visible,show:fullscreen \
      /scale:180 /scale-desktop:200 /network:auto /cert-ignore \
      /u:admin /p:password \
      /v:localhost:3394 > /dev/null 2>&1 &
}

delete_container() {
    echo -e "${RED}WARNING: This will permanently delete the container and all its data!${NC}"
    echo -e "${YELLOW}Please type the container name to confirm: ${CONTAINER_NAME}${NC}"
    read -r confirmation

    if [ "$confirmation" != "$CONTAINER_NAME" ]; then
        echo -e "${RED}Container name does not match. Aborting.${NC}"
        exit 1
    fi

    echo -e "${YELLOW}Stopping container...${NC}"
    docker --context default compose -f "$COMPOSE_FILE" down --remove-orphans || true

    echo -e "${YELLOW}Removing container...${NC}"
    docker --context default rm -f "$CONTAINER_NAME" 2>/dev/null || true

    if [ -d "$STORAGE_DIR" ]; then
        echo -e "${YELLOW}Removing storage directory...${NC}"
        rm -rf "$STORAGE_DIR"
        echo -e "${GREEN}Storage directory removed${NC}"
    fi

    echo -e "${GREEN}Container and storage deleted successfully${NC}"
}

# Parse command line arguments
case "${1:-}" in
    -m|--make)
        start_container
        ;;
    -s|--stop)
        stop_container
        ;;
    -d|--delete)
        delete_container
        ;;
    -h|--help)
        show_help
        ;;
    -c|--connect|"")
        connect_rdp
        ;;
    *)
        echo -e "${RED}Unknown option: $1${NC}"
        show_help
        exit 1
        ;;
esac
