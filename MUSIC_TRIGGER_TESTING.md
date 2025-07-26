# Music Trigger Feature - Manual Testing Guide

## Overview
The LinuxCutePet now includes a music-triggered animation feature that automatically plays dancing animations when audio activity is detected.

## Feature Components

### 1. Audio Monitoring
- **Cross-platform**: Works on Linux (PulseAudio) and Windows
- **Real-time**: Monitors system audio output every 100ms
- **Threshold-based**: Only triggers when audio exceeds user-defined volume threshold

### 2. Animation System
- **New States**: Added MusicTriggeredStart, MusicTriggeredLoop, MusicTriggeredEnd
- **Animation Path**: Uses `Assets/musicTriggered/` folder structure
- **Flow**: Start → Loop (continuous) → End (when audio stops)

### 3. Settings System
- **Persistent Storage**: Volume threshold saved to user config file
- **Context Menu Integration**: Settings → Trigger Volume submenu
- **Preset Options**: 10%, 20%, 30%, 40%, 50% volume thresholds
- **Visual Feedback**: Current setting marked with checkmark (✓)

### 4. Trigger Logic
- **Minimum Duration**: 3-second continuous audio required before animation starts
- **Auto-stop**: Animation ends immediately when audio activity stops
- **Priority**: Respects existing activities (won't interrupt manual activities)

## Manual Testing Steps

### Test 1: Settings Persistence
1. Run the application
2. Right-click to open context menu
3. Navigate to Settings → Trigger Volume
4. Select different threshold (e.g., 40%)
5. Close and restart application
6. Verify setting is remembered

### Test 2: Volume Threshold Menu
1. Right-click to open context menu
2. Check Settings → Trigger Volume submenu
3. Verify current selection is marked with ✓
4. Test selecting different thresholds
5. Confirm menu updates checkmark position

### Test 3: Music Animation Trigger (Linux)
1. Set trigger volume to 30%
2. Start playing music/audio on system
3. Wait 3 seconds
4. Verify pet starts dancing animation (musicTriggered/loop)
5. Stop audio
6. Verify pet returns to idle after exit animation

### Test 4: Activity Priority
1. Start a manual activity (e.g., Sleep)
2. Play loud music
3. Verify music trigger does NOT interrupt manual activity
4. Stop manual activity
5. Verify music trigger works normally again

### Test 5: Threshold Sensitivity
1. Set threshold to 10% (very sensitive)
2. Play quiet audio
3. Verify trigger activates
4. Set threshold to 50% (less sensitive)
5. Play same quiet audio
6. Verify trigger does NOT activate

## Expected Behavior

### Linux Systems (Primary)
- Uses `pactl list sink-inputs` to detect active audio streams
- Parses volume levels from PulseAudio output
- Fallback to process detection if pactl fails

### Windows Systems (Secondary)
- Uses basic process detection (audiodg.exe)
- Simulates volume levels for demonstration
- Could be enhanced with NAudio.CoreAudioApi in future

### Animation Assets
The feature uses existing assets in `Assets/musicTriggered/`:
- `000.png` - Initial frame (optional)
- `loop/000.png` to `loop/039.png` - Dancing animation (40 frames)
- `loopOut/000.png` to `loopOut/002.png` - Exit animation (3 frames)

## Troubleshooting

### No Audio Detection on Linux
1. Verify PulseAudio is running: `pulseaudio --check -v`
2. Test pactl command: `pactl list sink-inputs`
3. Check if user has audio permissions

### Settings Not Saving
1. Check if application has write permissions to user directory
2. Verify settings file location: `~/.config/LinuxCutePet/PetSettings.config`

### Animation Not Playing
1. Verify audio threshold is set appropriately
2. Check if other activities are running (they have priority)
3. Ensure audio plays for at least 3 seconds
4. Verify musicTriggered animation assets exist

## Development Notes

### Architecture
- `IAudioMonitor` interface for cross-platform audio detection
- `AudioMonitor` class with platform-specific implementations
- `SettingsManager` for persistent configuration storage
- Integration with existing animation state system

### File Additions
- `IAudioMonitor.cs` - Audio monitoring interface
- `AudioMonitor.cs` - Cross-platform audio monitoring implementation
- `SettingsManager.cs` - Settings persistence helper
- Enhanced `MainWindow.axaml.cs` with music trigger logic

### Performance
- 100ms polling interval for responsive audio detection
- 3-second delay prevents false triggers from brief sounds
- Automatic cleanup on application shutdown