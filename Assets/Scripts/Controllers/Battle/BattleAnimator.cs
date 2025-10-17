using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using RealmsOfEldor.Core.Battle;

namespace RealmsOfEldor.Controllers.Battle
{
    /// <summary>
    /// Coordinates all battle animations using DOTween and UniTask.
    /// Based on VCMI's animation staging system.
    /// Ensures animations play sequentially and don't overlap.
    /// </summary>
    public class BattleAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float movementSpeed = 5f;
        [SerializeField] private float attackDuration = 0.3f;
        [SerializeField] private float damageFadeDelay = 0.5f;
        [SerializeField] private float damageFadeDuration = 1f;
        [SerializeField] private float deathFadeDuration = 0.5f;
        [SerializeField] private float selectionBounceDuration = 0.3f;

        [Header("Effect Prefabs")]
        [SerializeField] private GameObject floatingTextPrefab;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private GameObject impactEffectPrefab;

        // Animation queue system
        private Queue<System.Func<UniTask>> animationQueue = new Queue<System.Func<UniTask>>();
        private bool isPlayingAnimations = false;

        // References
        private BattleStackRenderer stackRenderer;
        private Camera battleCamera;

        void Awake()
        {
            stackRenderer = FindFirstObjectByType<BattleStackRenderer>();
            battleCamera = Camera.main;
        }

        #region Animation Queue Management

        /// <summary>
        /// Adds an animation to the queue and starts processing if not already running.
        /// </summary>
        private void EnqueueAnimation(System.Func<UniTask> animation)
        {
            animationQueue.Enqueue(animation);

            if (!isPlayingAnimations)
            {
                ProcessAnimationQueue().Forget();
            }
        }

        /// <summary>
        /// Processes all animations in the queue sequentially.
        /// </summary>
        private async UniTask ProcessAnimationQueue()
        {
            isPlayingAnimations = true;

            while (animationQueue.Count > 0)
            {
                var animation = animationQueue.Dequeue();
                await animation();
            }

            isPlayingAnimations = false;
        }

        /// <summary>
        /// Waits for all queued animations to complete.
        /// </summary>
        public async UniTask WaitForAnimations()
        {
            while (isPlayingAnimations || animationQueue.Count > 0)
            {
                await UniTask.Yield();
            }
        }

        #endregion

        #region Movement Animation

        /// <summary>
        /// Animates a stack moving from one hex to another.
        /// </summary>
        public void AnimateMovement(int stackId, Vector2Int fromHex, Vector2Int toHex)
        {
            EnqueueAnimation(() => AnimateMovementAsync(stackId, fromHex, toHex));
        }

        private async UniTask AnimateMovementAsync(int stackId, Vector2Int fromHex, Vector2Int toHex)
        {
            var stackView = stackRenderer?.GetStackView(stackId);
            if (stackView == null)
            {
                Debug.LogWarning($"BattleAnimator: Cannot animate movement - stack {stackId} not found");
                return;
            }

            var fromPos = BattleHexGrid.HexToWorld(fromHex.x, fromHex.y);
            var toPos = BattleHexGrid.HexToWorld(toHex.x, toHex.y);

            // Calculate distance and duration
            var distance = Vector3.Distance(fromPos, toPos);
            var duration = distance / movementSpeed;

            Debug.Log($"BattleAnimator: Moving stack {stackId} from {fromHex} to {toHex} (duration: {duration:F2}s)");

            // Animate movement with slight bounce
            stackView.transform
                .DOMove(toPos, duration)
                .SetEase(Ease.InOutQuad);

            await UniTask.Delay((int)(duration * 1000));

            // Update internal position
            stackView.MoveTo(toHex.x, toHex.y);
        }

        /// <summary>
        /// Animates movement along a path (multiple hexes).
        /// </summary>
        public void AnimatePathMovement(int stackId, List<Vector2Int> path)
        {
            EnqueueAnimation(() => AnimatePathMovementAsync(stackId, path));
        }

        private async UniTask AnimatePathMovementAsync(int stackId, List<Vector2Int> path)
        {
            if (path == null || path.Count < 2)
                return;

            for (var i = 0; i < path.Count - 1; i++)
            {
                await AnimateMovementAsync(stackId, path[i], path[i + 1]);
            }
        }

        #endregion

        #region Attack Animation

        /// <summary>
        /// Animates a melee attack (attacker moves forward and back).
        /// </summary>
        public void AnimateMeleeAttack(int attackerId, int targetId, int damage)
        {
            EnqueueAnimation(() => AnimateMeleeAttackAsync(attackerId, targetId, damage));
        }

        private async UniTask AnimateMeleeAttackAsync(int attackerId, int targetId, int damage)
        {
            var attackerView = stackRenderer?.GetStackView(attackerId);
            var targetView = stackRenderer?.GetStackView(targetId);

            if (attackerView == null || targetView == null)
            {
                Debug.LogWarning($"BattleAnimator: Cannot animate melee attack - stacks not found");
                return;
            }

            var attackerPos = attackerView.transform.position;
            var targetPos = targetView.transform.position;
            var attackDirection = (targetPos - attackerPos).normalized;
            var lungeDistance = 0.3f; // Move 30% toward target

            Debug.Log($"BattleAnimator: Melee attack - stack {attackerId} → stack {targetId} ({damage} damage)");

            // Lunge forward
            var lungePos = attackerPos + attackDirection * lungeDistance;
            attackerView.transform
                .DOMove(lungePos, attackDuration * 0.5f)
                .SetEase(Ease.OutQuad);
            await UniTask.Delay((int)(attackDuration * 500));

            // Show damage
            ShowFloatingDamage(targetView.transform.position, damage);

            // Camera shake on impact
            ShakeCamera(0.1f, 0.1f);

            // Return to original position
            attackerView.transform
                .DOMove(attackerPos, attackDuration * 0.5f)
                .SetEase(Ease.InQuad);
            await UniTask.Delay((int)(attackDuration * 500));

            // Update target's amount
            targetView.UpdateAmount();
        }

        /// <summary>
        /// Animates a ranged attack (projectile from attacker to target).
        /// </summary>
        public void AnimateRangedAttack(int attackerId, int targetId, int damage)
        {
            EnqueueAnimation(() => AnimateRangedAttackAsync(attackerId, targetId, damage));
        }

        private async UniTask AnimateRangedAttackAsync(int attackerId, int targetId, int damage)
        {
            var attackerView = stackRenderer?.GetStackView(attackerId);
            var targetView = stackRenderer?.GetStackView(targetId);

            if (attackerView == null || targetView == null)
            {
                Debug.LogWarning($"BattleAnimator: Cannot animate ranged attack - stacks not found");
                return;
            }

            Debug.Log($"BattleAnimator: Ranged attack - stack {attackerId} → stack {targetId} ({damage} damage)");

            // Create projectile
            var projectile = CreateProjectile(attackerView.transform.position, targetView.transform.position);

            if (projectile != null)
            {
                // Animate projectile
                var duration = 0.5f;
                projectile.transform
                    .DOMove(targetView.transform.position, duration)
                    .SetEase(Ease.Linear);
                await UniTask.Delay((int)(duration * 1000));

                // Destroy projectile and show impact
                Destroy(projectile);
                ShowImpactEffect(targetView.transform.position);
            }

            // Show damage
            ShowFloatingDamage(targetView.transform.position, damage);

            // Update target's amount
            targetView.UpdateAmount();
        }

        #endregion

        #region Damage Display

        /// <summary>
        /// Shows floating damage text above the target.
        /// </summary>
        private void ShowFloatingDamage(Vector3 position, int damage)
        {
            GameObject textObj;

            if (floatingTextPrefab != null)
            {
                textObj = Instantiate(floatingTextPrefab, position, Quaternion.identity);
            }
            else
            {
                // Create simple floating text
                textObj = new GameObject("FloatingDamage");
                textObj.transform.position = position + Vector3.up * 0.5f;

                var textMesh = textObj.AddComponent<TextMesh>();
                textMesh.text = $"-{damage}";
                textMesh.fontSize = 20;
                textMesh.color = Color.red;
                textMesh.alignment = TextAlignment.Center;
                textMesh.anchor = TextAnchor.MiddleCenter;
            }

            // Animate upward and fade out
            var startPos = textObj.transform.position;
            var endPos = startPos + Vector3.up * 1f;

            var sequence = DOTween.Sequence();
            sequence.AppendInterval(damageFadeDelay);
            sequence.Append(textObj.transform.DOMove(endPos, damageFadeDuration).SetEase(Ease.OutQuad));

            // Fade out text
            var textMeshComponent = textObj.GetComponent<TextMesh>();
            if (textMeshComponent != null)
            {
                sequence.Join(DOTween.ToAlpha(
                    () => textMeshComponent.color,
                    x => textMeshComponent.color = x,
                    0f,
                    damageFadeDuration
                ));
            }

            sequence.OnComplete(() => Destroy(textObj));
        }

        #endregion

        #region Death Animation

        /// <summary>
        /// Animates stack death (fade out and destroy).
        /// </summary>
        public void AnimateDeath(int stackId)
        {
            EnqueueAnimation(() => AnimateDeathAsync(stackId));
        }

        private async UniTask AnimateDeathAsync(int stackId)
        {
            var stackView = stackRenderer?.GetStackView(stackId);
            if (stackView == null)
            {
                Debug.LogWarning($"BattleAnimator: Cannot animate death - stack {stackId} not found");
                return;
            }

            Debug.Log($"BattleAnimator: Stack {stackId} dying");

            // Get all SpriteRenderers in the stack view
            var spriteRenderers = stackView.GetComponentsInChildren<SpriteRenderer>();

            // Fade out all sprites using DOTween's ToAlpha
            foreach (var sprite in spriteRenderers)
            {
                DOTween.ToAlpha(
                    () => sprite.color,
                    x => sprite.color = x,
                    0f,
                    deathFadeDuration
                );
            }

            // Fade out TextMesh components
            var textMeshes = stackView.GetComponentsInChildren<TextMesh>();
            foreach (var textMesh in textMeshes)
            {
                DOTween.ToAlpha(
                    () => textMesh.color,
                    x => textMesh.color = x,
                    0f,
                    deathFadeDuration
                );
            }

            await UniTask.Delay((int)(deathFadeDuration * 1000));

            // Remove from renderer and destroy
            stackRenderer?.RemoveStack(stackId);
        }

        #endregion

        #region Selection Animation

        /// <summary>
        /// Animates stack selection (bounce effect).
        /// </summary>
        public void AnimateSelection(int stackId)
        {
            var stackView = stackRenderer?.GetStackView(stackId);
            if (stackView == null)
                return;

            // Kill any existing selection animation
            stackView.transform.DOKill();

            // Bounce animation
            var originalScale = stackView.transform.localScale;
            var sequence = DOTween.Sequence();
            sequence.Append(stackView.transform.DOScale(originalScale * 1.1f, selectionBounceDuration * 0.5f));
            sequence.Append(stackView.transform.DOScale(originalScale, selectionBounceDuration * 0.5f));
        }

        /// <summary>
        /// Animates hex highlight (pulse effect).
        /// </summary>
        public void AnimateHexHighlight(Vector2Int hex, Color color, float duration = 1f)
        {
            // TODO: Implement hex highlight pulse animation
            // This would animate the LineRenderer's color and width
            Debug.Log($"BattleAnimator: Highlighting hex {hex} with color {color}");
        }

        #endregion

        #region Camera Effects

        /// <summary>
        /// Shakes the camera for impact feedback.
        /// </summary>
        private void ShakeCamera(float strength, float duration)
        {
            if (battleCamera != null)
            {
                battleCamera.transform.DOShakePosition(duration, strength, 10, 90f);
            }
        }

        #endregion

        #region Effect Creation

        /// <summary>
        /// Creates a projectile GameObject.
        /// </summary>
        private GameObject CreateProjectile(Vector3 from, Vector3 to)
        {
            GameObject projectile;

            if (projectilePrefab != null)
            {
                projectile = Instantiate(projectilePrefab, from, Quaternion.identity);
            }
            else
            {
                // Create simple projectile sprite
                projectile = new GameObject("Projectile");
                projectile.transform.position = from;

                var spriteRenderer = projectile.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = CreateCircleSprite(8);
                spriteRenderer.color = new Color(1f, 0.5f, 0f); // Orange
                spriteRenderer.sortingOrder = 50;

                // Face target
                var direction = (to - from).normalized;
                var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                projectile.transform.rotation = Quaternion.Euler(0, 0, angle);
            }

            return projectile;
        }

        /// <summary>
        /// Shows impact effect at position.
        /// </summary>
        private void ShowImpactEffect(Vector3 position)
        {
            GameObject effect;

            if (impactEffectPrefab != null)
            {
                effect = Instantiate(impactEffectPrefab, position, Quaternion.identity);
            }
            else
            {
                // Create simple impact flash
                effect = new GameObject("ImpactEffect");
                effect.transform.position = position;

                var spriteRenderer = effect.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = CreateCircleSprite(16);
                spriteRenderer.color = Color.yellow;
                spriteRenderer.sortingOrder = 51;

                // Fade out quickly using DOTween.ToAlpha
                DOTween.ToAlpha(
                    () => spriteRenderer.color,
                    x => spriteRenderer.color = x,
                    0f,
                    0.2f
                ).OnComplete(() => Destroy(effect));
                effect.transform.DOScale(Vector3.one * 2f, 0.2f);
            }
        }

        /// <summary>
        /// Creates a simple circle sprite for projectiles/effects.
        /// </summary>
        private Sprite CreateCircleSprite(int size)
        {
            var texture = new Texture2D(size, size);
            var pixels = new Color[size * size];

            var center = size * 0.5f;
            var radius = size * 0.5f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);

                    pixels[y * size + x] = distance <= radius ? Color.white : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Clears all queued animations.
        /// </summary>
        public void ClearAnimations()
        {
            animationQueue.Clear();
            DOTween.KillAll();
            isPlayingAnimations = false;
        }

        /// <summary>
        /// Gets whether animations are currently playing.
        /// </summary>
        public bool IsAnimating => isPlayingAnimations || animationQueue.Count > 0;

        #endregion
    }
}
