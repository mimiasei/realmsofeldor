using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RealmsOfEldor.Editor
{
    /// <summary>
    /// Fixes the MapItemPrefab by adding required Layout Element component.
    /// </summary>
    public static class MapItemPrefabFixer
    {
        [MenuItem("Realms of Eldor/UI Tools/Fix MapItem Prefab Layout", priority = 102)]
        public static void FixMapItemPrefab()
        {
            // Find the prefab in the project
            string[] guids = AssetDatabase.FindAssets("MapItemPrefab t:Prefab");

            if (guids.Length == 0)
            {
                Debug.LogError("MapItemPrefab not found in project!");
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
            {
                Debug.LogError($"Failed to load prefab at path: {path}");
                return;
            }

            Debug.Log($"=== Fixing MapItemPrefab at {path} ===");

            // Load the prefab for editing
            GameObject prefabInstance = PrefabUtility.LoadPrefabContents(path);

            // Check and fix Button component
            var button = prefabInstance.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogWarning("Adding Button component...");
                button = prefabInstance.AddComponent<Button>();
            }
            Debug.Log("✓ Button component present");

            // Check and fix LayoutElement component
            var layoutElement = prefabInstance.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                Debug.Log("Adding LayoutElement component...");
                layoutElement = prefabInstance.AddComponent<LayoutElement>();
            }

            // Configure LayoutElement
            layoutElement.minHeight = 60;
            layoutElement.preferredHeight = 60;
            layoutElement.flexibleHeight = -1; // IMPORTANT: Disable flexible height
            layoutElement.flexibleWidth = -1;  // IMPORTANT: Disable flexible width
            Debug.Log("✓ LayoutElement configured (minHeight: 60, preferredHeight: 60, flexible disabled)");

            // Check RectTransform
            var rectTransform = prefabInstance.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // Set anchors to stretch horizontally, top-center vertically
                // This allows the Vertical Layout Group to position items correctly
                rectTransform.anchorMin = new Vector2(0f, 1f); // Top-left
                rectTransform.anchorMax = new Vector2(1f, 1f); // Top-right (stretches width)
                rectTransform.pivot = new Vector2(0.5f, 1f); // Pivot at top-center

                // Set size
                rectTransform.sizeDelta = new Vector2(0, 60); // Width = 0 because it stretches, Height = 60
                rectTransform.anchoredPosition = new Vector2(0, 0); // Position at anchor

                Debug.Log($"✓ RectTransform configured:");
                Debug.Log($"  Anchors: {rectTransform.anchorMin} to {rectTransform.anchorMax}");
                Debug.Log($"  Pivot: {rectTransform.pivot}");
                Debug.Log($"  SizeDelta: {rectTransform.sizeDelta}");
            }

            // Check for TextMeshProUGUI child
            var textComponent = prefabInstance.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (textComponent == null)
            {
                Debug.LogWarning("⚠ No TextMeshProUGUI component found in prefab! Map names won't display.");
            }
            else
            {
                Debug.Log("✓ TextMeshProUGUI component found");
            }

            // Save the modified prefab
            PrefabUtility.SaveAsPrefabAsset(prefabInstance, path);
            PrefabUtility.UnloadPrefabContents(prefabInstance);

            Debug.Log("=== MapItemPrefab fixed! ===");
            Debug.Log("Now run the game and the map list should display all items correctly.");
        }
    }
}
