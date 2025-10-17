using UnityEngine;

namespace RealmsOfEldor.Data
{
    /// <summary>
    /// Global game settings ScriptableObject.
    /// Contains configurable gameplay settings like hero movement speed.
    /// </summary>
    [CreateAssetMenu(fileName = "GameSettings", menuName = "Game/Game Settings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Hero Movement")]
        [Tooltip("Time in seconds for hero to move one tile")]
        [Range(0.1f, 2.0f)]
        public float heroMovementTimePerTile = 0.5f;

        [Header("Camera")]
        [Tooltip("Camera pan speed")]
        [Range(1f, 20f)]
        public float cameraPanSpeed = 8f;

        [Tooltip("Camera zoom speed")]
        [Range(0.5f, 5f)]
        public float cameraZoomSpeed = 2f;

        [Header("UI")]
        [Tooltip("Enable keyboard shortcuts")]
        public bool enableKeyboardShortcuts = true;

        [Tooltip("Double-tap threshold in seconds")]
        [Range(0.1f, 1.0f)]
        public float doubleTapThreshold = 0.3f;

        [Header("Battle")]
        [Tooltip("Enable battle animations")]
        public bool enableBattleAnimations = true;

        [Tooltip("Battle animation speed multiplier")]
        [Range(0.5f, 3.0f)]
        public float battleAnimationSpeed = 1.0f;

        private static GameSettings instance;

        /// <summary>
        /// Singleton instance - loads from Resources/GameSettings
        /// </summary>
        public static GameSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<GameSettings>("GameSettings");
                    if (instance == null)
                    {
                        Debug.LogWarning("GameSettings not found in Resources folder. Creating default settings.");
                        instance = CreateInstance<GameSettings>();
                    }
                }
                return instance;
            }
        }
    }
}
