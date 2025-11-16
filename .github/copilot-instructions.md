# AI Coding Agent Instructions for visionOSTemplate-2.2.4

## Project Overview
This is a **Unity 6 (6000.0.48f1)** project targeting **Apple visionOS** using **PolySpatial** for mixed reality experiences. The project combines XR Interaction Toolkit, AR Foundation, and custom architectural visualization systems with day/night mode crossfading and spatial UI navigation.

## Core Architecture

### PolySpatial & XR Integration
- **Platform**: visionOS development uses PolySpatial (v2.2.4) to bridge Unity content to RealityKit
- **Interaction Model**: Custom input bridging layer between Unity's XR Interaction Toolkit and visionOS spatial input
  - `SpatialTouchInputReader` handles primary/secondary touch and pinch gestures
  - `VisionOSFarCaster` and `PointNearCaster` route spatial interactions to XRI interactors
  - `PokeInteractorToggler` manages direct touch vs. indirect pinch interactions
- **Key Limitation**: PolySpatial doesn't support Material Property Blocks; use material instances directly (see `MaterialColorAffordanceReceiver`, `MaterialFloatAffordanceReceiver`)

### Room Management System
The core architectural visualization uses a crossfading sphere-based room system:

**Key Classes:**
- `RoomManager` (singleton): Orchestrates room transitions with DOTween-based crossfades
  - Manages sphere renderer opacity transitions between rooms
  - Handles button scale animations during room changes
  - Uses `RoomData` ScriptableObjects for room configuration
- `RoomData` (ScriptableObject): Defines day/night textures, room prefabs, and metadata
  - `GetTextureForMode(bool isNightMode)` returns appropriate texture
  - Backwards compatible with legacy `roomTexture` field
- `NightModeManager` (singleton): Syncs with `XRUIFlowManager`'s dark/light toggle
  - Crossfades textures on current room sphere when mode changes
  - Fires `OnNightModeChanged` event for system-wide coordination

**Pattern**: All room spheres use a custom shader with `_Opacity` and `_MainTex` parameters for crossfading.

**Manager Configuration in MainFinal**:
- `RoomManager.roomParent` = Camera Offset transform (rooms spawn as XR rig children)
- `NightModeManager` has companion `NightModeSetup` component on same GameObject
  - `autoSetupOnStart` = true, `createNightModeManager` = true
  - Both have `enableDebugLogs` = true for development
- Crossfade settings: 1s duration, InOutSine easing (consistent across both managers)

### UI Flow System
`XRUIFlowManager` (2400+ lines) manages the entire UI state machine:

**States**: `Start` → `MainMenu` → `MapView`
- Uses DOTween for scale-based show/hide animations (OutBack/InBack easing)
- **Interaction Debouncing**: `interactionDebounceTime` prevents rapid clicks (0.3s default)
- **Dark/Light Toggle**: 
  - Syncs sprite visibility (iconLight/iconDark) and button mesh color
  - Triggers `NightModeManager.SetNightMode()` for room texture updates
  - `syncColorTransitionWithSprites` keeps animations in sync
- **Map Navigation**: Maintains `currentRoomIndex` and `activeMapIndex` state
  - Updates `mapInfoText` and `roomInfoText` TMP components
  - Manages XRI interactable listeners for all buttons

**Critical Pattern**: All DOTween animations must call `DOTween.Kill(target)` in `OnDestroy()` to prevent memory leaks.

### Unity MCP Integration
This project has **Unity-MCP** installed (v0.21.0) for AI-assisted development:
- MCP server executable: `Library/mcp-server/osx-arm64/unity-mcp-server`
- Configuration: `.vscode/mcp.json` (stdio transport on port 8080)
- Enables AI tools to interact with Unity Editor (scene manipulation, asset management, script execution)
- See `Assets/com.IvanMurzak/AI Game Dev Installer/README.md` for full capabilities

## Key Dependencies

### Third-Party Packages
- **DOTween** (Demigiant): Animation library in `Assets/Plugins/Demigiant/DOTween/`
  - Used throughout for UI animations, room crossfades, and material property tweening
  - Prefer `DOTween.To()` for material properties (PolySpatial limitation)
- **TextMesh Pro**: UI text rendering (TMP_Text components in XRUIFlowManager)

### Unity Packages (manifest.json)
- `com.unity.polyspatial.visionos` (2.2.4): visionOS integration
- `com.unity.xr.interaction.toolkit` (3.0.8): XR input/interaction
- `com.unity.xr.hands` (1.4.1): Hand tracking
- `com.unity.render-pipelines.universal` (17.0.4): URP rendering
- `com.unity.inputsystem` (1.14.2): New Input System
- `com.ivanmurzak.unity.mcp` (0.21.0): AI development tools

## Development Workflows

### Testing & Building
- **No explicit test runner setup found** - use standard Unity Test Framework
- **Build Target**: visionOS (requires macOS + Xcode)
- **Editor Testing**: Use `EditorMouseLook.cs` for scene navigation without XR hardware
- **Play Mode**: XR rig required for proper interaction testing

### Scene Structure
- **Main Production Scene**: `MainFinal.unity` in `Assets/Wizio/`
- Legacy examples: `SampleScene.unity` and `SampleSceneUnbounded.unity` in `Assets/Scenes/`

**MainFinal Scene Architecture:**
- **XR Origin**: Standard visionOS XR rig with Camera Offset containing:
  - Main Camera (MainCamera tag)
  - XRI input system (Primary/Secondary Ray Interactors and Origins)
  - Camera parent is also `roomParent` for RoomManager - rooms spawn as children
  
- **Core Managers** (root level singletons):
  - `RoomManager`: Manages room prefab instantiation and crossfading (fadeDuration: 1s, InOutSine easing)
  - `NightModeManager`: Handles day/night texture switching with `NightModeSetup` helper
  - `UIManager`: Likely contains XRUIFlowManager for UI state machine
  
- **UI Hierarchy** (toggled by state):
  - `IntroTestButton`: Initial start screen with ButtonAffordance
  - `MainMenu`: Container for map/room controls (hidden by default)
    - MapButton, DarkLightToggle (day/night), MapInfo, RoomInfo displays
  - `MapMain`: Floor plan view system (hidden by default)
    - CloseButton, MapList with 6 map buttons (Map1Button - Map6Button)
    - Individual map containers: Patio, 1ºD, 1ºE, 2ºA, 3ºA, Rooftop
    - Each contains: map image + multiple MapTeleportButton instances for room hotspots
    
- **Room Data Assets**: 36 RoomData ScriptableObjects in `Assets/Wizio/Rooms/`
  - Organized by floor: `1D/`, `1E/`, `2A/`, `3A/`, `Patio/`
  - Naming: `E-{Floor}-C{Number}.asset` (e.g., E-2A-C01.asset)
  - Each references room prefab + day/night textures

### Custom Scripts Location
All project-specific code lives in `Assets/Scripts/`:
- Manager pattern (singletons with `Instance` property)
- Extensive use of `[SerializeField]` for Unity Editor exposure
- Debug logging controlled by `enableDebugLogs` fields

### Shader & Material Setup
- Custom shaders in `Assets/Shaders/` and `Assets/ShaderGraphs/`
- **visionOS Constraint**: Use URP shaders compatible with RealityKit/MaterialX
- Stereo 360 panorama support documented in `Assets/Documentation/Stereo360PanoramaForVisionOS.md`

## Conventions & Patterns

### Coding Style
- C# naming: PascalCase public, camelCase private, prefixed with `m_` in XRI samples
- Singleton pattern: `public static ClassName Instance { get; private set; }`
- Coroutines for async operations (room crossfades, mode transitions)
- `#pragma warning disable` for intentional unused fields (e.g., `animationDelay`)

### Unity-Specific Patterns
- **ScriptableObjects** for data assets (RoomData, material presets)
- **Extension methods** in `RoomManagerExtensions.cs` for manager augmentation
- **XRI Interactables**: Always pair `XRSimpleInteractable` with listener setup/teardown
- **Material Instances**: Create via `renderer.material` (auto-instantiates), never use shared materials with PolySpatial

### Error Prevention
1. **DOTween Cleanup**: Always call `DOTween.Kill()` in `OnDestroy()`
2. **Null Checks**: Validate references before use (common pattern: `if (obj == null) return;`)
3. **State Guards**: Use flags like `isTransitioning`, `isFading` to prevent re-entrancy
4. **PolySpatial Compatibility**: Avoid MaterialPropertyBlocks, use material instances

## File Organization
```
Assets/
├── Scripts/              # Core project logic (managers, UI, room system)
├── Scenes/               # Unity scenes
├── Materials/            # Material assets
├── Shaders/              # Custom HLSL shaders
├── ShaderGraphs/         # Shader Graph assets
├── Plugins/Demigiant/    # DOTween library
├── Samples/              # XRI and visionOS example code
├── Documentation/        # Setup guides (e.g., stereo panorama)
└── com.IvanMurzak/       # Unity-MCP installer
```

## When Making Changes

### Working with Room Data ScriptableObjects
**Location**: `Assets/Wizio/Rooms/` organized by floor (1D/, 1E/, 2A/, 3A/, Patio/)
**Count**: 36 RoomData assets total
- **Patio**: 1 asset (Patio.asset) 
- **1D Floor**: 9 assets (E-1D-C01 through E-1D-C09)
- **1E Floor**: 6 assets (E-1E-C01 through E-1E-C06)
- **2A Floor**: 12 assets (E-2A-C01 through E-2A-C12)
- **3A Floor**: 8 assets (E-3A-C01 through E-3A-C08)

Each RoomData asset contains:
- `roomName`: Display name for UI
- `dayTexture`: 360° equirectangular texture for day mode
- `nightTexture`: 360° equirectangular texture for night mode
- `roomPrefab`: Prefab containing inverted sphere mesh + teleport buttons
- `roomTexture`: Legacy field (use `dayTexture` instead)

### Modifying Room System
1. Update `RoomData` ScriptableObjects (not code) to change room textures
2. Ensure day/night textures are set; `GetTextureForMode()` handles fallback
3. Test crossfade timing in `RoomManager.fadeDuration` and `NightModeManager.crossfadeDuration`

### Adding New UI Flows
1. Extend `XRUIFlowManager.UIState` enum for new states
2. Follow pattern: store original scales in `Awake()`, animate with DOTween, restore in transitions
3. Add debouncing with `CanInteract()` helper to prevent spam

### XR Interaction Changes
- Samples in `Assets/Samples/XR Interaction Toolkit/3.0.8/visionOS/` show patterns
- Custom affordance receivers required for PolySpatial (material-based, not property blocks)
- Input bridging: modify `SpatialTouchInputReader` for new gesture types

**Button Pattern in MainFinal**:
- Every interactive button has child `ButtonAffordance` GameObject
- Uses `XRSimpleInteractable` for touch/pinch detection
- MapTeleportButton instances positioned on floor plan images as hotspots
- Text labels use TextMeshPro components
- Outlines used to highlight active/selected map buttons

### Working with AI Tools (Unity-MCP)
- Open `Window/AI Connector (Unity-MCP)` in Unity Editor to configure
- AI can read/modify GameObjects, assets, scripts, and call reflection methods
- Use `[Description]` attribute on methods/fields to guide AI understanding
