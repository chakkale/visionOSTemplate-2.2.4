# Stereo 360 Panorama for visionOS

This package provides a solution for displaying stereo 360-degree panoramic images in visionOS using Unity's RealityKit integration.

## Overview

The traditional Unity VR approach for displaying stereo panoramas doesn't work directly with RealityKit on visionOS due to differences in shader compilation and rendering pipelines. This solution provides:

1. A custom shader compatible with RealityKit/MaterialX
2. C# script controllers for easy implementation
3. Tools to help with setup and debugging

## Setup Instructions

### 1. Project Configuration

Ensure your project is set up correctly for visionOS development:

- Use Unity 2022.3.18f1 or newer
- Install the visionOS Build Support module
- Install the PolySpatial package
- Set the XR Plugin Management > Apple visionOS > App Mode to "RealityKit with PolySpatial"

### 2. Preparing Your Stereo Panorama Texture

The shader expects a stereo panorama in vertical stack format:
- The image should be divided horizontally with left and right eye views stacked
- By default, the left eye view is expected on top, but this can be configured

Make sure your texture:
- Has appropriate dimensions (ideally square or 2:1 ratio)
- Is set to use linear color space
- Has appropriate import settings:
  - sRGB (Color Texture): Enabled
  - Texture Shape: 2D
  - Generate Mip Maps: Enabled
  - Wrap Mode: Repeat

### 3. Implementation Options

#### Option A: Quick Setup with Sample Script

1. Create an empty GameObject in your scene
2. Add the `SampleUsage.cs` script from the StereoVisionOS.Samples namespace
3. Assign your stereo panorama texture in the inspector
4. Configure the eye layout (Left Eye on Top) as needed
5. Check "Create On Start" for automatic initialization
6. Run the scene

#### Option B: Manual Setup

1. Create a sphere in your scene
2. Scale it appropriately (e.g., 50 units in radius)
3. Flip the normals inward (the `FlipSphereNormals` method in the scripts can help)
4. Add the `Stereo360PanoramaController.cs` component
5. Assign your panorama texture
6. Configure eye layout and debug options

#### Option C: Runtime Creation

Use the `Stereo360PanoramaSetup.cs` script to create the panorama sphere at runtime:

```csharp
var setup = gameObject.AddComponent<Stereo360PanoramaSetup>();
setup.panoramaTexture = myPanoramaTexture;
setup.leftEyeOnTop = true;
setup.CreatePanoramaSphere();
```

## Technical Details

### RealityKit Compatibility

The implementation works with RealityKit by:

1. Using a shader approach that's compatible with MaterialX translation
2. Handling stereo separation through script control rather than complex shader code
3. Avoiding operations that can't be properly translated from Unity to MaterialX

### Shader Graph Notes

If you prefer using a Shader Graph instead of the provided shader:

1. Create a new Universal RP Unlit Shader Graph
2. Implement the radial coordinate conversion
3. Handle the stereo eye separation based on XR eye index
4. Set up the texture sampling with edge fix logic
5. Export the shader graph for use in your materials

Refer to the included README in the ShaderGraphs folder for more details.

### Debugging

The shader includes a debug mode that:
- Shows UV coordinates visually
- Applies different colors for left and right eyes (red for left, blue for right)
- Displays a grid pattern to help visualize the mapping

Enable debug mode either through:
- The inspector on the controller component
- The `SetDebugMode(true)` method on the controller
- The context menu "Toggle Debug Mode" on the sample script

## Known Limitations

1. Edge seam artifacts may still be visible in some panoramas
2. Limited shading model (unlit only)
3. No direct control over individual eye UV offsets
4. Only compatible with vertical-stack stereo format

## Troubleshooting

### Common Issues

1. **Material appears black or missing:**
   - Ensure your RealityKit mode is properly set up in XR Plugin Management
   - Check that the shader was imported correctly
   - Verify texture is assigned and has proper import settings

2. **Texture shows in editor but not on device:**
   - Ensure RealityKit compatibility settings are correct
   - Check build settings for proper visionOS configuration

3. **Stereo effect is reversed:**
   - Toggle the "Left Eye On Top" setting

4. **Eyes are swapped:**
   - Toggle the "Left Eye On Top" setting

## Advanced Configuration

The `Stereo360PanoramaController` component provides methods for runtime control:

- `SetPanoramaTexture(Texture2D)` - Change the panorama texture
- `SetLeftEyeOnTop(bool)` - Configure the eye layout
- `SetDebugMode(bool)` - Enable/disable debug visualization

## License

This package is provided under the MIT License. Feel free to use, modify, and distribute as needed. 