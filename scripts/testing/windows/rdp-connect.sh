#!/bin/bash

# RDP connection script for dockur/windows VM
# Username: admin
# Password: password
# Port: 3394

xfreerdp +clipboard +fonts /sound /mic /smart-sizing /f \
  /floatbar:sticky:off,default:visible,show:fullscreen \
  /scale:180 /scale-desktop:200 /network:auto /cert-ignore \
  /u:admin /p:password \
  /v:localhost:3394 > /dev/null 2>&1 &
