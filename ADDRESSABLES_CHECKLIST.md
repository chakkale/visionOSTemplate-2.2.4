# Addressables Setup Checklist

Use this checklist to track your progress through the addressables setup process.

## Phase 1: Unity Setup ⚙️

### Initial Configuration
- [ ] Unity Addressables package installed (check Packages/manifest.json)
- [ ] Run `Window > Addressables Workflow` (opens helper window)
- [ ] Click "Setup Remote Content Configuration" in workflow window
- [ ] Verify: Window shows "Addressables Setup" with ✓

### Mark Assets as Addressable
- [ ] Click "Mark Panoramic Textures as Addressable" in workflow window
- [ ] Verify: Shows "59 textures marked as addressable ✓"
- [ ] Open `Window > Asset Management > Addressables > Groups`
- [ ] Verify: "Remote_Textures" group exists with 59 entries

### Populate RoomData
- [ ] Click "Auto-Populate RoomData Addresses" in workflow window
- [ ] Verify: Shows "36 RoomData addresses populated ✓"
- [ ] Click "Validate RoomData Addresses"
- [ ] Verify: "All 36 RoomData assets have valid addressable addresses!"

### Create Download Scene
- [ ] Create new scene: `File > New Scene`
- [ ] Save as `Assets/Scenes/DownloadScene.unity`
- [ ] Create UI hierarchy:
  ```
  Canvas (Screen Space - Overlay)
  ├── DownloadPanel
  │   ├── StatusText (TextMeshPro)
  │   ├── ProgressBar (UI Slider, value 0-1)
  │   ├── ProgressText (TextMeshPro, "0%")
  │   └── DownloadSizeText (TextMeshPro)
  └── ErrorPanel (disabled by default)
      ├── ErrorText (TextMeshPro)
      └── RetryButton (Button)
  ```
- [ ] Create empty GameObject: "DownloadManager"
- [ ] Add component: `InitialDownloadScene`
- [ ] Assign all UI references in inspector
- [ ] Set `Main Scene Name` = "MainFinal"
- [ ] Set `Skip Download In Editor` = true (for testing)

### Update MainFinal Scene
- [ ] Open `Assets/Wizio/MainFinal.unity`
- [ ] Create empty GameObject: "TextureDownloadManager"
- [ ] Add component: `TextureDownloadManager`
- [ ] Enable `Enable Debug Logs` (optional, for testing)
- [ ] Save scene

### Update Build Settings
- [ ] Open `File > Build Settings`
- [ ] Clear existing scenes (if any)
- [ ] Add `DownloadScene` - must be index 0
- [ ] Add `MainFinal` - must be index 1
- [ ] Verify scene order is correct

---

## Phase 2: GitHub Setup 🐙

### Prepare GitHub Repository
- [ ] Have GitHub account
- [ ] Have repository for your project (or create one)
- [ ] Note your username: `_____________`
- [ ] Note your repo name: `_____________`

### Configure Remote URL
- [ ] In workflow window, enter Remote Load Path:
  ```
  https://github.com/YOUR-USERNAME/YOUR-REPO/releases/download/v1.0.0
  ```
- [ ] Replace YOUR-USERNAME and YOUR-REPO with actual values
- [ ] Click "Update Remote Load Path"
- [ ] Verify: Success dialog appears

---

## Phase 3: Build Addressables 🔨

### Build Content
- [ ] In workflow window, click "Build Addressables Content"
- [ ] Wait for build to complete (2-5 minutes)
- [ ] Verify: "Build Complete" dialog appears
- [ ] Click "Open ServerData Folder"
- [ ] Verify folder contains:
  - [ ] `catalog_[date].json` file
  - [ ] `catalog_[date].hash` file
  - [ ] Multiple `.bundle` files (~59 files)

### Calculate Savings (Optional)
- [ ] Open `Tools > Addressables > Calculate Size Savings`
- [ ] Review size savings
- [ ] Note: Total texture size = `_____________`
- [ ] Confirm app will be under 4GB

---

## Phase 4: Deploy to GitHub 🚀

### Create Release
- [ ] In workflow window, click "Open GitHub Releases"
- [ ] Click "Create a new release"
- [ ] Set tag: `v1.0.0` (must match URL)
- [ ] Set title: "Content Bundle v1.0.0"
- [ ] Add description (optional)

### Upload Files
- [ ] Drag ALL files from `ServerData/VisionOS/` to release:
  - [ ] Both catalog files (.json and .hash)
  - [ ] All .bundle files
- [ ] Verify all files uploaded (should see 59+ files)
- [ ] Click "Publish release"

### Verify URLs
- [ ] After publishing, click on one .bundle file
- [ ] Copy URL - should look like:
  ```
  https://github.com/USERNAME/REPO/releases/download/v1.0.0/filename.bundle
  ```
- [ ] Verify base URL matches RemoteLoadPath you set

---

## Phase 5: Build & Test visionOS App 📱

### Build Project
- [ ] Open `File > Build Settings`
- [ ] Select platform: `VisionOS`
- [ ] Click `Build` (or `Build and Run`)
- [ ] Wait for Xcode project generation
- [ ] Open in Xcode

### First Launch Test
- [ ] Install app on visionOS device/simulator
- [ ] Launch app
- [ ] Expected: Download scene appears
- [ ] Expected: Progress bar shows download
- [ ] Expected: "Downloading content..." message
- [ ] Wait for download to complete
- [ ] Expected: Automatically transitions to MainFinal
- [ ] Expected: First room loads correctly

### Subsequent Launch Test
- [ ] Close app completely
- [ ] Relaunch app
- [ ] Expected: Download scene appears briefly
- [ ] Expected: "Content ready!" or similar
- [ ] Expected: Immediately proceeds to MainFinal
- [ ] Expected: No re-download

### Room Switching Test
- [ ] Navigate between rooms
- [ ] Expected: Rooms load quickly (from cache)
- [ ] Expected: Smooth crossfade transitions
- [ ] No download indicators

### Day/Night Toggle Test
- [ ] Toggle dark/light mode
- [ ] Expected: Texture crossfades smoothly
- [ ] Expected: Loads from cache (instant)
- [ ] Try multiple rooms

---

## Phase 6: Polish & Production 💎

### Debug Logging
- [ ] Test with debug logs enabled
- [ ] Check for any errors in Xcode console
- [ ] Disable debug logs for production:
  - [ ] TextureDownloadManager: `enableDebugLogs = false`
  - [ ] InitialDownloadScene: `enableDebugLogs = false`
  - [ ] RoomManager: `enableDebugLogs = false`
  - [ ] NightModeManager: `enableDebugLogs = false`

### Download Scene Polish
- [ ] Add app logo to download scene
- [ ] Style progress bar to match app design
- [ ] Add loading animation (optional)
- [ ] Test error panel (disconnect internet during download)
- [ ] Verify retry button works

### Error Handling Test
- [ ] Test with no internet connection
- [ ] Expected: Error message appears
- [ ] Expected: Retry button shown
- [ ] Connect to internet and retry
- [ ] Expected: Download succeeds

### Production Settings
- [ ] Set `skipDownloadInEditor = false` in InitialDownloadScene
- [ ] Verify RemoteLoadPath is correct production URL
- [ ] Build final release

---

## Phase 7: Update Workflow (Future) 🔄

When you need to update textures:

### Update Content
- [ ] Replace/add textures in `Assets/Wizio/Renders/`
- [ ] Update RoomData assets if needed
- [ ] Run `Mark Panoramic Textures as Addressable` again
- [ ] Run `Auto-Populate RoomData Addresses` again

### New Build
- [ ] Increment version: e.g., `v1.0.1`
- [ ] Update RemoteLoadPath to new version
- [ ] Build addressables content
- [ ] Create new GitHub release with new version tag
- [ ] Upload new ServerData files
- [ ] Update app and rebuild

---

## Troubleshooting Checklist 🔧

If something doesn't work:

### Downloads Fail
- [ ] Check RemoteLoadPath URL is correct
- [ ] Verify GitHub release is published (not draft)
- [ ] Verify all files uploaded successfully
- [ ] Check device has internet connection
- [ ] Check Xcode console for error messages

### Textures Don't Load
- [ ] Verify RoomData addresses match Addressables Groups
- [ ] Check TextureDownloadManager is in MainFinal scene
- [ ] Enable debug logs and check console
- [ ] Verify addressables were built

### App Still Over 4GB
- [ ] Check textures are in Remote_Textures group
- [ ] Verify you built addressables
- [ ] Check Build Report to confirm textures not in bundle
- [ ] Run Size Calculator to verify

### Slow Downloads
- [ ] Check texture compression in Addressables Groups
- [ ] Verify LZ4 compression is enabled
- [ ] Consider reducing texture resolution
- [ ] Test on different internet connections

---

## Quick Reference Commands 📝

All under `Tools > Addressables/` menu:

| Command | When to Use |
|---------|-------------|
| Setup Remote Content Configuration | First time setup |
| Mark Panoramic Textures | After adding new textures |
| Auto-Populate RoomData Addresses | After creating new rooms |
| Validate RoomData Addresses | Before building |
| Calculate Size Savings | To see size impact |

Also:
- `Window > Addressables Workflow` - Main workflow interface
- `Window > Asset Management > Addressables > Groups` - Manage groups
- `Window > Asset Management > Addressables > Profiles` - Edit URLs

---

## Success Criteria ✅

You've successfully set up addressables when:

- ✅ DownloadScene is Build Index 0
- ✅ TextureDownloadManager is in MainFinal
- ✅ All 36 RoomData have addresses populated
- ✅ All 59 textures marked as addressable
- ✅ ServerData folder contains ~59 bundle files
- ✅ All bundles uploaded to GitHub Release
- ✅ RemoteLoadPath matches GitHub Release URL
- ✅ App downloads content on first launch
- ✅ App uses cached content on subsequent launches
- ✅ Rooms and day/night switching work smoothly
- ✅ App is under 4GB

---

## Need Help? 🆘

Check these resources:

1. **QUICKSTART_ADDRESSABLES.md** - Quick reference guide
2. **ADDRESSABLES_SETUP_GUIDE.md** - Detailed documentation
3. **ADDRESSABLES_IMPLEMENTATION_SUMMARY.md** - Technical overview
4. Unity Console with debug logs enabled
5. Xcode console for runtime errors

---

**Last Updated**: November 2024  
**Version**: 1.0.0
