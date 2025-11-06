using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

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
    /// Includes full camera controls (pan, zoom) from CameraController.
    /// Used by both adventure map and battle scenes for consistent rendering.
    ///
    /// Architecture: Parent rig (this GameObject) handles X/Z panning,
    /// child Camera handles Y/Z zoom and X rotation (tilt).
    /// </summary>
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
        [Tooltip("Camera tilt at 100% zoom / close-up (shallow angle, e.g., -30°)")]
        [SerializeField] private float cameraTiltAngle = -30f;

        [Tooltip("Camera tilt at 0% zoom / zoomed out (steep angle, e.g., -60°)")]
        [SerializeField] private float maxZoomCamRot = -60f;

        [Tooltip("Field of view (20-30° for minimal distortion)")]
        [SerializeField] private float fieldOfView = 25f;

        [Tooltip("Camera height above ground")]
        [SerializeField] private float cameraHeight = 30f;

        [Tooltip("Initial camera Z position on scene start (should be between minPerspZoom and maxPerspZoom)")]
        [SerializeField] private float initialZoomPosition = -20f;

        [Header("Lighting")]
        [Tooltip("Enable automatic directional light setup")]
        [SerializeField] private bool createDirectionalLight = true;

        [Tooltip("Sun light angle (affects shadow direction)")]
        [SerializeField] private Vector2 sunAngle = new Vector2(50f, -30f);

        [Tooltip("Sun light intensity")]
        [SerializeField] private float sunIntensity = 1.0f;

        [Tooltip("Enable shadows")]
        [SerializeField] private bool enableShadows = true;

        [Header("Camera Controls")]
        [Tooltip("Pan speed with WASD/Arrow keys")]
        [SerializeField] private float panSpeed = 20f;

        [Tooltip("Pan speed when mouse near screen edges")]
        [SerializeField] private float edgePanSpeed = 15f;

        [Tooltip("Distance from screen edge to trigger edge pan (pixels)")]
        [SerializeField] private float edgePanThreshold = 10f;

        [Tooltip("Zoom acceleration (how quickly zoom momentum builds up)")]
        [SerializeField] private float zoomAcceleration = 15f;

        [Tooltip("Zoom deceleration / friction (how quickly momentum slows down, 0-1)")]
        [SerializeField] private float zoomFriction = 0.92f;

        [Tooltip("Minimum view distance (far from ground, zoomed out) - hypotenuse of Y and Z")]
        [SerializeField] private float minViewDistance = 15f;

        [Tooltip("Maximum view distance (close to ground, zoomed in) - hypotenuse of Y and Z")]
        [SerializeField] private float maxViewDistance = 55f;

        [Tooltip("How many units before max zoom to start rotation transition")]
        [SerializeField] private float zoomUnitsBeforeToStartCamRot = 5f;

        [Header("Map Bounds")]
        [Tooltip("Constrain camera to stay within map bounds")]
        [SerializeField] private bool constrainToBounds = false;

        [SerializeField] private Vector2 mapMinBounds = Vector2.zero;
        [SerializeField] private Vector2 mapMaxBounds = new Vector2(100f, 100f);

        [Header("Input Toggles")]
        [Tooltip("Enable WASD/Arrow key panning")]
        [SerializeField] private bool enableKeyboardPan = true;

        [Tooltip("Enable panning when mouse near screen edges (DISABLE if camera jitters)")]
        [SerializeField] private bool enableEdgePan = false;

        [Tooltip("Enable middle mouse button drag panning")]
        [SerializeField] private bool enableMouseDrag = true;

        [Tooltip("Enable scroll wheel zoom")]
        [SerializeField] private bool enableZoom = true;

        private Camera mainCamera;
        private Transform cameraTransform; // The actual camera transform (child)
        private Transform rigTransform; // Parent rig for panning (this GameObject's transform)
        private GameObject groundPlane;
        private Light directionalLight;

        // Camera control state
        private Vector3 dragOrigin;
        private bool isDragging;
        private CancellationTokenSource moveCts;
        private float zoomMomentum; // Current momentum for zoom
        private float targetRotation; // Target rotation during transition
        private float rotationVelocity; // SmoothDamp velocity for rotation

        void Awake()
        {
            // IMPORTANT: Validate camera settings before setup
            if (cameraHeight <= 0)
            {
                Debug.LogError($"Cartographer: Camera Height is {cameraHeight}, but must be POSITIVE (above ground)! Auto-correcting to 30.");
                cameraHeight = 30f;
            }

            if (cameraTiltAngle > 0)
            {
                Debug.LogError($"Cartographer: Camera Tilt Angle is POSITIVE ({cameraTiltAngle}°), but should be NEGATIVE to look down at ground! Auto-correcting to {-cameraTiltAngle}°.");
                cameraTiltAngle = -Mathf.Abs(cameraTiltAngle);
            }

            if (cameraTiltAngle > -10f)
            {
                Debug.LogWarning($"Cartographer: Camera Tilt Angle ({cameraTiltAngle}°) is very shallow. Recommend -30° to -60° for good ground visibility.");
            }

            // Create parent-child hierarchy for separating pan and zoom
            // This script's GameObject becomes the parent rig (handles X/Z panning)
            // Camera becomes child (handles local Y/Z zoom and X rotation)
            // NOTE: SetupCameraHierarchy() creates the camera component on child
            SetupCameraHierarchy();

            // Setup camera for isometric 2.5D rendering
            SetupCamera();

            // Calculate initial view distance
            var initialLocalPos = cameraTransform.localPosition;
            var initialViewDistance = Mathf.Sqrt(initialLocalPos.y * initialLocalPos.y + initialLocalPos.z * initialLocalPos.z);

            Debug.Log($"Cartographer: Initial camera local position: Y={initialLocalPos.y:F2}, Z={initialLocalPos.z:F2}");
            Debug.Log($"Cartographer: Initial view distance: {initialViewDistance:F2}");
            Debug.Log($"Cartographer: View distance range: {minViewDistance} to {maxViewDistance}, rotation transition at: {minViewDistance + zoomUnitsBeforeToStartCamRot}");

            // Create the 3D ground plane
            CreateGroundPlane();

            // Setup lighting
            if (createDirectionalLight)
            {
                SetupLighting();
            }
        }

        void OnDestroy()
        {
            moveCts?.Cancel();
            moveCts?.Dispose();
        }

        void Update()
        {
            HandleKeyboardPan();
            HandleEdgePan();
            HandleMouseDrag();
            HandleZoom();
        }

        /// <summary>
        /// Sets up the camera hierarchy: parent rig for panning, child camera for zoom.
        /// This separates pan (X/Z world movement) from zoom (local Y/Z movement).
        /// </summary>
        private void SetupCameraHierarchy()
        {
            // This GameObject becomes the rig (parent) - it already has Cartographer script
            rigTransform = transform;
            rigTransform.name = "CameraRig";

            // Remove MainCamera tag from rig if it has one
            if (rigTransform.gameObject.CompareTag("MainCamera"))
            {
                rigTransform.gameObject.tag = "Untagged";
                Debug.Log("Cartographer: Removed MainCamera tag from rig");
            }

            // Check if old camera component exists on rig
            Camera oldCamera = rigTransform.GetComponent<Camera>();
            if (oldCamera != null)
            {
                // URP adds additional components that depend on Camera
                // Find and destroy all components that might depend on Camera
                var allComponents = rigTransform.GetComponents<Component>();
                foreach (var component in allComponents)
                {
                    // Skip the transform, Cartographer script itself, and the Camera (we'll destroy it last)
                    if (component is Transform || component is Cartographer || component is Camera)
                        continue;

                    // Check if this is a URP camera data component (by name check, not type)
                    if (component.GetType().Name.Contains("CameraData"))
                    {
                        DestroyImmediate(component);
                        Debug.Log($"Cartographer: Removed {component.GetType().Name} from rig");
                    }
                }

                // Now safe to destroy the Camera
                DestroyImmediate(oldCamera);
                Debug.Log("Cartographer: Removed old camera component from rig");
            }

            // Create child GameObject for the camera
            GameObject cameraChild = new GameObject("Camera");
            cameraTransform = cameraChild.transform;
            cameraTransform.SetParent(rigTransform);
            cameraTransform.localPosition = Vector3.zero;
            cameraTransform.localRotation = Quaternion.identity;

            // Create new camera component on child
            mainCamera = cameraChild.AddComponent<Camera>();
            mainCamera.eventMask = 0; // Disable mouse events

            // IMPORTANT: Tag as MainCamera so Camera.main finds this one
            cameraChild.tag = "MainCamera";

            Debug.Log($"Cartographer: Camera hierarchy created - Rig: {rigTransform.name}, Camera: {cameraTransform.name}, tagged as MainCamera");
        }

        /// <summary>
        /// Configures camera for Song of Conquest style isometric 2.5D rendering.
        /// Positions parent rig at ground center (X/Z), child camera handles height/zoom (local Y/Z) and rotation.
        /// </summary>
        private void SetupCamera()
        {
            // Perspective camera (not orthographic!)
            mainCamera.orthographic = false;
            mainCamera.fieldOfView = fieldOfView;

            // Ground plane center (what we want to look at)
            // Ground mesh is centered but positioned so map tiles align at (0,0)
            // So center is at half the ground size
            Vector3 groundCenter = new Vector3(groundSize.x / 2f, 0f, groundSize.y / 2f);

            // Position parent rig at ground center (X/Z only, Y stays at 0)
            rigTransform.position = new Vector3(groundCenter.x, 0f, groundCenter.z);

            // Position child camera with local Y (height) and local Z (zoom distance)
            // initialZoomPosition is negative, positioning camera behind the rig
            cameraTransform.localPosition = new Vector3(0f, cameraHeight, initialZoomPosition);

            // Rotate child camera (not rig) - only X tilt
            // Unity's Euler X rotation:
            // - Positive X = look DOWN (towards ground)
            // - Negative X = look UP (towards sky)
            // cameraTiltAngle is stored as negative (e.g., -30°) for semantic meaning
            // We need positive Euler X to look down, so we use Mathf.Abs() to convert -30° → 30°
            cameraTransform.localRotation = Quaternion.Euler(Mathf.Abs(cameraTiltAngle), 0f, 0f);

            // Make sure camera can see far enough
            mainCamera.nearClipPlane = 0.3f;
            mainCamera.farClipPlane = 1000f;

            // Clear to solid color (not skybox)
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);

            Debug.Log($"Cartographer: Camera configured:");
            Debug.Log($"  Ground size: {groundSize}");
            Debug.Log($"  Ground center: {groundCenter}");
            Debug.Log($"  Rig position (world): {rigTransform.position}");
            Debug.Log($"  Camera local position: {cameraTransform.localPosition} (height: {cameraHeight}, initial Z: {initialZoomPosition})");
            Debug.Log($"  Camera world position: {cameraTransform.position}");
            Debug.Log($"  Camera local rotation (euler): {cameraTransform.localRotation.eulerAngles}");
            Debug.Log($"  Camera tilt: close={cameraTiltAngle}°, far={maxZoomCamRot}°");
            Debug.Log($"  FOV: {fieldOfView}°, Near: {mainCamera.nearClipPlane}, Far: {mainCamera.farClipPlane}");
            Debug.Log($"  Distance to ground center: {Vector3.Distance(cameraTransform.position, groundCenter):F2}");
            Debug.Log($"  Map bounds: {mapMinBounds} to {mapMaxBounds}");
            Debug.Log($"  Constrain to bounds: {constrainToBounds}");

            // Test raycast to verify camera can see ground
            Ray testRay = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
            Plane testPlane = new Plane(Vector3.up, Vector3.zero);
            if (testPlane.Raycast(testRay, out float testEnter))
            {
                Vector3 testHit = testRay.GetPoint(testEnter);
                Debug.Log($"  ✓ Center screen raycast hits ground at: {testHit}");
            }
            else
            {
                Debug.LogWarning($"  ⚠️ Center screen raycast MISSES ground plane! Camera may be pointing wrong direction.");
            }
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

            // Position ground plane so it starts at (0, 0, 0) and extends to (groundSize.x, 0, groundSize.y)
            // The mesh is centered at origin, so we offset by half the size to align with map tiles
            groundPlane.transform.position = new Vector3(groundSize.x / 2f, 0f, groundSize.y / 2f);
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
        /// Updates the ground material (for texture splatting shader).
        /// </summary>
        public void SetGroundMaterial(Material material)
        {
            if (groundPlane != null)
            {
                MeshRenderer renderer = groundPlane.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.material = material;
                    Debug.Log($"Cartographer: Ground material updated to: {material.name}");
                }
            }
        }

        /// <summary>
        /// Updates the splatmap texture for InnoGames/Terrain shader.
        /// Call this when map changes to regenerate terrain distribution.
        /// </summary>
        public void SetSplatmap(Texture2D splatmap)
        {
            if (groundPlane != null)
            {
                MeshRenderer renderer = groundPlane.GetComponent<MeshRenderer>();
                if (renderer != null && renderer.material != null)
                {
                    // Check if material uses InnoGames/Terrain shader
                    if (renderer.material.shader.name == "InnoGames/Terrain")
                    {
                        renderer.material.SetTexture("_SplatMap", splatmap);
                        Debug.Log($"Cartographer: Splatmap updated ({splatmap.width}x{splatmap.height})");
                    }
                    else
                    {
                        Debug.LogWarning($"Cartographer: Material shader is '{renderer.material.shader.name}', not 'InnoGames/Terrain'. Splatmap not applied.");
                    }
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
            // Safety check: ensure mouse is within screen bounds
            Vector3 mousePos = Input.mousePosition;
            if (mousePos.x < 0 || mousePos.x > Screen.width ||
                mousePos.y < 0 || mousePos.y > Screen.height)
            {
                worldPos = Vector3.zero;
                return false;
            }

            try
            {
                Ray ray = mainCamera.ScreenPointToRay(mousePos);
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero); // Plane at Y=0, facing up

                if (groundPlane.Raycast(ray, out float enter))
                {
                    worldPos = ray.GetPoint(enter);
                    return true;
                }
            }
            catch (System.Exception)
            {
                // Silently catch any raycast exceptions
                worldPos = Vector3.zero;
                return false;
            }

            worldPos = Vector3.zero;
            return false;
        }

        /// <summary>
        /// Debug visualization.
        /// Shows both rig position and camera position in hierarchy.
        /// </summary>
        void OnDrawGizmos()
        {
            // Draw ground plane bounds (positioned to align with map at origin)
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3(groundSize.x / 2f, 0f, groundSize.y / 2f);
            Gizmos.DrawWireCube(center, new Vector3(groundSize.x, 0.1f, groundSize.y));

            // Draw camera hierarchy visualization
            if (mainCamera != null && cameraTransform != null && rigTransform != null)
            {
                // Draw rig position as orange sphere (parent)
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(rigTransform.position, 0.5f);

                // Draw camera position as green sphere (child)
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(cameraTransform.position, 1f);

                // Draw line connecting rig to camera
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(rigTransform.position, cameraTransform.position);

                // Draw camera forward ray (what camera is looking at)
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(cameraTransform.position, cameraTransform.forward * 20f);

                // Draw camera up direction
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(cameraTransform.position, cameraTransform.up * 5f);

                // Draw camera right direction
                Gizmos.color = Color.red;
                Gizmos.DrawRay(cameraTransform.position, cameraTransform.right * 5f);

                // Draw line from camera to ground center
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(cameraTransform.position, center);

                // Raycast from camera center to show where it's looking (Play mode only)
                if (Application.isPlaying && Screen.width > 0 && Screen.height > 0)
                {
                    try
                    {
                        Ray centerRay = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
                        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                        if (groundPlane.Raycast(centerRay, out float enter))
                        {
                            Vector3 hitPoint = centerRay.GetPoint(enter);
                            Gizmos.color = Color.white;
                            Gizmos.DrawWireSphere(hitPoint, 0.5f);
                            Gizmos.DrawLine(cameraTransform.position, hitPoint);
                        }
                    }
                    catch (System.Exception)
                    {
                        // Silently catch raycast errors in gizmos
                    }
                }
            }
        }

        // ==================== CAMERA CONTROLS (from CameraController) ====================

        /// <summary>
        /// Handles keyboard WASD/Arrow key panning.
        /// Moves parent rig on X/Z plane (not child camera).
        /// </summary>
        private void HandleKeyboardPan()
        {
            if (!enableKeyboardPan)
                return;

            var moveDir = Vector3.zero;

            // Map keyboard input to X,Z movement (ground plane)
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                moveDir.z += 1f; // Forward on ground
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                moveDir.z -= 1f; // Backward on ground
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                moveDir.x -= 1f; // Left
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                moveDir.x += 1f; // Right

            // Only move if there's actual input
            if (moveDir != Vector3.zero)
            {
                var newPos = rigTransform.position + moveDir.normalized * panSpeed * Time.deltaTime;
                rigTransform.position = ConstrainPosition(newPos);
            }
        }

        /// <summary>
        /// Handles edge panning (mouse near screen edges).
        /// Moves parent rig on X/Z plane (not child camera).
        /// </summary>
        private void HandleEdgePan()
        {
            if (!enableEdgePan)
                return;

            // Bounds check: only pan if mouse is actually within screen
            var mousePos = Input.mousePosition;
            if (mousePos.x < 0 || mousePos.x > Screen.width ||
                mousePos.y < 0 || mousePos.y > Screen.height)
            {
                return; // Mouse outside screen, don't edge pan
            }

            var moveDir = Vector3.zero;

            if (mousePos.x < edgePanThreshold)
                moveDir.x -= 1f;
            else if (mousePos.x > Screen.width - edgePanThreshold)
                moveDir.x += 1f;

            if (mousePos.y < edgePanThreshold)
                moveDir.z -= 1f; // Z for ground plane
            else if (mousePos.y > Screen.height - edgePanThreshold)
                moveDir.z += 1f;

            // Only move if there's actual edge detection
            if (moveDir != Vector3.zero)
            {
                var newPos = rigTransform.position + moveDir.normalized * edgePanSpeed * Time.deltaTime;
                rigTransform.position = ConstrainPosition(newPos);
            }
        }

        /// <summary>
        /// Handles middle mouse button drag panning.
        /// Moves parent rig on X/Z plane (not child camera).
        /// </summary>
        private void HandleMouseDrag()
        {
            if (!enableMouseDrag)
                return;

            // Check if mouse is within screen bounds
            Vector3 mousePos = Input.mousePosition;
            if (mousePos.x < 0 || mousePos.x > Screen.width || mousePos.y < 0 || mousePos.y > Screen.height)
            {
                if (isDragging)
                    isDragging = false;
                return;
            }

            if (Input.GetMouseButtonDown(2)) // Middle mouse button
            {
                // Get ground point under mouse
                if (GetMouseWorldPosition(out Vector3 worldPos))
                {
                    dragOrigin = worldPos;
                    isDragging = true;
                }
            }

            if (Input.GetMouseButton(2) && isDragging)
            {
                if (GetMouseWorldPosition(out Vector3 currentPos))
                {
                    // Calculate difference on X,Z plane only
                    Vector3 diff = dragOrigin - currentPos;
                    diff.y = 0; // Don't pan vertically

                    var newPos = rigTransform.position + diff;
                    rigTransform.position = ConstrainPosition(newPos);

                    // Update drag origin for next frame to avoid accumulation
                    dragOrigin = currentPos;
                }
            }

            if (Input.GetMouseButtonUp(2))
            {
                isDragging = false;
            }
        }

        /// <summary>
        /// Handles scroll wheel zoom with momentum-based acceleration.
        /// Uses view distance (hypotenuse of Y and Z) to limit zoom, allowing flexible camera positioning.
        /// Features:
        /// - Momentum builds up when scrolling (acceleration)
        /// - Coasts to a smooth stop after scrolling stops (friction)
        /// - Automatic rotation change when zooming in close
        /// - Maintains ground focus point during rotation transitions
        /// - Zoom limits based on distance from ground point, not just Z position
        /// </summary>
        private void HandleZoom()
        {
            if (!enableZoom)
                return;

            var scrollDelta = Input.mouseScrollDelta.y;

            // Add momentum from scroll input (acceleration)
            // INVERTED: Scroll forward (positive) zooms in (negative momentum), scroll back zooms out
            if (scrollDelta != 0)
            {
                zoomMomentum -= scrollDelta * zoomAcceleration * Time.deltaTime; // Note the minus sign
            }

            // Apply friction to momentum (deceleration)
            zoomMomentum *= zoomFriction;

            // Check if momentum is negligible
            var hasMomentum = Mathf.Abs(zoomMomentum) >= 0.001f;

            // Get current rotation early for checks
            float currentRotation = cameraTransform.localRotation.eulerAngles.x;
            if (currentRotation > 180f) currentRotation -= 360f;

            if (!hasMomentum)
            {
                zoomMomentum = 0f;

                // Even without momentum, continue smoothing rotation if it hasn't reached target yet
                // Check if rotation needs to continue smoothing
                var rotationDelta = Mathf.Abs(currentRotation - Mathf.Abs(targetRotation));
                if (rotationDelta < 0.1f)
                {
                    return; // Rotation and zoom both settled
                }
                // Otherwise, continue to apply rotation smoothing below
            }

            // Work with LOCAL position of child camera
            var currentLocalPos = cameraTransform.localPosition;
            var currentY = currentLocalPos.y;
            var currentZ = currentLocalPos.z;

            // Calculate current view distance (hypotenuse from camera to ground point directly below)
            // This is the "true" distance from camera to ground, accounting for both height and backward distance
            var currentViewDistance = Mathf.Sqrt(currentY * currentY + currentZ * currentZ);

            // Apply momentum to view distance (don't clamp yet - let it overshoot for rotation)
            var targetViewDistance = currentViewDistance + zoomMomentum;

            // Calculate TARGET rotation based on target view distance (before clamping)
            // This ensures rotation reaches target even when momentum would overshoot
            // At max distance (far zoom): use maxZoomCamRot (steep angle like -60°, looking more down)
            // At min distance (close zoom): use cameraTiltAngle (shallow angle like -30°, looking more forward)
            var rotationTransitionStart = minViewDistance + zoomUnitsBeforeToStartCamRot;

            if (targetViewDistance >= rotationTransitionStart)
            {
                // Far zoom: use steep angle to see more of the map
                targetRotation = maxZoomCamRot;
            }
            else if (targetViewDistance <= minViewDistance)
            {
                // Max zoom (closest): use shallow angle for close-up view
                targetRotation = cameraTiltAngle;
            }
            else
            {
                // Transition zone: interpolate from shallow (cameraTiltAngle) to steep (maxZoomCamRot)
                var t = Mathf.InverseLerp(minViewDistance, rotationTransitionStart, targetViewDistance);
                // Apply easing for smooth rotation (ease in/out)
                t = t * t * (3f - 2f * t); // Smoothstep
                targetRotation = Mathf.Lerp(cameraTiltAngle, maxZoomCamRot, t);
            }

            // Now clamp view distance for actual position
            var newViewDistance = Mathf.Clamp(targetViewDistance, minViewDistance, maxViewDistance);

            // Stop momentum if we hit the bounds
            if (newViewDistance == minViewDistance || newViewDistance == maxViewDistance)
            {
                zoomMomentum = 0f;
            }

            var distanceDelta = newViewDistance - currentViewDistance;

            // If no momentum and no distance change, we're only updating rotation
            if (!hasMomentum && Mathf.Abs(distanceDelta) < 0.001f)
            {
                // Keep view distance constant, only update rotation
                newViewDistance = currentViewDistance;
                distanceDelta = 0f;
            }

            // Work with local position
            var localPos = currentLocalPos;

            // Smoothly interpolate rotation using SmoothDamp to match momentum feel
            // This creates a smooth acceleration/deceleration that matches the zoom momentum
            var smoothedRotation = Mathf.SmoothDampAngle(
                currentRotation,
                Mathf.Abs(targetRotation),
                ref rotationVelocity,
                0.15f, // Smooth time - matches the "feel" of momentum
                Mathf.Infinity,
                Time.deltaTime
            );

            var isRotationChanging = Mathf.Abs(smoothedRotation - currentRotation) > 0.01f;

            if (isRotationChanging)
            {
                // ONLY compensate Y position when rotation is actively changing
                // This maintains the ground point during rotation transition

                // Calculate where camera is currently looking on the ground (y=0 plane)
                // Camera looks DOWN and FORWARD. With camera at local (0, y, z) and rotation rot:
                // Ground point Z = camera.localZ + |camera.localY| / tan(|rot|)
                var currentTanAngle = Mathf.Tan(Mathf.Abs(currentRotation) * Mathf.Deg2Rad);

                // Safety check: prevent division by zero or very small values
                if (Mathf.Abs(currentTanAngle) < 0.001f)
                    currentTanAngle = 0.001f * Mathf.Sign(currentTanAngle);

                // Calculate ground point BEFORE zoom movement
                var groundPointZ = currentZ + currentY / currentTanAngle;

                // Calculate new Y and Z based on new view distance and smoothed rotation
                // Using right triangle: viewDistance is hypotenuse, Y and Z are the legs
                // Y / viewDistance = sin(rotation), Z / viewDistance = -cos(rotation)
                // Note: Z is negative because camera looks in -Z direction in local space
                var rotRad = Mathf.Abs(smoothedRotation) * Mathf.Deg2Rad;
                var newY = newViewDistance * Mathf.Sin(rotRad);
                var newZ = -newViewDistance * Mathf.Cos(rotRad);

                // Adjust Z to maintain ground point
                var newTanAngle = Mathf.Tan(Mathf.Abs(smoothedRotation) * Mathf.Deg2Rad);
                if (Mathf.Abs(newTanAngle) < 0.001f)
                    newTanAngle = 0.001f * Mathf.Sign(newTanAngle);

                // Recalculate to maintain ground point: newZ = groundPointZ - newY / tan(smoothedRot)
                newZ = groundPointZ - newY / newTanAngle;

                localPos.y = newY;
                localPos.z = newZ;
            }
            else
            {
                // Rotation is constant: scale Y and Z proportionally to achieve new view distance
                // Maintain the same angle: newY/newZ = currentY/currentZ
                var rotRad = Mathf.Abs(smoothedRotation) * Mathf.Deg2Rad;

                // Calculate new Y and Z from view distance and current rotation
                // Y / viewDistance = sin(rotation), Z / viewDistance = -cos(rotation)
                localPos.y = newViewDistance * Mathf.Sin(rotRad);
                localPos.z = -newViewDistance * Mathf.Cos(rotRad);
            }

            // Apply smoothed rotation with Y and Z locked to 0 (only X tilt changes)
            // smoothedRotation is already positive (from SmoothDampAngle output)
            cameraTransform.localRotation = Quaternion.Euler(smoothedRotation, 0f, 0f);

            // Apply local position to child camera (no world position constraints)
            cameraTransform.localPosition = localPos;
        }

        /// <summary>
        /// Constrains rig position to map bounds.
        /// Works with parent rig position on X/Z plane.
        /// </summary>
        private Vector3 ConstrainPosition(Vector3 position)
        {
            if (!constrainToBounds)
                return position;

            // Get camera rotation from child transform
            var rotation = cameraTransform.localRotation.eulerAngles.x;
            if (rotation > 180f) rotation -= 360f;

            // Get camera world Y position (height above ground)
            var cameraWorldY = cameraTransform.position.y;

            // Calculate the ground point where camera center looks (on X,Z plane at Y=0)
            var tanAngle = Mathf.Tan(Mathf.Abs(rotation) * Mathf.Deg2Rad);

            // Safety check: prevent division by zero or very small values
            if (Mathf.Abs(tanAngle) < 0.001f)
                tanAngle = 0.001f * Mathf.Sign(tanAngle);

            // Ground center Z calculation using rig position + camera offset
            var cameraWorldZ = position.z + cameraTransform.localPosition.z;
            var groundCenterZ = cameraWorldZ + cameraWorldY / tanAngle;

            // For X bounds: use horizontal FOV calculation
            // Distance to ground center along view ray
            var distanceToGround = cameraWorldY / Mathf.Sin(Mathf.Abs(rotation) * Mathf.Deg2Rad);
            var horizontalFOV = mainCamera.fieldOfView * mainCamera.aspect;
            var groundWidth = 2f * distanceToGround * Mathf.Tan(horizontalFOV * 0.5f * Mathf.Deg2Rad);

            // X bounds: keep camera so edges don't go outside map
            var minX = mapMinBounds.x + groundWidth / 2f;
            var maxX = mapMaxBounds.x - groundWidth / 2f;

            if (minX >= maxX)
                position.x = (mapMinBounds.x + mapMaxBounds.x) / 2f;
            else
                position.x = Mathf.Clamp(position.x, minX, maxX);

            // For Z bounds: constrain based on ground point, not rig position
            // We want the ground point to stay within map bounds with some buffer
            var minGroundZ = mapMinBounds.y + 5f; // Use mapBounds.y for Z axis
            var maxGroundZ = mapMaxBounds.y - 5f;

            if (minGroundZ >= maxGroundZ)
            {
                // Map too small, center on it
                groundCenterZ = (mapMinBounds.y + mapMaxBounds.y) / 2f;
            }
            else
            {
                groundCenterZ = Mathf.Clamp(groundCenterZ, minGroundZ, maxGroundZ);
            }

            // Convert ground point back to rig position
            position.z = groundCenterZ - cameraWorldY / tanAngle - cameraTransform.localPosition.z;

            // Safety check: ensure position is valid (no NaN or Infinity)
            if (float.IsNaN(position.x) || float.IsInfinity(position.x) ||
                float.IsNaN(position.y) || float.IsInfinity(position.y) ||
                float.IsNaN(position.z) || float.IsInfinity(position.z))
            {
                Debug.LogError($"ConstrainPosition produced invalid position: {position}. Returning unchanged.");
                return rigTransform.position; // Return current position if calculation failed
            }

            return position;
        }

        /// <summary>
        /// Smooth rig movement to target position.
        /// Moves parent rig, not child camera.
        /// </summary>
        public async UniTask MoveToAsync(Vector3 targetPosition, float duration = 1f, CancellationToken ct = default)
        {
            // Cancel any previous movement
            moveCts?.Cancel();
            moveCts?.Dispose();
            moveCts = CancellationTokenSource.CreateLinkedTokenSource(ct, this.GetCancellationTokenOnDestroy());

            var startPos = rigTransform.position;
            targetPosition = ConstrainPosition(targetPosition);
            var elapsedTime = 0f;

            try
            {
                while (elapsedTime < duration)
                {
                    elapsedTime += Time.deltaTime;
                    var t = Mathf.Clamp01(elapsedTime / duration);
                    t = Mathf.SmoothStep(0f, 1f, t); // Smooth ease in/out
                    rigTransform.position = Vector3.Lerp(startPos, targetPosition, t);

                    await UniTask.Yield(PlayerLoopTiming.Update, moveCts.Token);
                }

                rigTransform.position = targetPosition;
            }
            catch (System.OperationCanceledException)
            {
                // Movement was cancelled
            }
        }

        /// <summary>
        /// Fire-and-forget wrapper for backwards compatibility.
        /// </summary>
        public void MoveTo(Vector3 targetPosition, float duration = 1f)
        {
            MoveToAsync(targetPosition, duration).Forget();
        }

        /// <summary>
        /// Center rig on a position instantly (maintains current zoom).
        /// </summary>
        public void CenterOn(Vector3 position)
        {
            position.z = rigTransform.position.z; // Maintain rig Z position
            rigTransform.position = ConstrainPosition(position);
        }

        /// <summary>
        /// Set map bounds from map size.
        /// </summary>
        public void SetMapBounds(int width, int height)
        {
            mapMinBounds = new Vector2(0, 0);
            mapMaxBounds = new Vector2(width, height);
        }

        /// <summary>
        /// Check if a world position is visible in camera view.
        /// </summary>
        public bool IsPositionVisible(Vector3 worldPosition)
        {
            var viewportPos = mainCamera.WorldToViewportPoint(worldPosition);
            return viewportPos.x >= 0 && viewportPos.x <= 1 &&
                   viewportPos.y >= 0 && viewportPos.y <= 1;
        }

        /// <summary>
        /// Context menu helper to recenter camera.
        /// </summary>
        [ContextMenu("Recenter Camera")]
        public void RecenterCamera()
        {
            SetupCamera();
        }

        /// <summary>
        /// Updates the ground plane size (useful when map size changes).
        /// Recreates the ground mesh with new dimensions.
        /// </summary>
        public void SetGroundSize(float width, float height)
        {
            groundSize = new Vector2(width, height);

            // Recreate ground plane with new size
            if (groundPlane != null)
            {
                Destroy(groundPlane);
            }

            CreateGroundPlane();

            // Recenter camera to look at new ground center
            SetupCamera();

            Debug.Log($"Cartographer: Ground size updated to {width}x{height}");
        }

        /// <summary>
        /// Enables or disables camera controls.
        /// </summary>
        public void EnableControls(bool enabled)
        {
            enableKeyboardPan = enabled;
            enableEdgePan = enabled;
            enableMouseDrag = enabled;
            enableZoom = enabled;
        }

        /// <summary>
        /// Gets the current ground size.
        /// </summary>
        public Vector2 GetGroundSize()
        {
            return groundSize;
        }

        /// <summary>
        /// Gets the camera component.
        /// </summary>
        public Camera GetCamera()
        {
            return mainCamera;
        }

        /// <summary>
        /// Gets the current zoom level as a percentage (0-100).
        /// 0% = maximum zoom out (maxViewDistance), 100% = maximum zoom in (minViewDistance).
        /// </summary>
        public float GetZoomPercentage()
        {
            var localPos = cameraTransform.localPosition;
            var currentViewDistance = Mathf.Sqrt(localPos.y * localPos.y + localPos.z * localPos.z);
            // Invert: min distance = 100% zoom, max distance = 0% zoom
            return (1f - Mathf.InverseLerp(minViewDistance, maxViewDistance, currentViewDistance)) * 100f;
        }

        /// <summary>
        /// Gets the current view distance from camera to ground point.
        /// </summary>
        public float GetViewDistance()
        {
            var localPos = cameraTransform.localPosition;
            return Mathf.Sqrt(localPos.y * localPos.y + localPos.z * localPos.z);
        }

        /// <summary>
        /// Gets the view distance range (min and max).
        /// </summary>
        public Vector2 GetViewDistanceRange()
        {
            return new Vector2(minViewDistance, maxViewDistance);
        }
    }
}
