using UnityEngine;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Makes a sprite quad always face the camera using cylindrical billboarding.
    /// The sprite rotates horizontally (Y-axis) to face the camera but stays upright (no X or Z rotation).
    /// This is used for the 2.5D rendering system where sprites cast shadows onto the ground plane.
    ///
    /// Based on RENDERING_2_5D_BILLBOARD_SYSTEM.md - Script-Based Billboard Component (Phase 3)
    /// </summary>
    public class BillboardSprite : MonoBehaviour
    {
        [Header("Billboard Settings")]
        [Tooltip("The camera to face. If null, uses Camera.main.")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("If true, billboard only rotates around Y-axis (stays upright). If false, full billboard rotation.")]
        [SerializeField] private bool cylindricalBillboard = true;

        [Tooltip("Offset applied to rotation (degrees). Use this to adjust sprite facing if needed.")]
        [SerializeField] private float rotationOffset = 0f;

        void Start()
        {
            // If no camera assigned, use Camera.main
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null)
                {
                    Debug.LogWarning($"BillboardSprite on {gameObject.name}: No camera found!");
                }
            }
        }

        void LateUpdate()
        {
            if (targetCamera == null)
                return;

            if (cylindricalBillboard)
            {
                // Cylindrical billboard: Y-axis rotation only (sprite stays upright)
                var directionToCamera = targetCamera.transform.position - transform.position;
                directionToCamera.y = 0f; // Project onto horizontal plane

                // Only rotate if we have a valid direction
                if (directionToCamera.sqrMagnitude > 0.001f)
                {
                    var targetRotation = Quaternion.LookRotation(directionToCamera);
                    // Apply rotation offset if specified
                    if (Mathf.Abs(rotationOffset) > 0.001f)
                    {
                        targetRotation *= Quaternion.Euler(0f, rotationOffset, 0f);
                    }
                    transform.rotation = targetRotation;
                }
            }
            else
            {
                // Full billboard: faces camera completely
                var directionToCamera = targetCamera.transform.position - transform.position;
                if (directionToCamera.sqrMagnitude > 0.001f)
                {
                    var targetRotation = Quaternion.LookRotation(directionToCamera);
                    if (Mathf.Abs(rotationOffset) > 0.001f)
                    {
                        targetRotation *= Quaternion.Euler(0f, rotationOffset, 0f);
                    }
                    transform.rotation = targetRotation;
                }
            }
        }
    }
}
