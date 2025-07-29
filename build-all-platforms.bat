@echo off
REM Cross-Platform Build Script for Linux Cute Pet (Windows Version)
REM This script builds platform-specific executables

echo 🐱 Linux Cute Pet - Cross-Platform Builder
echo ==========================================

REM Create dist directory
if not exist dist mkdir dist

REM Build for Linux (x64)
echo 📦 Building for Linux x64...
dotnet publish LinuxCutePet.sln ^
    -r linux-x64 ^
    --self-contained ^
    -o ./dist/linux-x64

echo ✅ Linux x64 build complete: ./dist/linux-x64/

REM Build for Windows (x64) 
echo 📦 Building for Windows x64...
dotnet publish LinuxCutePet.sln ^
    -r win-x64 ^
    --self-contained ^
    -o ./dist/windows-x64

echo ✅ Windows x64 build complete: ./dist/windows-x64/

REM Build for macOS (x64)
echo 📦 Building for macOS x64...
dotnet publish LinuxCutePet.sln ^
    -r osx-x64 ^
    --self-contained ^
    -p:PublishSingleFile=true ^
    -o ./dist/macos-x64

echo ✅ macOS x64 build complete: ./dist/macos-x64/

REM Build for macOS (ARM64 - Apple Silicon)
echo 📦 Building for macOS ARM64 (Apple Silicon)...
dotnet publish LinuxCutePet.sln ^
    -r osx-arm64 ^
    --self-contained ^
    -p:PublishSingleFile=true ^
    -o ./dist/macos-arm64

echo ✅ macOS ARM64 build complete: ./dist/macos-arm64/

echo.
echo 🎉 All builds completed successfully!
echo.
echo 📁 Distribution files:
echo   Linux:       ./dist/linux-x64/PetViewerLinux
echo   Windows:     ./dist/windows-x64/PetViewerLinux.exe
echo   macOS x64:   ./dist/macos-x64/PetViewerLinux
echo   macOS ARM:   ./dist/macos-arm64/PetViewerLinux
echo.
echo 🚀 To test on Windows:
echo   ./dist/windows-x64/PetViewerLinux.exe

pause
