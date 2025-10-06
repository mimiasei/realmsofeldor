# Animated Tiles Setup Guide - Realms of Eldor

**Date**: 2025-10-05
**Use Case**: Animated water with 21 frames

---

## Overview

Unity's **Animated Tile** system (from the `2D Tilemap Extras` package) allows you to create frame-by-frame animations for tiles. This is perfect for animated water, lava, torches, etc.

**Package Status**: ✅ Already installed (`com.unity.2d.tilemap.extras: 5.0.1`)

---

## Step-by-Step Setup: Animated Water Tile

### Step 1: Prepare Your Water Animation Frames

**What you need:**
- 21 sprite frames for water animation
- Sprites should be imported as **Multiple** sprites (sliced)
- All frames should be the same size (e.g., 32x32, 64x64, 128x128)

**Import Settings:**
1. Select your water animation sprite sheet in Project window
2. Inspector → Sprite Mode: **Multiple**
3. Inspector → Pixels Per Unit: Match your tile size (e.g., 128)
4. Click **Sprite Editor** → Slice into 21 frames
5. Click **Apply**

---

### Step 2: Create an Animated Tile Asset

**In Unity Editor:**

1. **Right-click** in Project window → `Assets/Data/Tiles/` (create folder if needed)
2. Select **Create → 2D → Tiles → Animated Tile**
3. Name it: `WaterAnimatedTile`

**Inspector Configuration:**

```
Animated Tile Settings:
├─ Number of Animated Sprites: 21
├─ Minimum Speed: 1.0
├─ Maximum Speed: 1.0
├─ Start Time: 0
├─ Start Frame: 0
└─ Tile Animation Flags:
   └─ (None needed - loop automatically)
```

---

### Step 3: Assign Sprite Frames

**In the Animated Tile Inspector:**

1. **Expand** "Animated Sprites" array (shows 21 slots)
2. For each slot (0-20):
   - Click **Select** button (or drag sprite from Project)
   - Choose the corresponding water frame sprite
3. **Order matters!** Frame 0 → Frame 20 in sequence

**Quick Method:**
- Select all 21 water sprites in Project window
- Drag them into the Animated Tile asset
- Unity auto-assigns them to slots

---

### Step 4: Assign to TerrainData

**Update your Water TerrainData ScriptableObject:**

1. Find `Assets/Data/Terrain/WaterTerrainData.asset`
2. Inspector → **Tile Variants** array
3. **Remove** existing static water tile (if any)
4. **Add** WaterAnimatedTile to slot 0
5. Set **Size: 1** (only 1 variant = the animated tile)

**Result:**
```
WaterTerrainData:
├─ Terrain Type: Water
├─ Tile Variants (Size: 1)
│  └─ [0] WaterAnimatedTile
├─ Minimap Color: Blue
└─ Movement Cost: N/A (water is impassable)
```

---

### Step 5: Test in Scene

**How to verify:**

1. **Play** the AdventureMap scene
2. Water tiles should now be **animated**!
3. All 21 frames should loop continuously
4. Animation speed: ~21 frames per second (adjustable)

**If water doesn't animate:**
- Check Console for errors
- Verify WaterTerrainData has WaterAnimatedTile assigned
- Ensure MapRenderer has WaterTerrainData in terrainDataArray
- Confirm sprites are correctly ordered in AnimatedTile

---

## Advanced Configuration

### Animation Speed

**Constant Speed** (synchronized water):
```
Minimum Speed: 1.0
Maximum Speed: 1.0
```

**Variable Speed** (natural variation):
```
Minimum Speed: 0.8
Maximum Speed: 1.2
```
Each water tile will animate at a slightly different speed, creating a more natural look.

### Start Frame Randomization

**Problem**: All water tiles start on frame 0 → synchronized waves
**Solution**: Unity randomizes start frame automatically when `Start Frame = 0` and `Start Time = 0`

If you want **all tiles synchronized**:
```
Start Frame: 0 (specific frame)
Start Time: 0
```

If you want **randomized/offset waves**:
- Unity handles this automatically with AnimatedTile
- Each tile instance picks a random start time

---

## Multiple Water Variants (Optional)

**If you want different water animations:**

1. Create **multiple** AnimatedTile assets:
   - `WaterAnimatedTile_Calm` (slow animation)
   - `WaterAnimatedTile_Rough` (fast animation)
   - `WaterAnimatedTile_Deep` (different color)

2. Assign **all variants** to WaterTerrainData:
```
WaterTerrainData:
├─ Tile Variants (Size: 3)
│  ├─ [0] WaterAnimatedTile_Calm
│  ├─ [1] WaterAnimatedTile_Rough
│  └─ [2] WaterAnimatedTile_Deep
```

3. **GameMap** will randomly choose one variant per tile!

**Result**: Varied water surfaces (calm, rough, deep) placed randomly.

---

## Integration with Existing Code

### ✅ **No Code Changes Needed!**

The current implementation **already supports AnimatedTile**:

```csharp
// MapRenderer.cs line 197
var unityTile = terrainData.GetTileVariant(tile.VisualVariant);
terrainTilemap.SetTile(tilePos, unityTile);
```

**Why it works:**
- `AnimatedTile` inherits from `TileBase`
- `GetTileVariant()` returns `TileBase`
- Tilemap accepts any `TileBase` (including AnimatedTile)
- Animation runs automatically via Tilemap system

**Architecture:**
```
TerrainData.tileVariants[0] = WaterAnimatedTile
    ↓
GetTileVariant(0) returns AnimatedTile
    ↓
terrainTilemap.SetTile() accepts AnimatedTile
    ↓
Tilemap automatically plays animation! ✨
```

---

## Performance Considerations

### How Many Animated Tiles?

**Unity Performance:**
- AnimatedTile uses Unity's Tilemap animation system
- Very efficient for 2D games
- Hundreds of animated tiles: ✅ Fine
- Thousands of animated tiles: ⚠️ May impact performance

**For a 30x30 map with water:**
- Assume 20% water tiles = ~180 tiles
- **Impact**: Minimal (well within performance budget)

### Optimization Tips

1. **Shared Animation**: All water tiles use the **same** AnimatedTile asset → animations are shared
2. **Sprite Atlas**: Put all 21 water frames in a Sprite Atlas → reduces draw calls
3. **Frame Rate**: 21 FPS is fine. Don't need 60 FPS for water animation.
4. **URP Batching**: With URP + SRP Batcher, animated tiles batch efficiently

---

## Troubleshooting

### Water Tiles Not Animating

**Check:**
1. ✅ WaterAnimatedTile has 21 sprites assigned
2. ✅ WaterTerrainData.tileVariants contains WaterAnimatedTile
3. ✅ MapRenderer.terrainDataArray contains WaterTerrainData
4. ✅ Scene is in **Play mode** (animations don't play in Edit mode)

**Debug:**
```csharp
// Add to MapRenderer.UpdateTile() temporarily:
Debug.Log($"Setting tile at {pos}: {unityTile.GetType().Name}");
// Should print: "AnimatedTile"
```

### Animation Too Fast/Slow

**Adjust speed in AnimatedTile:**
```
Minimum Speed: 0.5  (slower)
Maximum Speed: 0.5
```
or
```
Minimum Speed: 2.0  (faster)
Maximum Speed: 2.0
```

**Speed = frames per second multiplier**
- 1.0 = default speed
- 0.5 = half speed
- 2.0 = double speed

### All Water Tiles Look Identical

**This is expected if you only have 1 variant!**

**Solutions:**
1. Create multiple AnimatedTile variants (calm, rough, deep)
2. Assign all to WaterTerrainData.tileVariants
3. GameMap will randomly distribute them

---

## Other Animated Terrain Ideas

Once you have animated water working, consider:

### Lava
- 15-20 frames of bubbling lava
- Speed: 1.0-1.5 (faster than water)
- Red/orange glow

### Swamp
- 8-12 frames of murky water
- Speed: 0.5-0.8 (slower, more ominous)
- Green/brown tint

### Magical Terrain
- Glowing crystals (pulsing animation)
- Portal tiles (swirling effect)
- Cursed ground (dark energy)

### Environmental Effects
- Flowing rivers (directional water)
- Waterfalls (vertical animation)
- Torches (fire animation on objects layer)

---

## File Structure

**Recommended organization:**

```
Assets/
└─ Data/
   ├─ Sprites/
   │  └─ Terrain/
   │     ├─ WaterAnimation.png (21 frames)
   │     ├─ LavaAnimation.png
   │     └─ ...
   ├─ Tiles/
   │  ├─ Animated/
   │  │  ├─ WaterAnimatedTile.asset
   │  │  ├─ WaterAnimatedTile_Calm.asset
   │  │  ├─ WaterAnimatedTile_Rough.asset
   │  │  └─ LavaAnimatedTile.asset
   │  └─ Static/
   │     ├─ GrassTile_0.asset
   │     ├─ GrassTile_1.asset
   │     └─ ...
   └─ Terrain/
      ├─ WaterTerrainData.asset
      ├─ GrassTerrainData.asset
      └─ ...
```

---

## Summary Checklist

Setting up animated water in 5 steps:

- [ ] 1. Import 21 water sprites (Multiple mode, sliced)
- [ ] 2. Create AnimatedTile asset (Create → 2D → Tiles → Animated Tile)
- [ ] 3. Assign 21 sprites to AnimatedTile (drag or select)
- [ ] 4. Assign AnimatedTile to WaterTerrainData.tileVariants[0]
- [ ] 5. Press Play → Water animates! ✨

**No code changes needed** - existing architecture supports it!

---

## References

- [Unity Animated Tile Docs](https://docs.unity3d.com/Packages/com.unity.2d.tilemap.extras@5.0/manual/AnimatedTile.html)
- [2D Tilemap Extras Package](https://docs.unity3d.com/Packages/com.unity.2d.tilemap.extras@5.0/manual/index.html)
- TerrainData.cs: `Assets/Scripts/Data/TerrainData.cs`
- MapRenderer.cs: `Assets/Scripts/Controllers/MapRenderer.cs`

---

## Technical Notes

### How AnimatedTile Works Internally

```csharp
// AnimatedTile inherits from TileBase
public class AnimatedTile : TileBase
{
    public Sprite[] m_AnimatedSprites;
    public float m_MinSpeed = 1f;
    public float m_MaxSpeed = 1f;

    // Unity's Tilemap calls this every frame
    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        // Calculates which frame to show based on time
        // Updates sprite automatically
    }
}
```

**Key insight**: Unity's Tilemap system handles all animation logic. You just provide the sprites!

### SSOT Compliance

**Single Source of Truth:**
- AnimatedTile asset = source of truth for animation (sprites + speed)
- TerrainData.tileVariants = source of truth for which tiles to use
- GameMap.SetTerrain() = source of truth for variant selection

**No duplication** - everything references the AnimatedTile asset.

### DRY Compliance

**No repeated code:**
- MapRenderer doesn't need special "animation" code
- AnimatedTile handles its own animation
- Same `SetTile()` call works for static and animated tiles

**Existing code works unchanged!**

---

## Conclusion

Animated tiles in Unity are **incredibly easy** once you understand the workflow:

1. **Create AnimatedTile asset** (one-time setup)
2. **Assign sprites** (drag and drop)
3. **Reference in TerrainData** (existing system)
4. **Play!** (automatic animation)

Your 21-frame water animation will look great! The HOMM3-style aesthetic really benefits from animated water. 🌊
