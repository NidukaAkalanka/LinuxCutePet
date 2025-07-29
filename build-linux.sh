#!/bin/bash
# Build for Linux only

echo "📦 Building for Linux x64..."
dotnet publish LinuxCutePet.sln \
    -r linux-x64 \
    --self-contained \
    -p:PublishSingleFile=true \
    -o ./dist/linux-x64

echo "✅ Linux build complete!"
echo "🚀 Run with: ./dist/linux-x64/PetViewerLinux"
