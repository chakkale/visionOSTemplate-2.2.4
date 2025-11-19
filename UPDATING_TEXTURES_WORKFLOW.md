# Updating Room Textures Workflow

## When You Want to Replace/Update Room Textures

### Method 1: Replace Existing Texture Files (Recommended - Easiest)

**If you want to replace a texture with a new one:**

1. **Replace the texture file directly in Unity:**
   - Find the old texture in `Assets/Wizio/Renders/` (or wherever your textures are)
   - Delete the old texture or overwrite it with the new file (keep the same filename)
   - Unity will automatically keep the same GUID
   - The addressable reference will still work!

2. **Rebuild addressables:**
   ```bash
   # In Unity: Window > Asset Management > Addressables > Groups
   # Click: Build > New Build > Default Build Script
   ```

3. **Upload to GitHub:**
   ```bash
   ./update-release.sh
   ```

4. **Done!** 
   - The app will download the new texture automatically
   - No code changes needed
   - Bundle hash will be different, so clients will re-download

---

### Method 2: Add Completely New Textures

**If you're adding brand NEW textures (not replacing existing ones):**

1. **Import new textures into Unity**
   - Place them in `Assets/Wizio/Renders/` or appropriate folder

2. **Mark them as Addressable:**
   - Select the texture in Unity
   - Check "Addressable" in the Inspector
   - Set the Group to: `Remote_Textures`
   - Set a meaningful Address (e.g., `SG_Int_NewRoom_360`)
   - Add label: `remote`

3. **Update RoomData assets (if needed):**
   - Open the RoomData ScriptableObject in Inspector
   - Update `dayTextureAddress` to your new texture address
   - Update `nightTextureAddress` if you have a night version
   - **Important:** Keep texture reference fields EMPTY (null)

4. **Rebuild addressables:**
   ```bash
   # In Unity: Window > Asset Management > Addressables > Groups
   # Click: Build > New Build > Default Build Script
   ```

5. **Upload to GitHub:**
   ```bash
   ./update-release.sh
   ```

---

### Method 3: Bulk Texture Update (Multiple Files)

**If you're updating many textures at once:**

1. **Prepare your new textures:**
   - Name them to match existing textures (to keep same addresses)
   - Or note which RoomData assets need address updates

2. **Import into Unity:**
   - Drag new textures into Unity project
   - Overwrite old ones OR import with new names

3. **If using new names, update addresses:**
   - Use Find & Replace in your code editor on RoomData .asset files
   - Or manually update each RoomData asset in Unity Inspector
   - Update `dayTextureAddress` and `nightTextureAddress` fields

4. **Rebuild and upload:**
   ```bash
   # In Unity: rebuild addressables
   ./update-release.sh
   ```

---

## Important: Texture Requirements

Your textures should be:
- **Format:** Equirectangular 360° panorama
- **Recommended resolution:** 8K (8192x4096) or 16K for high quality
- **File format:** PNG or JPG
- **Naming convention:** Use descriptive names like `SG_Int_FloorName_360_RoomNumber`

---

## Quick Reference: File Locations

```
Assets/
├── Wizio/
│   ├── Renders/              # Your texture files go here
│   └── Rooms/                # RoomData ScriptableObjects
│       ├── 1D/*.asset        # Floor 1D room configs
│       ├── 1E/*.asset        # Floor 1E room configs
│       ├── 2A/*.asset        # Floor 2A room configs
│       ├── 3A/*.asset        # Floor 3A room configs
│       └── Patio/*.asset     # Patio room configs
```

---

## Troubleshooting

**Q: I replaced a texture but the app still shows the old one**
- Clear addressables cache: Delete `Library/com.unity.addressables/`
- Rebuild addressables
- On device: Delete app and reinstall (clears downloaded cache)

**Q: New texture doesn't download**
- Check it's marked as Addressable with "remote" label
- Check the address matches what's in RoomData
- Verify it's in `Remote_Textures` group, not `Default Local Group`

**Q: Bundle size is huge**
- Make sure texture compression is enabled in Import Settings
- Check texture max size (8192 or 4096 is usually enough)
- Use JPG instead of PNG for smaller file size

---

## Pro Tip: Version Control

If you want to keep different versions:

1. **Name textures with versions:**
   - `SG_Int_D-3D_360_C01_v1.jpg`
   - `SG_Int_D-3D_360_C01_v2.jpg`

2. **Update address in RoomData:**
   - Change `dayTextureAddress` from `SG_Int_D-3D_360_C01_v1` to `SG_Int_D-3D_360_C01_v2`

3. **Keep old textures marked as addressable:**
   - Users on old app versions can still download them
   - Remove old ones after everyone updates

---

## Automated Workflow (Future Enhancement)

You could create a script to:
1. Watch a folder for new texture files
2. Automatically mark them as addressable
3. Auto-build and upload
4. Send notification when complete

Let me know if you want help creating this!
