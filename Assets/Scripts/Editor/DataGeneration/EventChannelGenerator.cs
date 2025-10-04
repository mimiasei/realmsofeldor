using UnityEngine;
using UnityEditor;
using RealmsOfEldor.Data;
using RealmsOfEldor.Data.EventChannels;

namespace RealmsOfEldor.Editor
{
    /// <summary>
    /// Editor tool to generate event channel ScriptableObject assets.
    /// Menu: Realms of Eldor/Generate/Event Channel Assets
    /// </summary>
    public class EventChannelGenerator
    {
        [MenuItem("Realms of Eldor/Generate/Event Channel Assets")]
        public static void GenerateEventChannels()
        {
            var folderPath = "Assets/Data/EventChannels";

            // Ensure folder exists
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/Data", "EventChannels");
            }

            // Generate each event channel
            CreateEventChannel<GameEventChannel>("GameEventChannel", folderPath);
            CreateEventChannel<MapEventChannel>("MapEventChannel", folderPath);
            CreateEventChannel<BattleEventChannel>("BattleEventChannel", folderPath);
            CreateEventChannel<UIEventChannel>("UIEventChannel", folderPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=green>✓ Generated all event channel assets in {folderPath}</color>");
        }

        private static void CreateEventChannel<T>(string name, string folderPath) where T : ScriptableObject
        {
            var assetPath = $"{folderPath}/{name}.asset";

            // Check if asset already exists
            var existingAsset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existingAsset != null)
            {
                Debug.Log($"  → Skipped {name} (already exists)");
                return;
            }

            // Create new asset
            var eventChannel = ScriptableObject.CreateInstance<T>();

            // Create and save asset
            AssetDatabase.CreateAsset(eventChannel, assetPath);
            Debug.Log($"  ✓ Created {name}");
        }
    }
}
