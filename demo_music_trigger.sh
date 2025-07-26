#!/bin/bash

# LinuxCutePet Music Trigger Demo Script
# This script helps demonstrate the music trigger feature

echo "================================================"
echo "LinuxCutePet Music Trigger Feature Demo"
echo "================================================"
echo ""

# Check if PulseAudio is available
if command -v pactl &> /dev/null; then
    echo "✓ PulseAudio found - music trigger should work on this system"
    
    # Show current audio status
    echo ""
    echo "Current audio status:"
    echo "---------------------"
    pactl list sink-inputs | head -10
    
    if pactl list sink-inputs | grep -q "Sink Input"; then
        echo "🎵 Audio activity detected!"
    else
        echo "🔇 No audio activity detected"
    fi
    
else
    echo "⚠️  PulseAudio not found - music trigger may not work"
    echo "   Consider installing pulseaudio-utils package"
fi

echo ""
echo "To test the music trigger feature:"
echo "1. Run LinuxCutePet: dotnet run"
echo "2. Right-click pet → Settings → Trigger Volume → Select threshold"
echo "3. Play music or audio on your system"
echo "4. Wait 3 seconds - pet should start dancing!"
echo "5. Stop audio - pet should return to idle"
echo ""

# Check if audio files exist for testing
if [ -d "/usr/share/sounds" ]; then
    echo "Available system sounds for testing:"
    find /usr/share/sounds -name "*.wav" -o -name "*.ogg" | head -5
    echo ""
    echo "To play a test sound: aplay /usr/share/sounds/alsa/Front_Left.wav"
fi

echo "================================================"