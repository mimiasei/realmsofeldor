using UnityEngine;
using System.Collections.Generic;
using RealmsOfEldor.Core;
using RealmsOfEldor.Core.Events;
using RealmsOfEldor.Utilities;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Spawns and manages hero visual representations on the adventure map.
    /// Listens to hero creation events and instantiates HeroController prefabs.
    /// </summary>
    public class HeroSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject heroPrefab;

        [Header("Settings")]
        [SerializeField] private Transform heroContainer;
        [SerializeField] private Color[] playerColors = {
            Color.red,      // Player 0
            Color.blue,     // Player 1
            Color.green,    // Player 2
            Color.yellow,   // Player 3
            Color.cyan,     // Player 4
            Color.magenta,  // Player 5
            Color.white,    // Player 6
            Color.gray      // Player 7
        };

        [Header("Event Channels")]
        [SerializeField] private GameEventChannel gameEvents;

        private Dictionary<int, HeroController> spawnedHeroes = new Dictionary<int, HeroController>();

        void Awake()
        {
            if (heroContainer == null)
            {
                // Create container for heroes
                var container = new GameObject("Heroes");
                heroContainer = container.transform;
                heroContainer.SetParent(transform);
            }
        }

        void OnEnable()
        {
            if (gameEvents != null)
            {
                gameEvents.OnHeroCreated += HandleHeroCreated;
                gameEvents.OnHeroDefeated += HandleHeroDefeated;
                Debug.Log("✓ HeroSpawner subscribed to OnHeroCreated event");
            }
            else
            {
                Debug.LogError("⚠️ HeroSpawner: gameEvents is NULL! Cannot subscribe to events.");
            }
        }

        void OnDisable()
        {
            if (gameEvents != null)
            {
                gameEvents.OnHeroCreated -= HandleHeroCreated;
                gameEvents.OnHeroDefeated -= HandleHeroDefeated;
            }
        }

        /// <summary>
        /// Spawns a hero visual when hero is created in game state.
        /// </summary>
        private void HandleHeroCreated(int heroId)
        {
            Debug.Log($"🎯 HeroSpawner.HandleHeroCreated called for heroId={heroId}");

            // Get hero from GameState
            var gameState = GameStateManager.Instance?.State;
            if (gameState == null)
            {
                Debug.LogError("Cannot spawn hero: GameState is null!");
                return;
            }

            var hero = gameState.GetHero(heroId);
            if (hero == null)
            {
                Debug.LogError($"Cannot spawn hero: hero {heroId} not found in GameState!");
                return;
            }

            if (spawnedHeroes.ContainsKey(hero.Id))
            {
                Debug.LogWarning($"Hero {hero.Id} already spawned!");
                return;
            }

            SpawnHero(hero);
        }

        /// <summary>
        /// Spawns a hero GameObject at the specified position.
        /// </summary>
        private void SpawnHero(Hero hero)
        {
            if (heroPrefab == null)
            {
                Debug.LogError("Cannot spawn hero: heroPrefab is not assigned!");
                return;
            }

            // Convert tile position to 3D world position (X,Z ground plane)
            // In 3D: X = horizontal left/right, Y = height above ground, Z = horizontal forward/back
            var worldPosition = new Vector3(
                hero.Position.X + 0.5f,  // Center on tile X
                0.5f,                     // Slightly above ground (so billboard is visible)
                hero.Position.Y + 0.5f   // Center on tile Z (map Y becomes world Z)
            );

            // Instantiate hero prefab
            var heroGO = Instantiate(heroPrefab, worldPosition, Quaternion.identity, heroContainer);
            heroGO.name = $"Hero_{hero.Id}_{hero.CustomName}";

            // Get HeroController component
            var heroController = heroGO.GetComponent<HeroController>();
            if (heroController == null)
            {
                Debug.LogError($"Hero prefab is missing HeroController component!");
                Destroy(heroGO);
                return;
            }

            // Initialize hero controller
            heroController.Initialize(hero, worldPosition);

            // Set player color
            var playerColor = GetPlayerColor(hero.Owner);
            heroController.SetPlayerColor(playerColor);

            // Track spawned hero
            spawnedHeroes[hero.Id] = heroController;

            Debug.Log($"✓ Spawned hero visual: {hero.CustomName} at {worldPosition}");
        }

        /// <summary>
        /// Handles hero defeat by removing visual representation.
        /// </summary>
        private void HandleHeroDefeated(int heroId)
        {
            if (spawnedHeroes.TryGetValue(heroId, out var heroController))
            {
                spawnedHeroes.Remove(heroId);
                // HeroController handles its own defeat animation and destruction
                Debug.Log($"Hero {heroId} defeated, visual will be removed");
            }
        }

        /// <summary>
        /// Gets the color for a specific player.
        /// </summary>
        private Color GetPlayerColor(int playerId)
        {
            if (playerId >= 0 && playerId < playerColors.Length)
                return playerColors[playerId];

            return Color.white; // Default
        }

        /// <summary>
        /// Get hero controller for a specific hero ID.
        /// </summary>
        public HeroController GetHeroController(int heroId)
        {
            if (spawnedHeroes.TryGetValue(heroId, out var controller))
            {
                // Clean up if controller was destroyed
                if (controller == null)
                {
                    spawnedHeroes.Remove(heroId);
                    return null;
                }
                return controller;
            }
            return null;
        }

        /// <summary>
        /// Get all spawned hero controllers.
        /// </summary>
        public IEnumerable<HeroController> GetAllHeroControllers()
        {
            // Clean up any destroyed controllers
            var destroyedKeys = new List<int>();
            foreach (var kvp in spawnedHeroes)
            {
                if (kvp.Value == null)
                    destroyedKeys.Add(kvp.Key);
            }

            foreach (var key in destroyedKeys)
                spawnedHeroes.Remove(key);

            return spawnedHeroes.Values;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // Ensure we have enough colors for 8 players
            if (playerColors == null || playerColors.Length < 8)
            {
                playerColors = new Color[8] {
                    Color.red, Color.blue, Color.green, Color.yellow,
                    Color.cyan, Color.magenta, Color.white, Color.gray
                };
            }
        }
#endif
    }
}
