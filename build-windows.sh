#!/bin/bash
# Build for Windows only

echo "📦 Building for Windows x64..."
dotnet publish LinuxCutePet.sln \
    -r win-x64 \
    --self-contained \
    -p:PublishSingleFile=true \
    -o ./dist/windows-x64

echo "✅ Windows build complete!"
echo "🚀 Run on Windows with: ./dist/windows-x64/PetViewerLinux.exe"
