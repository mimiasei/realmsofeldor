using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

namespace RealmsOfEldor.Editor
{
    /// <summary>
    /// Migrates TerrainData from tileVariants to spriteVariants.
    /// Extracts sprites from Tile assets and assigns them to spriteVariants array.
    /// </summary>
    public class TerrainDataMigrationTool : EditorWindow
    {
        [MenuItem("Tools/Realms of Eldor/Migrate TerrainData Tiles to Sprites")]
        public static void ShowWindow()
        {
            GetWindow<TerrainDataMigrationTool>("TerrainData Migration");
        }

        private void OnGUI()
        {
            GUILayout.Label("Migrate Tile Variants to Sprite Variants", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("This tool extracts sprites from tileVariants (2D tilemap)\nand assigns them to spriteVariants (3D billboard rendering).", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Migrate All TerrainData Assets", GUILayout.Height(40)))
            {
                MigrateAllTerrainData();
            }

            GUILayout.Space(10);
            GUILayout.Label("This will scan Assets/Data/Terrain/ and update all TerrainData assets.", EditorStyles.helpBox);
        }

        private static void MigrateAllTerrainData()
        {
            var guids = AssetDatabase.FindAssets("t:TerrainData", new[] { "Assets/Data/Terrain" });
            var updatedCount = 0;
            var skippedCount = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var terrainData = AssetDatabase.LoadAssetAtPath<Data.TerrainData>(path);

                if (terrainData != null)
                {
                    if (MigrateTerrainData(terrainData))
                    {
                        EditorUtility.SetDirty(terrainData);
                        updatedCount++;
                        Debug.Log($"✓ Migrated {terrainData.name}: {terrainData.spriteVariants?.Length ?? 0} sprites");
                    }
                    else
                    {
                        skippedCount++;
                        Debug.Log($"⊘ Skipped {terrainData.name} (no tile variants or already migrated)");
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Migration Complete",
                $"Updated: {updatedCount}\nSkipped: {skippedCount}",
                "OK"
            );
        }

        /// <summary>
        /// Migrates a single TerrainData from tileVariants to spriteVariants.
        /// Returns true if migration occurred, false if skipped.
        /// </summary>
        private static bool MigrateTerrainData(Data.TerrainData terrainData)
        {
            // Skip if no tile variants
            if (terrainData.tileVariants == null || terrainData.tileVariants.Length == 0)
            {
                return false;
            }

            // Skip if spriteVariants already populated (manual assignment or previous migration)
            if (terrainData.spriteVariants != null && terrainData.spriteVariants.Length > 0)
            {
                // Check if all sprites are assigned
                bool allAssigned = true;
                foreach (var sprite in terrainData.spriteVariants)
                {
                    if (sprite == null)
                    {
                        allAssigned = false;
                        break;
                    }
                }

                if (allAssigned)
                {
                    Debug.Log($"⊘ {terrainData.name} already has sprite variants assigned, skipping");
                    return false;
                }
            }

            // Extract sprites from tiles
            var sprites = new List<Sprite>();
            foreach (var tileBase in terrainData.tileVariants)
            {
                if (tileBase == null)
                    continue;

                // Try to cast to Tile (Unity's standard Tile class)
                if (tileBase is Tile tile)
                {
                    if (tile.sprite != null)
                    {
                        sprites.Add(tile.sprite);
                    }
                }
                // Try to handle AnimatedTile using reflection (if 2D Tilemap Extras package is installed)
                else if (tileBase.GetType().Name == "AnimatedTile")
                {
                    // AnimatedTile has m_AnimatedSprites field
                    var animatedSpritesField = tileBase.GetType().GetField("m_AnimatedSprites");
                    if (animatedSpritesField != null)
                    {
                        var animatedSprites = animatedSpritesField.GetValue(tileBase) as Sprite[];
                        if (animatedSprites != null && animatedSprites.Length > 0)
                        {
                            // Use first frame as representative sprite
                            sprites.Add(animatedSprites[0]);
                        }
                    }
                }
                // Try using reflection for other tile types
                else
                {
                    var spriteProperty = tileBase.GetType().GetProperty("sprite");
                    if (spriteProperty != null)
                    {
                        var sprite = spriteProperty.GetValue(tileBase) as Sprite;
                        if (sprite != null)
                        {
                            sprites.Add(sprite);
                        }
                    }
                }
            }

            // Assign extracted sprites
            if (sprites.Count > 0)
            {
                terrainData.spriteVariants = sprites.ToArray();
                return true;
            }

            return false;
        }

        [MenuItem("Tools/Realms of Eldor/Clear Sprite Variants (Reset)")]
        public static void ClearAllSpriteVariants()
        {
            if (!EditorUtility.DisplayDialog(
                "Clear Sprite Variants",
                "This will clear all spriteVariants from TerrainData assets.\nYou can re-run migration afterwards.\n\nContinue?",
                "Yes, Clear",
                "Cancel"))
            {
                return;
            }

            var guids = AssetDatabase.FindAssets("t:TerrainData", new[] { "Assets/Data/Terrain" });
            var clearedCount = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var terrainData = AssetDatabase.LoadAssetAtPath<Data.TerrainData>(path);

                if (terrainData != null && terrainData.spriteVariants != null && terrainData.spriteVariants.Length > 0)
                {
                    terrainData.spriteVariants = new Sprite[0];
                    EditorUtility.SetDirty(terrainData);
                    clearedCount++;
                    Debug.Log($"✓ Cleared {terrainData.name}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Clear Complete",
                $"Cleared {clearedCount} TerrainData assets",
                "OK"
            );
        }
    }
}
