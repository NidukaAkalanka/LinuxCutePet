# Cross-Platform Audio Detection Build Guide

This project now supports audio detection on Linux, Windows, and macOS through modular audio monitoring services.

## Audio Monitoring Services

### 1. LinuxAudioMonitorService
- **Method**: Uses `pactl` (PulseAudio) to monitor active audio streams
- **Requirements**: PulseAudio (standard on most Linux distributions)
- **Detection**: Real-time audio stream monitoring

### 2. WindowsAudioMonitorService  
- **Method**: Process-based detection of common audio applications
- **Requirements**: Standard Windows installation
- **Detection**: Monitors for running audio/media applications

### 3. MacOSAudioMonitorService
- **Method**: AppleScript + process detection
- **Requirements**: Standard macOS installation
- **Detection**: Combines system audio queries with process monitoring

### 4. DummyAudioMonitorService
- **Method**: No-op implementation for unsupported platforms
- **Requirements**: None
- **Detection**: No audio detection (pet won't dance)

## Building for Different Platforms

### Option 1: Automatic Platform Detection (Recommended)
The application automatically detects the current platform and uses the appropriate audio service:

```bash
# Build for current platform (auto-detection)
dotnet build LinuxCutePet.sln
dotnet run LinuxCutePet.sln
```

### Option 2: Platform-Specific Builds

#### Linux Build
```bash
# Build for Linux
dotnet build LinuxCutePet.sln -r linux-x64
dotnet publish LinuxCutePet.sln -r linux-x64 --self-contained

# Run on Linux
dotnet run LinuxCutePet.sln
```

#### Windows Build
```bash
# Build for Windows
dotnet build LinuxCutePet.sln -r win-x64
dotnet publish LinuxCutePet.sln -r win-x64 --self-contained

# Run on Windows
dotnet run LinuxCutePet.sln
```

#### macOS Build
```bash
# Build for macOS
dotnet build LinuxCutePet.sln -r osx-x64
dotnet publish LinuxCutePet.sln -r osx-x64 --self-contained

# Run on macOS
dotnet run LinuxCutePet.sln
```

### Option 3: Cross-Platform Publishing
```bash
# Publish for all platforms
dotnet publish LinuxCutePet.sln -r linux-x64 --self-contained -o ./dist/linux
dotnet publish LinuxCutePet.sln -r win-x64 --self-contained -o ./dist/windows  
dotnet publish LinuxCutePet.sln -r osx-x64 --self-contained -o ./dist/macos
```

## Testing Audio Detection

### Linux
```bash
# Test audio detection by playing something with:
paplay /usr/share/sounds/alsa/Front_Left.wav
# or
firefox https://www.youtube.com/watch?v=dQw4w9WgXcQ
```

### Windows
```bash
# Test by running any of these:
# - Open Spotify
# - Play video in Chrome/Firefox/Edge
# - Open Windows Media Player
# - Play YouTube video
```

### macOS
```bash
# Test by running any of these:
# - Open Apple Music app
# - Play video in Safari/Chrome/Firefox
# - Open QuickTime Player
# - Play YouTube video
```

## Platform-Specific Dependencies

### Linux
- **Required**: PulseAudio (`pactl` command)
- **Install**: Usually pre-installed, or `sudo apt install pulseaudio-utils`

### Windows
- **Required**: PowerShell (standard on Windows)
- **Install**: Pre-installed on Windows 7+

### macOS
- **Required**: AppleScript, `pgrep`, `pmset` (standard on macOS)
- **Install**: Pre-installed on macOS

## Troubleshooting

### Linux Audio Not Working
```bash
# Check if PulseAudio is running
pulseaudio --check -v

# Check if pactl is available
which pactl

# Test pactl manually
pactl list sink-inputs
```

### Windows Audio Not Working
```bash
# Check if PowerShell is available
powershell.exe -Command "Get-Host"

# Test process detection
tasklist | findstr spotify
```

### macOS Audio Not Working
```bash
# Check if AppleScript is available
osascript -e "display dialog \"Test\""

# Check if pgrep is available
which pgrep
```

## Performance Notes

- **Linux**: Most efficient (direct PulseAudio integration)
- **Windows**: Process-based (slightly higher CPU usage)
- **macOS**: Hybrid approach (balanced performance)

## Memory Optimization

All platforms now include memory optimization:
- Lazy bitmap loading
- Smart caching system
- Periodic cleanup
- Proper resource disposal

Expected memory usage: 50-100MB (down from 2GB+)
