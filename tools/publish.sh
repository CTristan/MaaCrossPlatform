#!/bin/bash
# MaaGui Publishing Script

# Set configuration
CONFIG=Release
VERSION=$(git describe --tags --always || echo "1.0.0")

echo "Publishing MaaGui v$VERSION..."

# Windows x64
echo "Building Windows x64..."
dotnet publish src/MaaGui/MaaGui.csproj -c $CONFIG -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o publish/win-x64

# macOS arm64 (Apple Silicon)
echo "Building macOS arm64..."
dotnet publish src/MaaGui/MaaGui.csproj -c $CONFIG -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o publish/osx-arm64

# macOS x64 (Intel)
echo "Building macOS x64..."
dotnet publish src/MaaGui/MaaGui.csproj -c $CONFIG -r osx-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o publish/osx-x64

# Linux x64
echo "Building Linux x64..."
dotnet publish src/MaaGui/MaaGui.csproj -c $CONFIG -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o publish/linux-x64

echo "Publishing complete. Check the publish/ directory."
