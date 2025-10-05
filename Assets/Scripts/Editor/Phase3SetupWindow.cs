using UnityEditor;
using UnityEngine;

namespace RealmsOfEldor.Editor
{
    /// <summary>
    /// Consolidated editor window for Phase 3 map system setup.
    /// Provides one-click access to all generation and setup tools.
    /// </summary>
    public class Phase3SetupWindow : EditorWindow
    {
        [MenuItem("Realms of Eldor/Phase 3 Map Setup")]
        public static void ShowWindow()
        {
            GetWindow<Phase3SetupWindow>("Phase 3 Map Setup");
        }

        void OnGUI()
        {
            GUILayout.Label("Phase 3 Map System Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "Follow these steps in order to set up the map visualization system:",
                MessageType.Info
            );

            EditorGUILayout.Space();

            // Step 1: Generate Sprites
            GUILayout.Label("Step 1: Generate Assets", EditorStyles.boldLabel);

            if (GUILayout.Button("Generate Placeholder Terrain Sprites", GUILayout.Height(35)))
            {
                PlaceholderSpriteGenerator.GeneratePlaceholderSprites();
            }

            if (GUILayout.Button("Generate Terrain Data Assets", GUILayout.Height(35)))
            {
                TerrainDataGenerator.GenerateTerrainData();
            }

            if (GUILayout.Button("Generate Event Channel Assets", GUILayout.Height(35)))
            {
                EventChannelGenerator.GenerateEventChannels();
            }

            if (GUILayout.Button("Create Tile Assets & Link to TerrainData", GUILayout.Height(35)))
            {
                TileAssetGenerator.GenerateTileAssets();
            }

            EditorGUILayout.Space();

            // Step 2: Create Scene
            GUILayout.Label("Step 2: Create Scene", EditorStyles.boldLabel);

            if (GUILayout.Button("Create Adventure Map Scene", GUILayout.Height(35)))
            {
                AdventureMapSceneSetup.CreateAdventureMapScene();
            }

            EditorGUILayout.Space();

            // Instructions
            GUILayout.Label("Step 3: Manual Configuration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. Open AdventureMap scene (Assets/Scenes/)\n" +
                "2. Select Grid GameObject, configure MapRenderer:\n" +
                "   - Assign all 10 TerrainData assets from Assets/Data/Terrain/\n" +
                "   - Assign MapEventChannel from Assets/Data/EventChannels/\n" +
                "3. Add MapTestInitializer component to a GameObject\n" +
                "4. Press Play to test!",
                MessageType.Info
            );

            EditorGUILayout.Space();

            // Documentation
            if (GUILayout.Button("Open CHANGES.md (Full Instructions)", GUILayout.Height(30)))
            {
                var changesPath = System.IO.Path.Combine(Application.dataPath, "..", "CHANGES.md");
                EditorUtility.RevealInFinder(changesPath);
            }
        }
    }
}
