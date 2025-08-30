#!/usr/bin/env sh
echo "Building for Linux..."
dotnet publish -r linux-x64 \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    --self-contained true
exit

echo "Building for Windows..."
dotnet publish -r win-x64 \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    --self-contained true
