using UnityEngine;
using UnityEngine.Tilemaps;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Adapts Unity Tilemaps to work with Cartographer's 3D ground plane system.
    /// Rotates tilemaps to lie flat on X,Z plane and positions them correctly.
    ///
    /// Attach this to the parent GameObject containing your Tilemaps.
    /// It will automatically rotate and position all child Tilemaps for 3D rendering.
    /// </summary>
    [ExecuteAlways]
    public class TilemapCartographerAdapter : MonoBehaviour
    {
        [Header("Tilemap Configuration")]
        [Tooltip("Automatically find and configure all child Tilemaps")]
        [SerializeField] private bool autoConfigureOnStart = true;

        [Tooltip("Height offset for tilemaps (Y position in 3D space)")]
        [SerializeField] private float tilemapHeight = 0f;

        [Tooltip("Vertical offset between tilemap layers (terrain, objects, highlight)")]
        [SerializeField] private float layerOffset = 0.01f;

        [Header("Debug")]
        [Tooltip("Show debug logs")]
        [SerializeField] private bool showDebugLogs = true;

        private Tilemap[] tilemaps;

        void Start()
        {
            if (autoConfigureOnStart)
            {
                ConfigureTilemaps();
            }
        }

        /// <summary>
        /// Configures all child Tilemaps for Cartographer 3D rendering.
        /// Call this manually if you disable autoConfigureOnStart.
        /// </summary>
        [ContextMenu("Configure Tilemaps for 3D")]
        public void ConfigureTilemaps()
        {
            tilemaps = GetComponentsInChildren<Tilemap>(true);

            if (tilemaps.Length == 0)
            {
                Debug.LogWarning("TilemapCartographerAdapter: No Tilemaps found in children!");
                return;
            }

            if (showDebugLogs)
                Debug.Log($"TilemapCartographerAdapter: Configuring {tilemaps.Length} tilemaps for 3D rendering...");

            for (int i = 0; i < tilemaps.Length; i++)
            {
                ConfigureTilemap(tilemaps[i], i);
            }

            if (showDebugLogs)
                Debug.Log($"✅ TilemapCartographerAdapter: Configured {tilemaps.Length} tilemaps");
        }

        /// <summary>
        /// Configures a single tilemap for 3D rendering.
        /// </summary>
        private void ConfigureTilemap(Tilemap tilemap, int layerIndex)
        {
            Transform tilemapTransform = tilemap.transform;

            // Rotate tilemap to lie flat on X,Z plane
            // Tilemap's "up" (Y axis) becomes world "forward" (Z axis)
            // Tilemap's "forward" (Z axis) becomes world "up" (Y axis)
            tilemapTransform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // Position at ground level with slight offset per layer
            float heightWithOffset = tilemapHeight + (layerIndex * layerOffset);
            tilemapTransform.localPosition = new Vector3(0f, heightWithOffset, 0f);

            // Enable shadow receiving on TilemapRenderer
            var tilemapRenderer = tilemap.GetComponent<TilemapRenderer>();
            if (tilemapRenderer != null)
            {
                tilemapRenderer.receiveShadows = true;
                tilemapRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                if (showDebugLogs)
                {
                    Debug.Log($"  ✓ {tilemap.name}: Rotation=(90,0,0), Position=(0,{heightWithOffset:F3},0), Shadows=Receive");
                }
            }
        }

        /// <summary>
        /// Resets tilemaps back to 2D configuration.
        /// </summary>
        [ContextMenu("Reset to 2D")]
        public void ResetTo2D()
        {
            tilemaps = GetComponentsInChildren<Tilemap>(true);

            foreach (var tilemap in tilemaps)
            {
                tilemap.transform.localRotation = Quaternion.identity;
                tilemap.transform.localPosition = Vector3.zero;

                var tilemapRenderer = tilemap.GetComponent<TilemapRenderer>();
                if (tilemapRenderer != null)
                {
                    tilemapRenderer.receiveShadows = false;
                }
            }

            Debug.Log("TilemapCartographerAdapter: Reset tilemaps to 2D configuration");
        }

        /// <summary>
        /// Updates tilemap height at runtime.
        /// </summary>
        public void SetTilemapHeight(float height)
        {
            tilemapHeight = height;
            ConfigureTilemaps();
        }

        void OnValidate()
        {
            // Auto-configure when values change in inspector (only in edit mode)
            if (!Application.isPlaying && autoConfigureOnStart)
            {
                ConfigureTilemaps();
            }
        }
    }
}
