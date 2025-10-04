using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using RealmsOfEldor.Controllers;

namespace RealmsOfEldor.Editor
{
    /// <summary>
    /// Editor tool to set up the AdventureMap scene with all required GameObjects and components.
    /// Menu: Realms of Eldor/Setup/Create Adventure Map Scene
    /// </summary>
    public class AdventureMapSceneSetup
    {
        [MenuItem("Realms of Eldor/Setup/Create Adventure Map Scene")]
        public static void CreateAdventureMapScene()
        {
            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "AdventureMap";

            // Create root GameObject for organization
            var rootMap = new GameObject("Map");

            // Create Grid + Tilemaps
            CreateGridAndTilemaps(rootMap);

            // Create Camera with CameraController
            CreateCamera();

            // Create UI Canvas
            CreateUICanvas();

            // Create EventSystem for UI
            CreateEventSystem();

            // Create GameManagers object
            CreateGameManagers();

            // Save scene
            var scenePath = "Assets/Scenes/AdventureMap.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"<color=green>✓ Adventure Map scene created at {scenePath}</color>");

            // Select root object
            Selection.activeGameObject = rootMap;
        }

        private static void CreateGridAndTilemaps(GameObject parent)
        {
            // Create Grid
            var gridObj = new GameObject("Grid");
            gridObj.transform.SetParent(parent.transform);
            var grid = gridObj.AddComponent<Grid>();
            grid.cellSize = new Vector3(1, 1, 0);
            grid.cellLayout = GridLayout.CellLayout.Rectangle;

            // Create Terrain Tilemap
            var terrainObj = new GameObject("Terrain");
            terrainObj.transform.SetParent(gridObj.transform);
            var terrainTilemap = terrainObj.AddComponent<Tilemap>();
            var terrainRenderer = terrainObj.AddComponent<TilemapRenderer>();
            terrainRenderer.sortingLayerName = "Default";
            terrainRenderer.sortingOrder = 0;

            // Create Objects Tilemap
            var objectsObj = new GameObject("Objects");
            objectsObj.transform.SetParent(gridObj.transform);
            var objectsTilemap = objectsObj.AddComponent<Tilemap>();
            var objectsRenderer = objectsObj.AddComponent<TilemapRenderer>();
            objectsRenderer.sortingLayerName = "Default";
            objectsRenderer.sortingOrder = 1;

            // Create Highlights Tilemap
            var highlightsObj = new GameObject("Highlights");
            highlightsObj.transform.SetParent(gridObj.transform);
            var highlightsTilemap = highlightsObj.AddComponent<Tilemap>();
            var highlightsRenderer = highlightsObj.AddComponent<TilemapRenderer>();
            highlightsRenderer.sortingLayerName = "Default";
            highlightsRenderer.sortingOrder = 2;

            // Add MapRenderer component to Grid
            var mapRenderer = gridObj.AddComponent<MapRenderer>();

            // Use reflection to set private fields (since they're SerializeField)
            var type = typeof(MapRenderer);
            type.GetField("terrainTilemap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(mapRenderer, terrainTilemap);
            type.GetField("objectsTilemap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(mapRenderer, objectsTilemap);
            type.GetField("highlightsTilemap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(mapRenderer, highlightsTilemap);

            Debug.Log("✓ Grid and Tilemaps created (Terrain, Objects, Highlights)");
        }

        private static void CreateCamera()
        {
            var cameraObj = new GameObject("Main Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
            camera.tag = "MainCamera";

            // Add CameraController
            var cameraController = cameraObj.AddComponent<CameraController>();

            // Set some default values
            var type = typeof(CameraController);
            type.GetField("panSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(cameraController, 10f);
            type.GetField("edgePanSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(cameraController, 8f);
            type.GetField("scrollSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(cameraController, 2f);
            type.GetField("minZoom", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(cameraController, 2f);
            type.GetField("maxZoom", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(cameraController, 10f);

            Debug.Log("✓ Camera with CameraController created");
        }

        private static void CreateUICanvas()
        {
            var canvasObj = new GameObject("UI Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var canvasScaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasScaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Create placeholder UI groups
            CreateUIGroup(canvasObj.transform, "TopBar");
            CreateUIGroup(canvasObj.transform, "BottomBar");
            CreateUIGroup(canvasObj.transform, "LeftPanel");
            CreateUIGroup(canvasObj.transform, "RightPanel");

            Debug.Log("✓ UI Canvas created with placeholder UI groups");
        }

        private static void CreateUIGroup(Transform parent, string name)
        {
            var groupObj = new GameObject(name);
            groupObj.transform.SetParent(parent);
            var rectTransform = groupObj.AddComponent<RectTransform>();

            // Set anchors based on name
            switch (name)
            {
                case "TopBar":
                    rectTransform.anchorMin = new Vector2(0, 1);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    rectTransform.pivot = new Vector2(0.5f, 1);
                    rectTransform.sizeDelta = new Vector2(0, 60);
                    rectTransform.anchoredPosition = Vector2.zero;
                    break;
                case "BottomBar":
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(1, 0);
                    rectTransform.pivot = new Vector2(0.5f, 0);
                    rectTransform.sizeDelta = new Vector2(0, 100);
                    rectTransform.anchoredPosition = Vector2.zero;
                    break;
                case "LeftPanel":
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(0, 1);
                    rectTransform.pivot = new Vector2(0, 0.5f);
                    rectTransform.sizeDelta = new Vector2(200, 0);
                    rectTransform.anchoredPosition = Vector2.zero;
                    break;
                case "RightPanel":
                    rectTransform.anchorMin = new Vector2(1, 0);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    rectTransform.pivot = new Vector2(1, 0.5f);
                    rectTransform.sizeDelta = new Vector2(200, 0);
                    rectTransform.anchoredPosition = Vector2.zero;
                    break;
            }
        }

        private static void CreateEventSystem()
        {
            var eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            Debug.Log("✓ EventSystem created");
        }

        private static void CreateGameManagers()
        {
            var managersObj = new GameObject("GameManagers");
            Object.DontDestroyOnLoad(managersObj);

            // Add placeholder comment component
            var comment = managersObj.AddComponent<SceneComment>();
            comment.comment = "Add GameStateManager, CreatureDatabase, HeroDatabase, SpellDatabase here";

            Debug.Log("✓ GameManagers object created (add database singletons here)");
        }
    }

    /// <summary>
    /// Simple component to add comments to GameObjects in the scene
    /// </summary>
    public class SceneComment : MonoBehaviour
    {
        [TextArea(3, 10)]
        public string comment;
    }
}
