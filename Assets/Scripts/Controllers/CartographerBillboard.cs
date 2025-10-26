using UnityEngine;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Renders a 2D sprite as a billboard quad using the Cartographer system.
    /// Uses shader-based rotation to always face the camera (Song of Conquest style).
    ///
    /// This component:
    /// - Creates a quad mesh with the sprite texture
    /// - Applies CartographerBillboard shader for automatic camera-facing
    /// - Enables shadow casting onto the ground plane
    /// - Positions sprite at ground level (Y=0) by default
    ///
    /// Usage: Attach to any GameObject that should be rendered as a billboard sprite.
    /// </summary>
    public class CartographerBillboard : MonoBehaviour
    {
        [Header("Sprite")]
        [Tooltip("The 2D sprite to display on the billboard")]
        [SerializeField] private Sprite sprite;

        [Tooltip("Color tint for the sprite")]
        [SerializeField] private Color tint = Color.white;

        [Tooltip("Alpha cutoff threshold (0-1)")]
        [SerializeField] [Range(0f, 1f)] private float alphaCutoff = 0.5f;

        [Header("Billboard Settings")]
        [Tooltip("Height offset from ground (Y position)")]
        [SerializeField] private float heightOffset = 0.5f;

        [Tooltip("Scale multiplier for the billboard")]
        [SerializeField] private float scale = 1f;

        [Tooltip("Enable shadow casting")]
        [SerializeField] private bool castShadows = true;

        [Header("Debug")]
        [Tooltip("Show debug gizmos")]
        [SerializeField] private bool showDebugGizmos = false;

        private GameObject billboardQuad;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Material billboardMaterial;

        void Awake()
        {
            if (billboardQuad == null)
            {
                CreateBillboardQuad();
            }
            UpdateBillboard();
        }

        void OnEnable()
        {
            // Find existing quad by name if we lost the reference
            if (billboardQuad == null)
            {
                Transform existingQuad = transform.Find("BillboardQuad");
                if (existingQuad != null)
                {
                    billboardQuad = existingQuad.gameObject;
                    meshFilter = billboardQuad.GetComponent<MeshFilter>();
                    meshRenderer = billboardQuad.GetComponent<MeshRenderer>();
                    billboardMaterial = meshRenderer.material;
                }
                else
                {
                    CreateBillboardQuad();
                }
            }
            UpdateBillboard();
        }

        void OnDisable()
        {
            if (billboardQuad != null && Application.isPlaying)
            {
                billboardQuad.SetActive(false);
            }
        }

        /// <summary>
        /// Creates the billboard quad mesh and renderer.
        /// </summary>
        private void CreateBillboardQuad()
        {
            if (billboardQuad != null)
                return;

            // Create child GameObject for the billboard quad
            billboardQuad = new GameObject("BillboardQuad");
            billboardQuad.transform.SetParent(transform);
            billboardQuad.transform.localPosition = Vector3.zero;
            billboardQuad.transform.localRotation = Quaternion.identity;

            // Add mesh components
            meshFilter = billboardQuad.AddComponent<MeshFilter>();
            meshRenderer = billboardQuad.AddComponent<MeshRenderer>();

            // Create quad mesh
            meshFilter.mesh = CreateQuadMesh();

            // Create material with Cartographer billboard shader
            Shader billboardShader = Shader.Find("RealmsOfEldor/CartographerBillboard");
            if (billboardShader == null)
            {
                Debug.LogError("CartographerBillboard: Shader 'RealmsOfEldor/CartographerBillboard' not found!");
                billboardShader = Shader.Find("Unlit/Transparent");
            }

            billboardMaterial = new Material(billboardShader);
            billboardMaterial.name = "CartographerBillboardMaterial";
            billboardMaterial.SetColor("_Color", Color.white);
            meshRenderer.material = billboardMaterial;

            // Configure shadow casting
            meshRenderer.shadowCastingMode = castShadows
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false; // Billboards don't receive shadows on themselves
        }

        /// <summary>
        /// Creates a centered quad mesh for the billboard.
        /// </summary>
        private Mesh CreateQuadMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "BillboardQuad";

            // Vertices centered at origin
            mesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, 0f), // Bottom-left
                new Vector3(0.5f, -0.5f, 0f),  // Bottom-right
                new Vector3(-0.5f, 0.5f, 0f),  // Top-left
                new Vector3(0.5f, 0.5f, 0f)    // Top-right
            };

            // UVs (0,0 = bottom-left, 1,1 = top-right)
            mesh.uv = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };

            // Triangles
            mesh.triangles = new int[]
            {
                0, 2, 1, // First triangle
                2, 3, 1  // Second triangle
            };

            // Normals (facing forward, but shader will override)
            mesh.normals = new Vector3[]
            {
                Vector3.forward,
                Vector3.forward,
                Vector3.forward,
                Vector3.forward
            };

            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Updates the billboard with current sprite and settings.
        /// </summary>
        public void UpdateBillboard()
        {
            if (billboardMaterial == null || sprite == null)
                return;

            // Set texture
            billboardMaterial.mainTexture = sprite.texture;

            // Set material properties
            billboardMaterial.SetColor("_Color", tint);
            billboardMaterial.SetFloat("_Cutoff", alphaCutoff);

            // Scale quad based on sprite size and scale multiplier
            if (billboardQuad != null)
            {
                Vector3 spriteSize = sprite.bounds.size;

                // Debug: Log the actual sprite size
                if (Application.isPlaying && Time.frameCount % 60 == 0) // Log once per second
                {
                    Debug.Log($"CartographerBillboard: Sprite '{sprite.name}' bounds.size = {spriteSize}, scale = {scale}, final scale = ({spriteSize.x * scale}, {spriteSize.y * scale})");
                }

                billboardQuad.transform.localScale = new Vector3(spriteSize.x * scale, spriteSize.y * scale, 1f);

                // Position at height offset
                billboardQuad.transform.localPosition = new Vector3(0f, heightOffset, 0f);
            }

            // Update shadow casting
            if (meshRenderer != null)
            {
                meshRenderer.shadowCastingMode = castShadows
                    ? UnityEngine.Rendering.ShadowCastingMode.On
                    : UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        /// <summary>
        /// Sets the sprite to display.
        /// </summary>
        public void SetSprite(Sprite newSprite)
        {
            sprite = newSprite;
            UpdateBillboard();
        }

        /// <summary>
        /// Sets the color tint.
        /// </summary>
        public void SetTint(Color newTint)
        {
            tint = newTint;
            if (billboardMaterial != null)
            {
                billboardMaterial.SetColor("_Color", tint);
            }
        }

        /// <summary>
        /// Sets the height offset from ground.
        /// </summary>
        public void SetHeightOffset(float height)
        {
            heightOffset = height;
            if (billboardQuad != null)
            {
                billboardQuad.transform.localPosition = new Vector3(0f, heightOffset, 0f);
            }
        }

        /// <summary>
        /// Sets the scale multiplier.
        /// </summary>
        public void SetScale(float newScale)
        {
            scale = newScale;
            UpdateBillboard();
        }

        /// <summary>
        /// Enables or disables shadow casting.
        /// </summary>
        public void SetCastShadows(bool enabled)
        {
            castShadows = enabled;
            if (meshRenderer != null)
            {
                meshRenderer.shadowCastingMode = enabled
                    ? UnityEngine.Rendering.ShadowCastingMode.On
                    : UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        /// <summary>
        /// Gets the current sprite.
        /// </summary>
        public Sprite GetSprite()
        {
            return sprite;
        }

        void OnValidate()
        {
            // Update when values change in inspector
            if (billboardQuad != null)
            {
                UpdateBillboard();
            }
        }

        void Update()
        {
            // Allow runtime updates during Play mode for testing
            if (Application.isPlaying)
            {
                UpdateBillboard();
            }
        }

        void OnDrawGizmos()
        {
            if (!showDebugGizmos)
                return;

            // Draw position marker
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.1f);

            // Draw height line
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * heightOffset);

            // Draw sprite bounds (approximate)
            if (sprite != null)
            {
                Gizmos.color = Color.cyan;
                Vector3 spriteSize = sprite.bounds.size * scale;
                Vector3 center = transform.position + Vector3.up * heightOffset;
                Gizmos.DrawWireCube(center, new Vector3(spriteSize.x, spriteSize.y, 0.1f));
            }
        }

        void OnDestroy()
        {
            // Clean up material
            if (billboardMaterial != null)
            {
                if (Application.isPlaying)
                    Destroy(billboardMaterial);
                else
                    DestroyImmediate(billboardMaterial);
            }
        }
    }
}
