# LinuxCutePet Build Types

This project supports two different build approaches, each with their own advantages:

## 🎯 Quick Reference

| Feature | Self-Contained | Mod-Friendly |
|---------|-----------------|--------------|
| **File Size** | ~200MB | ~40MB executable + ~100MB assets |
| **Runtime Required** | ❌ No | ✅ .NET 8 Runtime |
| **Animation Modding** | ❌ Requires rebuild | ✅ Easy file replacement |
| **Distribution** | Single executable | Executable + Assets folder |
| **Startup Speed** | Fast | Fast |
| **Portability** | High | Medium |

## 🔧 Build Commands

### Self-Contained Builds (Traditional)
```bash
# All platforms
./build-all-platforms.sh

# Individual platforms
./build-linux.sh      # Linux only
./build-windows.sh     # Windows only  
./build-macos.sh       # macOS only
```

**Output**: `dist/` folder with large standalone executables

### Mod-Friendly Builds (Recommended for Modding)
```bash
# All platforms
./build-mod-friendly.sh

# Test the build
./test-mod-friendly.sh
```

**Output**: `builds/mod-friendly/` folder with smaller executables + external Assets

## 📁 File Structure Comparison

### Self-Contained Build Structure
```
dist/
├── linux-x64/
│   └── PetViewerLinux              # ~200MB executable (everything included)
├── windows-x64/
│   └── PetViewerLinux.exe          # ~200MB executable
└── macos-x64/
    └── PetViewerLinux              # ~200MB executable
```

### Mod-Friendly Build Structure
```
builds/mod-friendly/
├── linux-x64/
│   ├── PetViewerLinux              # ~40MB executable
│   ├── Assets/                     # ~100MB asset folder
│   └── [.NET libraries]            # Framework dependencies
├── windows-x64/
│   ├── PetViewerLinux.exe
│   ├── Assets/
│   └── [.NET libraries]
└── macos-x64/
    ├── PetViewerLinux
    ├── Assets/
    └── [.NET libraries]
```

## 🎨 When to Use Each Build Type

### Use Self-Contained Builds When:
- ✅ You want maximum portability
- ✅ Target system may not have .NET runtime
- ✅ You don't need to modify animations
- ✅ One-file distribution is preferred
- ✅ You're sharing with non-technical users

### Use Mod-Friendly Builds When:
- ✅ You want to customize animations
- ✅ You're comfortable installing .NET runtime
- ✅ You prefer smaller file sizes
- ✅ You want to create animation packs
- ✅ You're developing or experimenting

## 🚀 Getting Started

### For End Users
1. **Download self-contained build** for your platform
2. Run the executable directly (no installation needed)

### For Modders/Developers
1. **Download mod-friendly build** for your platform
2. Install .NET 8 Runtime: https://dotnet.microsoft.com/download/dotnet/8.0
3. Customize animations in the `Assets/` folder
4. Share your animation packs with others

## 🛠️ Development Workflow

### Building Both Types
```bash
# Build self-contained (for distribution)
./build-all-platforms.sh

# Build mod-friendly (for development/modding)
./build-mod-friendly.sh

# Test mod-friendly build
./test-mod-friendly.sh
```

### Project Configuration
The project is configured to support both build types through `PetViewerLinux.csproj`:

```xml
<!-- Assets excluded from embedding (enables mod-friendly builds) -->
<None Remove="Assets\**" />

<!-- Commented out: Would embed assets into executable -->
<!-- <AvaloniaResource Include="Assets\**" /> -->
```

The `GetAssetsDirectory()` method in `MainWindow.axaml.cs` automatically detects the executable location and loads external assets, making both build types work seamlessly.

## 📋 Troubleshooting

### Self-Contained Builds
- **Large file size**: Normal behavior, everything is included
- **Slow build time**: Normal, includes entire .NET runtime

### Mod-Friendly Builds
- **Missing .NET runtime**: Install from Microsoft's website
- **Assets not loading**: Ensure `Assets/` folder is next to executable
- **Permission errors**: Use `chmod +x` on Linux/macOS

## 🔍 Technical Details

### Asset Loading Method
Both build types use the same asset loading code:

```csharp
private string GetAssetsDirectory()
{
    // Get the directory where the executable is located
    string executablePath = AppContext.BaseDirectory;
    return System.IO.Path.Combine(executablePath, "Assets");
}
```

This ensures compatibility between embedded assets (self-contained) and external assets (mod-friendly).

### Build Configuration Differences

**Self-Contained**:
```bash
dotnet publish -r linux-x64 --self-contained -p:PublishSingleFile=true
```

**Mod-Friendly**:
```bash
dotnet publish -r linux-x64 --no-self-contained
```

The key differences:
- `--self-contained` vs `--no-self-contained`
- `PublishSingleFile=true` vs separate files
- Asset embedding vs external asset folder

---

Choose the build type that best fits your needs! 🚀
