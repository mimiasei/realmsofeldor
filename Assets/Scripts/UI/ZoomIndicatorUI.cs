using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RealmsOfEldor.Controllers;

namespace RealmsOfEldor.UI
{
    /// <summary>
    /// Displays current camera zoom level on the adventure map HUD.
    /// Shows zoom as a percentage (0% = max zoom out, 100% = max zoom in).
    /// </summary>
    public class ZoomIndicatorUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Cartographer cartographer;
        [SerializeField] private TextMeshProUGUI zoomText;
        [SerializeField] private Slider zoomSlider;

        [Header("Display Settings")]
        [SerializeField] private bool showPercentage = true;
        [SerializeField] private bool showSlider = true;
        [SerializeField] private string textFormat = "Zoom: {0:F0}%";

        [Header("Update Settings")]
        [SerializeField] private float updateInterval = 0.1f; // Update 10 times per second

        private float lastUpdateTime;

        void Start()
        {
            // Auto-find cartographer if not assigned
            if (cartographer == null)
            {
                cartographer = FindFirstObjectByType<Cartographer>();
            }

            if (cartographer == null)
            {
                Debug.LogError("ZoomIndicatorUI: Cartographer not found! Please assign it in the inspector.");
                enabled = false;
                return;
            }

            // Configure slider if present
            if (zoomSlider != null)
            {
                zoomSlider.minValue = 0f;
                zoomSlider.maxValue = 100f;
                zoomSlider.interactable = false; // Read-only display
            }

            // Show/hide elements based on settings
            if (zoomText != null) zoomText.gameObject.SetActive(showPercentage);
            if (zoomSlider != null) zoomSlider.gameObject.SetActive(showSlider);

            // Initial update
            UpdateDisplay();
        }

        void Update()
        {
            // Throttle updates to avoid excessive overhead
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateDisplay();
                lastUpdateTime = Time.time;
            }
        }

        /// <summary>
        /// Updates the zoom display based on current camera position.
        /// </summary>
        private void UpdateDisplay()
        {
            if (cartographer == null)
                return;

            // Get zoom percentage directly from Cartographer
            var zoomPercent = cartographer.GetZoomPercentage();

            // Update text
            if (zoomText != null && showPercentage)
            {
                zoomText.text = string.Format(textFormat, zoomPercent);
            }

            // Update slider
            if (zoomSlider != null && showSlider)
            {
                zoomSlider.value = zoomPercent;
            }
        }

        /// <summary>
        /// Manually forces a display update (useful when zoom changes rapidly).
        /// </summary>
        public void ForceUpdate()
        {
            UpdateDisplay();
        }

        /// <summary>
        /// Sets a custom text format string.
        /// Use {0} as placeholder for zoom percentage.
        /// </summary>
        public void SetTextFormat(string format)
        {
            textFormat = format;
            UpdateDisplay();
        }

        /// <summary>
        /// Shows or hides the zoom indicator.
        /// </summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

#if UNITY_EDITOR
        [ContextMenu("Test Update Display")]
        private void TestUpdate()
        {
            if (Application.isPlaying)
            {
                UpdateDisplay();
                Debug.Log($"Zoom updated: {zoomText?.text}");
            }
        }
#endif
    }
}
