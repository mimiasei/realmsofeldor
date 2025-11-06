using UnityEngine;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Generates simple placeholder prefabs for map objects using billboards.
    /// These can be replaced with proper sprites later.
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
            // Create Resource Prefab (Yellow Billboard)
            ResourcePrefab = CreatePrefabWithBillboard("ResourcePrefab", new Color(1f, 0.84f, 0f), 0.8f);

            // Create Mine Prefab (Gray Billboard)
            MinePrefab = CreatePrefabWithBillboard("MinePrefab", new Color(0.5f, 0.5f, 0.5f), 1.0f);

            // Create Dwelling Prefab (Brown Billboard)
            DwellingPrefab = CreatePrefabWithBillboard("DwellingPrefab", new Color(0.6f, 0.4f, 0.2f), 1.2f);

            // Create Obstacle Prefab (Green Billboard for trees/bushes)
            ObstaclePrefab = CreatePrefabWithBillboard("ObstaclePrefab", new Color(0.2f, 0.6f, 0.2f), 0.6f);
        }

        /// <summary>
        /// Creates a prefab with a billboard sprite using CartographerBillboard component.
        /// </summary>
        private GameObject CreatePrefabWithBillboard(string name, Color color, float size)
        {
            var prefab = new GameObject(name);

            // Create a simple square sprite
            var sprite = CreateSquareSprite(color, 64);

            // Add CartographerBillboard component
            var billboard = prefab.AddComponent<CartographerBillboard>();
            billboard.SetSprite(sprite);
            billboard.SetTint(color);
            billboard.SetHeightOffset(0.5f); // Lift slightly off ground
            billboard.SetCastShadows(true);

            // Scale
            prefab.transform.localScale = Vector3.one * size;

            // Make prefab inactive initially (will be instantiated by MapRenderer)
            prefab.SetActive(false);

            return prefab;
        }

        /// <summary>
        /// Creates a simple square sprite texture.
        /// </summary>
        private Sprite CreateSquareSprite(Color color, int size)
        {
            var texture = new Texture2D(size, size);
            var pixels = new Color[size * size];

            for (var i = 0; i < pixels.Length; i++)
            {
                // Create border effect
                var x = i % size;
                var y = i / size;
                var isBorder = x < 2 || x >= size - 2 || y < 2 || y >= size - 2;
                pixels[i] = isBorder ? Color.Lerp(color, Color.white, 0.3f) : color;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
