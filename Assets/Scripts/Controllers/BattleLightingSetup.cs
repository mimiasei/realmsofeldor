using UnityEngine;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Configures lighting and shadows for the battle scene in 2.5D billboard rendering.
    /// Creates a directional light that casts shadows from billboard sprites onto the ground plane.
    ///
    /// Based on RENDERING_2_5D_BILLBOARD_SYSTEM.md - Phase 4: Lighting and Shadows
    /// </summary>
    public class BattleLightingSetup : MonoBehaviour
    {
        [Header("Directional Light Settings")]
        [Tooltip("If null, will create a new directional light")]
        [SerializeField] private Light directionalLight;

        [Tooltip("Color of the main directional light")]
        [SerializeField] private Color lightColor = new Color(1f, 0.95f, 0.9f); // Warm white

        [Tooltip("Intensity of the main directional light")]
        [SerializeField] [Range(0f, 2f)] private float lightIntensity = 1.0f;

        [Tooltip("Angle of light (X rotation, looking down at battlefield)")]
        [SerializeField] [Range(0f, 90f)] private float lightAngle = 45f;

        [Tooltip("Direction of light (Y rotation)")]
        [SerializeField] [Range(0f, 360f)] private float lightDirection = 135f;

        [Header("Shadow Settings")]
        [Tooltip("Enable shadow casting")]
        [SerializeField] private bool enableShadows = true;

        [Tooltip("Shadow resolution quality")]
        [SerializeField] private UnityEngine.Rendering.LightShadowResolution shadowResolution = UnityEngine.Rendering.LightShadowResolution.Medium;

        [Tooltip("Shadow strength (0 = no shadows, 1 = fully opaque)")]
        [SerializeField] [Range(0f, 1f)] private float shadowStrength = 0.5f;

        [Header("Ambient Lighting")]
        [Tooltip("Color of ambient light (fills in shadows)")]
        [SerializeField] private Color ambientColor = new Color(0.4f, 0.4f, 0.5f); // Bluish ambient

        [Tooltip("Ambient light intensity")]
        [SerializeField] [Range(0f, 2f)] private float ambientIntensity = 0.3f;

        void Awake()
        {
            SetupDirectionalLight();
            SetupAmbientLighting();
        }

        /// <summary>
        /// Creates or configures the directional light for the battle scene.
        /// </summary>
        private void SetupDirectionalLight()
        {
            // Find or create directional light
            if (directionalLight == null)
            {
                // Try to find existing directional light
                directionalLight = FindFirstObjectByType<Light>();

                if (directionalLight == null || directionalLight.type != LightType.Directional)
                {
                    // Create new directional light
                    var lightObj = new GameObject("Directional Light");
                    lightObj.transform.SetParent(transform);
                    directionalLight = lightObj.AddComponent<Light>();
                    directionalLight.type = LightType.Directional;

                    Debug.Log("BattleLightingSetup: Created new directional light");
                }
            }

            // Configure light properties
            directionalLight.color = lightColor;
            directionalLight.intensity = lightIntensity;

            // Position light (angle down at battlefield)
            directionalLight.transform.rotation = Quaternion.Euler(lightAngle, lightDirection, 0f);

            // Configure shadows
            if (enableShadows)
            {
                directionalLight.shadows = LightShadows.Soft; // Soft shadows look better
                directionalLight.shadowStrength = shadowStrength;
                directionalLight.shadowResolution = shadowResolution;

                // Shadow bias settings to prevent acne and peter-panning
                directionalLight.shadowBias = 0.05f;
                directionalLight.shadowNormalBias = 0.4f;
                directionalLight.shadowNearPlane = 0.2f;

                Debug.Log($"BattleLightingSetup: Enabled shadows (strength: {shadowStrength}, resolution: {shadowResolution})");
            }
            else
            {
                directionalLight.shadows = LightShadows.None;
                Debug.Log("BattleLightingSetup: Shadows disabled");
            }

            // Culling mask (render all layers by default)
            directionalLight.cullingMask = -1;

            Debug.Log($"BattleLightingSetup: Directional light configured - angle: {lightAngle}°, direction: {lightDirection}°, intensity: {lightIntensity}");
        }

        /// <summary>
        /// Configures ambient lighting to fill in shadows.
        /// </summary>
        private void SetupAmbientLighting()
        {
            // Set ambient light color and intensity
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColor * ambientIntensity;
            RenderSettings.ambientIntensity = ambientIntensity;

            Debug.Log($"BattleLightingSetup: Ambient lighting configured - color: {ambientColor}, intensity: {ambientIntensity}");
        }

        /// <summary>
        /// Updates lighting in real-time (for debugging/tuning).
        /// </summary>
        void Update()
        {
            if (directionalLight != null && Application.isPlaying)
            {
                // Allow real-time adjustment in play mode
                directionalLight.color = lightColor;
                directionalLight.intensity = lightIntensity;
                directionalLight.transform.rotation = Quaternion.Euler(lightAngle, lightDirection, 0f);
                directionalLight.shadowStrength = shadowStrength;

                RenderSettings.ambientLight = ambientColor * ambientIntensity;
            }
        }

        /// <summary>
        /// Toggles shadow casting on/off.
        /// </summary>
        [ContextMenu("Toggle Shadows")]
        public void ToggleShadows()
        {
            enableShadows = !enableShadows;
            if (directionalLight != null)
            {
                directionalLight.shadows = enableShadows ? LightShadows.Soft : LightShadows.None;
                Debug.Log($"BattleLightingSetup: Shadows {(enableShadows ? "enabled" : "disabled")}");
            }
        }
    }
}
