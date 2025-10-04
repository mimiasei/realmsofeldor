using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using RealmsOfEldor.Core;
using RealmsOfEldor.Data.EventChannels;
using RealmsOfEldor.Utilities;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// MonoBehaviour controller for hero representation on the adventure map.
    /// Handles visual representation, animations, and movement.
    /// Subscribes to hero events and updates visuals accordingly.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class HeroController : MonoBehaviour
    {
        [Header("Hero Data")]
        [SerializeField] private int heroInstanceId;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite heroSprite;
        [SerializeField] private Color playerColor = Color.white;

        [Header("Animation")]
        [SerializeField] private float movementSpeed = 2f;
        [SerializeField] private Ease movementEase = Ease.InOutQuad;
        [SerializeField] private float bobHeight = 0.1f;
        [SerializeField] private float bobDuration = 0.5f;

        [Header("Selection")]
        [SerializeField] private GameObject selectionIndicator;
        [SerializeField] private Color selectedColor = Color.yellow;
        [SerializeField] private Color unselectedColor = Color.white;

        [Header("Event Channels")]
        [SerializeField] private GameEventChannel gameEvents;
        [SerializeField] private MapEventChannel mapEvents;
        [SerializeField] private UIEventChannel uiEvents;

        private Hero heroData;
        private bool isSelected = false;
        private bool isMoving = false;
        private Tween currentMovementTween;

        void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            if (selectionIndicator != null)
                selectionIndicator.SetActive(false);
        }

        void OnEnable()
        {
            if (gameEvents != null)
            {
                gameEvents.OnHeroMoved += HandleHeroMoved;
                gameEvents.OnHeroDefeated += HandleHeroDefeated;
            }

            if (mapEvents != null)
            {
                mapEvents.OnHeroTeleported += HandleHeroTeleported;
            }
        }

        void OnDisable()
        {
            if (gameEvents != null)
            {
                gameEvents.OnHeroMoved -= HandleHeroMoved;
                gameEvents.OnHeroDefeated -= HandleHeroDefeated;
            }

            if (mapEvents != null)
            {
                mapEvents.OnHeroTeleported -= HandleHeroTeleported;
            }

            // Clean up tweens
            currentMovementTween?.Kill();
        }

        /// <summary>
        /// Initializes the hero controller with hero data.
        /// </summary>
        public void Initialize(Hero hero, Vector3 worldPosition)
        {
            heroData = hero;
            heroInstanceId = hero.InstanceId;
            transform.position = worldPosition;

            UpdateVisuals();
        }

        /// <summary>
        /// Updates visual representation based on hero data.
        /// </summary>
        private void UpdateVisuals()
        {
            if (heroData == null || spriteRenderer == null)
                return;

            // Set sprite (would load from hero database in production)
            if (heroSprite != null)
                spriteRenderer.sprite = heroSprite;

            // Apply player color tint
            spriteRenderer.color = playerColor;

            // Update selection indicator
            if (selectionIndicator != null)
                selectionIndicator.SetActive(isSelected);
        }

        /// <summary>
        /// Moves hero to target position with animation.
        /// </summary>
        public async UniTask MoveToAsync(Vector3 targetWorldPosition, float? customSpeed = null)
        {
            if (isMoving)
            {
                Debug.LogWarning($"Hero {heroData?.Name} is already moving!");
                return;
            }

            isMoving = true;

            var speed = customSpeed ?? movementSpeed;
            var distance = Vector3.Distance(transform.position, targetWorldPosition);
            var duration = distance / speed;

            // Kill any existing tween
            currentMovementTween?.Kill();

            // Create movement sequence with bob animation
            var sequence = DOTween.Sequence();

            // Main movement
            sequence.Append(transform.DOMove(targetWorldPosition, duration).SetEase(movementEase));

            // Bobbing animation during movement
            var bobTween = transform.DOMoveY(transform.position.y + bobHeight, bobDuration / 2f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
            sequence.Join(bobTween);

            currentMovementTween = sequence;

            await sequence.AsyncWaitForCompletion().AsUniTask();

            // Reset Y position after bobbing
            transform.position = new Vector3(transform.position.x, targetWorldPosition.y, transform.position.z);

            isMoving = false;
            currentMovementTween = null;
        }

        /// <summary>
        /// Teleports hero instantly to target position (no animation).
        /// </summary>
        public void TeleportTo(Vector3 targetWorldPosition)
        {
            // Kill any ongoing movement
            currentMovementTween?.Kill();
            isMoving = false;

            transform.position = targetWorldPosition;
        }

        /// <summary>
        /// Sets the selection state of this hero.
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;

            if (selectionIndicator != null)
                selectionIndicator.SetActive(selected);

            // Update sprite color
            if (spriteRenderer != null)
                spriteRenderer.color = selected ? selectedColor : unselectedColor;
        }

        /// <summary>
        /// Sets the player color for this hero's visual representation.
        /// </summary>
        public void SetPlayerColor(Color color)
        {
            playerColor = color;
            if (spriteRenderer != null && !isSelected)
                spriteRenderer.color = color;
        }

        /// <summary>
        /// Handles hero click for selection.
        /// </summary>
        void OnMouseDown()
        {
            if (uiEvents != null && heroData != null)
            {
                uiEvents.RaiseHeroSelected(heroData);
            }
        }

        /// <summary>
        /// Handles hero hover for tooltips.
        /// </summary>
        void OnMouseEnter()
        {
            if (uiEvents != null && heroData != null)
            {
                var tooltipText = $"{heroData.Name} (Lvl {heroData.Level})\nMP: {heroData.MovementPoints}/{heroData.MaxMovementPoints}";
                uiEvents.RaiseShowTooltip(tooltipText);
            }
        }

        void OnMouseExit()
        {
            if (uiEvents != null)
            {
                uiEvents.RaiseHideTooltip();
            }
        }

        // ===== Event Handlers =====

        private void HandleHeroMoved(Hero hero, Position oldPos, Position newPos)
        {
            if (hero.InstanceId != heroInstanceId)
                return;

            // Movement animation is handled by AdventureMapController
            // This just updates the hero data reference
            heroData = hero;
        }

        private void HandleHeroTeleported(Hero hero, Position oldPos, Position newPos)
        {
            if (hero.InstanceId != heroInstanceId)
                return;

            heroData = hero;

            // Instant teleport (no animation)
            // World position conversion would be done by MapRenderer
            // For now, just log
            Debug.Log($"{hero.Name} teleported from {oldPos} to {newPos}");
        }

        private void HandleHeroDefeated(Hero hero)
        {
            if (hero.InstanceId != heroInstanceId)
                return;

            // Play defeat animation and destroy
            PlayDefeatedAnimationAsync().Forget();
        }

        private async UniTaskVoid PlayDefeatedAnimationAsync()
        {
            // Fade out animation
            if (spriteRenderer != null)
            {
                await spriteRenderer.DOFade(0f, 0.5f).AsyncWaitForCompletion().AsUniTask();
            }

            // Destroy the game object
            Destroy(gameObject);
        }

        // ===== Public Getters =====

        public Hero HeroData => heroData;
        public int HeroInstanceId => heroInstanceId;
        public bool IsSelected => isSelected;
        public bool IsMoving => isMoving;

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            // Draw hero position indicator
            Gizmos.color = isSelected ? Color.yellow : Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.3f);

            // Draw hero name
            if (heroData != null)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, heroData.Name);
            }
        }
#endif
    }
}
