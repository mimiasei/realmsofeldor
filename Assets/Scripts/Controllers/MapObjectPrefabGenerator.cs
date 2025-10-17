using UnityEngine;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Generates simple placeholder prefabs for map objects.
    /// These can be replaced with proper sprites/models later.
    /// </summary>
    public class MapObjectPrefabGenerator : MonoBehaviour
    {
        [Header("Generated Prefabs")]
        public GameObject ResourcePrefab { get; private set; }
        public GameObject MinePrefab { get; private set; }
        public GameObject DwellingPrefab { get; private set; }
        public GameObject ObstaclePrefab { get; private set; }

        private void Awake()
        {
            CreatePrefabs();
        }

        private void CreatePrefabs()
        {
            // Create Resource Prefab (Yellow Cube)
            ResourcePrefab = CreatePrefabWithCube("ResourcePrefab", new Color(1f, 0.84f, 0f), 0.5f);

            // Create Mine Prefab (Gray Cube)
            MinePrefab = CreatePrefabWithCube("MinePrefab", new Color(0.5f, 0.5f, 0.5f), 0.7f);

            // Create Dwelling Prefab (Brown Cube)
            DwellingPrefab = CreatePrefabWithCube("DwellingPrefab", new Color(0.6f, 0.4f, 0.2f), 0.8f);

            // Create Obstacle Prefab (Green Cube for trees/bushes, can vary by type later)
            ObstaclePrefab = CreatePrefabWithCube("ObstaclePrefab", new Color(0.2f, 0.6f, 0.2f), 0.4f);
        }

        private GameObject CreatePrefabWithCube(string name, Color color, float size)
        {
            var prefab = new GameObject(name);

            // Add a cube as visual representation
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(prefab.transform);
            cube.transform.localPosition = new Vector3(0, 0, -0.5f);
            cube.transform.localScale = new Vector3(size, size, size);

            // Set color
            var renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.color = color;
                renderer.material = material;
            }

            // Remove collider from visual cube (we don't need physics)
            var collider = cube.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            // Make prefab inactive initially (will be instantiated by MapRenderer)
            prefab.SetActive(false);

            return prefab;
        }

        /// <summary>
        /// Creates placeholder sprites for 2D rendering (alternative to cubes)
        /// </summary>
        private GameObject CreatePrefabWithSprite(string name, Color color, float size)
        {
            var prefab = new GameObject(name);

            // Add sprite renderer
            var spriteRenderer = prefab.AddComponent<SpriteRenderer>();

            // Create a simple square texture
            var texture = new Texture2D(32, 32);
            for (var y = 0; y < 32; y++)
            {
                for (var x = 0; x < 32; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();

            // Create sprite from texture
            var sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = 1; // Above terrain

            // Scale
            prefab.transform.localScale = Vector3.one * size;

            prefab.SetActive(false);

            return prefab;
        }
    }
}
