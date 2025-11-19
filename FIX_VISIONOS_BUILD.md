# Fix visionOS Build - Room Switching Issue

## Problem
The logs show the OLD error message without the new debug logging, meaning your visionOS build is using the old compiled code. The updated `RoomManager.cs` with improved renderer detection isn't included.

## Critical Logs Missing
You should see these logs (but you're not):
```
[RoomManager] FindSphereRenderer searching in: E-2A-C07(Clone)
[RoomManager] Found 3 MeshRenderers in children
[RoomManager] Found renderer with _Opacity using sharedMaterial: Sphere
```

Instead you only see:
```
RoomManager: No MeshRenderer with correct shader found in new room prefab!
```

This confirms the build is outdated.

## Solution: Rebuild Everything

### Step 1: Clean Unity Build Cache
In Unity Editor:
```
Edit > Preferences > Cache Server
Click "Clean Cache"

Then:
File > Build Settings
Click "Clean" (if available)
```

### Step 2: Rebuild Addressables
```
Window > Asset Management > Addressables > Groups
Click "Build" > "Clean Build" > "All"
Then: "Build" > "New Build" > "Default Build Script"
```

### Step 3: Upload New Addressables to GitHub
```bash
cd /Volumes/DA-MacExternal/DA/Documents/visionOSTemplate-2.2.4
./update-release.sh
```

### Step 4: Build visionOS from Scratch
In Unity Editor:
```
File > Build Settings
1. Select visionOS platform
2. Click "Build" (not "Build and Run")
3. Choose a NEW folder or delete the old build folder first
4. Wait for build to complete
```

### Step 5: Run in Xcode Simulator
1. Open the newly generated Xcode project
2. Clean build folder: Product > Clean Build Folder (Cmd+Shift+K)
3. Build: Product > Build (Cmd+B)
4. Run: Product > Run (Cmd+R)

### Step 6: Verify Logs
When you click a room button, you should now see:
```
[RoomManager] FindSphereRenderer searching in: [PrefabName](Clone)
[RoomManager] Found X MeshRenderers in children
[RoomManager] Found renderer with _Opacity using sharedMaterial: Sphere
```

Or at least:
```
[RoomManager] Found MeshRenderer on root: [PrefabName](Clone)
```

Or as last resort:
```
[RoomManager] No renderer with _Opacity found, using first MeshRenderer: [Name]
```

## If Still Failing After Rebuild

If you still see "No MeshRenderer found at all", the prefabs themselves might be the issue. This would mean:

### Check Prefab Structure
1. In Unity, open one of the room prefabs: `Assets/Wizio/Rooms/2A/E-2A-C07.prefab`
2. Check the hierarchy - does it have a GameObject with a MeshRenderer?
3. Select the MeshRenderer, check the Material - what shader is it using?
4. Does the material have an `_Opacity` property?

### Possible Prefab Issues:
- **No MeshRenderer component**: Prefabs might be empty or only contain teleport buttons
- **Wrong shader**: Material doesn't have `_Opacity` property
- **Build stripping**: PolySpatial might be removing components during build

### Emergency Fallback
If prefabs truly have no MeshRenderer, we need to change the room system architecture. Let me know and I'll modify it to work without requiring a sphere renderer (using a shared material approach instead).

## Quick Verification (Before Building)
Test in Unity Editor Play Mode first:
1. Enter Play Mode
2. Try switching rooms
3. Check Console for the new `[RoomManager] FindSphereRenderer` logs
4. If you see them in Editor, proceed with visionOS build
5. If you DON'T see them even in Editor, Unity didn't recompile - try restarting Unity
