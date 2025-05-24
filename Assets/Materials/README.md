# Simple Frosty Glass UI Shader

This shader creates a lightweight frosted glass effect for 3D panel UI elements that blurs what's behind them. Optimized for visionOS and newer Unity versions.

## Usage

1. Create a UI panel or 3D object to use as your glass surface
2. Apply the `FrostyGlassUI` material to this object
3. Adjust the properties to achieve your desired look

## Properties

- **Blur Size (0-10)**: Controls how blurry the background appears (higher values = more blur)
- **Frost Intensity (0-1)**: Controls the opacity/intensity of the frost effect
- **Frost Color**: Sets the color and transparency of the glass (alpha controls overall transparency)
- **Noise Scale (1-50)**: Adjusts the scale of the frost pattern (higher values = finer pattern)

## Implementation

This shader uses the URP's `SampleSceneColor` function to sample the scene behind the object, which is more compatible with newer Unity versions than the older GrabPass technique. It applies:

- A 5-tap blur pattern for background blurring
- Simple procedural noise for the frosted texture
- Subtle rim effect to enhance the glass appearance
- Proper transparency blending

## Compatibility Notes

This shader is specifically designed for:
- Unity Universal Render Pipeline (URP)
- visionOS
- Modern hardware (M-series Apple chips)

## Performance

This version is optimized for mobile/AR performance by:
- Using fewer blur samples (5 instead of 9)
- Simplified noise calculations
- No extra texture requirements

If you need to improve performance further, try reducing the Blur Size parameter. 