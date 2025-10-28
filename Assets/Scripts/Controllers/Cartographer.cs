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

        [Header("Camera Controls")]
        [Tooltip("Pan speed with WASD/Arrow keys")]
        [SerializeField] private float panSpeed = 20f;

        [Tooltip("Pan speed when mouse near screen edges")]
        [SerializeField] private float edgePanSpeed = 15f;

        [Tooltip("Distance from screen edge to trigger edge pan (pixels)")]
        [SerializeField] private float edgePanThreshold = 10f;

        [Tooltip("Zoom speed (scroll wheel sensitivity)")]
        [SerializeField] private float zoomSpeed = 2.5f;

        [Tooltip("Minimum zoom distance (closer to ground)")]
        [SerializeField] private float minPerspZoom = -50f;

        [Tooltip("Maximum zoom distance (further from ground)")]
        [SerializeField] private float maxPerspZoom = -10f;

        [Tooltip("How many units before max zoom to start rotation transition")]
        [SerializeField] private float zoomUnitsBeforeToStartCamRot = 5f;

        [Tooltip("Camera rotation at normal zoom (looking down angle)")]
        [SerializeField] private float regularCamRot = -30f;

        [Tooltip("Camera rotation at max zoom (steeper angle)")]
        [SerializeField] private float maxZoomCamRot = -60f;

        [Header("Map Bounds")]
        [Tooltip("Constrain camera to stay within map bounds")]
        [SerializeField] private bool constrainToBounds = true;

        [SerializeField] private Vector2 mapMinBounds = Vector2.zero;
        [SerializeField] private Vector2 mapMaxBounds = new Vector2(100f, 100f);

        [Header("Input Toggles")]
        [Tooltip("Enable WASD/Arrow key panning")]
        [SerializeField] private bool enableKeyboardPan = true;

        [Tooltip("Enable panning when mouse near screen edges")]
        [SerializeField] private bool enableEdgePan = true;

        [Tooltip("Enable middle mouse button drag panning")]
        [SerializeField] private bool enableMouseDrag = true;

        [Tooltip("Enable scroll wheel zoom")]
        [SerializeField] private bool enableZoom = true;

        private Camera mainCamera;
        private GameObject groundPlane;
        private Light directionalLight;

        // Camera control state
        private Vector3 dragOrigin;
        private bool isDragging;
        private CancellationTokenSource moveCts;
        private float targetZPosition;
        private float zoomVelocity;

        void Awake()
        {
            mainCamera = GetComponent<Camera>();

            // Initialize target zoom position
            targetZPosition = transform.position.z;

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
        /// Configures camera for Song of Conquest style isometric 2.5D rendering.
        /// </summary>
        private void SetupCamera()
        {
            // Perspective camera (not orthographic!)
            mainCamera.orthographic = false;
            mainCamera.fieldOfView = fieldOfView;

            // Ground plane center (what we want to look at)
            Vector3 groundCenter = new Vector3(groundSize.x / 2f, 0f, groundSize.y / 2f);

            // Simple position calculation: place camera above and behind ground center
            // Using the configured height and distance values
            float cameraX = groundCenter.x;
            float cameraY = cameraHeight;
            float cameraZ = groundCenter.z + cameraDistance; // cameraDistance is negative, so this moves camera back

            Vector3 cameraPos = new Vector3(cameraX, cameraY, cameraZ);

            // Apply Y rotation (isometric angle) by rotating position around ground center
            float yawRad = cameraYRotation * Mathf.Deg2Rad;
            Vector3 offset = cameraPos - groundCenter;
            float rotatedX = offset.x * Mathf.Cos(yawRad) - offset.z * Mathf.Sin(yawRad);
            float rotatedZ = offset.x * Mathf.Sin(yawRad) + offset.z * Mathf.Cos(yawRad);
            cameraPos = groundCenter + new Vector3(rotatedX, offset.y, rotatedZ);

            // Set camera position
            transform.position = cameraPos;

            // Point camera at ground center
            transform.LookAt(groundCenter);

            // Apply additional tilt if needed (adjust X rotation)
            Vector3 eulerAngles = transform.eulerAngles;
            eulerAngles.x = cameraTiltAngle;
            eulerAngles.y = cameraYRotation;
            transform.eulerAngles = eulerAngles;

            // Make sure camera can see far enough
            mainCamera.nearClipPlane = 0.3f;
            mainCamera.farClipPlane = 1000f;

            // Clear to solid color (not skybox)
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);

            Debug.Log($"Cartographer: Camera configured:");
            Debug.Log($"  Ground size: {groundSize}");
            Debug.Log($"  Ground center: {groundCenter}");
            Debug.Log($"  Camera position: {transform.position}");
            Debug.Log($"  Camera rotation: {transform.rotation.eulerAngles}");
            Debug.Log($"  FOV: {fieldOfView}°, Near: {mainCamera.nearClipPlane}, Far: {mainCamera.farClipPlane}");
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

        // ==================== CAMERA CONTROLS (from CameraController) ====================

        /// <summary>
        /// Handles keyboard WASD/Arrow key panning.
        /// Note: Movement is on X,Z plane (not X,Y like old 2D system).
        /// </summary>
        private void HandleKeyboardPan()
        {
            if (!enableKeyboardPan)
                return;

            var moveDir = Vector3.zero;

            // Map keyboard input to X,Z movement (ground plane)
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                moveDir.z += 1f; // Forward on ground (was Y in 2D)
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                moveDir.z -= 1f; // Backward on ground
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                moveDir.x -= 1f; // Left
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                moveDir.x += 1f; // Right

            if (moveDir != Vector3.zero)
            {
                var newPos = transform.position + moveDir.normalized * panSpeed * Time.deltaTime;
                transform.position = ConstrainPosition(newPos);
            }
        }

        /// <summary>
        /// Handles edge panning (mouse near screen edges).
        /// </summary>
        private void HandleEdgePan()
        {
            if (!enableEdgePan)
                return;

            var mousePos = Input.mousePosition;
            var moveDir = Vector3.zero;

            if (mousePos.x < edgePanThreshold)
                moveDir.x -= 1f;
            else if (mousePos.x > Screen.width - edgePanThreshold)
                moveDir.x += 1f;

            if (mousePos.y < edgePanThreshold)
                moveDir.z -= 1f; // Changed from Y to Z for ground plane
            else if (mousePos.y > Screen.height - edgePanThreshold)
                moveDir.z += 1f;

            if (moveDir != Vector3.zero)
            {
                var newPos = transform.position + moveDir.normalized * edgePanSpeed * Time.deltaTime;
                transform.position = ConstrainPosition(newPos);
            }
        }

        /// <summary>
        /// Handles middle mouse button drag panning.
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

                    var newPos = transform.position + diff;
                    transform.position = ConstrainPosition(newPos);

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
        /// Handles scroll wheel zoom with smooth camera rotation transition.
        /// This is the sophisticated zoom system from CameraController with:
        /// - Smooth damping for zoom feel
        /// - Automatic rotation change when zooming in close
        /// - Maintains ground focus point during rotation transitions
        /// </summary>
        private void HandleZoom()
        {
            if (!enableZoom)
                return;

            var scrollDelta = Input.mouseScrollDelta.y;

            // Perspective zoom with smooth easing and rotation (exact copy from CameraController)
            if (scrollDelta != 0)
            {
                // Update target Z position (positive scroll = zoom in = increase Z toward 0)
                targetZPosition += scrollDelta * zoomSpeed;
                targetZPosition = Mathf.Clamp(targetZPosition, minPerspZoom, maxPerspZoom);
            }

            // Smooth zoom with quick ease in/out (exponential decay for snappy feel)
            var currentZ = transform.position.z;
            var newZ = Mathf.SmoothDamp(currentZ, targetZPosition, ref zoomVelocity, 0.15f, Mathf.Infinity, Time.deltaTime);
            var deltaZ = newZ - currentZ;

            // Calculate rotation based on zoom level
            // Rotation stays at regularCamRot until we're close to max zoom, then transitions to maxZoomCamRot
            float xRotation;
            var rotationTransitionStart = maxPerspZoom - zoomUnitsBeforeToStartCamRot;

            if (newZ <= rotationTransitionStart)
            {
                // Far zoom: regular rotation
                xRotation = regularCamRot;
            }
            else if (newZ >= maxPerspZoom)
            {
                // Max zoom: steeper rotation
                xRotation = maxZoomCamRot;
            }
            else
            {
                // Transition zone: interpolate from regularCamRot to maxZoomCamRot
                var t = Mathf.InverseLerp(rotationTransitionStart, maxPerspZoom, newZ);
                // Apply easing for smooth rotation (ease in/out)
                t = t * t * (3f - 2f * t); // Smoothstep
                xRotation = Mathf.Lerp(regularCamRot, maxZoomCamRot, t);
            }

            var pos = transform.position;

            // Check if rotation is changing
            var currentRotation = transform.eulerAngles.x;
            if (currentRotation > 180f) currentRotation -= 360f;

            var isRotationChanging = Mathf.Abs(xRotation - currentRotation) > 0.01f;

            if (isRotationChanging)
            {
                // ONLY compensate Y position when rotation is actively changing
                // This maintains the ground point during rotation transition

                // Calculate where camera is currently looking on the ground (y=0 plane)
                // Camera looks DOWN and FORWARD. With camera at (x, y, z) and rotation rot:
                // Ground point Z = camera.z + |camera.y| / tan(|rot|)  [ADAPTED FOR 3D COORDS]
                var currentTanAngle = Mathf.Tan(Mathf.Abs(currentRotation) * Mathf.Deg2Rad);
                var groundPointZ = pos.z + pos.y / currentTanAngle;

                // Apply Z movement
                pos.z = newZ;

                // Recalculate camera Y to maintain same ground point with new rotation
                // Solve: groundPointZ = pos.z + pos.y / tan(|newRot|)
                // Therefore: pos.y = (groundPointZ - pos.z) * tan(|newRot|)
                var newTanAngle = Mathf.Tan(Mathf.Abs(xRotation) * Mathf.Deg2Rad);
                pos.y = (groundPointZ - pos.z) * newTanAngle;
            }
            else
            {
                // Rotation is constant: move in straight diagonal line
                // Y movement is proportional to Z movement at fixed angle
                var tanAngle = Mathf.Tan(Mathf.Abs(xRotation) * Mathf.Deg2Rad);
                deltaZ = newZ - currentZ;
                pos.z = newZ;
                pos.y += deltaZ * tanAngle; // Move diagonally at constant angle
            }

            // Apply rotation
            var rot = transform.eulerAngles;
            rot.x = xRotation;
            transform.eulerAngles = rot;

            // Apply position
            transform.position = ConstrainPosition(pos);
        }

        /// <summary>
        /// Constrains camera position to map bounds.
        /// Adapted for 3D ground plane (X,Z instead of X,Y).
        /// </summary>
        private Vector3 ConstrainPosition(Vector3 position)
        {
            if (!constrainToBounds)
                return position;

            // Perspective camera with rotation: calculate what ground area is visible
            var rotation = transform.eulerAngles.x;
            if (rotation > 180f) rotation -= 360f;

            // Calculate the ground point where camera center looks (on X,Z plane at Y=0)
            var tanAngle = Mathf.Tan(Mathf.Abs(rotation) * Mathf.Deg2Rad);
            var groundCenterZ = position.z + position.y / tanAngle; // ADAPTED: Y/tan instead of Z*tan

            // For X bounds: use horizontal FOV calculation
            // Distance to ground center along view ray
            var distanceToGround = position.y / Mathf.Sin(Mathf.Abs(rotation) * Mathf.Deg2Rad);
            var horizontalFOV = mainCamera.fieldOfView * mainCamera.aspect;
            var groundWidth = 2f * distanceToGround * Mathf.Tan(horizontalFOV * 0.5f * Mathf.Deg2Rad);

            // X bounds: keep camera so edges don't go outside map
            var minX = mapMinBounds.x + groundWidth / 2f;
            var maxX = mapMaxBounds.x - groundWidth / 2f;

            if (minX >= maxX)
                position.x = (mapMinBounds.x + mapMaxBounds.x) / 2f;
            else
                position.x = Mathf.Clamp(position.x, minX, maxX);

            // For Z bounds: constrain based on ground point, not camera position
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

            // Convert ground point back to camera position
            position.z = groundCenterZ - position.y / tanAngle;

            return position;
        }

        /// <summary>
        /// Smooth camera movement to target position.
        /// </summary>
        public async UniTask MoveToAsync(Vector3 targetPosition, float duration = 1f, CancellationToken ct = default)
        {
            // Cancel any previous movement
            moveCts?.Cancel();
            moveCts?.Dispose();
            moveCts = CancellationTokenSource.CreateLinkedTokenSource(ct, this.GetCancellationTokenOnDestroy());

            var startPos = transform.position;
            targetPosition = ConstrainPosition(targetPosition);
            var elapsedTime = 0f;

            try
            {
                while (elapsedTime < duration)
                {
                    elapsedTime += Time.deltaTime;
                    var t = Mathf.Clamp01(elapsedTime / duration);
                    t = Mathf.SmoothStep(0f, 1f, t); // Smooth ease in/out
                    transform.position = Vector3.Lerp(startPos, targetPosition, t);

                    await UniTask.Yield(PlayerLoopTiming.Update, moveCts.Token);
                }

                transform.position = targetPosition;
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
        /// Center camera on a position instantly.
        /// </summary>
        public void CenterOn(Vector3 position)
        {
            position.z = transform.position.z; // Maintain Z position
            transform.position = ConstrainPosition(position);
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
    }
}
