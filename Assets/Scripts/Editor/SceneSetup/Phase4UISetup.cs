using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using RealmsOfEldor.UI;
using RealmsOfEldor.Controllers;
using RealmsOfEldor.Core.Events;
using RealmsOfEldor.Core.Events.EventChannels;
using RealmsOfEldor.Data;
using RealmsOfEldor.Database;

namespace RealmsOfEldor.Editor.SceneSetup
{
    /// <summary>
    /// Editor tool to automate Phase 4 Adventure Map UI setup
    /// Menu: Realms of Eldor/Setup/Setup Phase 4 UI Components
    /// </summary>
    public static class Phase4UISetup
    {
        [MenuItem("Realms of Eldor/Setup/Setup Phase 4 UI Components")]
        public static void SetupPhase4UI()
        {
            // Find or create UI Canvas
            var canvas = FindOrCreateCanvas();

            // Create UI components
            CreateResourceBarUI(canvas);
            CreateInfoBarUI(canvas);
            CreateTurnControlUI(canvas);
            CreateHeroPanelUI(canvas);

            // Wire event channels
            WireEventChannels();

            // Add database singletons
            AddDatabaseSingletons();

            // Configure MapRenderer with terrain data
            ConfigureMapRenderer();

            // Add GameInitializer
            AddGameInitializer();

            // Create sample hero
            CreateSampleHero();

            Debug.Log("✅ Phase 4 UI setup complete!");
            EditorUtility.DisplayDialog("Phase 4 Setup",
                "UI components created successfully!\n\n" +
                "Components created:\n" +
                "• ResourceBarUI (top)\n" +
                "• InfoBarUI (bottom-left)\n" +
                "• TurnControlUI (bottom-right)\n" +
                "• HeroPanelUI (left side - shows selected hero)\n" +
                "• Databases (HeroDatabase, CreatureDatabase, SpellDatabase)\n" +
                "• GameInitializer (initializes game state)\n" +
                "• TestHero (sample hero at 15,15)\n\n" +
                "Event channels have been wired automatically.\n" +
                "Database singletons loaded with ScriptableObject data.\n\n" +
                "Press Play to test!",
                "OK");
        }

        private static Canvas FindOrCreateCanvas()
        {
            var canvas = GameObject.Find("UICanvas")?.GetComponent<Canvas>();
            if (canvas == null)
            {
                var canvasObj = new GameObject("UICanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();

                var scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                Debug.Log("Created UICanvas");
            }
            return canvas;
        }

        private static void CreateResourceBarUI(Canvas canvas)
        {
            if (canvas.transform.Find("ResourceBar") != null)
            {
                Debug.Log("ResourceBar already exists, skipping");
                return;
            }

            var resourceBarObj = new GameObject("ResourceBar");
            resourceBarObj.transform.SetParent(canvas.transform, false);

            var rectTransform = resourceBarObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(0.5f, 1);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(0, 60);

            var resourceBarUI = resourceBarObj.AddComponent<ResourceBarUI>();

            // Create text fields for resources
            CreateResourceTextField(resourceBarObj, "GoldText", new Vector2(50, -30));
            CreateResourceTextField(resourceBarObj, "WoodText", new Vector2(200, -30));
            CreateResourceTextField(resourceBarObj, "OreText", new Vector2(350, -30));
            CreateResourceTextField(resourceBarObj, "MercuryText", new Vector2(500, -30));
            CreateResourceTextField(resourceBarObj, "SulfurText", new Vector2(650, -30));
            CreateResourceTextField(resourceBarObj, "CrystalText", new Vector2(800, -30));
            CreateResourceTextField(resourceBarObj, "GemsText", new Vector2(950, -30));
            CreateResourceTextField(resourceBarObj, "DateText", new Vector2(1600, -30));

            Debug.Log("Created ResourceBarUI");
        }

        private static void CreateInfoBarUI(Canvas canvas)
        {
            if (canvas.transform.Find("InfoBar") != null)
            {
                Debug.Log("InfoBar already exists, skipping");
                return;
            }

            var infoBarObj = new GameObject("InfoBar");
            infoBarObj.transform.SetParent(canvas.transform, false);

            var rectTransform = infoBarObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(0, 0);
            rectTransform.pivot = new Vector2(0, 0);
            rectTransform.anchoredPosition = new Vector2(10, 10);
            rectTransform.sizeDelta = new Vector2(192, 192);

            var infoBarUI = infoBarObj.AddComponent<InfoBarUI>();

            // Create info panel background
            var image = infoBarObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            // Create text field for info display
            var infoTextObj = new GameObject("InfoText");
            infoTextObj.transform.SetParent(infoBarObj.transform, false);
            var infoTextRect = infoTextObj.AddComponent<RectTransform>();
            infoTextRect.anchorMin = Vector2.zero;
            infoTextRect.anchorMax = Vector2.one;
            infoTextRect.offsetMin = new Vector2(10, 10);
            infoTextRect.offsetMax = new Vector2(-10, -10);

            var infoText = infoTextObj.AddComponent<TextMeshProUGUI>();
            infoText.fontSize = 14;
            infoText.color = Color.white;
            infoText.alignment = TextAlignmentOptions.TopLeft;

            Debug.Log("Created InfoBarUI");
        }

        private static void CreateTurnControlUI(Canvas canvas)
        {
            if (canvas.transform.Find("TurnControl") != null)
            {
                Debug.Log("TurnControl already exists, skipping");
                return;
            }

            var turnControlObj = new GameObject("TurnControl");
            turnControlObj.transform.SetParent(canvas.transform, false);

            var rectTransform = turnControlObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1, 0);
            rectTransform.anchorMax = new Vector2(1, 0);
            rectTransform.pivot = new Vector2(1, 0);
            rectTransform.anchoredPosition = new Vector2(-10, 10);
            rectTransform.sizeDelta = new Vector2(200, 100);

            var turnControlUI = turnControlObj.AddComponent<TurnControlUI>();

            // Create day counter text
            var dayTextObj = new GameObject("DayText");
            dayTextObj.transform.SetParent(turnControlObj.transform, false);
            var dayTextRect = dayTextObj.AddComponent<RectTransform>();
            dayTextRect.anchorMin = new Vector2(0, 1);
            dayTextRect.anchorMax = new Vector2(1, 1);
            dayTextRect.pivot = new Vector2(0.5f, 1);
            dayTextRect.anchoredPosition = new Vector2(0, -10);
            dayTextRect.sizeDelta = new Vector2(0, 30);

            var dayText = dayTextObj.AddComponent<TextMeshProUGUI>();
            dayText.fontSize = 18;
            dayText.color = Color.white;
            dayText.alignment = TextAlignmentOptions.Center;
            dayText.text = "Day 1";

            // Create end turn button
            var buttonObj = new GameObject("EndTurnButton");
            buttonObj.transform.SetParent(turnControlObj.transform, false);
            var buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0, 0);
            buttonRect.anchorMax = new Vector2(1, 0);
            buttonRect.pivot = new Vector2(0.5f, 0);
            buttonRect.anchoredPosition = new Vector2(0, 10);
            buttonRect.sizeDelta = new Vector2(0, 50);

            var button = buttonObj.AddComponent<Button>();
            var buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.6f, 0.3f, 1f);

            var buttonTextObj = new GameObject("Text");
            buttonTextObj.transform.SetParent(buttonObj.transform, false);
            var buttonTextRect = buttonTextObj.AddComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;

            var buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
            buttonText.fontSize = 20;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.text = "End Turn";

            Debug.Log("Created TurnControlUI");
        }

        private static void CreateHeroPanelUI(Canvas canvas)
        {
            if (canvas.transform.Find("HeroPanel") != null)
            {
                Debug.Log("HeroPanel already exists, skipping");
                return;
            }

            var heroPanelObj = new GameObject("HeroPanel");
            heroPanelObj.transform.SetParent(canvas.transform, false);

            var rectTransform = heroPanelObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0.5f);
            rectTransform.anchorMax = new Vector2(0, 0.5f);
            rectTransform.pivot = new Vector2(0, 0.5f);
            rectTransform.anchoredPosition = new Vector2(10, 0);
            rectTransform.sizeDelta = new Vector2(250, 400);

            // Add background
            var bgImage = heroPanelObj.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            var heroPanelUI = heroPanelObj.AddComponent<HeroPanelUI>();

            // Create hero name text
            var nameTextObj = new GameObject("HeroName");
            nameTextObj.transform.SetParent(heroPanelObj.transform, false);
            var nameTextRect = nameTextObj.AddComponent<RectTransform>();
            nameTextRect.anchorMin = new Vector2(0, 1);
            nameTextRect.anchorMax = new Vector2(1, 1);
            nameTextRect.pivot = new Vector2(0.5f, 1);
            nameTextRect.anchoredPosition = new Vector2(0, -10);
            nameTextRect.sizeDelta = new Vector2(-20, 30);

            var nameText = nameTextObj.AddComponent<TextMeshProUGUI>();
            nameText.fontSize = 20;
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = Color.white;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.text = "Hero Name";

            // Create level/class text
            var levelTextObj = new GameObject("LevelClass");
            levelTextObj.transform.SetParent(heroPanelObj.transform, false);
            var levelTextRect = levelTextObj.AddComponent<RectTransform>();
            levelTextRect.anchorMin = new Vector2(0, 1);
            levelTextRect.anchorMax = new Vector2(1, 1);
            levelTextRect.pivot = new Vector2(0.5f, 1);
            levelTextRect.anchoredPosition = new Vector2(0, -45);
            levelTextRect.sizeDelta = new Vector2(-20, 25);

            var levelText = levelTextObj.AddComponent<TextMeshProUGUI>();
            levelText.fontSize = 16;
            levelText.color = Color.yellow;
            levelText.alignment = TextAlignmentOptions.Center;
            levelText.text = "Level 1";

            // Create stats section (Attack, Defense, SpellPower, Knowledge)
            CreateHeroStat(heroPanelObj, "Attack", new Vector2(0, -85), "ATK");
            CreateHeroStat(heroPanelObj, "Defense", new Vector2(0, -115), "DEF");
            CreateHeroStat(heroPanelObj, "SpellPower", new Vector2(0, -145), "PWR");
            CreateHeroStat(heroPanelObj, "Knowledge", new Vector2(0, -175), "KNW");

            // Create additional stats
            CreateHeroStat(heroPanelObj, "Experience", new Vector2(0, -215), "EXP");
            CreateHeroStat(heroPanelObj, "Mana", new Vector2(0, -245), "MP");
            CreateHeroStat(heroPanelObj, "Movement", new Vector2(0, -275), "MOV");

            Debug.Log("Created HeroPanelUI");
        }

        private static void CreateHeroStat(GameObject parent, string name, Vector2 position, string label)
        {
            var statObj = new GameObject(name);
            statObj.transform.SetParent(parent.transform, false);

            var statRect = statObj.AddComponent<RectTransform>();
            statRect.anchorMin = new Vector2(0, 1);
            statRect.anchorMax = new Vector2(1, 1);
            statRect.pivot = new Vector2(0.5f, 1);
            statRect.anchoredPosition = position;
            statRect.sizeDelta = new Vector2(-20, 25);

            // Label
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(statObj.transform, false);
            var labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(0, 1);
            labelRect.pivot = new Vector2(0, 0.5f);
            labelRect.anchoredPosition = new Vector2(10, 0);
            labelRect.sizeDelta = new Vector2(100, 0);

            var labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.fontSize = 14;
            labelText.color = Color.gray;
            labelText.alignment = TextAlignmentOptions.Left;
            labelText.text = label + ":";

            // Value
            var valueObj = new GameObject("Value");
            valueObj.transform.SetParent(statObj.transform, false);
            var valueRect = valueObj.AddComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(1, 0);
            valueRect.anchorMax = new Vector2(1, 1);
            valueRect.pivot = new Vector2(1, 0.5f);
            valueRect.anchoredPosition = new Vector2(-10, 0);
            valueRect.sizeDelta = new Vector2(80, 0);

            var valueText = valueObj.AddComponent<TextMeshProUGUI>();
            valueText.fontSize = 14;
            valueText.color = Color.white;
            valueText.alignment = TextAlignmentOptions.Right;
            valueText.text = "0";
        }

        private static void CreateResourceTextField(GameObject parent, string name, Vector2 position)
        {
            var textObj = new GameObject(name);
            textObj.transform.SetParent(parent.transform, false);

            var rectTransform = textObj.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = new Vector2(140, 40);

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;
            text.text = name.Replace("Text", ": 0");
        }

        private static void AddDatabaseSingletons()
        {
            // Create Databases container
            var databasesObj = GameObject.Find("Databases");
            if (databasesObj == null)
            {
                databasesObj = new GameObject("Databases");
                Debug.Log("Created Databases container");
            }

            // Add HeroDatabase
            if (databasesObj.GetComponent<HeroDatabase>() == null)
            {
                var heroDb = databasesObj.AddComponent<HeroDatabase>();

                // Load and assign hero type data
                var heroTypes = AssetDatabase.FindAssets("t:HeroTypeData", new[] { "Assets/Data/Heroes" });
                if (heroTypes.Length > 0)
                {
                    var heroList = new System.Collections.Generic.List<HeroTypeData>();
                    foreach (var guid in heroTypes)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        var heroType = AssetDatabase.LoadAssetAtPath<HeroTypeData>(path);
                        if (heroType != null)
                        {
                            heroList.Add(heroType);
                        }
                    }

                    SetPrivateField(heroDb, "heroTypes", heroList);
                    Debug.Log($"Created HeroDatabase with {heroList.Count} hero types");
                }
                else
                {
                    Debug.LogWarning("No HeroTypeData assets found. Run 'Generate Sample Data' first.");
                }
            }

            // Add CreatureDatabase
            if (databasesObj.GetComponent<CreatureDatabase>() == null)
            {
                var creatureDb = databasesObj.AddComponent<CreatureDatabase>();

                // Load and assign creature data
                var creatures = AssetDatabase.FindAssets("t:CreatureData", new[] { "Assets/Data/Creatures" });
                if (creatures.Length > 0)
                {
                    var creatureList = new System.Collections.Generic.List<CreatureData>();
                    foreach (var guid in creatures)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        var creature = AssetDatabase.LoadAssetAtPath<CreatureData>(path);
                        if (creature != null)
                        {
                            creatureList.Add(creature);
                        }
                    }

                    SetPrivateField(creatureDb, "creatures", creatureList);
                    Debug.Log($"Created CreatureDatabase with {creatureList.Count} creatures");
                }
            }

            // Add SpellDatabase
            if (databasesObj.GetComponent<SpellDatabase>() == null)
            {
                var spellDb = databasesObj.AddComponent<SpellDatabase>();

                // Load and assign spell data
                var spells = AssetDatabase.FindAssets("t:SpellData", new[] { "Assets/Data/Spells" });
                if (spells.Length > 0)
                {
                    var spellList = new System.Collections.Generic.List<SpellData>();
                    foreach (var guid in spells)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        var spell = AssetDatabase.LoadAssetAtPath<SpellData>(path);
                        if (spell != null)
                        {
                            spellList.Add(spell);
                        }
                    }

                    SetPrivateField(spellDb, "spells", spellList);
                    Debug.Log($"Created SpellDatabase with {spellList.Count} spells");
                }
            }
        }

        private static void ConfigureMapRenderer()
        {
            var mapRenderer = GameObject.Find("Map")?.GetComponent<MapRenderer>();
            if (mapRenderer == null)
            {
                Debug.LogWarning("MapRenderer not found. Skipping terrain data auto-assignment.");
                return;
            }

            // Auto-load all TerrainData assets from Assets/Data/Terrain
            var terrainDataGuids = AssetDatabase.FindAssets("t:TerrainData", new[] { "Assets/Data/Terrain" });
            var terrainDataList = new System.Collections.Generic.List<Data.TerrainData>();

            foreach (var guid in terrainDataGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var terrainData = AssetDatabase.LoadAssetAtPath<Data.TerrainData>(path);
                if (terrainData != null)
                {
                    terrainDataList.Add(terrainData);
                }
            }

            if (terrainDataList.Count > 0)
            {
                SetPrivateField(mapRenderer, "terrainDataArray", terrainDataList.ToArray());
                Debug.Log($"Configured MapRenderer with {terrainDataList.Count} TerrainData assets");
            }
            else
            {
                Debug.LogWarning("No TerrainData assets found in Assets/Data/Terrain. Create terrain data first.");
            }
        }

        private static void AddGameInitializer()
        {
            if (GameObject.Find("GameInitializer") != null)
            {
                Debug.Log("GameInitializer already exists, skipping");
                return;
            }

            var initializerObj = new GameObject("GameInitializer");
            var initializer = initializerObj.AddComponent<GameInitializer>();

            // Wire HeroDatabase reference
            var databasesObj = GameObject.Find("Databases");
            if (databasesObj != null)
            {
                var heroDb = databasesObj.GetComponent<HeroDatabase>();
                if (heroDb != null)
                {
                    SetPrivateField(initializer, "heroDatabase", heroDb);
                    Debug.Log("Wired HeroDatabase to GameInitializer");
                }
            }

            Debug.Log("Created GameInitializer component");
        }

        private static void WireEventChannels()
        {
            // Load event channels
            var gameEvents = AssetDatabase.LoadAssetAtPath<GameEventChannel>("Assets/Data/EventChannels/GameEventChannel.asset");
            var mapEvents = AssetDatabase.LoadAssetAtPath<MapEventChannel>("Assets/Data/EventChannels/MapEventChannel.asset");
            var uiEvents = AssetDatabase.LoadAssetAtPath<UIEventChannel>("Assets/Data/EventChannels/UIEventChannel.asset");

            if (gameEvents == null || mapEvents == null || uiEvents == null)
            {
                Debug.LogWarning("⚠️ Event channels not found. Run 'Generate Event Channel Assets' first.");
                return;
            }

            // Find UI components and wire them
            var resourceBar = GameObject.Find("ResourceBar")?.GetComponent<ResourceBarUI>();
            var infoBar = GameObject.Find("InfoBar")?.GetComponent<InfoBarUI>();
            var turnControl = GameObject.Find("TurnControl")?.GetComponent<TurnControlUI>();

            if (resourceBar != null)
            {
                SetPrivateField(resourceBar, "gameEvents", gameEvents);
                Debug.Log("Wired ResourceBarUI to GameEventChannel");
            }

            if (infoBar != null)
            {
                SetPrivateField(infoBar, "gameEvents", gameEvents);
                SetPrivateField(infoBar, "uiEvents", uiEvents);
                Debug.Log("Wired InfoBarUI to event channels");
            }

            if (turnControl != null)
            {
                SetPrivateField(turnControl, "gameEvents", gameEvents);
                SetPrivateField(turnControl, "uiEvents", uiEvents);
                Debug.Log("Wired TurnControlUI to event channels");
            }
        }

        private static void CreateSampleHero()
        {
            if (GameObject.Find("TestHero") != null)
            {
                Debug.Log("TestHero already exists, skipping");
                return;
            }

            var heroObj = new GameObject("TestHero");
            var spriteRenderer = heroObj.AddComponent<SpriteRenderer>();
            spriteRenderer.color = Color.blue;

            var heroController = heroObj.AddComponent<HeroController>();

            // Position at center of map
            heroObj.transform.position = new Vector3(15, 15, 0);

            Debug.Log("Created sample hero at (15, 15)");
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                Debug.LogWarning($"Field '{fieldName}' not found on {obj.GetType().Name}");
            }
        }
    }
}
