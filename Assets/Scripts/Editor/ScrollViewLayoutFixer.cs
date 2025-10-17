using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RealmsOfEldor.Editor
{
    /// <summary>
    /// Helper tool to diagnose and fix ScrollView layout issues.
    /// </summary>
    public static class ScrollViewLayoutFixer
    {
        [MenuItem("Realms of Eldor/UI Tools/Check MapSelection ScrollView Layout", priority = 100)]
        public static void CheckScrollViewLayout()
        {
            // Find MapListScrollView in current scene
            var scrollView = GameObject.Find("MapListScrollView");
            if (scrollView == null)
            {
                Debug.LogError("MapListScrollView not found in scene! Make sure MapSelection scene is open.");
                return;
            }

            Debug.Log("=== Checking ScrollView Layout ===");

            // Check Viewport
            var viewport = scrollView.transform.Find("Viewport");
            if (viewport == null)
            {
                Debug.LogError("Viewport not found under MapListScrollView!");
                return;
            }

            // Check Content
            var content = viewport.Find("Content");
            if (content == null)
            {
                Debug.LogError("Content not found under Viewport!");
                return;
            }

            Debug.Log("✓ ScrollView structure found");

            // Check Content components
            var rectTransform = content.GetComponent<RectTransform>();
            var verticalLayout = content.GetComponent<VerticalLayoutGroup>();
            var contentSizeFitter = content.GetComponent<ContentSizeFitter>();

            Debug.Log($"Content RectTransform: {(rectTransform != null ? "✓" : "✗")}");
            Debug.Log($"  Anchors: {rectTransform?.anchorMin} to {rectTransform?.anchorMax}");
            Debug.Log($"  Size: {rectTransform?.sizeDelta}");

            Debug.Log($"VerticalLayoutGroup: {(verticalLayout != null ? "✓" : "✗")}");
            if (verticalLayout != null)
            {
                Debug.Log($"  Spacing: {verticalLayout.spacing}");
                Debug.Log($"  Child Control Size: Width={verticalLayout.childControlWidth}, Height={verticalLayout.childControlHeight}");
                Debug.Log($"  Child Force Expand: Width={verticalLayout.childForceExpandWidth}, Height={verticalLayout.childForceExpandHeight}");
            }

            Debug.Log($"ContentSizeFitter: {(contentSizeFitter != null ? "✓" : "✗")}");
            if (contentSizeFitter != null)
            {
                Debug.Log($"  Horizontal Fit: {contentSizeFitter.horizontalFit}");
                Debug.Log($"  Vertical Fit: {contentSizeFitter.verticalFit}");
            }

            // Check children
            Debug.Log($"Content has {content.childCount} children");
            for (int i = 0; i < content.childCount; i++)
            {
                var child = content.GetChild(i);
                var childRect = child.GetComponent<RectTransform>();
                var childLayout = child.GetComponent<LayoutElement>();
                Debug.Log($"  Child [{i}] {child.name}:");
                Debug.Log($"    Position: {childRect.anchoredPosition}");
                Debug.Log($"    Size: {childRect.sizeDelta}");
                Debug.Log($"    Anchors: {childRect.anchorMin} to {childRect.anchorMax}");
                if (childLayout != null)
                {
                    Debug.Log($"    LayoutElement: min={childLayout.minHeight}, pref={childLayout.preferredHeight}");
                }
                else
                {
                    Debug.Log($"    LayoutElement: MISSING");
                }
            }

            Debug.Log("=== Check Complete ===");
        }

        [MenuItem("Realms of Eldor/UI Tools/Fix MapSelection ScrollView Layout", priority = 101)]
        public static void FixScrollViewLayout()
        {
            // Find MapListScrollView in current scene
            var scrollView = GameObject.Find("MapListScrollView");
            if (scrollView == null)
            {
                Debug.LogError("MapListScrollView not found in scene! Make sure MapSelection scene is open.");
                return;
            }

            var viewport = scrollView.transform.Find("Viewport");
            var content = viewport?.Find("Content");

            if (content == null)
            {
                Debug.LogError("Content not found!");
                return;
            }

            Debug.Log("=== Fixing ScrollView Layout ===");

            // Fix RectTransform anchors (should stretch horizontally, anchored at top)
            var rectTransform = content.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f); // Top-left
            rectTransform.anchorMax = new Vector2(1f, 1f); // Top-right (stretches width)
            rectTransform.pivot = new Vector2(0.5f, 1f); // Pivot at top-center
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(0, 0); // Width stretches, height managed by ContentSizeFitter
            Debug.Log("✓ Fixed RectTransform anchors to stretch-top (0,1) to (1,1)");

            // Ensure VerticalLayoutGroup exists
            var verticalLayout = content.GetComponent<VerticalLayoutGroup>();
            if (verticalLayout == null)
            {
                verticalLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
                Debug.Log("✓ Added VerticalLayoutGroup");
            }

            verticalLayout.childAlignment = TextAnchor.UpperCenter;
            verticalLayout.childControlWidth = true;
            verticalLayout.childControlHeight = false;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.childForceExpandHeight = false;
            verticalLayout.spacing = 10;
            Debug.Log("✓ Configured VerticalLayoutGroup");

            // Ensure ContentSizeFitter exists
            var contentSizeFitter = content.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter == null)
            {
                contentSizeFitter = content.gameObject.AddComponent<ContentSizeFitter>();
                Debug.Log("✓ Added ContentSizeFitter");
            }

            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Debug.Log("✓ Configured ContentSizeFitter (Vertical: Preferred Size)");

            // Force layout rebuild
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            Debug.Log("✓ Forced layout rebuild");

            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("=== Fix Complete! Save the scene. ===");
        }
    }
}
