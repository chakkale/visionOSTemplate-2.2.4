# Addressables Remote Content Setup Guide

## Overview
This guide explains how to set up remote content delivery for your visionOS app using Unity Addressables and GitHub Releases as a free hosting service. This allows you to keep your app under Apple's 4GB size limit while delivering high-resolution 360° panoramic textures on-demand.

## Benefits
- **Persistent Caching**: Content downloads once and stays cached
- **Automatic Updates**: Users get new content without app updates
- **Under 4GB**: Meets Apple's size requirements
- **Free Hosting**: GitHub Releases provides free, reliable hosting

---

## Step 1: Initial Setup in Unity

### 1.1 Run Addressables Configuration
1. Open Unity Editor
2. Go to `Tools > Addressables > Setup Remote Content Configuration`
3. This creates:
   - AddressableAssetSettings
   - Remote_Textures group for panoramic images
   - Profile for remote content delivery

### 1.2 Mark Panoramic Textures as Addressable
1. Go to `Tools > Addressables > Mark Panoramic Textures as Addressable`
2. This automatically finds all textures in `Assets/Wizio/Renders/` and adds them to the Remote_Textures group
3. Verify: Open `Window > Asset Management > Addressables > Groups`
4. You should see "Remote_Textures" group with all your panoramic images

### 1.3 Update RoomData Assets
For each RoomData asset in `Assets/Wizio/Rooms/`:
1. Select the RoomData asset
2. In the Inspector, find "Day/Night Textures - Addressables" section
3. Fill in the addressable addresses:
   - **dayTextureAddress**: Enter the filename without extension (e.g., "SG_Int_E-4D_360_C01")
   - **nightTextureAddress**: Enter the night texture filename (e.g., "SG_Int_E-4D_360_C01_Night")
4. The address must match exactly what you see in the Addressables Groups window

**Example:**
```
Day Texture Address: SG_Int_E-4D_360_C01
Night Texture Address: SG_Int_E-4D_360_C01_Night
```

### 1.4 Create Initial Download Scene
1. Create new scene: `File > New Scene`
2. Save as `DownloadScene.unity` in `Assets/Scenes/`
3. Add GameObject: `Create Empty` → Name it "DownloadManager"
4. Add Component: `InitialDownloadScene` script
5. Create UI (using Unity UI):
   ```
   Canvas (Screen Space - Overlay)
   ├── DownloadPanel
   │   ├── StatusText (TextMeshPro)
   │   ├── ProgressBar (Slider)
   │   ├── ProgressText (TextMeshPro)
   │   └── DownloadSizeText (TextMeshPro)
   └── ErrorPanel (initially disabled)
       ├── ErrorText (TextMeshPro)
       └── RetryButton
   ```
6. Assign all UI references in `InitialDownloadScene` inspector
7. Set `Main Scene Name` to "MainFinal"

### 1.5 Add TextureDownloadManager to MainFinal Scene
1. Open `MainFinal.unity`
2. Create Empty GameObject: Name it "TextureDownloadManager"
3. Add Component: `TextureDownloadManager` script
4. Enable debug logs if desired

### 1.6 Update Build Settings
1. Go to `File > Build Settings`
2. Add scenes in this order:
   - `DownloadScene` (index 0 - first to load)
   - `MainFinal` (index 1)
3. Click "Add Open Scenes" or drag them from Project window

---

## Step 2: Build Addressables Content

### 2.1 Configure Remote Load Path
1. Open `Window > Asset Management > Addressables > Profiles`
2. Select "Remote Content" profile
3. Find `RemoteLoadPath` variable
4. Update value to: `https://github.com/YOUR-USERNAME/YOUR-REPO/releases/download/v1.0.0`
   - Replace `YOUR-USERNAME` with your GitHub username
   - Replace `YOUR-REPO` with your repository name
   - Use a version tag like `v1.0.0` (you'll create this in GitHub)

### 2.2 Build Addressables
1. Open `Window > Asset Management > Addressables > Groups`
2. Click `Build > New Build > Default Build Script`
3. Wait for build to complete
4. This creates `ServerData` folder in your project root with structure:
   ```
   ServerData/
   └── VisionOS/  (or your build target)
       ├── catalog_*.json
       ├── catalog_*.hash
       └── [texture bundle files].bundle
   ```

---

## Step 3: Upload to GitHub Releases

### 3.1 Prepare Content for Upload
1. Navigate to your project's `ServerData/VisionOS/` folder
2. You should see:
   - Catalog files (JSON + hash)
   - Multiple `.bundle` files (one per texture or small groups)

### 3.2 Create GitHub Release
1. Go to your GitHub repository
2. Click `Releases` (right sidebar)
3. Click `Create a new release`
4. Fill in:
   - **Tag**: `v1.0.0` (must match what you set in RemoteLoadPath)
   - **Release title**: "Content Bundle v1.0.0"
   - **Description**: Optional description of content

### 3.3 Upload Bundle Files
1. In the release draft, scroll to "Attach binaries"
2. Drag and drop ALL files from `ServerData/VisionOS/`:
   - All `.bundle` files
   - Both catalog files (`.json` and `.hash`)
3. Click "Publish release"

### 3.4 Verify URLs
After publishing, each file will have a URL like:
```
https://github.com/YOUR-USERNAME/YOUR-REPO/releases/download/v1.0.0/catalog_2024.11.16.json
https://github.com/YOUR-USERNAME/YOUR-REPO/releases/download/v1.0.0/[hash].bundle
```

The `RemoteLoadPath` you set earlier should match the base URL (everything before the filename).

---

## Step 4: Build and Test

### 4.1 Build visionOS App
1. Go to `File > Build Settings`
2. Select `VisionOS` platform
3. Click `Build` (or `Build and Run` if device connected)
4. Unity will create an Xcode project

### 4.2 Test Download Flow
**First Launch:**
1. Launch the app
2. You should see the download scene
3. Progress bar shows download progress
4. Once complete, automatically transitions to MainFinal

**Subsequent Launches:**
1. App checks for updates
2. If content is cached, proceeds directly to MainFinal
3. No re-download unless you publish a new release

### 4.3 Clear Cache for Testing
If you need to test the download again:
1. Delete app from device
2. Reinstall
OR
1. Add a debug button in your app
2. Call `TextureDownloadManager.Instance.ClearCache()`
3. Restart app

---

## Step 5: Updating Content

### 5.1 When to Update
Update content when you:
- Add new rooms/textures
- Replace existing textures with higher quality versions
- Fix texture issues

### 5.2 Update Process
1. Make changes to textures in Unity
2. Update RoomData assets if needed
3. Rebuild addressables: `Window > Asset Management > Addressables > Groups > Build > New Build > Default Build Script`
4. Create new GitHub release with new version tag (e.g., `v1.0.1`)
5. Upload new `ServerData/VisionOS/` files to the new release
6. Update `RemoteLoadPath` in Addressables profiles to point to new version
7. Rebuild your app OR use Addressables' content update workflow (advanced)

### 5.3 Content Update Workflow (Advanced)
Unity Addressables supports updating content without rebuilding the entire app:
1. Use `Check for Content Update Restrictions` before building
2. Build `Update a Previous Build` instead of `Default Build Script`
3. Upload only changed bundles
4. Users get updates automatically on next launch

See Unity documentation for full details: https://docs.unity3d.com/Packages/com.unity.addressables@latest

---

## Troubleshooting

### Problem: Download fails with 404 error
**Solution:**
- Verify `RemoteLoadPath` matches your GitHub Release URL exactly
- Ensure release is published (not draft)
- Check all files uploaded successfully

### Problem: Textures not loading in app
**Solution:**
- Verify RoomData addresses match texture names in Addressables Groups
- Check TextureDownloadManager is in MainFinal scene
- Enable debug logs and check Unity Console

### Problem: App still over 4GB
**Solution:**
- Ensure you're building with textures marked as Remote, not Local
- Check Build Report: textures should NOT be in app bundle
- Verify textures are in Remote_Textures group, not Default Local Group

### Problem: Slow download on first launch
**Solution:**
- This is normal for 6.4GB of textures
- Consider:
  - Reducing texture resolution for remote delivery
  - Compressing textures (LZ4 compression set in Addressables Groups)
  - Showing estimated time in download UI

### Problem: Content updates not appearing
**Solution:**
- Users need to close and reopen app to check for updates
- Set `DisableCatalogUpdateOnStartup = false` in AddressableAssetSettings
- Consider adding "Check for Updates" button in app settings

---

## Free Hosting Alternatives to GitHub Releases

If GitHub Releases doesn't meet your needs:

### 1. **Cloudflare R2** (Recommended)
- **Pros**: 10GB free storage, no egress fees, fast CDN
- **Cons**: Requires account setup
- **Setup**: Create bucket, enable public access, upload files
- **URL format**: `https://[bucket-name].[account-id].r2.cloudflarestorage.com/`

### 2. **Google Firebase Storage**
- **Pros**: 5GB free, good reliability
- **Cons**: Egress costs after free tier
- **Setup**: Firebase console → Storage → Upload files
- **URL format**: `https://firebasestorage.googleapis.com/v0/b/[project].appspot.com/o/`

### 3. **AWS S3 Free Tier**
- **Pros**: Reliable, scalable
- **Cons**: Complex setup, egress costs
- **Free tier**: 5GB storage, 20,000 GET requests/month

### 4. **Backblaze B2**
- **Pros**: Very affordable, 10GB free
- **Cons**: Setup complexity
- **URL format**: `https://f[id].backblazeb2.com/file/[bucket]/`

---

## Best Practices

1. **Version Management**: Use semantic versioning (v1.0.0, v1.0.1, etc.)
2. **Testing**: Always test download flow before shipping
3. **Monitoring**: Track download failures in production
4. **Compression**: Use LZ4 (fast) or LZMA (smaller) compression in Addressables
5. **Preload Critical Textures**: Consider bundling 1-2 rooms locally for instant initial experience
6. **Error Handling**: Provide retry mechanism and offline fallback
7. **Analytics**: Track which textures are accessed most frequently

---

## File Checklist

Before deploying, verify these files exist:

**Unity Project:**
- ✅ `Assets/Scripts/AddressablesSetup.cs`
- ✅ `Assets/Scripts/TextureDownloadManager.cs`
- ✅ `Assets/Scripts/InitialDownloadScene.cs`
- ✅ `Assets/Scripts/RoomData.cs` (updated)
- ✅ `Assets/Scripts/RoomManager.cs` (updated)
- ✅ `Assets/Scripts/NightModeManager.cs` (updated)
- ✅ `Assets/Scenes/DownloadScene.unity`
- ✅ All RoomData assets have addressable addresses filled in

**Build Output:**
- ✅ `ServerData/VisionOS/catalog_*.json`
- ✅ `ServerData/VisionOS/catalog_*.hash`
- ✅ `ServerData/VisionOS/*.bundle` files (59+ files)

**GitHub:**
- ✅ Release created with correct version tag
- ✅ All bundle files uploaded
- ✅ Catalog files uploaded

---

## Support & Resources

- **Unity Addressables Documentation**: https://docs.unity3d.com/Packages/com.unity.addressables@latest
- **GitHub Releases**: https://docs.github.com/en/repositories/releasing-projects-on-github
- **visionOS Development**: https://developer.apple.com/visionos/

For questions or issues, check the Unity Console logs with debug logging enabled.
