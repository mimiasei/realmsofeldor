using UnityEngine;

namespace RealmsOfEldor.Controllers.Battle
{
    /// <summary>
    /// Static camera controller for battle scene.
    /// Uses 3D perspective camera with fixed position, only X rotation is adjustable.
    /// Matches Cartographer's coordinate system (X,Z ground plane) but without pan/zoom.
    /// </summary>
    public class BattleCameraController : MonoBehaviour
    {
        [Header("Camera Settings")]
        [Tooltip("Camera tilt angle (rotation X) - negative values look down at battlefield")]
        [SerializeField] private float cameraTiltAngle = -45f;

        [Tooltip("Field of view (20-30° for minimal distortion)")]
        [SerializeField] private float fieldOfView = 25f;

        [Tooltip("Camera height above ground plane (Y axis)")]
        [SerializeField] private float cameraHeight = 20f;

        [Tooltip("Camera distance back from center (negative Z)")]
        [SerializeField] private float cameraDistance = -15f;

        [Tooltip("Horizontal offset from battlefield center (X axis)")]
        [SerializeField] private float cameraOffsetX = 0f;

        [Header("Battlefield Settings")]
        [Tooltip("Battlefield center position (where camera looks at)")]
        [SerializeField] private Vector3 battlefieldCenter = new Vector3(7.5f, 0f, 5f);

        [Header("Lighting")]
        [Tooltip("Enable automatic directional light setup")]
        [SerializeField] private bool createDirectionalLight = true;

        [Tooltip("Sun light angle (X=altitude, Y=azimuth)")]
        [SerializeField] private Vector2 sunAngle = new Vector2(50f, -30f);

        [Tooltip("Sun light intensity")]
        [SerializeField] private float sunIntensity = 1.0f;

        [Tooltip("Enable shadows")]
        [SerializeField] private bool enableShadows = true;

        private Camera mainCamera;
        private Light directionalLight;

        void Awake()
        {
            // Get or create camera
            mainCamera = GetComponent<Camera>();
            if (mainCamera == null)
            {
                mainCamera = gameObject.AddComponent<Camera>();
            }

            SetupCamera();

            if (createDirectionalLight)
            {
                SetupLighting();
            }
        }

        /// <summary>
        /// Configures camera for battle scene 3D rendering.
        /// Static position with adjustable tilt angle.
        /// </summary>
        private void SetupCamera()
        {
            // Perspective camera (matches Cartographer)
            mainCamera.orthographic = false;
            mainCamera.fieldOfView = fieldOfView;

            // Position camera: center + offsets
            transform.position = new Vector3(
                battlefieldCenter.x + cameraOffsetX,
                cameraHeight,
                battlefieldCenter.z + cameraDistance
            );

            // Rotate camera (only X rotation to look down at battlefield)
            // Negate cameraTiltAngle because Unity's Euler X rotation is inverted
            transform.rotation = Quaternion.Euler(-cameraTiltAngle, 0f, 0f);

            // Clip planes
            mainCamera.nearClipPlane = 0.3f;
            mainCamera.farClipPlane = 1000f;

            // Clear to solid color
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);

            // Disable mouse events (battle uses custom input)
            mainCamera.eventMask = 0;

            Debug.Log($"BattleCameraController: Camera configured");
            Debug.Log($"  Position: {transform.position}");
            Debug.Log($"  Rotation: {transform.rotation.eulerAngles}");
            Debug.Log($"  Tilt angle: {cameraTiltAngle}°");
            Debug.Log($"  FOV: {fieldOfView}°");
            Debug.Log($"  Looking at: {battlefieldCenter}");
        }

        /// <summary>
        /// Sets up directional light for shadows (sun).
        /// </summary>
        private void SetupLighting()
        {
            // Check if light already exists in scene
            directionalLight = FindFirstObjectByType<Light>();

            if (directionalLight == null || directionalLight.type != LightType.Directional)
            {
                // Create new directional light
                GameObject lightObj = new GameObject("BattleSun");
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

            // Set ambient lighting (darker for battle atmosphere)
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.3f, 0.3f, 0.35f);
            RenderSettings.ambientIntensity = 0.25f;

            Debug.Log($"BattleCameraController: Lighting configured - Sun angle: {sunAngle}, Shadows: {enableShadows}");
        }

        /// <summary>
        /// Updates camera position and rotation (call this after changing inspector values).
        /// </summary>
        [ContextMenu("Update Camera")]
        public void UpdateCamera()
        {
            SetupCamera();
        }

        /// <summary>
        /// Gets the camera component.
        /// </summary>
        public Camera GetCamera()
        {
            return mainCamera;
        }

        /// <summary>
        /// Sets the camera tilt angle at runtime.
        /// </summary>
        public void SetCameraTiltAngle(float angle)
        {
            cameraTiltAngle = angle;
            transform.rotation = Quaternion.Euler(-cameraTiltAngle, 0f, 0f);
        }

        /// <summary>
        /// Gets current camera tilt angle.
        /// </summary>
        public float GetCameraTiltAngle()
        {
            return cameraTiltAngle;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            // Draw battlefield center
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(battlefieldCenter, 0.5f);

            // Draw camera position
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.3f);

            // Draw line from camera to battlefield center
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, battlefieldCenter);

            // Draw camera forward direction
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.forward * 5f);
        }
#endif
    }
}
