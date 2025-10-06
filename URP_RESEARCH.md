# Universal Render Pipeline (URP) Research - Realms of Eldor

**Date**: 2025-10-05
**Unity Version**: 2023.2.6f2 / Unity 6
**URP Version**: 17.2.0

---

## Executive Summary

The project **already has URP installed and configured** but it's **not activated** as the active render pipeline. This document covers URP research, current project status, and implementation strategy.

---

## Current Project Status

### ✅ URP Package Installed
- **Package**: `com.unity.render-pipelines.universal` version `17.2.0`
- **Location**: Packages/manifest.json line 18
- **Status**: Installed but NOT active

### ✅ URP Assets Exist
- **UniversalRP.asset**: `/Assets/Settings/UniversalRP.asset`
  - Configured as URP Pipeline Asset
  - 2D Renderer type (m_RendererType: 1)
  - References Renderer2D asset (GUID: 424799608f7334c24bf367e4bbfa7f9a)
  - HDR enabled, MSAA enabled, SRP Batcher enabled

- **Renderer2D.asset**: `/Assets/Settings/Renderer2D.asset`
  - Universal Renderer Data for 2D
  - 4 light blend styles configured (Multiply, Additive, Multiply with Mask, Additive with Mask)
  - Post-processing enabled
  - 2D lighting shaders configured

### ❌ NOT Activated
- **GraphicsSettings.asset** line 40: `m_CustomRenderPipeline: {fileID: 0}`
- **Issue**: URP asset exists but is not assigned as the active render pipeline
- **Solution**: Assign UniversalRP.asset to GraphicsSettings.m_CustomRenderPipeline

---

## Why URP for This Project?

### Benefits for 2D Strategy Games

1. **2D Lighting System**
   - Dynamic 2D lights with normal maps
   - Shadows for 2D sprites
   - Per-pixel lighting for enhanced visuals
   - 4 blend modes: Multiply, Additive, Multiply with Mask, Additive with Mask

2. **Performance**
   - SRP Batcher: Optimizes rendering by reducing CPU overhead
   - Better batching for 2D sprites
   - Lower draw calls compared to built-in pipeline
   - Faster rendering on all platforms

3. **2D-Specific Features**
   - **Pixel Perfect Camera**: Ensures crisp pixel art rendering at any resolution
   - **2D Renderer**: Optimized specifically for 2D games
   - **Shader Graph**: Visual shader creation for custom 2D effects

4. **Post-Processing**
   - Bloom, color grading, vignette
   - Chromatic aberration
   - Film grain
   - All optimized for URP

5. **Future-Proofing**
   - Unity is deprecating built-in render pipeline
   - URP is the recommended path forward
   - Better support in future Unity versions

### HOMM3-Style Strategy Game Use Cases

1. **Hero Glow Effects**: Use 2D lights to highlight selected heroes
2. **Spell Effects**: Dynamic lighting for spell visuals (fireballs, lightning)
3. **Day/Night Cycle**: Ambient lighting changes (if implemented)
4. **Terrain Highlights**: Subtle lighting to show movement range
5. **Battle Effects**: Dramatic lighting for combat animations

---

## URP 2D Setup Best Practices

### Recommended Settings for Strategy Games

#### UniversalRP Asset Settings

```yaml
# Rendering
m_RendererType: 1                    # 2D Renderer
m_MSAA: 1                            # No MSAA (pixel art doesn't benefit)
m_RenderScale: 1                     # Full resolution
m_HDRColorBufferPrecision: 0         # 32-bit for better gradients
m_UseSRPBatcher: 1                   # CRITICAL for performance

# Lighting
m_MainLightRenderingMode: 1          # Per Pixel
m_AdditionalLightsRenderingMode: 1   # Per Pixel
m_AdditionalLightsPerObjectLimit: 4  # Limit for performance

# Shadows (optional for 2D)
m_MainLightShadowsSupported: 1       # Enable if using dramatic lighting
m_SoftShadowsSupported: 0            # Not needed for 2D pixel art

# Quality
m_ColorGradingMode: 0                # Low Dynamic Range (LDR) for pixel art
m_ColorGradingLutSize: 32            # Standard size
```

#### Renderer2D Asset Settings

```yaml
# Transparency
m_TransparencySortMode: 0            # Default (Orthographic)
m_TransparencySortAxis: {x: 0, y: 1, z: 0}  # Sort along Y-axis

# Lighting
m_HDREmulationScale: 1               # Standard HDR emulation
m_LightRenderTextureScale: 0.5       # Half resolution for performance
m_MaxLightRenderTextureCount: 16     # Sufficient for strategy game

# Light Blend Styles (4 channels)
- Multiply         # Channel 0 - Global lighting
- Additive         # Channel 1 - Hero glows, spell effects
- Multiply + Mask  # Channel 2 - Targeted effects
- Additive + Mask  # Channel 3 - Special effects
```

---

## URP Asset Creation via Script

### Key Findings from Research

1. **ScriptableObject.CreateInstance** can create URP assets
2. **AssetDatabase.CreateAsset** saves them to disk
3. **Some properties are read-only** and cannot be set via script
4. **SerializedObject workaround** can modify read-only properties

### Limitations

- **Renderer Features**: Cannot be added programmatically easily
- **Shader references**: Some internal shaders cannot be assigned via script
- **Post-processing**: Volume profiles must be created separately

### Recommended Approach

✅ **Use existing assets** (already created in project)
✅ **Create activation tool** to assign to GraphicsSettings
✅ **Verify configuration** rather than recreate from scratch
❌ **Don't recreate assets** - existing ones are properly configured

---

## Implementation Strategy

### Phase 1: Activation Tool ✅ RECOMMENDED

Create editor script to activate existing URP configuration:

```csharp
[MenuItem("Realms of Eldor/Setup/Activate URP")]
public static void ActivateURP()
{
    // 1. Load existing URP asset
    var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
        "Assets/Settings/UniversalRP.asset");

    // 2. Assign to GraphicsSettings
    GraphicsSettings.renderPipelineAsset = urpAsset;

    // 3. Also assign to quality levels
    QualitySettings.renderPipeline = urpAsset;

    // 4. Save changes
    EditorUtility.SetDirty(GraphicsSettings.GetGraphicsSettings());
    AssetDatabase.SaveAssets();
}
```

**Advantages:**
- Uses existing, properly configured assets
- Simple, low-risk implementation
- No asset generation complexity
- Immediate activation

### Phase 2: Verification Tool

Create tool to verify URP setup:

```csharp
[MenuItem("Realms of Eldor/Setup/Verify URP Setup")]
public static void VerifyURPSetup()
{
    // Check if URP is active
    // Check if renderer is 2D
    // Check if SRP Batcher is enabled
    // Report findings in console
}
```

### Phase 3: Quality Level Configuration

Configure all quality levels to use URP:

```csharp
// Set URP for all quality levels
for (int i = 0; i < QualitySettings.names.Length; i++)
{
    QualitySettings.SetQualityLevel(i);
    QualitySettings.renderPipeline = urpAsset;
}
```

---

## Migration Checklist

### Shaders
- ✅ **Sprites**: Use URP/2D/Sprite-Lit-Default (already default)
- ✅ **UI**: uGUI shaders are URP compatible
- ⚠️ **Custom Shaders**: May need conversion to Shader Graph or URP shaders

### Materials
- ✅ **Sprite materials**: Automatically use URP 2D shaders
- ✅ **UI materials**: No changes needed
- ⚠️ **Custom materials**: May need shader reassignment

### Lighting
- ✅ **2D Lights**: Can be added after URP activation
- ✅ **Global Light**: Automatically created by 2D renderer
- ✅ **Light blend styles**: Already configured in Renderer2D asset

### Post-Processing
- ⏳ **Volume**: Need to create Global Volume GameObject
- ⏳ **Profile**: Create Volume Profile asset if post-processing desired
- ⏳ **Effects**: Configure bloom, color grading as needed

### Camera
- ⚠️ **Pixel Perfect Camera**: Consider adding for crisp pixel art
- ✅ **Orthographic**: Already configured for 2D
- ✅ **HDR**: Supported by URP

---

## Testing Strategy

### After Activation

1. **Visual Test**
   - Run existing scenes
   - Verify sprites render correctly
   - Check UI displays properly
   - Ensure no visual regression

2. **Performance Test**
   - Compare frame times before/after
   - Verify SRP Batcher is active (Frame Debugger)
   - Check draw call reduction

3. **Lighting Test** (if using 2D lights)
   - Add 2D Light to scene
   - Verify lighting renders
   - Test blend modes

4. **Build Test**
   - Create test build
   - Verify URP works in standalone build
   - Check shader compilation

---

## Common Issues and Solutions

### Issue 1: Pink Materials
**Cause**: Shaders not compatible with URP
**Solution**: Reassign materials to URP shaders

### Issue 2: No Rendering
**Cause**: URP asset not assigned to all quality levels
**Solution**: Use Phase 3 tool to set all quality levels

### Issue 3: Performance Regression
**Cause**: SRP Batcher disabled or inefficient batching
**Solution**: Enable SRP Batcher, check material batching

### Issue 4: Lighting Not Working
**Cause**: 2D Renderer not properly configured
**Solution**: Verify Renderer2D.asset is assigned to URP asset

---

## Performance Optimization

### SRP Batcher (CRITICAL)
```yaml
m_UseSRPBatcher: 1  # MUST be enabled
```
- Reduces CPU overhead by ~50%
- Groups draw calls with same shader variant
- Essential for good performance

### Dynamic Batching
```yaml
m_SupportsDynamicBatching: 0  # Disable
```
- Not beneficial with SRP Batcher
- Adds CPU overhead
- URP uses GPU instancing instead

### Light Texture Scale
```yaml
m_LightRenderTextureScale: 0.5  # Half resolution
```
- 2D lights rendered at half resolution
- Significant performance gain
- Minimal visual impact for strategy games

### MSAA
```yaml
m_MSAA: 1  # Disabled for pixel art
```
- Not beneficial for pixel art
- Adds GPU cost
- Use post-processing for smoothing if needed

---

## References

### Unity Documentation
- [URP 2D Setup](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/Setup.html)
- [2D Game Development in URP](https://docs.unity3d.com/6000.0/Documentation/Manual/2d-urp-landing.html)
- [URP Asset Settings](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/universalrp-asset.html)

### Community Resources
- Unity Discussions: "Create URP asset file from Editor Script"
- Stack Overflow: "How to change URP asset property via Script"
- Unity Forums: "Should I use URP for turn-based JRPG?"

---

## Conclusion

**The project is 95% ready for URP:**
- ✅ URP package installed (v17.2.0)
- ✅ URP assets created and configured
- ✅ 2D Renderer properly set up
- ❌ **Only missing: Activation in GraphicsSettings**

**Recommendation:**
1. Create simple activation tool (5 minutes)
2. Run activation tool (1 click)
3. Test scenes (5 minutes)
4. Commit changes

**Risk Level**: Very Low (existing assets are already configured)

**Expected Outcome**: Immediate performance improvement from SRP Batcher, foundation for future 2D lighting features.
