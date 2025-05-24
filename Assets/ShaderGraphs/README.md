# Stereo 360 Panorama Shader Graph for visionOS

This is a guide for creating a Stereo360Panorama Shader Graph that works with visionOS and RealityKit.

## How to Create the Shader Graph

1. In Unity, create a new Shader Graph by right-clicking in the Project panel and selecting Create > Shader > Universal Render Pipeline > Unlit Shader Graph.

2. Name it `Stereo360PanoramaShaderGraph`.

3. Open the Shader Graph and add the following properties:
   - `_MainTex` (Texture2D) - The stereo panorama texture
   - `_LeftEyeOnTop` (Boolean) - Whether the left eye view is in the top half of the texture
   - `_DebugMode` (Boolean) - Whether to show debug visualization

4. Add a Custom Function Node with the following code:
   ```
   float2 ToRadialCoords(float3 coords)
   {
       float3 normalizedCoords = normalize(coords);
       float latitude = acos(normalizedCoords.y);
       float longitude = atan2(normalizedCoords.z, normalizedCoords.x);
       float2 sphereCoords = float2(longitude, latitude) * float2(0.5 / 3.14159, 1.0 / 3.14159);
       return float2(0.5, 1.0) - sphereCoords;
   }
   ```

5. Implement the stereo eye separation logic using an If node based on the current XR eye index.

6. Implement the sampling of the panorama texture with edge fix logic.

7. Add debug visualization if enabled.

8. Connect the final color to the Fragment output.

## MaterialX Compatibility Notes

To ensure compatibility with MaterialX for visionOS:

1. Avoid using complex custom functions that can't be translated to MaterialX.
2. Use standard mathematical operations available in MaterialX.
3. Use only supported texture sampling methods.
4. Use standard blending operations for transitions.
5. Be aware that some Unity shader graph nodes may not translate directly to MaterialX.

## Using the Shader

Attach the `Stereo360PanoramaController.cs` script to a sphere or skybox GameObject and assign your stereo panorama texture.

The shader automatically handles stereo separation based on the VR headset's eye rendering. 