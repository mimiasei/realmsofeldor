using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RealmsOfEldor.Editor
{
    /// <summary>
    /// Inspector tool to check MapItemPrefab configuration.
    /// </summary>
    public static class MapItemPrefabInspector
    {
        [MenuItem("Realms of Eldor/UI Tools/Inspect MapItem Prefab", priority = 103)]
        public static void InspectMapItemPrefab()
        {
            // Find the prefab
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

            Debug.Log($"=== Inspecting MapItemPrefab at {path} ===");

            // Check RectTransform
            var rectTransform = prefab.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Debug.Log("RectTransform:");
                Debug.Log($"  anchorMin: {rectTransform.anchorMin}");
                Debug.Log($"  anchorMax: {rectTransform.anchorMax}");
                Debug.Log($"  pivot: {rectTransform.pivot}");
                Debug.Log($"  anchoredPosition: {rectTransform.anchoredPosition}");
                Debug.Log($"  sizeDelta: {rectTransform.sizeDelta}");
            }
            else
            {
                Debug.LogError("No RectTransform found!");
            }

            // Check Button
            var button = prefab.GetComponent<Button>();
            Debug.Log($"Button: {(button != null ? "✓" : "✗")}");

            // Check LayoutElement
            var layoutElement = prefab.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                Debug.Log("LayoutElement:");
                Debug.Log($"  minHeight: {layoutElement.minHeight}");
                Debug.Log($"  preferredHeight: {layoutElement.preferredHeight}");
                Debug.Log($"  flexibleHeight: {layoutElement.flexibleHeight}");
                Debug.Log($"  minWidth: {layoutElement.minWidth}");
                Debug.Log($"  preferredWidth: {layoutElement.preferredWidth}");
                Debug.Log($"  flexibleWidth: {layoutElement.flexibleWidth}");
            }
            else
            {
                Debug.LogError("No LayoutElement found!");
            }

            // Check for TextMeshProUGUI child
            var textComponent = prefab.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            Debug.Log($"TextMeshProUGUI child: {(textComponent != null ? "✓" : "✗")}");

            Debug.Log("=== Inspection Complete ===");
        }
    }
}
