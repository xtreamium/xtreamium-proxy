#!/usr/bin/env bash

dotnet publish \
  -r win-x64 \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:PublishSingleFile=true \
  --self-contained true \
  -c Release -o ./publish/win/

dotnet publish \
  -r linux-x64 \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:PublishSingleFile=true \
  --self-contained true \
  -c Release -o ./publish/linux/
