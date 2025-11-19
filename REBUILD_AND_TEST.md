# Rebuild and Test Instructions

## What Was Fixed
The room switching issue on visionOS was caused by the `FindSphereRenderer` method failing to find the MeshRenderer in instantiated prefabs. This worked in the Unity Editor but failed in visionOS builds.

### Root Cause
- In visionOS/PolySpatial builds, material properties may not be accessible immediately after prefab instantiation
- The original code only checked `mr.material.HasProperty("_Opacity")` which would fail if materials weren't initialized
- No fallback logic existed

### Solution Applied
Made `FindSphereRenderer` and `FindSphereTransform` more robust:
1. Search by GameObject name first ("Sphere")
2. Check root GameObject's MeshRenderer
3. Try `sharedMaterial` before `material` (avoids creating instances unnecessarily)
4. Wrapped in try-catch for defensive material access
5. Added fallback to return first MeshRenderer if property check fails
6. Added extensive debug logging to diagnose future issues

## Steps to Rebuild and Test

### 1. Build Addressables
In Unity Editor:
```
Window > Asset Management > Addressables > Groups
Click "Build" > "New Build" > "Default Build Script"
```

Wait for build to complete (check console for "Build completed").

### 2. Upload New Bundles to GitHub Release
```bash
cd /Volumes/DA-MacExternal/DA/Documents/visionOSTemplate-2.2.4
./update-release.sh
```

Confirm when prompted to delete old files and upload new ones.

### 3. Build for visionOS
In Unity Editor:
```
File > Build Settings
- Select visionOS platform
- Click "Build" or "Build and Run"
```

Choose build location and wait for Xcode project generation.

### 4. Test in visionOS Simulator
In Xcode:
1. Open the generated Xcode project
2. Select visionOS Simulator as target
3. Click Run (Cmd+R)
4. Once app loads:
   - Open the map view
   - Click on different room hotspots
   - Verify rooms switch correctly (you should see different 360° panoramas)
   - Check Unity console logs for `[RoomManager] FindSphereRenderer` messages

### Expected Logs (Success)
```
[RoomManager] FindSphereRenderer searching in: E-2A-C07(Clone)
[RoomManager] Found 3 MeshRenderers in children
[RoomManager] Found renderer with _Opacity using sharedMaterial: Sphere
[RoomManager] CrossfadeToRoom started for: Main Bedroom
```

### If Still Failing
If you see "No MeshRenderer found" errors, check:
1. Are room prefabs actually being instantiated? (Should see "(Clone)" in hierarchy)
2. Do the prefabs have ANY MeshRenderer component?
3. Run `Assets > Reimport All` to ensure prefabs are up-to-date
4. Check if prefabs were corrupted during build process

### Verify in Editor First
Before building for visionOS:
1. Enter Play Mode in Unity Editor
2. Switch rooms - should work correctly
3. Check logs show renderer being found
4. If it works in editor but not on device, it's a PolySpatial/build-specific issue

## Notes
- The fix is backwards compatible - works in both Editor and visionOS builds
- Extensive logging added for future debugging
- No changes needed to RoomData ScriptableObjects or prefabs
