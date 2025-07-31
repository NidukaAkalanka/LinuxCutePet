#!/bin/bash
# Build for macOS only

set -e


echo "📦 Building for macOS x64..."
dotnet publish LinuxCutePet.sln \
    -r osx-x64 \
    --self-contained \
    -p:PublishSingleFile=true \
    -o ./dist/macos-x64



echo "📦 Building for macOS ARM64 (Apple Silicon)..."
dotnet publish LinuxCutePet.sln \
    -r osx-arm64 \
    --self-contained \
    -p:PublishSingleFile=true \
    -o ./dist/macos-arm64


echo "✅ macOS builds complete!"
echo "🚀 Run on macOS Intel: ./dist/macos-x64/PetViewerLinux"
echo "🚀 Run on macOS Apple Silicon: ./dist/macos-arm64/PetViewerLinux"
