# Removing Room Textures from Build

## Problem
Room panorama textures are still included in the build because:
1. RoomData ScriptableObjects have direct texture references (`dayTexture`, `nightTexture`, `roomTexture`)
2. These ScriptableObjects are referenced by the MainFinal scene
3. Unity automatically includes any assets referenced by scenes in the build

## Solution
Make RoomData assets downloadable via Addressables and remove direct texture references.

## Step-by-Step Instructions

### 1. Make RoomData Assets Addressable

**In Unity Editor:**
1. Go to `Tools > Addressables > Make RoomData Assets Addressable`
2. Keep the default group name: `Remote_Textures`
3. **Uncheck** "Dry Run"
4. Click "Make RoomData Addressable"
5. Wait for completion message

This will add all 36 RoomData assets to the addressables system so they can be downloaded.

### 2. Clear Direct Texture References

**In Unity Editor:**
1. Go to `Tools > Addressables > Clear RoomData Texture References`
2. Make sure all three options are checked:
   - ✓ Clear dayTexture
   - ✓ Clear nightTexture  
   - ✓ Clear roomTexture (legacy)
3. **Uncheck** "Dry Run"
4. Click "Clear References"
5. Wait for completion message

This removes the direct texture references that cause Unity to include textures in the build.

### 3. Update RoomManager Reference in MainFinal Scene

The RoomManager in MainFinal scene needs to load RoomData from addressables instead of having them assigned directly.

**Option A: Update via Unity Editor**
1. Open `Assets/Wizio/MainFinal.unity`
2. Find the RoomManager GameObject
3. In the Inspector, find the RoomManager component
4. Clear any direct references to RoomData assets in the inspector
5. Save the scene

**Option B: We'll update the code to load RoomData from addressables** (recommended)

### 4. Update XRUIFlowManager

The XRUIFlowManager also has references to RoomData that need to be loaded via addressables.

### 5. Rebuild Addressables

After making these changes:
1. `Window > Asset Management > Addressables > Groups`
2. `Build > New Build > Default Build Script`
3. Wait for build to complete
4. Run the update script: `./update-release.sh`

### 6. Verify Build Size

After rebuilding:
- The app build should be significantly smaller (no texture bundles)
- On first launch, the app will download:
  - All room textures (~1.2 GB)
  - All RoomData assets (~140 KB)
  - MainFinal scene bundle

## What Gets Downloaded

**Before:**
- DownloadScene only
- Textures downloaded separately

**After:**
- DownloadScene only
- Textures downloaded
- **RoomData assets downloaded**
- **MainFinal scene downloaded**

## Files Modified

- Added: `Assets/Scripts/Editor/ClearRoomDataTextureReferences.cs`
- Added: `Assets/Scripts/Editor/MakeRoomDataAddressable.cs`

## Next Steps

1. Run the two editor tools in order (Make Addressable → Clear References)
2. Update RoomManager/XRUIFlowManager to load RoomData from addressables
3. Rebuild addressables
4. Upload to GitHub releases
5. Test on device to confirm textures are NOT in build

## Troubleshooting

**If textures are still in the build:**
- Check that all RoomData assets have empty texture fields
- Make sure MainFinal scene doesn't have any direct texture references
- Verify RoomData assets are marked as addressable with "remote" label
- Check Build Report for which assets are being included
