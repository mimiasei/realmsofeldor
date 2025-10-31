using UnityEngine;
using RealmsOfEldor.Core;

namespace RealmsOfEldor.Controllers.Battle
{
    /// <summary>
    /// Test script to spawn multiple billboards in the battle scene to test shader performance.
    /// Attach this to any GameObject in the battle scene and it will spawn test sprites on Start.
    /// </summary>
    public class BattleBillboardTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private int spriteCount = 50;
        [SerializeField] private bool randomizeColors = true;
        [SerializeField] private Vector2 scaleRange = new Vector2(0.5f, 1.2f);

        void Start()
        {
            SpawnTestBillboards();
        }

        /// <summary>
        /// Spawns test billboards at random positions on the battlefield.
        /// </summary>
        [ContextMenu("Spawn Test Billboards")]
        public void SpawnTestBillboards()
        {
            Debug.Log($"BattleBillboardTest: Spawning {spriteCount} test billboards...");

            for (int i = 0; i < spriteCount; i++)
            {
                // Random hex position within battlefield bounds
                var hexX = Random.Range(0, BattleHexGrid.BATTLE_WIDTH);
                var hexY = Random.Range(0, BattleHexGrid.BATTLE_HEIGHT);
                var worldPos = BattleHexGrid.HexToWorld(hexX, hexY);

                // Add slight random Y offset for variety
                worldPos.y += Random.Range(-0.1f, 0.1f);

                // Create billboard GameObject
                var billboard = new GameObject($"TestBillboard_{i}");
                billboard.transform.position = worldPos;
                billboard.transform.SetParent(transform);

                // Create mesh for billboard
                var meshFilter = billboard.AddComponent<MeshFilter>();
                var meshRenderer = billboard.AddComponent<MeshRenderer>();

                meshFilter.mesh = CreateQuadMesh();

                // Use Cartographer billboard shader
                var billboardShader = Shader.Find("RealmsOfEldor/CartographerBillboard");
                if (billboardShader != null)
                {
                    var material = new Material(billboardShader);

                    // Create test sprite
                    var sprite = CreateTestSprite(randomizeColors);
                    material.mainTexture = sprite.texture;

                    // Random color tint
                    if (randomizeColors)
                    {
                        material.SetColor("_Color", Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.8f, 1f));
                    }

                    meshRenderer.material = material;
                    meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    meshRenderer.receiveShadows = false;

                    // Random scale
                    var scale = Random.Range(scaleRange.x, scaleRange.y);
                    billboard.transform.localScale = new Vector3(scale, scale, scale);

                    Debug.Log($"  Spawned billboard {i} at hex ({hexX}, {hexY}), world pos {worldPos}, scale {scale:F2}");
                }
                else
                {
                    Debug.LogError("BattleBillboardTest: Billboard shader not found!");
                    Destroy(billboard);
                    return;
                }
            }

            Debug.Log($"BattleBillboardTest: Successfully spawned {spriteCount} test billboards!");
        }

        /// <summary>
        /// Clears all test billboards spawned by this script.
        /// </summary>
        [ContextMenu("Clear Test Billboards")]
        public void ClearTestBillboards()
        {
            var childCount = transform.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name.StartsWith("TestBillboard_"))
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            Debug.Log($"BattleBillboardTest: Cleared {childCount} test billboards");
        }

        /// <summary>
        /// Creates a simple quad mesh for billboards.
        /// </summary>
        private Mesh CreateQuadMesh()
        {
            var mesh = new Mesh();
            mesh.name = "BillboardQuad";

            // Vertices for a quad centered at origin, 1 unit wide/tall
            mesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, 0f), // Bottom-left
                new Vector3(0.5f, -0.5f, 0f),  // Bottom-right
                new Vector3(-0.5f, 0.5f, 0f),  // Top-left
                new Vector3(0.5f, 0.5f, 0f)    // Top-right
            };

            // UVs
            mesh.uv = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };

            // Triangles
            mesh.triangles = new int[]
            {
                0, 2, 1,
                2, 3, 1
            };

            // Normals
            mesh.normals = new Vector3[]
            {
                Vector3.forward,
                Vector3.forward,
                Vector3.forward,
                Vector3.forward
            };

            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Creates a test sprite with random colors or patterns.
        /// </summary>
        private Sprite CreateTestSprite(bool randomize)
        {
            var size = 64;
            var texture = new Texture2D(size, size);
            var pixels = new Color[size * size];

            if (randomize)
            {
                // Random color with border
                var fillColor = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.8f, 1f);
                var borderColor = Color.Lerp(fillColor, Color.white, 0.5f);

                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        // Border
                        if (x < 2 || x >= size - 2 || y < 2 || y >= size - 2)
                        {
                            pixels[y * size + x] = borderColor;
                        }
                        // Fill
                        else
                        {
                            pixels[y * size + x] = fillColor;
                        }
                    }
                }
            }
            else
            {
                // Default magenta with yellow border
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        if (x < 2 || x >= size - 2 || y < 2 || y >= size - 2)
                        {
                            pixels[y * size + x] = Color.yellow;
                        }
                        else
                        {
                            pixels[y * size + x] = Color.magenta;
                        }
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            // 80 PPU to match battle stack size
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 80f);
        }
    }
}
