@echo off

REM Create mod-friendly builds (framework-dependent with external Assets)
echo Building mod-friendly LinuxCutePet...

REM Clean previous builds
dotnet clean

REM Create builds directory
if not exist "builds\mod-friendly" mkdir builds\mod-friendly

echo Building Linux x64...
dotnet publish -c Release -r linux-x64 --no-self-contained -o builds/mod-friendly/linux-x64

echo Building Windows x64...
dotnet publish -c Release -r win-x64 --no-self-contained -o builds/mod-friendly/win-x64

echo Building macOS x64...
dotnet publish -c Release -r osx-x64 --no-self-contained -o builds/mod-friendly/osx-x64

echo Building macOS ARM64...
dotnet publish -c Release -r osx-arm64 --no-self-contained -o builds/mod-friendly/osx-arm64

REM Copy Assets to each build
echo Copying Assets to builds...
xcopy /E /I Assets builds\mod-friendly\linux-x64\Assets
xcopy /E /I Assets builds\mod-friendly\win-x64\Assets
xcopy /E /I Assets builds\mod-friendly\osx-x64\Assets
xcopy /E /I Assets builds\mod-friendly\osx-arm64\Assets

REM Create zip packages
echo Creating zip packages...
cd builds\mod-friendly

REM Use PowerShell for compression
powershell -Command "Compress-Archive -Path linux-x64 -DestinationPath LinuxCutePet-linux-x64-mod-friendly.zip -Force"
powershell -Command "Compress-Archive -Path win-x64 -DestinationPath LinuxCutePet-win-x64-mod-friendly.zip -Force"
powershell -Command "Compress-Archive -Path osx-x64 -DestinationPath LinuxCutePet-osx-x64-mod-friendly.zip -Force"
powershell -Command "Compress-Archive -Path osx-arm64 -DestinationPath LinuxCutePet-osx-arm64-mod-friendly.zip -Force"

cd ..\..

echo Mod-friendly builds completed!
echo Files are in builds\mod-friendly\
echo.
echo NOTE: These builds require .NET 8 Runtime to be installed on the target system
echo Download from: https://dotnet.microsoft.com/download/dotnet/8.0
echo.
echo To modify animations:
echo 1. Navigate to the Assets folder next to the executable
echo 2. Replace or add PNG files following the naming convention (000.png, 001.png, etc.)
echo 3. Restart the application to see changes

pause
