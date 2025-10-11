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
        [SerializeField] private float zoomSpeed = 2.5f;
        [SerializeField] private float minZoom = 5f;
        [SerializeField] private float maxZoom = 20f;
        [SerializeField] private float minPerspZoom = -50f;
        [SerializeField] private float maxPerspZoom = -10f;
        [SerializeField] private float zoomUnitsBeforeToStartCamRot = 5f;
        [SerializeField] private float regularCamRot = -30f;
        [SerializeField] private float maxZoomCamRot = -60f;

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
        private float targetZoom;
        private float targetZPosition; // For perspective zoom
        private float zoomVelocity; // For smooth damping

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            targetZoom = mainCamera.orthographic ? mainCamera.orthographicSize : 0f;
            targetZPosition = transform.position.z;
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
            if (!enableKeyboardPan)
            {
                Debug.LogWarning("HandleKeyboardPan: enableKeyboardPan is FALSE");
                return;
            }

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

            if (mainCamera.orthographic)
            {
                // Orthographic zoom
                if (scrollDelta != 0)
                {
                    targetZoom -= scrollDelta * zoomSpeed;
                    targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
                }
                mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, targetZoom, Time.deltaTime * 10f);
            }
            else
            {
                // Perspective zoom with smooth easing and rotation
                if (scrollDelta != 0)
                {
                    // Update target Z position (positive scroll = zoom in = increase Z toward 0)
                    targetZPosition += scrollDelta * zoomSpeed;
                    targetZPosition = Mathf.Clamp(targetZPosition, minPerspZoom, maxPerspZoom);
                }

                // Smooth zoom with quick ease in/out (exponential decay for snappy feel)
                var currentZ = transform.position.z;
                var newZ = Mathf.SmoothDamp(currentZ, targetZPosition, ref zoomVelocity, 0.15f, Mathf.Infinity, Time.deltaTime);
                var deltaZ = newZ - currentZ;

                // Calculate rotation based on zoom level
                // Rotation stays at -30° until we're close to max zoom, then transitions to -50°
                float xRotation;
                var rotationTransitionStart = maxPerspZoom - zoomUnitsBeforeToStartCamRot; // Start rotating zoomUnitsBeforeToStartCamRot units before max zoom

                if (newZ <= rotationTransitionStart)
                {
                    // Far zoom:
                    xRotation = regularCamRot;
                }
                else if (newZ >= maxPerspZoom)
                {
                    // Max zoom:
                    xRotation = maxZoomCamRot;
                }
                else
                {
                    // Transition zone: interpolate from regularCamRot to maxZoomCamRot
                    var t = Mathf.InverseLerp(rotationTransitionStart, maxPerspZoom, newZ);
                    // Apply easing for smooth rotation (ease in/out)
                    t = t * t * (3f - 2f * t); // Smoothstep
                    xRotation = Mathf.Lerp(regularCamRot, maxZoomCamRot, t);
                }

                var pos = transform.position;

                // Check if rotation is changing
                var currentRotation = transform.eulerAngles.x;
                if (currentRotation > 180f) currentRotation -= 360f;

                var isRotationChanging = Mathf.Abs(xRotation - currentRotation) > 0.01f;

                if (isRotationChanging)
                {
                    // ONLY compensate Y position when rotation is actively changing
                    // This maintains the ground point during rotation transition

                    // Calculate where camera is currently looking on the ground (y=0 plane)
                    // Camera looks DOWN and FORWARD. With camera at (x, y, z) and rotation rot:
                    // Ground point Y = camera.y + |camera.z| * tan(|rot|)
                    var currentTanAngle = Mathf.Tan(Mathf.Abs(currentRotation) * Mathf.Deg2Rad);
                    var groundPointY = pos.y + Mathf.Abs(pos.z) * currentTanAngle;

                    // Apply Z movement
                    pos.z = newZ;

                    // Recalculate camera Y to maintain same ground point with new rotation
                    // Solve: groundPointY = pos.y + |pos.z| * tan(|newRot|)
                    // Therefore: pos.y = groundPointY - |pos.z| * tan(|newRot|)
                    var newTanAngle = Mathf.Tan(Mathf.Abs(xRotation) * Mathf.Deg2Rad);
                    pos.y = groundPointY - Mathf.Abs(pos.z) * newTanAngle;
                }
                else
                {
                    // Rotation is constant: move in straight diagonal line
                    // Y movement is proportional to Z movement at fixed angle (-30° or -50°)
                    var tanAngle = Mathf.Tan(Mathf.Abs(xRotation) * Mathf.Deg2Rad);
                    deltaZ = newZ - currentZ;
                    pos.z = newZ;
                    pos.y += deltaZ * tanAngle; // Move diagonally at constant angle
                }

                // Apply rotation
                var rot = transform.eulerAngles;
                rot.x = xRotation;
                transform.eulerAngles = rot;

                // Apply position
                transform.position = pos;
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

            if (mainCamera.orthographic)
            {
                // Orthographic camera: simple 2D bounds
                var camHeight = mainCamera.orthographicSize * 2f;
                var camWidth = camHeight * mainCamera.aspect;

                var minX = mapMinBounds.x + camWidth / 2f;
                var maxX = mapMaxBounds.x - camWidth / 2f;
                var minY = mapMinBounds.y + camHeight / 2f;
                var maxY = mapMaxBounds.y - camHeight / 2f;

                if (minX >= maxX)
                    position.x = (mapMinBounds.x + mapMaxBounds.x) / 2f;
                else
                    position.x = Mathf.Clamp(position.x, minX, maxX);

                if (minY >= maxY)
                    position.y = (mapMinBounds.y + mapMaxBounds.y) / 2f;
                else
                    position.y = Mathf.Clamp(position.y, minY, maxY);
            }
            else
            {
                // Perspective camera with rotation: calculate what ground area is visible
                var rotation = transform.eulerAngles.x;
                if (rotation > 180f) rotation -= 360f;

                // Calculate the ground point where camera center looks
                var tanAngle = Mathf.Tan(Mathf.Abs(rotation) * Mathf.Deg2Rad);
                var groundCenterY = position.y + Mathf.Abs(position.z) * tanAngle;

                // For X bounds: just use simple horizontal FOV calculation
                // Distance to ground center along view ray
                var distanceToGround = Mathf.Abs(position.z) / Mathf.Cos(Mathf.Abs(rotation) * Mathf.Deg2Rad);
                var horizontalFOV = mainCamera.fieldOfView * mainCamera.aspect;
                var groundWidth = 2f * distanceToGround * Mathf.Tan(horizontalFOV * 0.5f * Mathf.Deg2Rad);

                // X bounds: keep camera so edges don't go outside map
                var minX = mapMinBounds.x + groundWidth / 2f;
                var maxX = mapMaxBounds.x - groundWidth / 2f;

                if (minX >= maxX)
                    position.x = (mapMinBounds.x + mapMaxBounds.x) / 2f;
                else
                    position.x = Mathf.Clamp(position.x, minX, maxX);

                // For Y bounds: constrain based on ground point, not camera position
                // We want the ground point to stay within map bounds with some buffer
                var minGroundY = mapMinBounds.y + 5f; // Small buffer
                var maxGroundY = mapMaxBounds.y - 5f;

                if (minGroundY >= maxGroundY)
                {
                    // Map too small, center on it
                    groundCenterY = (mapMinBounds.y + mapMaxBounds.y) / 2f;
                }
                else
                {
                    groundCenterY = Mathf.Clamp(groundCenterY, minGroundY, maxGroundY);
                }

                // Convert ground point back to camera Y position
                position.y = groundCenterY - Mathf.Abs(position.z) * tanAngle;
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
