using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Controls camera movement for the adventure map.
    /// Supports panning, zooming, and smooth camera transitions.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private float panSpeed = 20f;
        [SerializeField] private float edgePanSpeed = 15f;
        [SerializeField] private float edgePanThreshold = 10f;
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float minZoom = 5f;
        [SerializeField] private float maxZoom = 20f;

        [Header("Map Bounds")]
        [SerializeField] private Vector2 mapMinBounds;
        [SerializeField] private Vector2 mapMaxBounds;
        [SerializeField] private bool constrainToBounds = true;

        [Header("Input")]
        [SerializeField] private bool enableKeyboardPan = true;
        [SerializeField] private bool enableEdgePan = true;
        [SerializeField] private bool enableMouseDrag = true;
        [SerializeField] private bool enableZoom = true;

        private Vector3 dragOrigin;
        private bool isDragging;
        private CancellationTokenSource moveCts;

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
        }

        private void OnDestroy()
        {
            moveCts?.Cancel();
            moveCts?.Dispose();
        }

        private void Update()
        {
            HandleKeyboardPan();
            HandleEdgePan();
            HandleMouseDrag();
            HandleZoom();
        }

        // Keyboard WASD/Arrow keys panning
        private void HandleKeyboardPan()
        {
            if (!enableKeyboardPan) return;

            var moveDir = Vector3.zero;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                moveDir.y += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                moveDir.y -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                moveDir.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                moveDir.x += 1f;

            if (moveDir != Vector3.zero)
            {
                var newPos = transform.position + moveDir.normalized * panSpeed * Time.deltaTime;
                transform.position = ConstrainPosition(newPos);
            }
        }

        // Edge panning (mouse near screen edges)
        private void HandleEdgePan()
        {
            if (!enableEdgePan) return;

            var mousePos = Input.mousePosition;
            var moveDir = Vector3.zero;

            if (mousePos.x < edgePanThreshold)
                moveDir.x -= 1f;
            else if (mousePos.x > Screen.width - edgePanThreshold)
                moveDir.x += 1f;

            if (mousePos.y < edgePanThreshold)
                moveDir.y -= 1f;
            else if (mousePos.y > Screen.height - edgePanThreshold)
                moveDir.y += 1f;

            if (moveDir != Vector3.zero)
            {
                var newPos = transform.position + moveDir.normalized * edgePanSpeed * Time.deltaTime;
                transform.position = ConstrainPosition(newPos);
            }
        }

        // Middle mouse button drag
        private void HandleMouseDrag()
        {
            if (!enableMouseDrag) return;

            if (Input.GetMouseButtonDown(2)) // Middle mouse button
            {
                dragOrigin = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                isDragging = true;
            }

            if (Input.GetMouseButton(2) && isDragging)
            {
                var currentPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                var diff = dragOrigin - currentPos;
                var newPos = transform.position + diff;
                transform.position = ConstrainPosition(newPos);
            }

            if (Input.GetMouseButtonUp(2))
            {
                isDragging = false;
            }
        }

        // Mouse scroll wheel zoom
        private void HandleZoom()
        {
            if (!enableZoom) return;

            var scrollDelta = Input.mouseScrollDelta.y;
            if (scrollDelta != 0)
            {
                var currentSize = mainCamera.orthographicSize;
                var newSize = currentSize - scrollDelta * zoomSpeed;
                newSize = Mathf.Clamp(newSize, minZoom, maxZoom);
                mainCamera.orthographicSize = newSize;
            }
        }

        // Smooth camera movement
        public async UniTask MoveToAsync(Vector3 targetPosition, float duration = 1f, CancellationToken ct = default)
        {
            // Cancel any previous movement
            moveCts?.Cancel();
            moveCts?.Dispose();
            moveCts = CancellationTokenSource.CreateLinkedTokenSource(ct, this.GetCancellationTokenOnDestroy());

            var startPos = transform.position;
            targetPosition = ConstrainPosition(targetPosition);
            var elapsedTime = 0f;

            try
            {
                while (elapsedTime < duration)
                {
                    elapsedTime += Time.deltaTime;
                    var t = Mathf.Clamp01(elapsedTime / duration);
                    t = Mathf.SmoothStep(0f, 1f, t); // Smooth ease in/out
                    transform.position = Vector3.Lerp(startPos, targetPosition, t);

                    await UniTask.Yield(PlayerLoopTiming.Update, moveCts.Token);
                }

                transform.position = targetPosition;
            }
            catch (System.OperationCanceledException)
            {
                // Movement was cancelled
            }
        }

        // Fire-and-forget wrapper for backwards compatibility
        public void MoveTo(Vector3 targetPosition, float duration = 1f)
        {
            MoveToAsync(targetPosition, duration).Forget();
        }

        // Center on position instantly
        public void CenterOn(Vector3 position)
        {
            position.z = transform.position.z; // Maintain Z position
            transform.position = ConstrainPosition(position);
        }

        // Constrain position to map bounds
        private Vector3 ConstrainPosition(Vector3 position)
        {
            if (!constrainToBounds)
                return position;

            // Calculate camera viewport bounds in world space
            var camHeight = mainCamera.orthographicSize * 2f;
            var camWidth = camHeight * mainCamera.aspect;

            var minX = mapMinBounds.x + camWidth / 2f;
            var maxX = mapMaxBounds.x - camWidth / 2f;
            var minY = mapMinBounds.y + camHeight / 2f;
            var maxY = mapMaxBounds.y - camHeight / 2f;

            // If camera is larger than map, center it instead of clamping
            if (minX >= maxX)
            {
                position.x = (mapMinBounds.x + mapMaxBounds.x) / 2f;
            }
            else
            {
                position.x = Mathf.Clamp(position.x, minX, maxX);
            }

            if (minY >= maxY)
            {
                position.y = (mapMinBounds.y + mapMaxBounds.y) / 2f;
            }
            else
            {
                position.y = Mathf.Clamp(position.y, minY, maxY);
            }

            return position;
        }

        // Set map bounds from map size
        public void SetMapBounds(int width, int height)
        {
            mapMinBounds = new Vector2(0, 0);
            mapMaxBounds = new Vector2(width, height);
        }

        // Utility methods
        public Vector3 GetMouseWorldPosition()
        {
            return mainCamera.ScreenToWorldPoint(Input.mousePosition);
        }

        public bool IsPositionVisible(Vector3 worldPosition)
        {
            var viewportPos = mainCamera.WorldToViewportPoint(worldPosition);
            return viewportPos.x >= 0 && viewportPos.x <= 1 &&
                   viewportPos.y >= 0 && viewportPos.y <= 1;
        }
    }
}
