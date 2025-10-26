using RealmsOfEldor.Core;
using UnityEngine;
using TMPro;
using RealmsOfEldor.Core.Battle;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Visual representation of a battle stack (creature unit).
    /// Based on VCMI's BattleStacksController creature rendering.
    /// Uses mesh-based billboard rendering for GPU-accelerated performance.
    /// </summary>
    public class BattleStackView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject creatureSpriteObject;
        [SerializeField] private MeshRenderer creatureMeshRenderer;
        [SerializeField] private TextMeshPro amountText;
        [SerializeField] private GameObject amountBadge;
        [SerializeField] private SpriteRenderer selectionIndicator;

        [Header("Visual Settings")]
        [SerializeField] private Color allyColor = new Color(0.2f, 0.8f, 0.2f, 0.5f);
        [SerializeField] private Color enemyColor = new Color(0.8f, 0.2f, 0.2f, 0.5f);
        [SerializeField] private Color selectedColor = Color.yellow;

        private BattleStack cachedStack;
        private bool isSelected = false;
        private Sprite currentSprite; // Store sprite reference for bounds/texture

        void Awake()
        {
            // Auto-create components if not assigned
            if (creatureSpriteObject == null)
            {
                creatureSpriteObject = new GameObject("CreatureSprite");
                creatureSpriteObject.transform.SetParent(transform);
                creatureSpriteObject.transform.localPosition = Vector3.zero;

                // Create MeshRenderer + MeshFilter for shader-based billboard (more efficient)
                var meshFilter = creatureSpriteObject.AddComponent<MeshFilter>();
                creatureMeshRenderer = creatureSpriteObject.AddComponent<MeshRenderer>();

                // Create a simple quad mesh for the sprite
                meshFilter.mesh = CreateQuadMesh();

                // Use billboard shader instead of sprite renderer
                var billboardShader = Shader.Find("RealmsOfEldor/BillboardCylindrical");
                if (billboardShader != null)
                {
                    var material = new Material(billboardShader);
                    creatureMeshRenderer.material = material;
                    creatureMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    creatureMeshRenderer.receiveShadows = false;
                }
                else
                {
                    Debug.LogWarning("BattleStackView: Billboard shader 'RealmsOfEldor/BillboardCylindrical' not found, using default unlit shader");
                    var material = new Material(Shader.Find("Unlit/Texture"));
                    creatureMeshRenderer.material = material;
                }
            }

            if (amountBadge == null)
            {
                CreateAmountBadge();
            }

            if (selectionIndicator == null)
            {
                CreateSelectionIndicator();
            }

            // Hide selection indicator by default
            selectionIndicator.enabled = false;

            // Add collider for mouse click detection if not present
            if (GetComponent<Collider2D>() == null)
            {
                var boxCollider = gameObject.AddComponent<BoxCollider2D>();
                // Size based on hex dimensions (1.0 world units now)
                boxCollider.size = new Vector2(0.8f, 0.8f);
            }
        }

        /// <summary>
        /// Initializes this view with a battle stack.
        /// </summary>
        public void Initialize(BattleStack stack, Sprite creatureIcon = null)
        {
            cachedStack = stack;

            // Get sprite to use (provided or placeholder)
            if (creatureIcon != null)
            {
                currentSprite = creatureIcon;
            }
            else
            {
                currentSprite = CreatePlaceholderSprite();
            }

            // Apply texture to billboard shader material
            if (creatureMeshRenderer != null && creatureMeshRenderer.material != null && currentSprite != null)
            {
                creatureMeshRenderer.material.mainTexture = currentSprite.texture;

                // Set proper scaling based on sprite bounds
                var scale = currentSprite.bounds.size; // Size in world units
                creatureSpriteObject.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            }
            else
            {
                Debug.LogError($"BattleStackView: Failed to initialize stack {stack.Id} - meshRenderer or material or sprite is null!");
            }

            // Update amount text
            UpdateAmount();

            // Position at hex location on X,Z ground plane
            var worldPos = BattleHexGrid.HexToWorld(stack.Position.X, stack.Position.Y);
            transform.position = worldPos; // worldPos is now (x, 0, z) on ground plane

            // Set team color
            var isAlly = stack.Side == BattleSide.Attacker;
            selectionIndicator.color = isAlly ? allyColor : enemyColor;
        }

        /// <summary>
        /// Updates the amount text to reflect current stack count.
        /// </summary>
        public void UpdateAmount()
        {
            if (cachedStack != null && amountText != null)
            {
                amountText.text = cachedStack.Count.ToString();
            }
        }

        /// <summary>
        /// Moves this stack view to a new hex position.
        /// </summary>
        public void MoveTo(int hexX, int hexY)
        {
            var worldPos = BattleHexGrid.HexToWorld(hexX, hexY);
            transform.position = worldPos;
        }

        /// <summary>
        /// Sets whether this stack is selected.
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;

            if (selected)
            {
                selectionIndicator.color = selectedColor;
                selectionIndicator.enabled = true;
            }
            else
            {
                var isAlly = cachedStack != null && cachedStack.Side == BattleSide.Attacker;
                selectionIndicator.color = isAlly ? allyColor : enemyColor;
                selectionIndicator.enabled = false;
            }
        }

        /// <summary>
        /// Shows damage dealt to this stack.
        /// </summary>
        public void ShowDamage(int damage)
        {
            // TODO: Implement floating damage text animation
            UpdateAmount();
        }

        /// <summary>
        /// Plays death animation and destroys the view.
        /// </summary>
        public void PlayDeathAnimation()
        {
            // TODO: Implement death animation (fade out, sprite change, etc.)
            Destroy(gameObject, 1f); // Destroy after 1 second
        }

        /// <summary>
        /// Creates the amount badge (purple background with white text).
        /// </summary>
        private void CreateAmountBadge()
        {
            amountBadge = new GameObject("AmountBadge");
            amountBadge.transform.SetParent(transform);
            // Position at bottom-right of creature sprite (hex is 1.0 units now)
            amountBadge.transform.localPosition = new Vector3(0.3f, -0.3f, -0.1f);

            // Background sprite
            var badgeSprite = amountBadge.AddComponent<SpriteRenderer>();
            badgeSprite.sprite = CreateBadgeSprite();
            badgeSprite.color = new Color(0.5f, 0.2f, 0.8f, 0.8f); // Purple
            badgeSprite.sortingOrder = 11;

            // Amount text
            var textObj = new GameObject("AmountText");
            textObj.transform.SetParent(amountBadge.transform);
            textObj.transform.localPosition = Vector3.zero;

            amountText = textObj.AddComponent<TextMeshPro>();
            amountText.text = "0";
            amountText.fontSize = 0.15f; // Scaled down from 4 to match new world scale
            amountText.alignment = TextAlignmentOptions.Center;
            amountText.color = Color.white;
            amountText.sortingOrder = 12;
        }

        /// <summary>
        /// Creates the selection indicator (circle/ring under creature).
        /// </summary>
        private void CreateSelectionIndicator()
        {
            var indicatorObj = new GameObject("SelectionIndicator");
            indicatorObj.transform.SetParent(transform);
            // Position below creature (hex is 1.0 units now)
            indicatorObj.transform.localPosition = new Vector3(0f, -0.4f, 0.1f);

            selectionIndicator = indicatorObj.AddComponent<SpriteRenderer>();
            selectionIndicator.sprite = CreateCircleSprite(16);
            selectionIndicator.color = allyColor;
            selectionIndicator.sortingOrder = 9; // Below stack sprite
            selectionIndicator.enabled = false;
        }

        /// <summary>
        /// Creates a simple placeholder sprite for creatures without icons.
        /// </summary>
        private Sprite CreatePlaceholderSprite()
        {
            var size = 64;
            var texture = new Texture2D(size, size);
            var pixels = new Color[size * size];

            // Create a colorful placeholder with border
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    // Border (bright yellow)
                    if (x < 2 || x >= size - 2 || y < 2 || y >= size - 2)
                    {
                        pixels[y * size + x] = Color.yellow;
                    }
                    // Fill (bright magenta for visibility)
                    else
                    {
                        pixels[y * size + x] = Color.magenta;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            // With hex size now 1.0 world unit, we want creature sprites to be about 0.8 units tall
            // A 64-pixel sprite should be ~0.8 world units, so PPU = 64 / 0.8 = 80
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 80f);
        }

        /// <summary>
        /// Creates a rounded badge sprite for amount display.
        /// </summary>
        private Sprite CreateBadgeSprite()
        {
            var size = 16;
            var texture = new Texture2D(size, size);
            var pixels = new Color[size * size];

            // Create a filled rounded rectangle
            var center = size * 0.5f;
            var radius = size * 0.4f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);

                    // Rounded corners
                    if (distance < radius)
                    {
                        pixels[y * size + x] = Color.white;
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            // Badge should be small - about 0.2 world units, so 16 pixels / 0.2 = 80 PPU
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 80f);
        }

        /// <summary>
        /// Creates a circle sprite for selection indicator.
        /// </summary>
        private Sprite CreateCircleSprite(int size)
        {
            var texture = new Texture2D(size, size);
            var pixels = new Color[size * size];

            var center = size * 0.5f;
            var outerRadius = size * 0.5f;
            var innerRadius = size * 0.35f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);

                    // Create ring (hollow circle)
                    if (distance >= innerRadius && distance <= outerRadius)
                    {
                        pixels[y * size + x] = Color.white;
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            // Selection indicator should be ~1.0 world units (size of hex), so 16 pixels / 1.0 = 16 PPU
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }

        /// <summary>
        /// Gets the stack this view represents.
        /// </summary>
        public BattleStack Stack => cachedStack;

        /// <summary>
        /// Gets the stack ID.
        /// </summary>
        public int StackId => cachedStack?.Id ?? -1;

        /// <summary>
        /// Handles mouse click on stack.
        /// </summary>
        void OnMouseDown()
        {
            // TODO: Raise event for stack selection
        }

        /// <summary>
        /// Creates a simple quad mesh for the billboard sprite.
        /// Centered at origin, 1x1 unit size (will be scaled by sprite bounds).
        /// </summary>
        private Mesh CreateQuadMesh()
        {
            var mesh = new Mesh();
            mesh.name = "BillboardQuad";

            // Vertices for a quad centered at origin, 1 unit wide/tall
            // In local space: vertices are offset from center
            mesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, 0f), // Bottom-left
                new Vector3(0.5f, -0.5f, 0f),  // Bottom-right
                new Vector3(-0.5f, 0.5f, 0f),  // Top-left
                new Vector3(0.5f, 0.5f, 0f)    // Top-right
            };

            // UVs for texture mapping (0,0 = bottom-left, 1,1 = top-right)
            mesh.uv = new Vector2[]
            {
                new Vector2(0, 0), // Bottom-left
                new Vector2(1, 0), // Bottom-right
                new Vector2(0, 1), // Top-left
                new Vector2(1, 1)  // Top-right
            };

            // Triangles (two triangles make a quad)
            mesh.triangles = new int[]
            {
                0, 2, 1, // First triangle
                2, 3, 1  // Second triangle
            };

            // Normals (pointing forward for lighting)
            mesh.normals = new Vector3[]
            {
                Vector3.forward,
                Vector3.forward,
                Vector3.forward,
                Vector3.forward
            };

            // Colors (white, will be tinted by material)
            mesh.colors = new Color[]
            {
                Color.white,
                Color.white,
                Color.white,
                Color.white
            };

            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
