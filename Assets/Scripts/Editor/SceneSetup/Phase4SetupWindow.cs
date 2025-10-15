using UnityEngine;
using UnityEditor;

namespace RealmsOfEldor.Editor.SceneSetup
{
    /// <summary>
    /// Editor window for Phase 4 Adventure Map UI setup
    /// Menu: Realms of Eldor/Phase 4 UI Setup
    /// </summary>
    public class Phase4SetupWindow : EditorWindow
    {
        private Vector2 scrollPosition;

        [MenuItem("Realms of Eldor/Phase 4 UI Setup")]
        public static void ShowWindow()
        {
            var window = GetWindow<Phase4SetupWindow>("Phase 4 UI Setup");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("Phase 4: Adventure Map UI Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This window guides you through setting up the Adventure Map UI components.\n\n" +
                "Phase 4 UI scripts are already implemented. This tool creates the Unity scene integration.",
                MessageType.Info);

            GUILayout.Space(10);

            // Step 1
            GUILayout.Label("Step 1: Setup UI Components", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Creates UI Canvas with ResourceBar, InfoBar, and TurnControl components.\n" +
                "Also creates a sample hero for testing.",
                MessageType.None);

            if (GUILayout.Button("Setup Phase 4 UI Components", GUILayout.Height(40)))
            {
                Phase4UISetup.SetupPhase4UI();
            }

            GUILayout.Space(15);

            // Step 2
            GUILayout.Label("Step 2: Manual Configuration (After Step 1)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "After running Step 1, you need to manually configure the following in Unity:",
                MessageType.Warning);

            EditorGUILayout.LabelField("A. ResourceBarUI Configuration:", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("   • Select UICanvas > ResourceBar");
            EditorGUILayout.LabelField("   • Assign text fields in Inspector:");
            EditorGUILayout.LabelField("     - Gold Text → GoldText");
            EditorGUILayout.LabelField("     - Wood Text → WoodText");
            EditorGUILayout.LabelField("     - Ore Text → OreText");
            EditorGUILayout.LabelField("     - Mercury Text → MercuryText");
            EditorGUILayout.LabelField("     - Sulfur Text → SulfurText");
            EditorGUILayout.LabelField("     - Crystal Text → CrystalText");
            EditorGUILayout.LabelField("     - Gems Text → GemsText");
            EditorGUILayout.LabelField("     - Date Text → DateText");

            GUILayout.Space(10);

            EditorGUILayout.LabelField("B. InfoBarUI Configuration:", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("   • Select UICanvas > InfoBar");
            EditorGUILayout.LabelField("   • Assign Info Text → InfoText");

            GUILayout.Space(10);

            EditorGUILayout.LabelField("C. TurnControlUI Configuration:", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("   • Select UICanvas > TurnControl");
            EditorGUILayout.LabelField("   • Assign Day Text → DayText");
            EditorGUILayout.LabelField("   • Assign End Turn Button → EndTurnButton");

            GUILayout.Space(10);

            EditorGUILayout.LabelField("D. HeroController Configuration:", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("   • Select TestHero GameObject");
            EditorGUILayout.LabelField("   • Create a Hero in code and assign to Hero Data field");
            EditorGUILayout.LabelField("   • Assign Game Events channel");
            EditorGUILayout.LabelField("   • Assign Map Events channel");

            GUILayout.Space(15);

            // Step 3
            GUILayout.Label("Step 3: Initialize Game State", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "You need to initialize the game state with players and heroes.\n" +
                "Add a GameInitializer script to handle this on scene start.",
                MessageType.Info);

            GUILayout.Space(15);

            // Step 4
            GUILayout.Label("Step 4: Test in Play Mode", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Press Play to test:\n" +
                "• Resource bar should display at top\n" +
                "• Info bar should display in bottom-left\n" +
                "• Turn control should display in bottom-right\n" +
                "• Click hero to select it\n" +
                "• Use arrow keys to move hero\n" +
                "• Click End Turn to advance day",
                MessageType.None);

            GUILayout.Space(15);

            // Links
            GUILayout.Label("Documentation", EditorStyles.boldLabel);
            if (GUILayout.Button("Open CHANGES.md (View Phase 4 Details)"))
            {
                System.Diagnostics.Process.Start("Assets/../CHANGES.md");
            }

            if (GUILayout.Button("Open PROJECT_SUMMARY.md"))
            {
                System.Diagnostics.Process.Start("Assets/../PROJECT_SUMMARY.md");
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
