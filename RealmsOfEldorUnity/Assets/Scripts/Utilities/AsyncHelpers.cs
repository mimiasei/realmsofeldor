using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace RealmsOfEldor.Utilities
{
    /// <summary>
    /// Helper utilities for async operations with UniTask.
    /// Provides common async patterns for game development.
    /// </summary>
    public static class AsyncHelpers
    {
        /// <summary>
        /// Delays execution for a specified duration (in seconds).
        /// Better than Coroutine WaitForSeconds - no GC allocation.
        /// </summary>
        public static async UniTask DelaySeconds(float seconds, CancellationToken cancellationToken = default)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Waits until a condition is true.
        /// Checks every frame by default.
        /// </summary>
        public static async UniTask WaitUntil(Func<bool> predicate, CancellationToken cancellationToken = default)
        {
            await UniTask.WaitUntil(predicate, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Waits while a condition is true.
        /// Checks every frame by default.
        /// </summary>
        public static async UniTask WaitWhile(Func<bool> predicate, CancellationToken cancellationToken = default)
        {
            await UniTask.WaitWhile(predicate, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Executes an action after a delay (fire-and-forget).
        /// </summary>
        public static void DelayedAction(float seconds, Action action, CancellationToken cancellationToken = default)
        {
            DelayedActionAsync(seconds, action, cancellationToken).Forget();
        }

        private static async UniTaskVoid DelayedActionAsync(float seconds, Action action, CancellationToken cancellationToken)
        {
            try
            {
                await DelaySeconds(seconds, cancellationToken);
                action?.Invoke();
            }
            catch (OperationCanceledException)
            {
                // Silently handle cancellation
            }
        }

        /// <summary>
        /// Repeats an action every interval until cancelled.
        /// </summary>
        public static async UniTask RepeatAction(float intervalSeconds, Action action, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                action?.Invoke();
                await DelaySeconds(intervalSeconds, cancellationToken);
            }
        }

        /// <summary>
        /// Fades a CanvasGroup alpha over time.
        /// </summary>
        public static async UniTask FadeCanvasGroup(CanvasGroup canvasGroup, float targetAlpha, float duration, CancellationToken cancellationToken = default)
        {
            if (canvasGroup == null)
                return;

            var startAlpha = canvasGroup.alpha;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            canvasGroup.alpha = targetAlpha;
        }

        /// <summary>
        /// Smoothly lerps a value over time.
        /// Returns the final value.
        /// </summary>
        public static async UniTask<float> LerpValue(float start, float end, float duration, Action<float> onUpdate, CancellationToken cancellationToken = default)
        {
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var value = Mathf.Lerp(start, end, t);
                onUpdate?.Invoke(value);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            onUpdate?.Invoke(end);
            return end;
        }

        /// <summary>
        /// Executes multiple async tasks in parallel and waits for all to complete.
        /// </summary>
        public static async UniTask WhenAll(params UniTask[] tasks)
        {
            await UniTask.WhenAll(tasks);
        }

        /// <summary>
        /// Executes multiple async tasks and returns when any one completes.
        /// </summary>
        public static async UniTask<int> WhenAny(params UniTask[] tasks)
        {
            return await UniTask.WhenAny(tasks);
        }

        /// <summary>
        /// Loads a scene asynchronously with progress callback.
        /// </summary>
        public static async UniTask LoadSceneAsync(string sceneName, Action<float> onProgress = null, CancellationToken cancellationToken = default)
        {
            var operation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
            if (operation == null)
            {
                Debug.LogError($"Failed to start loading scene: {sceneName}");
                return;
            }

            while (!operation.isDone)
            {
                onProgress?.Invoke(operation.progress);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            onProgress?.Invoke(1f);
        }

        /// <summary>
        /// Tries to execute an async operation with timeout.
        /// Returns true if completed within timeout, false if timed out.
        /// </summary>
        public static async UniTask<bool> TryWithTimeout(UniTask task, float timeoutSeconds)
        {
            var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(timeoutSeconds));
            var completedIndex = await UniTask.WhenAny(task, timeoutTask);
            return completedIndex == 0; // 0 = main task completed, 1 = timeout
        }

        /// <summary>
        /// Executes an async operation with retry logic.
        /// </summary>
        public static async UniTask<T> RetryAsync<T>(Func<UniTask<T>> operation, int maxRetries = 3, float delayBetweenRetries = 1f)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception e)
                {
                    if (i == maxRetries - 1)
                        throw;

                    Debug.LogWarning($"Retry {i + 1}/{maxRetries} after error: {e.Message}");
                    await DelaySeconds(delayBetweenRetries);
                }
            }

            throw new Exception("Retry failed");
        }
    }
}
