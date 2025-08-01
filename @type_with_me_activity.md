# Type With Me Activity

## Overview
The "Type with me" activity is a virtual pet activity where the pet appears to be typing alongside the user. This activity is perfect for coding sessions, writing, or any typing work where you want your virtual pet companion to be engaged in a similar activity.

## Activity Details
- **Activity Name**: `type_with_me`
- **Start Menu Text**: "Type with me"
- **Stop Menu Text**: "Stop typing"
- **Animation Path**: `menuTriggered/type_with_me`

## How to Use
1. **Start the Activity**: Right-click on the pet → Activities → "Type with me"
2. **Stop the Activity**: Right-click on the pet → "Stop typing"

## Animation Phases
The activity follows the standard three-phase animation pattern:

### 1. Start Phase (ActivityStart)
- Initial animation frames when the activity begins
- Located in: `Assets/menuTriggered/type_with_me/000.png` through `009.png`
- Shows the pet getting ready to type

### 2. Loop Phase (ActivityLoop)
- Continuous animation while the activity is active
- Located in: `Assets/menuTriggered/type_with_me/loop/`
- Shows the pet in a typing motion that repeats seamlessly
- Contains 10 frames (000.png through 009.png)

### 3. End Phase (ActivityEnd)
- Exit animation when the activity is stopped
- Located in: `Assets/menuTriggered/type_with_me/loopOut/`
- Shows the pet finishing up typing
- Contains 10 frames (000.png through 009.png)

## Technical Implementation
The activity is implemented using the standard `PetActivity` class with:
- Animation states: `ActivityStart`, `ActivityLoop`, `ActivityEnd`
- Integration with the context menu system
- Automatic animation frame loading from the asset directories

## Customization
To customize the typing animation:
1. Replace the PNG files in the respective directories
2. Maintain the numbered naming convention (000.png, 001.png, etc.)
3. Ensure all three directories (root, loop, loopOut) contain appropriate animations

## Use Cases
- Coding sessions
- Writing and documentation work
- General typing activities
- Creating a sense of companionship during work
- Motivational tool for productivity

This activity enhances the virtual pet experience by providing a relatable activity that mirrors common computer work, making the pet feel like an active participant in your workflow.