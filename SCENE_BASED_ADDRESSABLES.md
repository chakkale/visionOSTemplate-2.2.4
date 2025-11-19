# Scene-Based Addressables Setup Guide

## Quick Setup (Automated)

### Use the Auto-Setup Tool:
1. **Window → Addressables → Auto Setup All Content**
2. Click **"Setup All Addressables"**
3. This will automatically:
   - Find all 3.4GB of textures in `Assets/Wizio/Renders/`
   - Mark all 36 room prefabs as addressable
   - Mark MainFinal scene as addressable
   - Create Remote_Textures_All, Remote_Rooms_All, Remote_MainScene groups
4. **Build**: Window → Addressables → Groups → Build → New Build
5. **Deploy**: Upload `ServerData/VisionOS/` to your web server

**Expected bundle size: ~3.5GB** (3.4GB textures + prefabs + scene)

---

## Manual Setup (If Needed)

## Overview
This guide sets up a simplified Addressables workflow where:
- **DownloadScene** (initial scene) → Downloads **MainFinal** scene + all dependencies
- **MainFinal** scene contains everything (room prefabs, materials, UI)
- Room textures are loaded on-demand but bundled for efficient download
- No manual texture-by-texture addressable configuration needed

## Benefits
✅ **Simpler**: One scene download instead of individual texture management  
✅ **Automatic Dependencies**: Unity automatically bundles all scene dependencies  
✅ **Easier Testing**: Scene works locally and remotely without texture loading issues  
✅ **Better Caching**: Entire scene + dependencies downloaded once  
✅ **No Sync Issues**: All materials/textures loaded as Unity scene data  

---

## Step 1: Remove Old Addressable Texture Groups

1. Open **Window → Asset Management → Addressables → Groups**
2. Delete or empty the `Remote_Textures` group (we'll let Unity auto-bundle with the scene)
3. Keep `Default Local Group` for local assets
4. Keep `Dependencies` group (auto-generated)

---

## Step 2: Make MainFinal Scene Addressable

1. In Project window, navigate to `Assets/Wizio/`
2. Select `MainFinal.unity`
3. In Inspector, check **Addressable** checkbox
4. Set the **Address** to: `MainFinal` (this must match the address in InitialDownloadScene.cs)
5. Assign to group: Create new group called `Remote_MainScene` with these settings:
   - **Build Path**: `ServerData/[BuildTarget]`
   - **Load Path**: `http://YOUR_SERVER_URL/[BuildTarget]`
   - **Build & Load Paths**: Set to "Remote"

---

## Step 3: Configure Scene Dependencies

Unity will automatically include all scene dependencies, but we can optimize by creating logical groups:

### Option A: Auto-Bundle Everything (Simplest)
- Let Unity automatically bundle all MainFinal scene dependencies
- No additional configuration needed
- Larger initial download but simpler setup

### Option B: Organize by Floor (Optimized)
Create these groups in Addressables:

**Group: Remote_MainScene**
- MainFinal.unity scene
- RoomManager, UI scripts (already in scene)
- SkyFull material (already in scene)

**Group: Remote_Textures_Patio**
- All Patio textures (day/night)
- E-Patio-C01 prefab

**Group: Remote_Textures_1D**
- All 1D floor textures
- E-1D-C01 through E-1D-C09 prefabs

**Group: Remote_Textures_1E**
- All 1E floor textures
- E-1E-C01 through E-1E-C06 prefabs

**Group: Remote_Textures_2A**
- All 2A floor textures
- E-2A-C01 through E-2A-C12 prefabs

**Group: Remote_Textures_3A**
- All 3A floor textures
- E-3A-C01 through E-3A-C08 prefabs

---

## Step 4: Update RoomManager to Load Textures On-Demand

The current RoomManager already loads textures via TextureDownloadManager. Keep this approach but simplify:

**Current Flow (KEEP THIS):**
1. DownloadScene downloads MainFinal scene + dependencies
2. MainFinal scene loads with all prefabs, materials, UI
3. When user teleports to room:
   - RoomManager instantiates room prefab (already in scene)
   - TextureDownloadManager loads texture via addressables
   - Material gets texture applied
   - Crossfade happens

**What Changes:**
- Remove texture pre-loading in InitialDownloadScene.cs ✅ (already done)
- Keep TextureDownloadManager as-is (on-demand loading)
- Scene loads immediately without waiting for all textures

---

## Step 5: Update Build Settings

1. **File → Build Settings**
2. **Scenes in Build**:
   - Add `DownloadScene.unity` (index 0) - keep in build
   - **REMOVE** `MainFinal.unity` from build (it's now addressable only)
3. Set `DownloadScene` as the first/default scene

---

## Step 6: Configure Addressables Build Settings

1. **Window → Asset Management → Addressables → Settings**
2. **Profile → VisionOS** (or create if missing):
   - `BuildTarget`: `[BuildTarget]`
   - `LocalBuildPath`: `[UnityEngine.AddressableAssets.Addressables.BuildPath]/[BuildTarget]`
   - `LocalLoadPath`: `{UnityEngine.AddressableAssets.Addressables.RuntimePath}/[BuildTarget]`
   - `RemoteBuildPath`: `ServerData/[BuildTarget]`
   - `RemoteLoadPath`: `http://YOUR_SERVER_URL/[BuildTarget]`

3. **Build Settings:**
   - ✅ Build Remote Catalog
   - ✅ Compress Bundles (LZ4 for speed)
   - Content Update: Disable for now (enable later for updates)

---

## Step 7: Build Addressables Content

1. **Window → Asset Management → Addressables → Groups**
2. **Build → New Build → Default Build Script**
3. This generates:
   - `ServerData/VisionOS/` folder with bundles
   - `catalog.json`, `catalog.hash`
   - Scene bundle + dependency bundles

---

## Step 8: Deploy to Server

Upload the entire `ServerData/VisionOS/` folder to your web server at the URL configured in RemoteLoadPath.

Example structure:
```
http://your-server.com/VisionOS/
├── catalog.json
├── catalog.hash
├── mainfinal_scene_all.bundle        # MainFinal scene
├── remote_textures_patio_assets_all.bundle
├── remote_textures_1d_assets_all.bundle
├── remote_textures_1e_assets_all.bundle
├── remote_textures_2a_assets_all.bundle
├── remote_textures_3a_assets_all.bundle
└── ...
```

---

## Step 9: Update InitialDownloadScene Inspector

1. Open `DownloadScene.unity`
2. Select the GameObject with `InitialDownloadScene` script
3. **Settings:**
   - `Main Scene Address`: `MainFinal` (must match addressable key)
   - `Enable Debug Logs`: ✅ (for testing)
   - `Skip Download In Editor`: ✅ (for faster iteration in Unity Editor)

---

## Step 10: Test Workflow

### Test in Unity Editor (Local Mode):
1. Play `DownloadScene.unity`
2. Should skip download (skipDownloadInEditor = true)
3. Should load MainFinal scene locally from Assets folder
4. Test room switching - textures load from addressables

### Test in Unity Editor (Addressables Mode):
1. Set `skipDownloadInEditor = false` in InitialDownloadScene
2. **Window → Asset Management → Addressables → Groups**
3. **Play Mode Script**: Set to "Use Existing Build"
4. Play `DownloadScene.unity`
5. Should load MainFinal scene from addressables bundle
6. Check console for download progress

### Test on visionOS Simulator:
1. Build to Xcode with visionOS target
2. Run on Simulator
3. Should download MainFinal scene + textures on first launch
4. Subsequent launches use cached content
5. Room switching loads textures on-demand

---

## Step 11: Optimize Content Updates

For future updates without full re-download:

1. **Window → Asset Management → Addressables → Groups**
2. **Build → Update a Previous Build**
3. Select previous build's `addressables_content_state.bin`
4. This creates delta bundles with only changed content
5. Users download only what changed

---

## Troubleshooting

### MainFinal scene not loading
- Check address matches exactly: `MainFinal`
- Verify scene is marked addressable
- Check RemoteLoadPath URL is accessible
- Look for "Failed to load scene" errors in console

### Textures still not showing in visionOS
- This is the PolySpatial sync issue (separate from addressables)
- The new aggressive sync workarounds in RoomManager should help
- Check that textures are actually loaded (look for "Texture loaded successfully" logs)

### Download size is huge
- Check texture compression: Use ASTC 4x4 or 6x6 for visionOS
- Consider splitting by floor (Option B above)
- Enable "Compress Bundles" in Addressables settings

### Scene loads but objects missing
- Check that all prefabs referenced in scene are also addressable OR in the scene
- Verify material references aren't broken
- Check for "Missing (Material)" or "Missing (Texture2D)" in scene

---

## Key Files Modified

### InitialDownloadScene.cs
✅ Simplified to download MainFinal scene + dependencies  
✅ Removed room texture pre-loading  
✅ Now downloads entire scene in one operation  

### RoomManager.cs
✅ Enhanced with PolySpatial sync workarounds  
✅ Still loads textures on-demand via TextureDownloadManager  
✅ No changes needed for scene-based approach  

### TextureDownloadManager.cs
✅ Keep as-is for on-demand texture loading  
✅ Textures loaded when room is entered  

---

## Summary

**Old Approach:**
- Manual addressable setup for each texture
- Complex pre-loading in DownloadScene
- Texture-by-texture download tracking
- Easy to miss textures or get configuration wrong

**New Approach:**
- MainFinal scene is one addressable
- Unity auto-bundles all dependencies
- DownloadScene downloads scene + deps
- Textures load on-demand when entering rooms
- Much simpler, less error-prone

**Result:**
- ✅ Simpler setup
- ✅ Automatic dependency management
- ✅ Better caching
- ✅ Easier updates
- ✅ Should fix PolySpatial sync issues (materials/textures loaded as scene data)
