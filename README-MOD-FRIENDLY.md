# LinuxCutePet - Mod-Friendly Desktop Pet

A cross-platform desktop pet application built with .NET 8 and Avalonia UI. This version is designed to be **mod-friendly**, allowing easy customization of animations without rebuilding the application.

## 🎮 Features

- **Cross-platform**: Runs on Linux, Windows, and macOS
- **Interactive**: Click, drag, and interact with your desktop pet
- **Audio reactive**: Responds to system audio activity (on supported platforms)
- **Mod-friendly**: Easy animation replacement and customization
- **Lightweight**: Framework-dependent builds for smaller file sizes

## 📦 Installation

### Requirements

All mod-friendly builds require the **.NET 8 Runtime** to be installed on your system.

#### Download .NET 8 Runtime:
- **Linux**: https://dotnet.microsoft.com/download/dotnet/8.0
- **Windows**: https://dotnet.microsoft.com/download/dotnet/8.0  
- **macOS**: https://dotnet.microsoft.com/download/dotnet/8.0

### Linux Installation

1. Install .NET 8 Runtime:
   ```bash
   # Ubuntu/Debian
   sudo apt update
   sudo apt install dotnet-runtime-8.0
   
   # Fedora/CentOS/RHEL
   sudo dnf install dotnet-runtime-8.0
   
   # Arch Linux
   sudo pacman -S dotnet-runtime-8.0
   ```

2. Download and extract the mod-friendly Linux build
3. Make executable and run:
   ```bash
   chmod +x PetViewerLinux
   ./PetViewerLinux
   ```

### Windows Installation

1. Download and install .NET 8 Runtime from Microsoft
2. Download and extract the mod-friendly Windows build
3. Double-click `PetViewerLinux.exe` to run

### macOS Installation

1. Download and install .NET 8 Runtime from Microsoft
2. Download and extract the mod-friendly macOS build (choose x64 or ARM64 based on your Mac)
3. Run from terminal:
   ```bash
   chmod +x PetViewerLinux
   ./PetViewerLinux
   ```

## 🎨 Animation Modding Guide

The mod-friendly version stores all animations in an external `Assets` folder, making it easy to customize your pet's appearance.

### Asset Structure

```
Assets/
├── 000.png                    # Fallback/default frame
├── idle/                      # Idle animation frames
│   ├── 000.png
│   ├── 001.png
│   └── ...
├── autoTriggered/             # Automatic animations
│   ├── meow/
│   ├── yawning/
│   ├── think/
│   └── move/
├── clickTriggered/            # Click interaction animations
│   ├── click_HEAD/
│   └── click_BODY/
├── dragTriggered/             # Drag interaction animations
├── rightDragTriggered/        # Right-click drag animations
├── menuTriggered/             # Menu action animations
│   ├── sleep/
│   ├── study/
│   ├── game/
│   └── ...
├── musicTriggered/            # Music/audio reactive animations
├── startup/                   # Application start animation
└── shutdown/                  # Application close animation
```

### Animation Frame Naming

All animation frames follow a strict naming convention:
- `000.png`, `001.png`, `002.png`, etc.
- Frames are played in numerical order
- PNG format with transparency support

### Animation Types

#### Simple Animations
- **idle/**: Default animation when pet is doing nothing
- **startup/**: Plays when application starts
- **shutdown/**: Plays when application closes

#### Interactive Animations
- **clickTriggered/click_HEAD/**: When clicking the pet's head
- **clickTriggered/click_BODY/**: When clicking the pet's body
- **dragTriggered/**: When dragging the pet with left mouse button
- **rightDragTriggered/**: When dragging with right mouse button

#### Complex Animations (Loop Structure)
Some animations use a 3-phase structure:
1. **Initial frames**: `000.png`, `001.png`, etc. (entrance)
2. **loop/**: Repeating middle section
3. **loopOut/**: Exit transition

Examples:
- `menuTriggered/sleep/000.png` → `menuTriggered/sleep/loop/` → `menuTriggered/sleep/loopOut/`
- `musicTriggered/000.png` → `musicTriggered/loop/` → `musicTriggered/loopOut/`

### Creating Custom Animations

1. **Prepare your images**:
   - Use PNG format with transparency
   - Consistent size (recommended: 128x128 or 256x256 pixels)
   - Name files sequentially: `000.png`, `001.png`, etc.

2. **Replace animations**:
   - Navigate to the `Assets` folder next to your executable
   - Replace existing PNG files in the appropriate folder
   - Or create new animation folders following the same structure

3. **Test your changes**:
   - Restart the application to load new animations
   - The pet will automatically use your custom frames

### Example: Creating a Custom Idle Animation

1. Create 8 frames of your pet breathing or blinking
2. Name them: `000.png`, `001.png`, `002.png`, ..., `007.png`
3. Replace the files in `Assets/idle/`
4. Restart the application

### Advanced Modding: Movement Animations

The `autoTriggered/move/` folder contains complex movement animations:

```
move/
├── horizontal/
│   ├── leftToRight/
│   │   ├── walk/
│   │   └── crawl/
│   └── rightToLeft/
│       ├── walk/
│       └── crawl/
└── vertical/
    ├── topToBottom/
    │   ├── fall.left/
    │   └── fall.right/
    └── bottomToTop/
        ├── climb.left/
        └── climb.right/
```

Each movement type supports the loop structure for smooth continuous motion.

## 🛠️ Building from Source

If you want to build your own mod-friendly version:

```bash
# Clone the repository
git clone <repository-url>
cd LinuxCutePet-main

# Build mod-friendly versions for all platforms
./build-mod-friendly.sh

# Builds will be in builds/mod-friendly/
```

## 📋 Comparison: Mod-Friendly vs Self-Contained

| Feature | Mod-Friendly | Self-Contained |
|---------|-------------|----------------|
| File size | ~40MB | ~200MB |
| .NET Runtime | Required | Included |
| Animation modding | ✅ Easy | ❌ Requires rebuild |
| Portability | Medium | High |
| Startup time | Fast | Fast |

## 🔧 Troubleshooting

### "The application cannot start because .NET runtime is not installed"
- Install .NET 8 Runtime from https://dotnet.microsoft.com/download/dotnet/8.0

### "Animation not loading"
- Check file names (must be `000.png`, `001.png`, etc.)
- Ensure PNG format with transparency
- Verify file permissions
- Restart the application after making changes

### Linux: "Permission denied"
```bash
chmod +x PetViewerLinux
```

### macOS: "App cannot be opened because it is from an unidentified developer"
```bash
# Right-click the app and select "Open" from the context menu
# Or use terminal:
xattr -d com.apple.quarantine PetViewerLinux
```

## 🎨 Community Mods

Share your custom animations with the community! Create animation packs and share the `Assets` folder structure.

### Creating an Animation Pack

1. Customize your `Assets` folder
2. Create a ZIP file with your custom animations
3. Include a README describing what's changed
4. Share with the community

### Installing Animation Packs

1. Download an animation pack
2. Backup your current `Assets` folder
3. Extract the pack's `Assets` folder to replace yours
4. Restart the application

## 📄 License

[Include your license information here]

## 🤝 Contributing

Contributions are welcome! Whether it's code improvements, new animations, or bug fixes.

1. Fork the repository
2. Create your feature branch
3. Make your changes
4. Test with the mod-friendly build system
5. Submit a pull request

---

**Enjoy your customizable desktop pet!** 🐱✨
