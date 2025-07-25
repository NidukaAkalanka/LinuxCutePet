# Pet Activity System Guide

## Overview
The LinuxCutePet application now supports a reusable activity system that allows the pet to perform various activities with their own animations and context menu options.

## Current Activities
The application currently supports three activities:

1. **Sleep** - "Sleep" → "Awake"
2. **Study** - "Study" → "Stop studying"  
3. **Code** - "Code" → "Shutdown PC"

## Directory Structure
Each activity follows this directory structure under `Assets/menuTriggered/`:

```
Assets/menuTriggered/{activityName}/
├── 000.png           # Initial animation frames (optional)
├── 001.png           # Continue numbering as needed
├── 002.png
├── ...
├── loop/             # Looping animation frames
│   ├── 000.png
│   ├── 001.png
│   └── ...
└── loopOut/          # Exit animation frames
    ├── 000.png
    ├── 001.png
    └── ...
```

## Adding New Activities

### 1. Create Asset Directories
Create the directory structure for your new activity under `Assets/menuTriggered/{activityName}/`

### 2. Add Animation Frames
- **Initial Animation** (optional): Place numbered PNG files starting from `000.png` in the activity's root directory
- **Loop Animation** (required): Place numbered PNG files in the `loop/` subdirectory
- **Exit Animation** (required): Place numbered PNG files in the `loopOut/` subdirectory

### 3. Update Code
In `MainWindow.axaml.cs`, modify the `InitializePetActivities()` method to add your new activity:

```csharp
{
    "yourActivityName",
    new PetActivity(
        "yourActivityName",           // Internal identifier
        "Your Menu Text",             // Text shown in context menu to start
        "Your Stop Text",             // Text shown in context menu to stop
        "menuTriggered/yourActivityName", // Path to animation assets
        AnimationState.ActivityStart,    // Start state
        AnimationState.ActivityLoop,     // Loop state  
        AnimationState.ActivityEnd       // End state
    )
}
```

## How It Works

### Animation Flow
1. **Start**: Shows initial animation frames (if present), then transitions to loop
2. **Loop**: Continuously loops the animation until stopped
3. **End**: Plays exit animation, then returns to idle state

### Context Menu Behavior
- When no activity is active: Shows all available activities
- When an activity is active: Shows only the stop option for that activity
- Always shows "Exit" option

### User Interaction
- **Right-click**: Opens context menu
- **Left-click during activity**: Ignored (prevents accidental interruption)
- **Drag during activity**: Enabled (allows moving the pet without interrupting activity)
- **Right-click drag**: Triggers special right-drag animation (when not in activity)

## Technical Details

### PetActivity Class
```csharp
public class PetActivity
{
    public string Name { get; }              // Internal identifier
    public string StartMenuText { get; }     // Context menu start text
    public string StopMenuText { get; }      // Context menu stop text  
    public string AnimationPath { get; }     // Asset directory path
    public AnimationState StartState { get; } // Always ActivityStart
    public AnimationState LoopState { get; }  // Always ActivityLoop
    public AnimationState EndState { get; }   // Always ActivityEnd
}
```

### Animation States
- `ActivityStart`: Initial animation phase
- `ActivityLoop`: Continuous looping phase  
- `ActivityEnd`: Exit animation phase

The system automatically handles state transitions and animation loading based on the directory structure and naming conventions.
