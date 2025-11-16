# Quick Start: Addressables Remote Content

Follow these steps to get your remote content system up and running:

## ⚡ Quick Setup (5 minutes)

### 1. Install Package ✅
The Addressables package is already added to your `manifest.json`.
Unity will automatically install it when you return to the editor.

### 2. Configure Addressables
```
Tools > Addressables > Setup Remote Content Configuration
```
This creates all necessary settings and profiles.

### 3. Mark Textures as Addressable
```
Tools > Addressables > Mark Panoramic Textures as Addressable
```
This adds all 59 panoramic textures from `Assets/Wizio/Renders/` to remote delivery.

### 4. Auto-Populate RoomData
```
Tools > Addressables > Auto-Populate RoomData Addresses
```
This automatically fills in addressable addresses in all 36 RoomData assets based on their texture references.

### 5. Verify Setup
```
Tools > Addressables > Validate RoomData Addresses
```
Confirms all rooms have valid addressable addresses.

### 6. Update Remote URL
```
Window > Asset Management > Addressables > Profiles
```
- Select "Remote Content" profile
- Update `RemoteLoadPath` to your GitHub Release URL:
  ```
  https://github.com/YOUR-USERNAME/YOUR-REPO/releases/download/v1.0.0
  ```

### 7. Build Addressables
```
Window > Asset Management > Addressables > Groups
Build > New Build > Default Build Script
```
Wait for build to complete. This creates `ServerData/VisionOS/` folder.

### 8. Create Download Scene
1. Create new scene: `File > New Scene`
2. Save as `Assets/Scenes/DownloadScene.unity`
3. Add GameObject with `InitialDownloadScene` component
4. Create simple UI:
   - Canvas
   - StatusText (TextMeshPro)
   - ProgressBar (Slider)
   - ProgressText (TextMeshPro)
5. Assign UI references in inspector

### 9. Add TextureDownloadManager to MainFinal
1. Open `Assets/Wizio/MainFinal.unity`
2. Create Empty GameObject: "TextureDownloadManager"
3. Add `TextureDownloadManager` component

### 10. Update Build Settings
```
File > Build Settings
```
Add scenes in order:
1. DownloadScene (index 0)
2. MainFinal (index 1)

---

## 🚀 Deploy to GitHub Releases

### 1. Build Addressables (if not done yet)
```
Window > Asset Management > Addressables > Groups > Build > New Build > Default Build Script
```

### 2. Create GitHub Release
1. Go to your GitHub repo
2. Click **Releases** → **Create a new release**
3. Tag: `v1.0.0` (must match RemoteLoadPath)
4. Upload ALL files from `ServerData/VisionOS/`:
   - All `.bundle` files
   - `catalog_*.json`
   - `catalog_*.hash`
5. Click **Publish release**

### 3. Build visionOS App
```
File > Build Settings > VisionOS > Build
```

### 4. Test
- First launch: Downloads content with progress bar
- Subsequent launches: Uses cached content, loads instantly

---

## 📋 Verification Checklist

Before deploying, verify:

- ✅ Addressables package installed
- ✅ Remote_Textures group created with 59 textures
- ✅ All 36 RoomData assets have addresses filled in
- ✅ RemoteLoadPath points to your GitHub Release
- ✅ Addressables built (ServerData folder exists)
- ✅ All files uploaded to GitHub Release
- ✅ DownloadScene is Build Index 0
- ✅ MainFinal has TextureDownloadManager component

---

## 🧪 Testing Tips

### Test Download Flow
1. Enable `skipDownloadInEditor` in InitialDownloadScene for quick editor testing
2. Disable for actual device testing

### Clear Cache for Testing
Delete and reinstall app, OR add debug button:
```csharp
TextureDownloadManager.Instance.ClearCache();
```

### Monitor Downloads
Enable `enableDebugLogs` in:
- TextureDownloadManager
- InitialDownloadScene
- RoomManager
- NightModeManager

---

## ⚙️ Useful Menu Commands

All commands are under `Tools > Addressables/`:

| Command | Purpose |
|---------|---------|
| **Setup Remote Content Configuration** | Initial setup |
| **Mark Panoramic Textures as Addressable** | Add textures to remote group |
| **Auto-Populate RoomData Addresses** | Fill addresses automatically |
| **Validate RoomData Addresses** | Check for missing addresses |
| **Clear RoomData Addresses** | Reset all addresses |

---

## 🔧 Troubleshooting

### "TextureDownloadManager not found"
→ Add TextureDownloadManager component to MainFinal scene

### "Download failed: 404"
→ Check RemoteLoadPath matches GitHub Release URL exactly

### "Textures not loading"
→ Verify RoomData addresses match Addressables Group names

### App still over 4GB
→ Rebuild addressables, ensure textures in Remote_Textures group (not Default Local Group)

---

## 📚 Full Documentation

See `ADDRESSABLES_SETUP_GUIDE.md` for complete details on:
- Updating content
- Alternative hosting options
- Advanced content update workflow
- Best practices

---

## 🎯 What Happens Now?

### Before Addressables:
- All 59 high-res textures in app bundle
- 6.4GB app size
- ❌ Exceeds Apple's 4GB limit

### After Addressables:
- Textures hosted on GitHub Releases
- Downloaded on first launch
- Cached permanently
- ~200MB app size
- ✅ Under Apple's 4GB limit

---

## ⏱️ Expected Times

- **Setup**: 5 minutes
- **Build Addressables**: 2-5 minutes (depending on computer)
- **Upload to GitHub**: 5-15 minutes (depending on internet)
- **First Launch Download**: 5-20 minutes (depending on user's internet)
- **Subsequent Launches**: Instant (cached)

---

## 💡 Next Steps

1. Follow this quick start guide
2. Test thoroughly in Unity Editor
3. Build and test on actual visionOS device
4. Deploy to TestFlight for beta testing
5. Submit to App Store

Happy building! 🚀
