using UnityEditor;
using UnityEngine;
using RealmsOfEldor.Core;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Editor
{
    /// <summary>
    /// Quick asset creation tools for game objects.
    /// Provides menu items for rapid creation of mines, dwellings, creatures, and decorative objects.
    /// </summary>
    public class AssetCreationTools
    {
        private const string MENU_ROOT = "Realms of Eldor/Create Asset/";

        #region Mine Creation

        [MenuItem(MENU_ROOT + "Mine/Gold Mine")]
        public static void CreateGoldMine()
        {
            CreateMineAsset("Gold Mine", ResourceType.Gold, 1000);
        }

        [MenuItem(MENU_ROOT + "Mine/Wood Sawmill")]
        public static void CreateWoodSawmill()
        {
            CreateMineAsset("Sawmill", ResourceType.Wood, 2);
        }

        [MenuItem(MENU_ROOT + "Mine/Ore Mine")]
        public static void CreateOreMine()
        {
            CreateMineAsset("Ore Mine", ResourceType.Ore, 2);
        }

        [MenuItem(MENU_ROOT + "Mine/Mercury Lab")]
        public static void CreateMercuryLab()
        {
            CreateMineAsset("Alchemist's Lab", ResourceType.Mercury, 1);
        }

        [MenuItem(MENU_ROOT + "Mine/Sulfur Mine")]
        public static void CreateSulfurMine()
        {
            CreateMineAsset("Sulfur Dune", ResourceType.Sulfur, 1);
        }

        [MenuItem(MENU_ROOT + "Mine/Crystal Cavern")]
        public static void CreateCrystalCavern()
        {
            CreateMineAsset("Crystal Cavern", ResourceType.Crystal, 1);
        }

        [MenuItem(MENU_ROOT + "Mine/Gem Pond")]
        public static void CreateGemPond()
        {
            CreateMineAsset("Gem Pond", ResourceType.Gems, 1);
        }

        private static void CreateMineAsset(string name, ResourceType resourceType, int dailyProduction)
        {
            var assetName = EditorUtility.SaveFilePanelInProject(
                $"Create {name}",
                $"{name}.asset",
                "asset",
                $"Create a new {name} prefab",
                "Assets/Data/MapObjects/Mines"
            );

            if (string.IsNullOrEmpty(assetName))
                return;

            // Create a ScriptableObject to store mine configuration
            var mineConfig = ScriptableObject.CreateInstance<MapObjectConfig>();
            mineConfig.objectName = name;
            mineConfig.objectType = MapObjectType.Mine;
            mineConfig.description = $"Produces {dailyProduction} {resourceType} per day";
            mineConfig.resourceType = resourceType;
            mineConfig.productionAmount = dailyProduction;
            mineConfig.isBlocking = true;
            mineConfig.isVisitable = true;

            AssetDatabase.CreateAsset(mineConfig, assetName);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = mineConfig;

            Debug.Log($"✓ Created mine asset: {assetName}");
        }

        #endregion

        #region Dwelling Creation

        [MenuItem(MENU_ROOT + "Dwelling/Creature Dwelling")]
        public static void CreateCreatureDwelling()
        {
            var assetName = EditorUtility.SaveFilePanelInProject(
                "Create Creature Dwelling",
                "Dwelling.asset",
                "asset",
                "Create a new creature dwelling",
                "Assets/Data/MapObjects/Dwellings"
            );

            if (string.IsNullOrEmpty(assetName))
                return;

            var dwellingConfig = ScriptableObject.CreateInstance<MapObjectConfig>();
            dwellingConfig.objectName = "Creature Dwelling";
            dwellingConfig.objectType = MapObjectType.Dwelling;
            dwellingConfig.description = "Generates creatures weekly";
            dwellingConfig.creatureId = 1; // Default creature ID
            dwellingConfig.weeklyGrowth = 10;
            dwellingConfig.isBlocking = true;
            dwellingConfig.isVisitable = true;

            AssetDatabase.CreateAsset(dwellingConfig, assetName);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = dwellingConfig;

            Debug.Log($"✓ Created dwelling asset: {assetName}");
        }

        #endregion

        #region Creature Creation

        [MenuItem(MENU_ROOT + "Creature/New Creature")]
        public static void CreateNewCreature()
        {
            var window = EditorWindow.GetWindow<CreatureCreationWindow>("Create Creature");
            window.Show();
        }

        #endregion

        #region Visitable Object Creation

        [MenuItem(MENU_ROOT + "Visitable/Treasure Chest")]
        public static void CreateTreasureChest()
        {
            CreateVisitableAsset("Treasure Chest", "Grants gold or experience when visited");
        }

        [MenuItem(MENU_ROOT + "Visitable/Fountain")]
        public static void CreateFountain()
        {
            CreateVisitableAsset("Fountain", "Restores movement points");
        }

        [MenuItem(MENU_ROOT + "Visitable/Obelisk")]
        public static void CreateObelisk()
        {
            CreateVisitableAsset("Obelisk", "Reveals part of the puzzle map");
        }

        [MenuItem(MENU_ROOT + "Visitable/Windmill")]
        public static void CreateWindmill()
        {
            CreateVisitableAsset("Windmill", "Grants random resources weekly");
        }

        private static void CreateVisitableAsset(string name, string description)
        {
            var assetName = EditorUtility.SaveFilePanelInProject(
                $"Create {name}",
                $"{name}.asset",
                "asset",
                $"Create a new {name}",
                "Assets/Data/MapObjects/Visitable"
            );

            if (string.IsNullOrEmpty(assetName))
                return;

            var config = ScriptableObject.CreateInstance<MapObjectConfig>();
            config.objectName = name;
            config.objectType = MapObjectType.Visitable;
            config.description = description;
            config.isBlocking = false;
            config.isVisitable = true;

            AssetDatabase.CreateAsset(config, assetName);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = config;

            Debug.Log($"✓ Created visitable object: {assetName}");
        }

        #endregion

        #region Decorative Object Creation

        [MenuItem(MENU_ROOT + "Decorative/Rock")]
        public static void CreateRock()
        {
            CreateDecorativeAsset("Rock", "Large decorative rock", blocking: true);
        }

        [MenuItem(MENU_ROOT + "Decorative/Tree")]
        public static void CreateTree()
        {
            CreateDecorativeAsset("Tree", "Decorative tree", blocking: true);
        }

        [MenuItem(MENU_ROOT + "Decorative/Mountain")]
        public static void CreateMountain()
        {
            CreateDecorativeAsset("Mountain", "Large mountain obstacle", blocking: true);
        }

        [MenuItem(MENU_ROOT + "Decorative/Bush")]
        public static void CreateBush()
        {
            CreateDecorativeAsset("Bush", "Small decorative bush", blocking: false);
        }

        [MenuItem(MENU_ROOT + "Decorative/Flowers")]
        public static void CreateFlowers()
        {
            CreateDecorativeAsset("Flowers", "Decorative flowers", blocking: false);
        }

        private static void CreateDecorativeAsset(string name, string description, bool blocking)
        {
            var assetName = EditorUtility.SaveFilePanelInProject(
                $"Create {name}",
                $"{name}.asset",
                "asset",
                $"Create a new {name}",
                "Assets/Data/MapObjects/Decorative"
            );

            if (string.IsNullOrEmpty(assetName))
                return;

            var config = ScriptableObject.CreateInstance<MapObjectConfig>();
            config.objectName = name;
            config.objectType = MapObjectType.Decorative;
            config.description = description;
            config.isBlocking = blocking;
            config.isVisitable = false;

            AssetDatabase.CreateAsset(config, assetName);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = config;

            Debug.Log($"✓ Created decorative object: {assetName}");
        }

        #endregion

        #region Utility

        [MenuItem(MENU_ROOT + "Create Missing Folders")]
        public static void CreateAssetFolders()
        {
            var folders = new[]
            {
                "Assets/Data/MapObjects",
                "Assets/Data/MapObjects/Mines",
                "Assets/Data/MapObjects/Dwellings",
                "Assets/Data/MapObjects/Visitable",
                "Assets/Data/MapObjects/Decorative"
            };

            foreach (var folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    var parts = folder.Split('/');
                    var parent = string.Join("/", parts, 0, parts.Length - 1);
                    var newFolder = parts[parts.Length - 1];
                    AssetDatabase.CreateFolder(parent, newFolder);
                    Debug.Log($"✓ Created folder: {folder}");
                }
            }

            AssetDatabase.Refresh();
            Debug.Log("✅ All asset folders verified/created");
        }

        #endregion
    }

    /// <summary>
    /// Configuration ScriptableObject for map objects.
    /// Stores design-time data for mines, dwellings, and other objects.
    /// </summary>
    public class MapObjectConfig : ScriptableObject
    {
        [Header("Basic Info")]
        public string objectName;
        public MapObjectType objectType;
        [TextArea(3, 10)]
        public string description;

        [Header("Behavior")]
        public bool isBlocking = true;
        public bool isVisitable = false;

        [Header("Resource Properties")]
        public ResourceType resourceType;
        public int resourceAmount;

        [Header("Mine Properties")]
        public int productionAmount;

        [Header("Dwelling Properties")]
        public int creatureId;
        public int weeklyGrowth;

        [Header("Visuals")]
        public Sprite sprite;
        public Color tint = Color.white;
    }

    /// <summary>
    /// Editor window for creating creatures with a form interface.
    /// </summary>
    public class CreatureCreationWindow : EditorWindow
    {
        private string creatureName = "New Creature";
        private int creatureId = 1;
        private Faction faction = Faction.Neutral;
        private CreatureTier tier = CreatureTier.Tier1;
        private int attack = 5;
        private int defense = 5;
        private int minDamage = 1;
        private int maxDamage = 3;
        private int hitPoints = 10;
        private int speed = 5;
        private int shots = 0;
        private int weeklyGrowth = 10;
        private int goldCost = 100;
        private int aiValue = 100;
        private bool isFlying = false;
        private bool noMeleeRetal = false;

        void OnGUI()
        {
            GUILayout.Label("Create New Creature", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Basic info
            creatureName = EditorGUILayout.TextField("Creature Name", creatureName);
            creatureId = EditorGUILayout.IntField("Creature ID", creatureId);
            faction = (Faction)EditorGUILayout.EnumPopup("Faction", faction);
            tier = (CreatureTier)EditorGUILayout.EnumPopup("Tier", tier);

            EditorGUILayout.Space();

            // Combat stats
            GUILayout.Label("Combat Stats", EditorStyles.boldLabel);
            attack = EditorGUILayout.IntField("Attack", attack);
            defense = EditorGUILayout.IntField("Defense", defense);
            minDamage = EditorGUILayout.IntField("Min Damage", minDamage);
            maxDamage = EditorGUILayout.IntField("Max Damage", maxDamage);
            hitPoints = EditorGUILayout.IntField("Hit Points", hitPoints);
            speed = EditorGUILayout.IntField("Speed", speed);
            shots = EditorGUILayout.IntField("Shots (0 = melee)", shots);

            EditorGUILayout.Space();

            // Growth & Cost
            GUILayout.Label("Growth & Cost", EditorStyles.boldLabel);
            weeklyGrowth = EditorGUILayout.IntField("Weekly Growth", weeklyGrowth);
            goldCost = EditorGUILayout.IntField("Gold Cost", goldCost);
            aiValue = EditorGUILayout.IntField("AI Value", aiValue);

            EditorGUILayout.Space();

            // Abilities
            GUILayout.Label("Abilities", EditorStyles.boldLabel);
            isFlying = EditorGUILayout.Toggle("Flying", isFlying);
            noMeleeRetal = EditorGUILayout.Toggle("No Melee Retaliation", noMeleeRetal);

            EditorGUILayout.Space();

            if (GUILayout.Button("Create Creature", GUILayout.Height(40)))
            {
                CreateCreature();
            }
        }

        private void CreateCreature()
        {
            var creature = ScriptableObject.CreateInstance<CreatureData>();
            creature.creatureId = creatureId;
            creature.creatureName = creatureName;
            creature.faction = faction;
            creature.tier = tier;
            creature.attack = attack;
            creature.defense = defense;
            creature.minDamage = minDamage;
            creature.maxDamage = maxDamage;
            creature.hitPoints = hitPoints;
            creature.speed = speed;
            creature.shots = shots;
            creature.weeklyGrowth = weeklyGrowth;
            creature.cost = new ResourceCost(gold: goldCost);
            creature.aiValue = aiValue;
            creature.isFlying = isFlying;
            creature.noMeleeRetal = noMeleeRetal;

            // Ensure folder exists
            var factionFolder = $"Assets/Data/Creatures/{faction}";
            if (!AssetDatabase.IsValidFolder(factionFolder))
            {
                AssetDatabase.CreateFolder("Assets/Data/Creatures", faction.ToString());
            }

            var path = $"{factionFolder}/{creatureName}.asset";
            AssetDatabase.CreateAsset(creature, path);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = creature;

            Debug.Log($"✓ Created creature: {path}");

            // Reset form
            creatureId++;
            creatureName = "New Creature";
        }
    }
}
