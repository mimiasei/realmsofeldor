using RealmsOfEldor.Core;
using UnityEngine;
using TMPro;
using RealmsOfEldor.Core.Battle;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Visual representation of a battle stack (creature unit).
    /// Based on VCMI's BattleStacksController creature rendering.
    /// </summary>
    public class BattleStackView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer creatureSprite;
        [SerializeField] private TextMeshPro amountText;
        [SerializeField] private GameObject amountBadge;
        [SerializeField] private SpriteRenderer selectionIndicator;

        [Header("Visual Settings")]
        [SerializeField] private Color allyColor = new Color(0.2f, 0.8f, 0.2f, 0.5f);
        [SerializeField] private Color enemyColor = new Color(0.8f, 0.2f, 0.2f, 0.5f);
        [SerializeField] private Color selectedColor = Color.yellow;

        private BattleStack cachedStack;
        private bool isSelected = false;

        void Awake()
        {
            // Auto-create components if not assigned
            if (creatureSprite == null)
            {
                var spriteObj = new GameObject("CreatureSprite");
                spriteObj.transform.SetParent(transform);
                spriteObj.transform.localPosition = Vector3.zero;
                creatureSprite = spriteObj.AddComponent<SpriteRenderer>();
                creatureSprite.sortingOrder = 10; // Render stacks above field
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
                // Size based on hex dimensions
                boxCollider.size = new Vector2(BattleHexGrid.HEX_WIDTH * 0.8f, BattleHexGrid.HEX_HEIGHT * 0.8f);
            }
        }

        /// <summary>
        /// Initializes this view with a battle stack.
        /// </summary>
        public void Initialize(BattleStack stack, Sprite creatureIcon = null)
        {
            cachedStack = stack;

            // Set creature sprite (use placeholder if none provided)
            if (creatureIcon != null)
            {
                creatureSprite.sprite = creatureIcon;
            }
            else
            {
                creatureSprite.sprite = CreatePlaceholderSprite();
            }

            // Update amount text
            UpdateAmount();

            // Position at hex location
            var worldPos = BattleHexGrid.HexToWorld(stack.Position.X, stack.Position.Y);
            transform.position = worldPos;

            // Set team color
            var isAlly = stack.Side == BattleSide.Attacker; // TODO: Make this configurable
            selectionIndicator.color = isAlly ? allyColor : enemyColor;

            Debug.Log($"BattleStackView: Initialized stack {stack.Id} ({stack.Count}x creature {stack.CreatureId}) at hex ({stack.Position.X}, {stack.Position.Y})");
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
            Debug.Log($"BattleStackView: Stack {cachedStack?.Id} took {damage} damage");
            UpdateAmount();
        }

        /// <summary>
        /// Plays death animation and destroys the view.
        /// </summary>
        public void PlayDeathAnimation()
        {
            // TODO: Implement death animation (fade out, sprite change, etc.)
            Debug.Log($"BattleStackView: Stack {cachedStack?.Id} died");
            Destroy(gameObject, 1f); // Destroy after 1 second
        }

        /// <summary>
        /// Creates the amount badge (purple background with white text).
        /// </summary>
        private void CreateAmountBadge()
        {
            amountBadge = new GameObject("AmountBadge");
            amountBadge.transform.SetParent(transform);
            amountBadge.transform.localPosition = new Vector3(BattleHexGrid.HEX_WIDTH * 0.3f, -BattleHexGrid.HEX_HEIGHT * 0.3f, -0.1f);

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
            amountText.fontSize = 4;
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
            indicatorObj.transform.localPosition = new Vector3(0f, -BattleHexGrid.HEX_HEIGHT * 0.4f, 0.1f);

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
            var texture = new Texture2D(32, 32);
            var pixels = new Color[32 * 32];

            // Create a simple colored square
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(0.7f, 0.7f, 0.7f, 1f);
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
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

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
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

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
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
            Debug.Log($"BattleStackView: Stack {cachedStack?.Id} clicked");
            // TODO: Raise event for stack selection
        }
    }
}
