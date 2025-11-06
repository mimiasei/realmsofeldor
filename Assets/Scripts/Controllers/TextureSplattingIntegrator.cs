using UnityEngine;
using RealmsOfEldor.Core;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Integrates texture splatting with map generation workflow.
    /// Automatically generates splatmap from GameMap and applies it to Cartographer ground plane.
    ///
    /// Usage:
    /// 1. Attach to GameInitializer GameObject
    /// 2. Assign splatmapMaterial (material using InnoGames/Terrain shader)
    /// 3. System automatically generates and applies splatmap when map is created
    /// </summary>
    [RequireComponent(typeof(TerrainSplatmapGenerator))]
    public class TextureSplattingIntegrator : MonoBehaviour
    {
        [Header("Material Setup")]
        [Tooltip("Material using InnoGames/Terrain shader with 5 texture slots")]
        [SerializeField] private Material splatmapMaterial;

        [Header("Debug Options")]
        [Tooltip("Save splatmap to PNG file for debugging")]
        [SerializeField] private bool saveSplatmapDebug = false;

        [Tooltip("Filename for debug splatmap (saved to Assets/)")]
        [SerializeField] private string debugFilename = "Debug/splatmap_generated.png";

        [Header("Auto-Apply")]
        [Tooltip("Automatically apply splatmap when map is loaded/generated")]
        [SerializeField] private bool autoApply = true;

        private TerrainSplatmapGenerator generator;
        private Cartographer cartographer;
        private Texture2D currentSplatmap;

        private void Awake()
        {
            generator = GetComponent<TerrainSplatmapGenerator>();
            if (generator == null)
            {
                Debug.LogError("TextureSplattingIntegrator: TerrainSplatmapGenerator component not found!");
            }
        }

        private void Start()
        {
            // Find Cartographer in scene
            cartographer = FindFirstObjectByType<Cartographer>();
            if (cartographer == null)
            {
                Debug.LogWarning("TextureSplattingIntegrator: Cartographer not found in scene. Will search again when applying splatmap.");
            }

            // Apply material immediately if set
            if (splatmapMaterial != null && cartographer != null)
            {
                cartographer.SetGroundMaterial(splatmapMaterial);
            }
        }

        /// <summary>
        /// Called by GameInitializer after map generation is complete.
        /// Generates splatmap from GameMap and applies it to ground plane.
        /// </summary>
        public void OnMapGenerated(GameMap gameMap)
        {
            if (!autoApply)
            {
                Debug.Log("TextureSplattingIntegrator: Auto-apply disabled, skipping splatmap generation");
                return;
            }

            if (gameMap == null)
            {
                Debug.LogError("TextureSplattingIntegrator: Cannot generate splatmap, gameMap is null!");
                return;
            }

            ApplySplatmapToMap(gameMap);
        }

        /// <summary>
        /// Generates and applies splatmap for the given GameMap.
        /// Can be called manually or automatically via OnMapGenerated.
        /// </summary>
        public void ApplySplatmapToMap(GameMap gameMap)
        {
            if (generator == null)
            {
                Debug.LogError("TextureSplattingIntegrator: Generator not found!");
                return;
            }

            // Find Cartographer if not cached
            if (cartographer == null)
            {
                cartographer = FindFirstObjectByType<Cartographer>();
                if (cartographer == null)
                {
                    Debug.LogError("TextureSplattingIntegrator: Cartographer not found in scene!");
                    return;
                }
            }

            // Validate material
            if (splatmapMaterial == null)
            {
                Debug.LogError("TextureSplattingIntegrator: Splatmap material not assigned! Assign a material using InnoGames/Terrain shader.");
                return;
            }

            if (splatmapMaterial.shader.name != "InnoGames/Terrain")
            {
                Debug.LogWarning($"TextureSplattingIntegrator: Material shader is '{splatmapMaterial.shader.name}', expected 'InnoGames/Terrain'. This may not work correctly.");
            }

            Debug.Log("TextureSplattingIntegrator: Generating splatmap from GameMap...");
            Debug.Log(generator.GetMappingSummary());

            // Generate splatmap
            currentSplatmap = generator.GenerateSplatmap(gameMap);

            if (currentSplatmap == null)
            {
                Debug.LogError("TextureSplattingIntegrator: Splatmap generation failed!");
                return;
            }

            // Apply material (ensures it's set even if changed)
            cartographer.SetGroundMaterial(splatmapMaterial);

            // Apply splatmap to material
            cartographer.SetSplatmap(currentSplatmap);

            // Debug save if enabled
            if (saveSplatmapDebug)
            {
                generator.SaveSplatmapToPNG(currentSplatmap, debugFilename);
            }

            Debug.Log($"✓ TextureSplattingIntegrator: Splatmap applied successfully ({currentSplatmap.width}x{currentSplatmap.height})");
        }

        /// <summary>
        /// Regenerates splatmap from current GameMap (if available).
        /// Useful for refreshing after map changes.
        /// TODO: Implement when GameState.Map property is added (currently commented out in GameState.cs)
        /// </summary>
        public void RegenerateSplatmap()
        {
            Debug.LogWarning("TextureSplattingIntegrator: RegenerateSplatmap() not yet implemented - GameState.Map property not available");
            // TODO: Uncomment when GameState.Map is implemented
            // var gameState = GameStateManager.Instance?.State;
            // if (gameState?.Map == null)
            // {
            //     Debug.LogError("TextureSplattingIntegrator: No GameMap available in GameState!");
            //     return;
            // }
            //
            // ApplySplatmapToMap(gameState.Map);
        }

        /// <summary>
        /// Returns the currently generated splatmap texture.
        /// </summary>
        public Texture2D GetCurrentSplatmap()
        {
            return currentSplatmap;
        }

        /// <summary>
        /// Validates setup and logs any issues.
        /// </summary>
        [ContextMenu("Validate Setup")]
        public void ValidateSetup()
        {
            Debug.Log("=== TextureSplattingIntegrator Setup Validation ===");

            // Check generator
            if (generator == null)
            {
                Debug.LogError("✗ TerrainSplatmapGenerator component missing!");
            }
            else
            {
                Debug.Log("✓ TerrainSplatmapGenerator found");
                Debug.Log(generator.GetMappingSummary());
            }

            // Check material
            if (splatmapMaterial == null)
            {
                Debug.LogError("✗ Splatmap material not assigned!");
            }
            else
            {
                Debug.Log($"✓ Material assigned: {splatmapMaterial.name}");
                Debug.Log($"  Shader: {splatmapMaterial.shader.name}");

                if (splatmapMaterial.shader.name != "InnoGames/Terrain")
                {
                    Debug.LogWarning($"  ⚠ Shader is not 'InnoGames/Terrain'!");
                }
                else
                {
                    // Check texture assignments
                    var groundTex = splatmapMaterial.GetTexture("_GroundTexture");
                    var texA = splatmapMaterial.GetTexture("_TextureA");
                    var texB = splatmapMaterial.GetTexture("_TextureB");
                    var texC = splatmapMaterial.GetTexture("_TextureC");
                    var texD = splatmapMaterial.GetTexture("_TextureD");

                    Debug.Log($"  Ground Texture: {(groundTex != null ? groundTex.name : "NOT ASSIGNED")}");
                    Debug.Log($"  Texture A: {(texA != null ? texA.name : "NOT ASSIGNED")}");
                    Debug.Log($"  Texture B: {(texB != null ? texB.name : "NOT ASSIGNED")}");
                    Debug.Log($"  Texture C: {(texC != null ? texC.name : "NOT ASSIGNED")}");
                    Debug.Log($"  Texture D: {(texD != null ? texD.name : "NOT ASSIGNED")}");
                }
            }

            // Check Cartographer
            var cart = FindFirstObjectByType<Cartographer>();
            if (cart == null)
            {
                Debug.LogWarning("⚠ Cartographer not found in scene (may not be loaded yet)");
            }
            else
            {
                Debug.Log("✓ Cartographer found");
            }

            // Check GameState
            if (GameStateManager.Instance == null)
            {
                Debug.LogWarning("⚠ GameStateManager not initialized");
            }
            else
            {
                Debug.Log("✓ GameStateManager initialized");
                // TODO: Check GameState.Map when property is implemented
                Debug.Log("  (GameState.Map property not yet implemented - map is managed separately via MapEventChannel)");
            }

            Debug.Log("=== Validation Complete ===");
        }
    }
}
