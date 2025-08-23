# Night Mode System for visionOS Template

## Overview

The Night Mode System allows each room in your visionOS architectural visualization app to have different textures for day and night modes. Users can toggle between day and night using the existing DarkLightToggle button in the MainMenu.

## Components Added

### 1. Enhanced RoomData ScriptableObject
- **Day Texture**: Set the daylight texture for the room
- **Night Texture**: Set the nighttime texture for the room  
- **Backwards Compatibility**: Existing `roomTexture` field works as day texture if `dayTexture` is not set
- **Automatic Mode Detection**: Automatically selects appropriate texture based on current lighting mode

### 2. NightModeManager
- **Singleton Component**: Manages the global night mode state
- **Automatic Integration**: Monitors the XRUIFlowManager's dark/light toggle
- **Room Texture Updates**: Automatically updates current room texture when mode changes
- **Event System**: Provides events for other systems to respond to night mode changes

### 3. NightModeSetup
- **Auto Configuration**: Automatically sets up the night mode system in your scene
- **Validation Tools**: Context menu options to validate and debug the setup
- **RoomData Analysis**: Lists which rooms have night mode support

### 4. Updated RoomManager
- **Smart Texture Loading**: Automatically loads appropriate texture based on current mode
- **Seamless Integration**: Works with existing room transition system
- **Night Mode Updates**: Responds to night mode changes for current room

### 5. Updated XRUIFlowManager
- **Night Mode Notifications**: Notifies NightModeManager when dark/light mode is toggled
- **Seamless Integration**: Works with existing UI toggle system

## Setup Instructions

### Automatic Setup (Recommended)

1. **Scene Setup**: The NightModeManager and NightModeSetup components have been added to your MainFinal scene
2. **Validation**: Use the context menu on NightModeSetup to validate your setup
3. **Add Night Textures**: Configure night textures in your RoomData assets (see below)

### Manual Setup

If you need to set up the system in other scenes:

1. Create an empty GameObject named "NightModeManager"
2. Add the `NightModeManager` component
3. Add the `NightModeSetup` component
4. Use the "Setup Night Mode System" context menu option

### Adding Night Textures to Rooms

1. **Open RoomData Assets**: Navigate to your RoomData ScriptableObject assets
2. **Set Day Texture**: Assign the daylight texture to the "Day Texture" field
3. **Set Night Texture**: Assign the nighttime texture to the "Night Texture" field
4. **Save**: Save the asset

**Note**: If you don't set a night texture, the room will use the day texture for both modes.

## Usage

### For Users
- **Toggle Mode**: Click the DarkLightToggle button in the MainMenu
- **Visual Feedback**: The UI button changes appearance (sun/moon icons)
- **Room Updates**: Current room automatically updates to appropriate texture
- **Seamless Experience**: Works with all room navigation and transitions

### For Developers

#### Check Night Mode State
```csharp
bool isNight = NightModeManager.Instance.IsNightMode();
```

#### Listen for Night Mode Changes
```csharp
private void Start()
{
    if (NightModeManager.Instance != null)
    {
        NightModeManager.Instance.OnNightModeChanged += OnNightModeChanged;
    }
}

private void OnNightModeChanged(bool isNightMode)
{
    Debug.Log($"Night mode is now {(isNightMode ? "ON" : "OFF")}");
}
```

#### Manually Toggle Night Mode
```csharp
NightModeManager.Instance.ToggleNightMode();
```

## Debugging and Validation

### Context Menu Options (NightModeSetup)

1. **Setup Night Mode System**: Automatically configures the system
2. **Validate Night Mode Setup**: Checks if all components are properly configured
3. **List RoomData Night Mode Support**: Shows which rooms have night textures

### Context Menu Options (NightModeManager)

1. **Toggle Night Mode**: Test the night mode toggle
2. **Force Day Mode**: Set to day mode regardless of UI state
3. **Force Night Mode**: Set to night mode regardless of UI state

### Console Logs

Enable debug logs in NightModeManager and NightModeSetup to see detailed information about:
- Night mode state changes
- Room texture updates
- Component initialization
- Error conditions

## Technical Details

### Integration Points

1. **XRUIFlowManager**: Modified to notify NightModeManager when dark/light mode is toggled
2. **RoomManager**: Enhanced to load appropriate textures based on night mode
3. **RoomData**: Extended with day/night texture fields and helper methods

### Performance Considerations

- **Texture Memory**: Having both day and night textures increases memory usage
- **Texture Switching**: Uses Unity's material.SetTexture() for instant switching
- **No Additional Rendering**: Night mode only changes textures, not lighting or shaders

### Backwards Compatibility

- **Existing RoomData**: Works without modification (uses roomTexture as day texture)
- **Existing UI**: DarkLightToggle works exactly as before
- **Existing Navigation**: All room navigation features remain unchanged

## Troubleshooting

### Night Mode Not Working

1. **Check Components**: Use "Validate Night Mode Setup" context menu
2. **Check Console**: Look for error messages in the console
3. **Check RoomData**: Ensure night textures are assigned
4. **Check Scene References**: Verify DarkLightToggle exists in MainMenu

### Textures Not Changing

1. **Check Current Room**: Night mode only affects the currently active room
2. **Check Texture Assignment**: Verify night textures are assigned in RoomData
3. **Check Material Properties**: Ensure room materials use _MainTex property
4. **Manual Test**: Use "Force Night Mode" context menu option

### Performance Issues

1. **Texture Compression**: Ensure textures are properly compressed
2. **Texture Resolution**: Consider using lower resolution night textures if needed
3. **Memory Management**: Monitor texture memory usage in Profiler

## Future Enhancements

Potential areas for expansion:

1. **Lighting Changes**: Modify scene lighting along with textures
2. **Shader Properties**: Animate shader properties for smoother transitions
3. **Time-based Mode**: Automatically switch based on real-world time
4. **Multiple Time Periods**: Support for dawn, day, dusk, night modes
5. **Per-Room Settings**: Allow rooms to override global night mode settings

## File Locations

- **Scripts**: `Assets/Scripts/`
  - `NightModeManager.cs`
  - `NightModeSetup.cs` 
  - `RoomData.cs` (enhanced)
  - `RoomManager.cs` (enhanced)
  - `XRUIFlowManager.cs` (enhanced)

- **Scene Setup**: `Assets/Wizio/MainFinal.unity`
  - NightModeManager GameObject with required components

## Support

If you encounter issues:

1. Check the Unity Console for error messages
2. Use the validation tools provided in the context menus
3. Ensure all required components are present in the scene
4. Verify RoomData assets have appropriate textures assigned
