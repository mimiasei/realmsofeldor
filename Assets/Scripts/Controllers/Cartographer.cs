using UnityEngine;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Unified 2.5D rendering system for Realms of Eldor.
    /// Handles ground plane rendering and camera setup using Song of Conquest's approach:
    /// - 3D ground plane (X,Z horizontal, Y vertical)
    /// - Isometric perspective camera
    /// - Billboard sprites that face camera via shader
    /// - Real shadows from Unity's lighting system
    ///
    /// Used by both adventure map and battle scenes for consistent rendering.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class Cartographer : MonoBehaviour
    {
        [Header("Ground Plane")]
        [Tooltip("Material for the ground plane (should use a tileable texture)")]
        [SerializeField] private Material groundMaterial;

        [Tooltip("Size of the ground plane in world units")]
        [SerializeField] private Vector2 groundSize = new Vector2(100f, 100f);

        [Tooltip("UV tiling for ground texture")]
        [SerializeField] private Vector2 groundTextureTiling = new Vector2(10f, 10f);

        [Header("Camera Settings - Song of Conquest Style")]
        [Tooltip("Isometric camera angle (typically 45° for isometric)")]
        [SerializeField] private float cameraYRotation = 45f;

        [Tooltip("Camera tilt looking down at ground (30-50° typical)")]
        [SerializeField] private float cameraTiltAngle = 40f;

        [Tooltip("Field of view (20-30° for minimal distortion)")]
        [SerializeField] private float fieldOfView = 25f;

        [Tooltip("Camera height above ground")]
        [SerializeField] private float cameraHeight = 30f;

        [Tooltip("Camera distance back from center (negative Z)")]
        [SerializeField] private float cameraDistance = -40f;

        [Header("Lighting")]
        [Tooltip("Enable automatic directional light setup")]
        [SerializeField] private bool createDirectionalLight = true;

        [Tooltip("Sun light angle (affects shadow direction)")]
        [SerializeField] private Vector2 sunAngle = new Vector2(50f, -30f);

        [Tooltip("Sun light intensity")]
        [SerializeField] private float sunIntensity = 1.0f;

        [Tooltip("Enable shadows")]
        [SerializeField] private bool enableShadows = true;

        private Camera mainCamera;
        private GameObject groundPlane;
        private Light directionalLight;

        void Awake()
        {
            mainCamera = GetComponent<Camera>();

            // Setup camera for isometric 2.5D rendering
            SetupCamera();

            // Create the 3D ground plane
            CreateGroundPlane();

            // Setup lighting
            if (createDirectionalLight)
            {
                SetupLighting();
            }
        }

        /// <summary>
        /// Configures camera for Song of Conquest style isometric 2.5D rendering.
        /// </summary>
        private void SetupCamera()
        {
            // Perspective camera (not orthographic!)
            mainCamera.orthographic = false;
            mainCamera.fieldOfView = fieldOfView;

            // Ground plane center (what we want to look at)
            Vector3 groundCenter = new Vector3(groundSize.x / 2f, 0f, groundSize.y / 2f);

            // Calculate camera offset from ground center
            // We want camera ABOVE and BACK from the center
            float horizontalDistance = Mathf.Abs(cameraDistance); // Make sure it's positive distance

            // Convert isometric angle to radians for positioning
            float angleRad = cameraYRotation * Mathf.Deg2Rad;

            // Calculate offset in X and Z based on isometric angle
            float offsetX = -horizontalDistance * Mathf.Sin(angleRad);
            float offsetZ = -horizontalDistance * Mathf.Cos(angleRad);

            // Position camera above and offset from center
            transform.position = groundCenter + new Vector3(offsetX, cameraHeight, offsetZ);

            // Point camera at ground center using LookAt
            transform.LookAt(groundCenter);

            // Make sure camera can see far enough
            mainCamera.nearClipPlane = 0.3f;
            mainCamera.farClipPlane = 1000f;

            // Clear to solid color (not skybox)
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);

            Debug.Log($"Cartographer: Camera configured:");
            Debug.Log($"  Position: {transform.position}");
            Debug.Log($"  Rotation: {transform.rotation.eulerAngles}");
            Debug.Log($"  Ground center: {groundCenter}");
            Debug.Log($"  Camera offset: ({offsetX:F2}, {cameraHeight}, {offsetZ:F2})");
            Debug.Log($"  FOV: {fieldOfView}°, Near: {mainCamera.nearClipPlane}, Far: {mainCamera.farClipPlane}");
            Debug.Log($"  Camera forward direction: {transform.forward}");
            Debug.Log($"  Distance to ground center: {Vector3.Distance(transform.position, groundCenter):F2}");
        }

        /// <summary>
        /// Creates a 3D ground plane mesh at Y=0 on the X,Z plane.
        /// This is the fundamental 3D surface that receives shadows and textures.
        /// </summary>
        private void CreateGroundPlane()
        {
            groundPlane = new GameObject("GroundPlane");
            // DO NOT parent to camera - ground must stay at world origin!
            // groundPlane.transform.SetParent(transform);
            groundPlane.transform.position = Vector3.zero;
            groundPlane.transform.rotation = Quaternion.identity;

            // Add mesh components
            MeshFilter meshFilter = groundPlane.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = groundPlane.AddComponent<MeshRenderer>();

            // Create quad mesh for ground
            meshFilter.mesh = CreateGroundMesh();

            // Apply material
            if (groundMaterial != null)
            {
                meshRenderer.material = groundMaterial;
                Debug.Log($"Cartographer: Using assigned ground material: {groundMaterial.name}");
            }
            else
            {
                // Create default material
                meshRenderer.material = CreateDefaultGroundMaterial();
            }

            // FORCE enable the renderer
            meshRenderer.enabled = true;

            // Disable backface culling for ground plane (render both sides)
            if (meshRenderer.material != null)
            {
                meshRenderer.material.SetInt("_Cull", 0); // 0 = Off (render both sides), 2 = Back (default)
            }

            // Log for debugging
            Debug.Log($"Cartographer: Ground plane mesh bounds: {meshFilter.mesh.bounds}");
            Debug.Log($"Cartographer: Ground plane renderer enabled: {meshRenderer.enabled}, material: {meshRenderer.material?.name}, shader: {meshRenderer.material?.shader?.name}");

            // Configure shadow settings
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // Ground doesn't cast shadows
            meshRenderer.receiveShadows = true; // Ground RECEIVES shadows from billboards

            Debug.Log($"Cartographer: Ground plane created at Y=0, size {groundSize.x}x{groundSize.y}");
        }

        /// <summary>
        /// Creates a quad mesh for the ground plane.
        /// Lies flat on X,Z plane at Y=0.
        /// </summary>
        private Mesh CreateGroundMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "GroundPlaneMesh";

            float halfWidth = groundSize.x / 2f;
            float halfDepth = groundSize.y / 2f;

            // Vertices (on X,Z plane, Y=0)
            mesh.vertices = new Vector3[]
            {
                new Vector3(-halfWidth, 0f, -halfDepth), // Bottom-left
                new Vector3(halfWidth, 0f, -halfDepth),  // Bottom-right
                new Vector3(-halfWidth, 0f, halfDepth),  // Top-left
                new Vector3(halfWidth, 0f, halfDepth)    // Top-right
            };

            // UVs (tiled texture coordinates)
            mesh.uv = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(groundTextureTiling.x, 0f),
                new Vector2(0f, groundTextureTiling.y),
                new Vector2(groundTextureTiling.x, groundTextureTiling.y)
            };

            // Triangles (two triangles make a quad)
            // IMPORTANT: Winding order determines which side is "front"
            // Counter-clockwise winding = front face visible from above
            mesh.triangles = new int[]
            {
                0, 1, 2, // First triangle (counter-clockwise from above)
                2, 1, 3  // Second triangle (counter-clockwise from above)
            };

            // Normals (pointing up for lighting)
            mesh.normals = new Vector3[]
            {
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up
            };

            mesh.RecalculateBounds();

            Debug.Log($"Cartographer: Created ground mesh - Vertices: {mesh.vertexCount}, Triangles: {mesh.triangles.Length/3}, Bounds: {mesh.bounds}");

            return mesh;
        }

        /// <summary>
        /// Creates a default ground material if none is assigned.
        /// </summary>
        private Material CreateDefaultGroundMaterial()
        {
            // Try multiple shader fallbacks in order of preference
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");

            if (shader == null)
            {
                Debug.LogWarning("Cartographer: URP/Lit shader not found, trying Standard...");
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                Debug.LogWarning("Cartographer: Standard shader not found, trying Unlit/Color...");
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                Debug.LogError("Cartographer: No compatible shader found! Using error shader.");
                shader = Shader.Find("Hidden/InternalErrorShader");
            }

            Material material = new Material(shader);
            material.name = "DefaultGroundMaterial";

            // Create simple grass-colored texture
            Texture2D texture = new Texture2D(64, 64, TextureFormat.RGB24, false);
            Color grassColor = new Color(0.3f, 0.6f, 0.2f); // Green

            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    // Add slight variation
                    float variation = Random.Range(-0.05f, 0.05f);
                    texture.SetPixel(x, y, grassColor + new Color(variation, variation, variation));
                }
            }

            texture.Apply();
            texture.wrapMode = TextureWrapMode.Repeat;

            // Set color/texture based on shader type
            if (shader.name.Contains("Unlit/Color"))
            {
                material.SetColor("_Color", grassColor);
            }
            else
            {
                material.mainTexture = texture;
                if (shader.name.Contains("Standard") || shader.name.Contains("Lit"))
                {
                    material.SetColor("_Color", Color.white);
                }
            }

            Debug.Log($"Cartographer: Created default ground material using shader: {shader.name}");

            return material;
        }

        /// <summary>
        /// Sets up directional light for shadows (sun).
        /// </summary>
        private void SetupLighting()
        {
            // Check if light already exists
            directionalLight = FindFirstObjectByType<Light>();

            if (directionalLight == null || directionalLight.type != LightType.Directional)
            {
                // Create new directional light
                GameObject lightObj = new GameObject("Sun");
                // DO NOT parent to camera - light is a world object!
                // lightObj.transform.SetParent(transform);
                directionalLight = lightObj.AddComponent<Light>();
                directionalLight.type = LightType.Directional;
            }

            // Configure light
            directionalLight.color = new Color(1f, 0.95f, 0.85f); // Warm sunlight
            directionalLight.intensity = sunIntensity;
            directionalLight.transform.rotation = Quaternion.Euler(sunAngle.x, sunAngle.y, 0f);

            // Shadow settings
            if (enableShadows)
            {
                directionalLight.shadows = LightShadows.Soft;
                directionalLight.shadowStrength = 0.6f;
                directionalLight.shadowBias = 0.05f;
                directionalLight.shadowNormalBias = 0.4f;
                directionalLight.shadowNearPlane = 0.2f;
            }
            else
            {
                directionalLight.shadows = LightShadows.None;
            }

            // Set ambient lighting
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.4f, 0.4f, 0.5f); // Cool ambient
            RenderSettings.ambientIntensity = 0.3f;

            Debug.Log($"Cartographer: Lighting configured - Sun angle: {sunAngle}, Shadows: {enableShadows}");
        }

        /// <summary>
        /// Updates the ground texture (call this when map changes).
        /// </summary>
        public void SetGroundTexture(Texture2D texture)
        {
            if (groundPlane != null)
            {
                MeshRenderer renderer = groundPlane.GetComponent<MeshRenderer>();
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.mainTexture = texture;
                }
            }
        }

        /// <summary>
        /// Gets the ground plane transform for positioning objects.
        /// </summary>
        public Transform GetGroundPlane()
        {
            return groundPlane?.transform;
        }

        /// <summary>
        /// Converts a 2D map position (X,Y) to 3D world position on ground plane (X, 0, Z).
        /// </summary>
        public Vector3 MapToWorldPosition(int mapX, int mapY)
        {
            return new Vector3(mapX, 0f, mapY);
        }

        /// <summary>
        /// Converts a 2D map position with height to 3D world position.
        /// </summary>
        public Vector3 MapToWorldPosition(int mapX, int mapY, float height)
        {
            return new Vector3(mapX, height, mapY);
        }

        /// <summary>
        /// Converts a 3D world position to 2D map position.
        /// </summary>
        public Vector2Int WorldToMapPosition(Vector3 worldPos)
        {
            return new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.z));
        }

        /// <summary>
        /// Raycasts from camera to ground plane to get world position under mouse.
        /// </summary>
        public bool GetMouseWorldPosition(out Vector3 worldPos)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero); // Plane at Y=0, facing up

            if (groundPlane.Raycast(ray, out float enter))
            {
                worldPos = ray.GetPoint(enter);
                return true;
            }

            worldPos = Vector3.zero;
            return false;
        }

        /// <summary>
        /// Debug visualization.
        /// </summary>
        void OnDrawGizmos()
        {
            // Draw ground plane bounds
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3(groundSize.x / 2f, 0f, groundSize.y / 2f);
            Gizmos.DrawWireCube(center, new Vector3(groundSize.x, 0.1f, groundSize.y));

            // Draw camera view direction
            if (mainCamera != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position, transform.forward * 10f);
            }
        }

        /// <summary>
        /// Context menu helper to recenter camera.
        /// </summary>
        [ContextMenu("Recenter Camera")]
        public void RecenterCamera()
        {
            SetupCamera();
        }
    }
}
