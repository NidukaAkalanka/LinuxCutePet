#!/bin/bash

# Create mod-friendly builds (framework-dependent with external Assets)
echo "Building mod-friendly LinuxCutePet..."

# Clean previous builds
dotnet clean PetViewerLinux.csproj

# Create builds directory
mkdir -p builds/mod-friendly

echo "Building Linux x64..."
dotnet publish PetViewerLinux.csproj -c Release -r linux-x64 --no-self-contained -o builds/mod-friendly/linux-x64

echo "Building Windows x64..."
dotnet publish PetViewerLinux.csproj -c Release -r win-x64 --no-self-contained -o builds/mod-friendly/win-x64

echo "Building macOS x64..."
dotnet publish PetViewerLinux.csproj -c Release -r osx-x64 --no-self-contained -o builds/mod-friendly/osx-x64

echo "Building macOS ARM64..."
dotnet publish PetViewerLinux.csproj -c Release -r osx-arm64 --no-self-contained -o builds/mod-friendly/osx-arm64

# Copy Assets to each build
echo "Copying Assets to builds..."
cp -r Assets builds/mod-friendly/linux-x64/
cp -r Assets builds/mod-friendly/win-x64/
cp -r Assets builds/mod-friendly/osx-x64/
cp -r Assets builds/mod-friendly/osx-arm64/

# Create zip packages
echo "Creating zip packages..."
cd builds/mod-friendly

# Linux
zip -r LinuxCutePet-linux-x64-mod-friendly.zip linux-x64/

# Windows
zip -r LinuxCutePet-win-x64-mod-friendly.zip win-x64/

# macOS x64
zip -r LinuxCutePet-osx-x64-mod-friendly.zip osx-x64/

# macOS ARM64
zip -r LinuxCutePet-osx-arm64-mod-friendly.zip osx-arm64/

cd ../..

echo "Mod-friendly builds completed!"
echo "Files are in builds/mod-friendly/"
echo ""
echo "NOTE: These builds require .NET 8 Runtime to be installed on the target system"
echo "Download from: https://dotnet.microsoft.com/download/dotnet/8.0"
echo ""
echo "To modify animations:"
echo "1. Navigate to the Assets folder next to the executable"
echo "2. Replace or add PNG files following the naming convention (000.png, 001.png, etc.)"
echo "3. Restart the application to see changes"
