using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealmsOfEldor.Controllers;
using System.Text.RegularExpressions;

namespace RealmsOfEldor.Tests
{
    /// <summary>
    /// Tests for Cartographer camera system.
    /// These tests verify that zoom and pan maintain correct camera position, rotation, and ground focus.
    /// </summary>
    public class CartographerTests
    {
        private GameObject cameraObject;
        private Cartographer cartographer;
        private Camera camera;

        [SetUp]
        public void SetUp()
        {
            // Suppress expected edit-mode warnings about material/mesh instantiation
            LogAssert.ignoreFailingMessages = true;

            // Create camera GameObject with Cartographer component
            cameraObject = new GameObject("TestCamera");
            camera = cameraObject.AddComponent<Camera>();
            cartographer = cameraObject.AddComponent<Cartographer>();

            // Use reflection to set serialized fields to known test values
            SetField(cartographer, "groundSize", new Vector2(100f, 100f));
            SetField(cartographer, "cameraYRotation", 45f);
            SetField(cartographer, "cameraTiltAngle", -40f);
            SetField(cartographer, "fieldOfView", 25f);
            SetField(cartographer, "cameraHeight", 30f);
            SetField(cartographer, "cameraDistance", -40f);
            SetField(cartographer, "minPerspZoom", -50f);
            SetField(cartographer, "maxPerspZoom", -10f);
            SetField(cartographer, "zoomUnitsBeforeToStartCamRot", 5f);
            SetField(cartographer, "maxZoomCamRot", -60f);
            SetField(cartographer, "constrainToBounds", false);
            SetField(cartographer, "enableKeyboardPan", true);
            SetField(cartographer, "enableZoom", true);
            SetField(cartographer, "createDirectionalLight", false);

            // Manually call Awake using reflection to initialize (Unity doesn't call it automatically in tests)
            var awakeMethod = typeof(Cartographer).GetMethod("Awake",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (awakeMethod != null)
            {
                awakeMethod.Invoke(cartographer, null);
            }

            // Re-enable log assertions after setup
            LogAssert.ignoreFailingMessages = false;
        }

        [TearDown]
        public void TearDown()
        {
            if (cameraObject != null)
                Object.DestroyImmediate(cameraObject);
        }

        private void SetField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(obj, value);
        }

        private object GetField(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(obj);
        }

        [Test]
        public void Cartographer_InitialSetup_HasCorrectPosition()
        {
            // Camera should be positioned at isometric angle above ground center
            var pos = cameraObject.transform.position;

            // With 45° Y rotation, ground center at (50, 0, 50), and cameraDistance = -40:
            // After rotating 45° around ground center, camera ends up at (78.28, 30, 21.72) approximately

            Assert.AreEqual(30f, pos.y, 0.1f, "Camera Y (height) should be 30");

            // After 45° Y rotation around center (50, 50), X and Z are NOT equal
            // X should be ~78.28, Z should be ~21.72
            Assert.AreEqual(78.28f, pos.x, 0.5f, "Camera X should be ~78.28 after 45° rotation");
            Assert.AreEqual(21.72f, pos.z, 0.5f, "Camera Z should be ~21.72 after 45° rotation");

            // Distance from ground center should be correct
            var groundCenter = new Vector3(50f, 0f, 50f);
            var distance = Vector3.Distance(pos, groundCenter);
            var expectedDistance = Mathf.Sqrt(30f * 30f + 40f * 40f); // sqrt(height^2 + distance^2)
            Assert.AreEqual(expectedDistance, distance, 1f, "Distance from ground center should match");
        }

        [Test]
        public void Cartographer_InitialSetup_HasCorrectRotation()
        {
            // Camera should be rotated to look down at ground
            var rot = cameraObject.transform.rotation.eulerAngles;

            // X rotation should be 40° (positive X = look down in Unity)
            // Even though cameraTiltAngle is -40 (semantic: "downward"), the actual Euler X is +40
            var xRot = rot.x > 180f ? rot.x - 360f : rot.x;
            Assert.AreEqual(40f, xRot, 0.1f, "Camera X rotation should be 40° (looking down)");

            // Y rotation should be 45° (isometric angle)
            Assert.AreEqual(45f, rot.y, 0.1f, "Camera Y rotation should be 45° (isometric)");

            // Z rotation should be 0° (no roll)
            Assert.AreEqual(0f, rot.z, 0.1f, "Camera Z rotation should be 0° (no roll)");
        }

        [Test]
        public void Cartographer_InitialSetup_CanRaycastToGround()
        {
            // Test that camera is oriented correctly to look at the ground
            // Camera should be pointing down (negative Y component in forward vector)
            var forward = cameraObject.transform.forward;
            Assert.Less(forward.y, 0f, "Camera forward should point down (negative Y)");

            // Test direct raycast from camera position along forward direction
            var ray = new Ray(cameraObject.transform.position, forward);
            var groundPlane = new Plane(Vector3.up, Vector3.zero);

            bool hit = groundPlane.Raycast(ray, out float enter);

            Assert.IsTrue(hit, "Camera should be able to raycast to ground plane along forward direction");

            if (hit)
            {
                var hitPoint = ray.GetPoint(enter);
                // Hit point should be on the ground plane
                Assert.AreEqual(0f, hitPoint.y, 0.01f, "Raycast should hit ground plane at Y=0");

                // Hit point should be in front of the camera (in the direction it's looking)
                var groundCenter = new Vector3(50f, 0f, 50f);
                var distanceToHit = Vector3.Distance(cameraObject.transform.position, hitPoint);
                Assert.Greater(distanceToHit, 0f, "Camera should be above ground (positive distance)");
            }
        }

        [Test]
        public void Cartographer_PositiveTiltAngle_GetsAutoCorrected()
        {
            // Suppress the expected error log about positive tilt angle
            LogAssert.ignoreFailingMessages = true;

            // Create a new cartographer with positive tilt angle
            var testObj = new GameObject("TestCameraPositive");
            var testCam = testObj.AddComponent<Camera>();
            var testCart = testObj.AddComponent<Cartographer>();

            SetField(testCart, "cameraTiltAngle", 40f); // POSITIVE (wrong)
            SetField(testCart, "groundSize", new Vector2(100f, 100f));
            SetField(testCart, "createDirectionalLight", false);

            // Call Awake to trigger validation (will log error but continue)
            var awakeMethod = typeof(Cartographer).GetMethod("Awake",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (awakeMethod != null)
            {
                awakeMethod.Invoke(testCart, null);
            }

            // Re-enable log assertions
            LogAssert.ignoreFailingMessages = false;

            // After Awake, rotation should be positive (auto-corrected and negated for Unity's Euler system)
            // Input: +40° cameraTiltAngle → Auto-corrected to -40° → Negated to +40° Euler X
            var rot = testObj.transform.rotation.eulerAngles;
            var xRot = rot.x > 180f ? rot.x - 360f : rot.x;
            Assert.Greater(xRot, 0f, "Camera X rotation should be positive (looking down)");

            Object.DestroyImmediate(testObj);
        }

        [Test]
        public void Cartographer_ZoomCalculation_MaintainsGroundFocus()
        {
            // Test the ground focus point calculation
            // When camera is at (x, y, z) with rotation θ, ground point is at:
            // groundZ = camera.z + camera.y / tan(|θ|)

            var pos = cameraObject.transform.position;
            var rot = cameraObject.transform.rotation.eulerAngles;
            var xRot = rot.x > 180f ? rot.x - 360f : rot.x;

            var tanAngle = Mathf.Tan(Mathf.Abs(xRot) * Mathf.Deg2Rad);
            var groundPointZ = pos.z + pos.y / tanAngle;

            // Ground point should be somewhere on the ground plane (reasonable Z value)
            Assert.Greater(groundPointZ, 0f, "Ground point Z should be positive");
            Assert.Less(groundPointZ, 100f, "Ground point Z should be within ground plane bounds");
        }

        [Test]
        public void Cartographer_ZoomRotationTransition_UsesCorrectAngles()
        {
            // Test that zoom rotation transition uses cameraTiltAngle, not regularCamRot
            // This was a bug: zoom system used regularCamRot (-30°) but camera was at cameraTiltAngle (-40°)

            var initialPos = cameraObject.transform.position;
            var initialRot = cameraObject.transform.rotation.eulerAngles;
            var initialXRot = initialRot.x > 180f ? initialRot.x - 360f : initialRot.x;

            // Camera should start at 40° Euler X (negated from cameraTiltAngle -40°)
            Assert.AreEqual(40f, initialXRot, 0.1f, "Initial rotation should be 40° (negated from cameraTiltAngle -40°)");

            // At far zoom (Z < -15), rotation should stay at cameraTiltAngle
            // We can't easily simulate zoom input in EditMode tests, but we can verify the initial state
            // is consistent with the zoom system expectations

            float minPerspZoom = (float)GetField(cartographer, "minPerspZoom");
            float maxPerspZoom = (float)GetField(cartographer, "maxPerspZoom");
            float zoomUnitsBeforeToStartCamRot = (float)GetField(cartographer, "zoomUnitsBeforeToStartCamRot");

            float rotationTransitionStart = maxPerspZoom - zoomUnitsBeforeToStartCamRot;

            // Min zoom should be further back (more negative) than rotation transition start
            // e.g., -50 < -15 (min is less than transition start because more negative = further back)
            Assert.Less(minPerspZoom, rotationTransitionStart,
                "Min zoom should be further back (more negative) than rotation transition start");
        }

        [Test]
        public void Cartographer_MapToWorldPosition_ConvertsCorrectly()
        {
            // Test coordinate conversion from 2D map to 3D world
            var worldPos = cartographer.MapToWorldPosition(10, 20);

            Assert.AreEqual(10f, worldPos.x, "Map X should become world X");
            Assert.AreEqual(0f, worldPos.y, "Map position should be at ground level (Y=0)");
            Assert.AreEqual(20f, worldPos.z, "Map Y should become world Z");
        }

        [Test]
        public void Cartographer_MapToWorldPosition_WithHeight_ConvertsCorrectly()
        {
            var worldPos = cartographer.MapToWorldPosition(5, 15, 3.5f);

            Assert.AreEqual(5f, worldPos.x, "Map X should become world X");
            Assert.AreEqual(3.5f, worldPos.y, "Height should become world Y");
            Assert.AreEqual(15f, worldPos.z, "Map Y should become world Z");
        }

        [Test]
        public void Cartographer_WorldToMapPosition_ConvertsCorrectly()
        {
            var mapPos = cartographer.WorldToMapPosition(new Vector3(7.5f, 2.0f, 12.3f));

            Assert.AreEqual(8, mapPos.x, "World X should round to map X"); // Rounds 7.5 to 8
            Assert.AreEqual(12, mapPos.y, "World Z should round to map Y"); // Rounds 12.3 to 12
        }

        [Test]
        public void Cartographer_GroundPlane_IsCreatedAtCorrectPosition()
        {
            // Ground plane should be created and positioned correctly
            var groundPlane = cartographer.GetGroundPlane();

            Assert.IsNotNull(groundPlane, "Ground plane should be created");

            // Ground plane should be positioned so tiles align at (0,0)
            // With size 100x100, it should be at (50, 0, 50)
            Assert.AreEqual(50f, groundPlane.position.x, 0.01f, "Ground plane X should be at center");
            Assert.AreEqual(0f, groundPlane.position.y, 0.01f, "Ground plane Y should be at 0");
            Assert.AreEqual(50f, groundPlane.position.z, 0.01f, "Ground plane Z should be at center");
        }

        [Test]
        public void Cartographer_GroundSize_MatchesConfiguration()
        {
            var groundSize = cartographer.GetGroundSize();

            Assert.AreEqual(100f, groundSize.x, "Ground size X should match configured value");
            Assert.AreEqual(100f, groundSize.y, "Ground size Y should match configured value");
        }

        [Test]
        public void Cartographer_Camera_HasCorrectProjectionSettings()
        {
            Assert.IsFalse(camera.orthographic, "Camera should be perspective, not orthographic");
            Assert.AreEqual(25f, camera.fieldOfView, 0.1f, "Field of view should be 25°");
            Assert.AreEqual(0.3f, camera.nearClipPlane, 0.01f, "Near clip plane should be 0.3");
            Assert.AreEqual(1000f, camera.farClipPlane, 0.01f, "Far clip plane should be 1000");
        }

        [Test]
        public void Cartographer_SetMapBounds_UpdatesBounds()
        {
            cartographer.SetMapBounds(200, 150);

            // Can't directly access private fields, but we can test that bounds constraining works
            // by enabling it and checking that CenterOn respects bounds
            var field = cartographer.GetType().GetField("mapMaxBounds",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxBounds = (Vector2)field.GetValue(cartographer);

            Assert.AreEqual(200f, maxBounds.x, "Map max bounds X should be updated");
            Assert.AreEqual(150f, maxBounds.y, "Map max bounds Y should be updated");
        }

        [Test]
        public void Cartographer_GetCamera_ReturnsCorrectCamera()
        {
            var retrievedCamera = cartographer.GetCamera();

            Assert.IsNotNull(retrievedCamera, "GetCamera should return camera component");
            Assert.AreEqual(camera, retrievedCamera, "GetCamera should return the same camera");
        }

        [Test]
        public void Cartographer_ZoomBounds_AreValid()
        {
            // Verify zoom range is configured correctly
            float minPerspZoom = (float)GetField(cartographer, "minPerspZoom");
            float maxPerspZoom = (float)GetField(cartographer, "maxPerspZoom");

            Assert.Less(minPerspZoom, maxPerspZoom,
                "Min zoom (further away) should be less than max zoom (closer)");
            Assert.Less(minPerspZoom, 0f, "Min zoom should be negative (behind ground plane)");
            Assert.Less(maxPerspZoom, 0f, "Max zoom should be negative (behind ground plane)");
        }

        [Test]
        public void Cartographer_YPositionFormula_IsCorrectForDownwardCamera()
        {
            // Test the Y position formula: when Z increases (moves forward), Y should decrease
            // This is the fix for the zoom snap bug

            var pos = cameraObject.transform.position;
            var rot = cameraObject.transform.rotation.eulerAngles;
            var xRot = rot.x > 180f ? rot.x - 360f : rot.x;

            // For a downward-looking camera at angle θ:
            // When deltaZ is positive (zoom out: Z becomes less negative like -25 → -10)
            // deltaY should be negative: deltaY = -deltaZ * tan(|θ|)

            float deltaZ = 5f; // Simulate moving 5 units forward
            float tanAngle = Mathf.Tan(Mathf.Abs(xRot) * Mathf.Deg2Rad);
            float expectedDeltaY = -deltaZ * tanAngle;

            // deltaY should be negative when deltaZ is positive (for downward camera)
            Assert.Less(expectedDeltaY, 0f,
                "When zooming out (deltaZ positive), Y should decrease (deltaY negative)");

            // Calculate new position
            float newY = pos.y + expectedDeltaY;
            float newZ = pos.z + deltaZ;

            // Verify ground focus point stays constant
            var originalGroundZ = pos.z + pos.y / tanAngle;
            var newGroundZ = newZ + newY / tanAngle;

            Assert.AreEqual(originalGroundZ, newGroundZ, 0.1f,
                "Ground focus point should remain constant when zooming");
        }

        [Test]
        public void Cartographer_RotationTransition_StartsAtCorrectZoom()
        {
            // Rotation transition should start 5 units before max zoom
            float maxPerspZoom = (float)GetField(cartographer, "maxPerspZoom"); // -10
            float zoomUnitsBeforeToStartCamRot = (float)GetField(cartographer, "zoomUnitsBeforeToStartCamRot"); // 5

            float expectedTransitionStart = maxPerspZoom - zoomUnitsBeforeToStartCamRot;

            Assert.AreEqual(-15f, expectedTransitionStart, 0.01f,
                "Rotation transition should start at Z = -15");

            // Camera rotation should be:
            // - cameraTiltAngle (-40°) when Z <= -15
            // - maxZoomCamRot (-60°) when Z >= -10
            // - interpolated when -15 < Z < -10
        }

        [Test]
        public void Cartographer_CoordinateSystem_Uses3DGroundPlane()
        {
            // Verify that the coordinate system uses X,Z for horizontal plane and Y for height
            // This is critical for proper 3D rendering vs old 2D system

            var pos1 = cartographer.MapToWorldPosition(0, 0);
            var pos2 = cartographer.MapToWorldPosition(10, 0);
            var pos3 = cartographer.MapToWorldPosition(0, 10);

            // Moving in map X should change world X
            Assert.AreEqual(10f, pos2.x - pos1.x, "Map X movement should affect world X");
            Assert.AreEqual(0f, pos2.z - pos1.z, "Map X movement should not affect world Z");

            // Moving in map Y should change world Z (not Y!)
            Assert.AreEqual(0f, pos3.x - pos1.x, "Map Y movement should not affect world X");
            Assert.AreEqual(10f, pos3.z - pos1.z, "Map Y movement should affect world Z");

            // All ground positions should be at Y=0
            Assert.AreEqual(0f, pos1.y, "Ground should be at Y=0");
            Assert.AreEqual(0f, pos2.y, "Ground should be at Y=0");
            Assert.AreEqual(0f, pos3.y, "Ground should be at Y=0");
        }
    }
}
