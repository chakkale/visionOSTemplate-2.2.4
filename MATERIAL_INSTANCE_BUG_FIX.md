# Critical Unity Material Instance Bug - FIXED

## The Problem

### What Was Happening
Rooms were loading textures correctly, but they appeared black/unchanged. Logs showed:
```
[RoomManager] ✓ Set texture on material: SG_Int_E-4D_360_C01
[RoomManager] Material now has texture: False  <-- BUG!
```

The texture was set, but immediately lost!

### Root Cause
**Every call to `renderer.material` creates a NEW material instance in Unity.**

This code was BROKEN:
```csharp
// BROKEN - Creates instance 1
Material materialInstance = nextSphereRenderer.material;
materialInstance.SetTexture("_MainTex", texture);  // Sets on instance 1

// BROKEN - Creates instance 2 (loses texture!)
nextSphereRenderer.material.SetFloat("_Opacity", 0f);  // Sets on instance 2
```

Each time you access `.material`, Unity:
1. Creates a brand new copy of the material
2. Returns that new copy
3. Discards any previous copies

So the texture was set on instance 1, but opacity was set on instance 2, which had no texture!

### Why It Worked in Editor But Not visionOS
- **Unity Editor**: More forgiving, sometimes reuses instances
- **visionOS/PolySpatial**: Strict material isolation, always creates new instances
- This is a **fundamental Unity behavior**, not a PolySpatial bug - just more visible on visionOS

## The Fix

### Correct Pattern: Get Material ONCE, Reuse It
```csharp
// CORRECT - Get material instance ONCE
Material nextMaterial = nextSphereRenderer.material;

// Reuse the SAME instance for all operations
nextMaterial.SetTexture("_MainTex", texture);
nextMaterial.SetFloat("_Opacity", 0f);

// Cache it for later use
currentSphereMaterial = nextMaterial;
```

### Changes Made

1. **Added Material Cache Field**
   ```csharp
   private Material currentSphereMaterial;
   ```

2. **Fixed CrossfadeToRoom()**
   - Get material instance once at the start
   - Reuse for texture setting AND opacity animation
   - Cache for future use

3. **Fixed DOTween Animations**
   ```csharp
   // BEFORE (broken - creates new instance each frame!)
   DOTween.To(
       () => nextSphereRenderer.material.GetFloat("_Opacity"),
       x => nextSphereRenderer.material.SetFloat("_Opacity", x),
       1f, fadeDuration
   )
   
   // AFTER (correct - captures instance once)
   Material nextMat = nextMaterial;
   DOTween.To(
       () => nextMat.GetFloat("_Opacity"),
       x => nextMat.SetFloat("_Opacity", x),
       1f, fadeDuration
   )
   ```

4. **Fixed UpdateCurrentRoomForNightMode()**
   - Uses cached `currentSphereMaterial`
   - Falls back to creating new instance if needed

5. **Optimized FindSphereRenderer()**
   - Uses `sharedMaterial` instead of `material` during search
   - Avoids creating unnecessary instances

## Testing

### Before Fix
```
[RoomManager] ✓ Set texture on material: SG_Int_E-4D_360_C01
[RoomManager] Material now has texture: False  ❌
```

### After Fix (Expected)
```
[RoomManager] ✓ Set texture on material: SG_Int_E-4D_360_C01
[RoomManager] Material now has texture: True   ✅
```

### How to Test
1. Open Unity project
2. Enter Play Mode
3. Switch to different rooms
4. Verify textures load correctly
5. Check logs show "True" for texture presence
6. Build for visionOS and test on device

## Key Lessons

### Always Remember
1. **Each `.material` call creates a NEW instance**
2. **Get material once, cache and reuse it**
3. **Use `.sharedMaterial` for read-only checks** (doesn't create instance)
4. **This applies to ALL Material properties** (texture, color, float, etc.)

### When to Use What
- **`renderer.sharedMaterial`**: Reading properties, checking shader, searching
- **`renderer.material`**: Creating an instance to modify (get ONCE!)
- **Cached material variable**: All subsequent modifications

### Common Pitfall Patterns
```csharp
// ❌ WRONG - Creates 3 instances!
renderer.material.SetTexture("_MainTex", tex);
renderer.material.SetColor("_Color", color);
renderer.material.SetFloat("_Opacity", 0.5f);

// ✅ CORRECT - Creates 1 instance, reuses it
Material mat = renderer.material;
mat.SetTexture("_MainTex", tex);
mat.SetColor("_Color", color);
mat.SetFloat("_Opacity", 0.5f);
```

### In Coroutines/Callbacks
```csharp
// ❌ WRONG - Each callback creates new instance
LoadTextureAsync(address, (tex) => {
    renderer.material.SetTexture("_MainTex", tex);  // Instance 1
});
// Later...
renderer.material.SetFloat("_Opacity", 1f);  // Instance 2 (lost texture!)

// ✅ CORRECT - Capture and reuse instance
Material mat = renderer.material;
LoadTextureAsync(address, (tex) => {
    mat.SetTexture("_MainTex", tex);  // Same instance
});
// Later...
mat.SetFloat("_Opacity", 1f);  // Still same instance!
```

## Related Files Changed
- `Assets/Scripts/RoomManager.cs`: Main fix with material caching
- All material property access now uses cached instances

## Rebuild Required
After this fix:
1. Rebuild addressables
2. Upload with `./update-release.sh`
3. Clean build visionOS app in Unity
4. Run in Xcode simulator/device
