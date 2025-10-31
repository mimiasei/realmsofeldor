using UnityEngine;

namespace RealmsOfEldor.Controllers.Battle
{
    /// <summary>
    /// Creates a 3D ground plane mesh for the battle scene that can receive shadows.
    /// SpriteRenderer cannot receive shadows, so we need a real 3D mesh.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class BattleGroundPlane : MonoBehaviour
    {
        [Header("Ground Plane Settings")]
        [SerializeField] private Material groundMaterial;
        [SerializeField] private Texture2D groundTexture;
        [SerializeField] private Color groundColor = new Color(0.3f, 0.6f, 0.2f); // Grass green
        [SerializeField] private Vector2 planeSize = new Vector2(20f, 15f); // Battlefield size

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();

            CreateGroundPlane();
        }

        /// <summary>
        /// Creates the ground plane mesh and material.
        /// </summary>
        private void CreateGroundPlane()
        {
            // Create mesh
            meshFilter.mesh = CreatePlaneMesh();

            // Create or assign material
            if (groundMaterial == null)
            {
                groundMaterial = CreateDefaultMaterial();
            }

            meshRenderer.material = groundMaterial;

            // Configure shadow settings - this is the key to receiving shadows!
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = true; // This works on MeshRenderer but not SpriteRenderer!

            Debug.Log($"BattleGroundPlane: Created ground plane mesh at Y=0, size {planeSize.x}x{planeSize.y}");
            Debug.Log($"BattleGroundPlane: Shadow receiving: {meshRenderer.receiveShadows}");
        }

        /// <summary>
        /// Creates a horizontal plane mesh on the XZ plane at Y=0.
        /// </summary>
        private Mesh CreatePlaneMesh()
        {
            var mesh = new Mesh();
            mesh.name = "BattleGroundPlane";

            float halfWidth = planeSize.x / 2f;
            float halfDepth = planeSize.y / 2f;

            // Vertices (on X,Z plane at Y=0)
            mesh.vertices = new Vector3[]
            {
                new Vector3(-halfWidth, 0f, -halfDepth), // Bottom-left
                new Vector3(halfWidth, 0f, -halfDepth),  // Bottom-right
                new Vector3(-halfWidth, 0f, halfDepth),  // Top-left
                new Vector3(halfWidth, 0f, halfDepth)    // Top-right
            };

            // UVs for texture mapping
            mesh.uv = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };

            // Triangles (counter-clockwise winding when viewed from above for front face pointing up)
            // Unity uses counter-clockwise winding for front faces
            // When looking down at XZ plane (from +Y), counter-clockwise order:
            mesh.triangles = new int[]
            {
                0, 2, 1, // First triangle: bottom-left, top-left, bottom-right
                1, 2, 3  // Second triangle: bottom-right, top-left, top-right
            };

            // Normals (pointing up for proper lighting)
            mesh.normals = new Vector3[]
            {
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up
            };

            mesh.RecalculateBounds();

            Debug.Log($"BattleGroundPlane: Created mesh with bounds: {mesh.bounds}");

            return mesh;
        }

        /// <summary>
        /// Creates a default material for the ground plane.
        /// </summary>
        private Material CreateDefaultMaterial()
        {
            // Try URP Lit shader first (supports shadows)
            var shader = Shader.Find("Universal Render Pipeline/Lit");

            if (shader == null)
            {
                Debug.LogWarning("BattleGroundPlane: URP/Lit shader not found, trying Standard...");
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                Debug.LogError("BattleGroundPlane: No lit shader found! Shadows may not work.");
                shader = Shader.Find("Unlit/Color");
            }

            var material = new Material(shader);
            material.name = "BattleGroundMaterial";

            // Apply texture if provided
            if (groundTexture != null)
            {
                material.mainTexture = groundTexture;
            }
            else
            {
                // Create simple grass texture
                groundTexture = CreateGrassTexture();
                material.mainTexture = groundTexture;
            }

            // Set color
            material.color = groundColor;

            Debug.Log($"BattleGroundPlane: Created material using shader: {shader.name}");

            return material;
        }

        /// <summary>
        /// Creates a simple grass texture.
        /// </summary>
        private Texture2D CreateGrassTexture()
        {
            var size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGB24, false);

            var grassBase = groundColor;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Add some noise/variation
                    float variation = Random.Range(-0.1f, 0.1f);
                    Color color = grassBase + new Color(variation, variation, variation * 0.5f);
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            texture.wrapMode = TextureWrapMode.Repeat;

            return texture;
        }

        /// <summary>
        /// Updates the ground plane size at runtime.
        /// </summary>
        [ContextMenu("Recreate Ground Plane")]
        public void RecreateGroundPlane()
        {
            CreateGroundPlane();
        }

        /// <summary>
        /// Sets the ground texture.
        /// </summary>
        public void SetGroundTexture(Texture2D texture)
        {
            groundTexture = texture;
            if (meshRenderer != null && meshRenderer.material != null)
            {
                meshRenderer.material.mainTexture = texture;
            }
        }

        /// <summary>
        /// Sets the ground color.
        /// </summary>
        public void SetGroundColor(Color color)
        {
            groundColor = color;
            if (meshRenderer != null && meshRenderer.material != null)
            {
                meshRenderer.material.color = color;
            }
        }
    }
}
