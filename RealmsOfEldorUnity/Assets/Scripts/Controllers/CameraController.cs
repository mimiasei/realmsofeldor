using UnityEngine;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Controls the camera for the adventure map.
    /// Handles panning, zooming, and camera bounds.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float panSpeed = 10f;
        [SerializeField] private float edgePanBorderSize = 20f;
        [SerializeField] private bool enableEdgePan = true;
        [SerializeField] private bool enableKeyboardPan = true;
        [SerializeField] private bool enableMouseDrag = true;

        [Header("Zoom Settings")]
        [SerializeField] private float zoomSpeed = 2f;
        [SerializeField] private float minZoom = 5f;
        [SerializeField] private float maxZoom = 20f;

        [Header("Bounds")]
        [SerializeField] private bool constrainToBounds = true;
        [SerializeField] private float minX = 0f;
        [SerializeField] private float maxX = 50f;
        [SerializeField] private float minY = 0f;
        [SerializeField] private float maxY = 50f;

        private Camera cam;
        private Vector3 lastMousePosition;
        private bool isDragging;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Update()
        {
            HandleKeyboardPan();
            HandleEdgePan();
            HandleMouseDrag();
            HandleZoom();
        }

        /// <summary>
        /// Handles camera panning with WASD or arrow keys.
        /// </summary>
        private void HandleKeyboardPan()
        {
            if (!enableKeyboardPan)
                return;

            var moveX = 0f;
            var moveY = 0f;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                moveY = 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                moveY = -1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                moveX = -1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                moveX = 1f;

            if (moveX != 0f || moveY != 0f)
            {
                var move = new Vector3(moveX, moveY, 0f) * (panSpeed * Time.deltaTime);
                transform.position += move;
                ClampCameraToBounds();
            }
        }

        /// <summary>
        /// Handles camera panning when mouse is near screen edges.
        /// </summary>
        private void HandleEdgePan()
        {
            if (!enableEdgePan)
                return;

            var mousePos = Input.mousePosition;
            var moveX = 0f;
            var moveY = 0f;

            // Right edge
            if (mousePos.x >= Screen.width - edgePanBorderSize)
                moveX = 1f;
            // Left edge
            else if (mousePos.x <= edgePanBorderSize)
                moveX = -1f;

            // Top edge
            if (mousePos.y >= Screen.height - edgePanBorderSize)
                moveY = 1f;
            // Bottom edge
            else if (mousePos.y <= edgePanBorderSize)
                moveY = -1f;

            if (moveX != 0f || moveY != 0f)
            {
                var move = new Vector3(moveX, moveY, 0f) * (panSpeed * Time.deltaTime);
                transform.position += move;
                ClampCameraToBounds();
            }
        }

        /// <summary>
        /// Handles camera panning with middle mouse button drag.
        /// </summary>
        private void HandleMouseDrag()
        {
            if (!enableMouseDrag)
                return;

            // Start drag
            if (Input.GetMouseButtonDown(2)) // Middle mouse button
            {
                isDragging = true;
                lastMousePosition = cam.ScreenToWorldPoint(Input.mousePosition);
            }

            // End drag
            if (Input.GetMouseButtonUp(2))
            {
                isDragging = false;
            }

            // Dragging
            if (isDragging)
            {
                var currentMousePosition = cam.ScreenToWorldPoint(Input.mousePosition);
                var delta = lastMousePosition - currentMousePosition;
                transform.position += delta;
                ClampCameraToBounds();

                lastMousePosition = cam.ScreenToWorldPoint(Input.mousePosition);
            }
        }

        /// <summary>
        /// Handles camera zoom with mouse scroll wheel.
        /// </summary>
        private void HandleZoom()
        {
            var scroll = Input.GetAxis("Mouse ScrollWheel");

            if (Mathf.Abs(scroll) > 0.01f)
            {
                var newSize = cam.orthographicSize - scroll * zoomSpeed;
                cam.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
                ClampCameraToBounds();
            }
        }

        /// <summary>
        /// Clamps camera position to defined bounds.
        /// </summary>
        private void ClampCameraToBounds()
        {
            if (!constrainToBounds)
                return;

            var pos = transform.position;

            // Calculate camera viewport bounds
            var verticalSize = cam.orthographicSize;
            var horizontalSize = verticalSize * cam.aspect;

            // Clamp position
            pos.x = Mathf.Clamp(pos.x, minX + horizontalSize, maxX - horizontalSize);
            pos.y = Mathf.Clamp(pos.y, minY + verticalSize, maxY - verticalSize);

            transform.position = pos;
        }

        /// <summary>
        /// Sets the camera bounds based on map size.
        /// </summary>
        public void SetBounds(float width, float height)
        {
            minX = 0f;
            maxX = width;
            minY = 0f;
            maxY = height;
        }

        /// <summary>
        /// Centers the camera on a specific position.
        /// </summary>
        public void CenterOn(Vector3 position)
        {
            var pos = transform.position;
            pos.x = position.x;
            pos.y = position.y;
            transform.position = pos;
            ClampCameraToBounds();
        }

        /// <summary>
        /// Smoothly moves camera to a position.
        /// </summary>
        public void MoveTo(Vector3 target, float duration = 0.5f)
        {
            StopAllCoroutines();
            StartCoroutine(MoveToCoroutine(target, duration));
        }

        private System.Collections.IEnumerator MoveToCoroutine(Vector3 target, float duration)
        {
            var startPos = transform.position;
            target.z = startPos.z; // Preserve Z

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);

                // Smooth easing
                t = t * t * (3f - 2f * t);

                var pos = Vector3.Lerp(startPos, target, t);
                transform.position = pos;
                ClampCameraToBounds();

                yield return null;
            }

            transform.position = new Vector3(target.x, target.y, startPos.z);
            ClampCameraToBounds();
        }

        /// <summary>
        /// Gets the world position of the mouse cursor.
        /// </summary>
        public Vector3 GetMouseWorldPosition()
        {
            var mousePos = Input.mousePosition;
            return cam.ScreenToWorldPoint(mousePos);
        }

        /// <summary>
        /// Checks if a world position is visible in the camera viewport.
        /// </summary>
        public bool IsPositionVisible(Vector3 worldPosition)
        {
            var viewportPoint = cam.WorldToViewportPoint(worldPosition);
            return viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
                   viewportPoint.y >= 0 && viewportPoint.y <= 1;
        }
    }
}
