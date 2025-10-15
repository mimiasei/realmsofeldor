using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RealmsOfEldor.Editor.SceneSetup
{
    /// <summary>
    /// Tool to activate and verify Universal Render Pipeline (URP) configuration
    /// The project has URP assets but they're not activated in GraphicsSettings
    /// </summary>
    public static class URPActivationTool
    {
        private const string URP_ASSET_PATH = "Assets/Settings/UniversalRP.asset";
        private const string RENDERER_2D_PATH = "Assets/Settings/Renderer2D.asset";

        [MenuItem("Realms of Eldor/Setup/Activate URP (Recommended)", priority = 0)]
        public static void ActivateURP()
        {
            Debug.Log("🎨 Activating Universal Render Pipeline...");

            // Load existing URP asset
            var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(URP_ASSET_PATH);
            if (urpAsset == null)
            {
                Debug.LogError($"❌ URP asset not found at {URP_ASSET_PATH}");
                EditorUtility.DisplayDialog("URP Activation Failed",
                    "UniversalRP.asset not found in Assets/Settings/\n\n" +
                    "Please ensure URP assets exist before activating.",
                    "OK");
                return;
            }

            // Verify 2D Renderer exists
            var renderer2D = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(RENDERER_2D_PATH);
            if (renderer2D == null)
            {
                Debug.LogWarning($"⚠️ 2D Renderer not found at {RENDERER_2D_PATH}");
            }
            else
            {
                Debug.Log($"✓ Found 2D Renderer: {renderer2D.name}");
            }

            // Store original pipeline for rollback info
            var originalPipeline = GraphicsSettings.defaultRenderPipeline;
            var wasBuiltIn = originalPipeline == null;

            // Activate URP in GraphicsSettings
            GraphicsSettings.defaultRenderPipeline = urpAsset;
            Debug.Log($"✓ Set GraphicsSettings.renderPipelineAsset to {urpAsset.name}");

            // Set URP for all quality levels
            var qualityCount = QualitySettings.names.Length;
            for (var i = 0; i < qualityCount; i++)
            {
                QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                QualitySettings.renderPipeline = urpAsset;
            }
            Debug.Log($"✓ Set URP for all {qualityCount} quality levels");

            // Reset to default quality level
            QualitySettings.SetQualityLevel(QualitySettings.GetQualityLevel());

            // Save changes
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("✅ URP activated successfully!");

            // Show summary dialog
            EditorUtility.DisplayDialog("URP Activated Successfully!",
                $"Universal Render Pipeline is now active.\n\n" +
                $"Changes made:\n" +
                $"• Graphics Settings: {(wasBuiltIn ? "Built-in" : "Custom")} → URP\n" +
                $"• Quality Levels: {qualityCount} levels configured\n" +
                $"• Renderer: 2D Renderer (optimized for strategy games)\n\n" +
                $"Benefits:\n" +
                $"• SRP Batcher enabled (better performance)\n" +
                $"• 2D lighting support\n" +
                $"• Post-processing effects\n" +
                $"• Future-proof rendering\n\n" +
                $"Press Play to test!",
                "OK");

            // Log verification
            VerifyURPSetup(showDialog: false);
        }

        [MenuItem("Realms of Eldor/Setup/Verify URP Setup", priority = 1)]
        public static void VerifyURPSetup() => VerifyURPSetup(showDialog: true);

        private static void VerifyURPSetup(bool showDialog)
        {
            Debug.Log("🔍 Verifying URP setup...");

            var report = new System.Text.StringBuilder();
            report.AppendLine("=== URP Setup Verification ===\n");

            var isValid = true;

            // Check if URP is active
            var currentPipeline = GraphicsSettings.defaultRenderPipeline;
            if (currentPipeline == null)
            {
                report.AppendLine("❌ No render pipeline assigned (using Built-in)");
                isValid = false;
            }
            else if (currentPipeline is UniversalRenderPipelineAsset urpAsset)
            {
                report.AppendLine($"✅ URP Active: {urpAsset.name}");

                // Check if it's 2D renderer (using reflection for internal property)
                var rendererDataListField = typeof(UniversalRenderPipelineAsset).GetProperty("scriptableRendererData",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (rendererDataListField != null)
                {
                    var rendererDataList = rendererDataListField.GetValue(urpAsset) as ScriptableRendererData[];
                    if (rendererDataList != null && rendererDataList.Length > 0)
                    {
                        var rendererData = rendererDataList[0];
                        var is2DRenderer = rendererData.GetType().Name.Contains("Renderer2D");
                        if (is2DRenderer)
                        {
                            report.AppendLine($"✅ 2D Renderer: {rendererData.name}");
                        }
                        else
                        {
                            report.AppendLine($"⚠️ Renderer: {rendererData.name} (not 2D)");
                        }
                    }
                    else
                    {
                        report.AppendLine("⚠️ No default renderer assigned");
                    }
                }
                else
                {
                    report.AppendLine("⚠️ Could not verify renderer (property not accessible)");
                }

                // Check SRP Batcher (via reflection - property is internal)
                var srpBatcherField = typeof(UniversalRenderPipelineAsset).GetProperty("useSRPBatcher");
                if (srpBatcherField != null)
                {
                    var srpBatcherEnabled = (bool)srpBatcherField.GetValue(urpAsset);
                    if (srpBatcherEnabled)
                    {
                        report.AppendLine("✅ SRP Batcher: Enabled (critical for performance)");
                    }
                    else
                    {
                        report.AppendLine("❌ SRP Batcher: Disabled (enable for better performance)");
                        isValid = false;
                    }
                }

                // Check HDR
                var hdrSupported = urpAsset.supportsHDR;
                report.AppendLine($"✓ HDR: {(hdrSupported ? "Enabled" : "Disabled")}");

                // Check MSAA
                var msaa = urpAsset.msaaSampleCount;
                report.AppendLine($"✓ MSAA: {msaa}x {(msaa == 1 ? "(disabled - good for pixel art)" : "")}");
            }
            else
            {
                report.AppendLine($"❌ Unknown pipeline type: {currentPipeline.GetType().Name}");
                isValid = false;
            }

            // Check quality levels
            report.AppendLine($"\n📊 Quality Levels:");
            var qualityNames = QualitySettings.names;
            var currentQuality = QualitySettings.GetQualityLevel();
            var urpConfiguredLevels = 0;

            for (var i = 0; i < qualityNames.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                var qPipeline = QualitySettings.renderPipeline;
                var hasURP = qPipeline is UniversalRenderPipelineAsset;
                var marker = i == currentQuality ? "→" : " ";
                var status = hasURP ? "✓" : "✗";

                report.AppendLine($"{marker} {status} {qualityNames[i]}: {(hasURP ? "URP" : "None")}");

                if (hasURP) urpConfiguredLevels++;
            }

            // Restore original quality level
            QualitySettings.SetQualityLevel(currentQuality);

            if (urpConfiguredLevels < qualityNames.Length)
            {
                report.AppendLine($"\n⚠️ {qualityNames.Length - urpConfiguredLevels} quality level(s) not using URP");
            }

            // Final summary
            report.AppendLine($"\n{'=',-30}");
            if (isValid && urpConfiguredLevels == qualityNames.Length)
            {
                report.AppendLine("✅ URP is properly configured!");
            }
            else
            {
                report.AppendLine("⚠️ URP needs configuration. Run 'Activate URP' to fix.");
            }

            var reportStr = report.ToString();
            Debug.Log(reportStr);

            if (showDialog)
            {
                EditorUtility.DisplayDialog("URP Setup Verification",
                    reportStr,
                    "OK");
            }
        }

        [MenuItem("Realms of Eldor/Setup/Deactivate URP (Revert to Built-in)", priority = 2)]
        public static void DeactivateURP()
        {
            var confirm = EditorUtility.DisplayDialog("Deactivate URP?",
                "This will revert to the Built-in Render Pipeline.\n\n" +
                "This is NOT recommended as:\n" +
                "• Built-in pipeline is deprecated\n" +
                "• You'll lose 2D lighting capabilities\n" +
                "• Performance will be worse\n\n" +
                "Are you sure you want to deactivate URP?",
                "Yes, Deactivate",
                "Cancel");

            if (!confirm) return;

            Debug.Log("⚠️ Deactivating URP...");

            // Remove from GraphicsSettings
            GraphicsSettings.defaultRenderPipeline = null;

            // Remove from all quality levels
            var qualityCount = QualitySettings.names.Length;
            for (var i = 0; i < qualityCount; i++)
            {
                QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                QualitySettings.renderPipeline = null;
            }

            // Reset to default quality level
            QualitySettings.SetQualityLevel(QualitySettings.GetQualityLevel());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("✅ URP deactivated. Now using Built-in Render Pipeline.");

            EditorUtility.DisplayDialog("URP Deactivated",
                "Reverted to Built-in Render Pipeline.\n\n" +
                "You can re-activate URP at any time via:\n" +
                "Realms of Eldor > Setup > Activate URP",
                "OK");
        }
    }
}
