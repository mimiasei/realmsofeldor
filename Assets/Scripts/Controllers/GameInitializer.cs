using UnityEngine;
using System.Linq;
using RealmsOfEldor.Core;
using RealmsOfEldor.Core.Events;
using RealmsOfEldor.Database;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Initializes the game state with default players, heroes, and resources
    /// Attach to a GameObject in the Adventure Map scene
    /// </summary>
    public class GameInitializer : MonoBehaviour
    {
        [Header("Initialization Settings")]
        [SerializeField] private bool initializeOnStart = true;
        [SerializeField] private int numberOfPlayers = 2;
        [SerializeField] private int startingGold = 10000;
        [SerializeField] private int startingWood = 20;
        [SerializeField] private int startingOre = 20;

        [Header("Hero Settings")]
        [SerializeField] private bool createStartingHeroes = true;
        [SerializeField] private Position player1HeroPosition = new Position(5, 5);
        [SerializeField] private Position player2HeroPosition = new Position(25, 25);

        [Header("Event Channels")]
        [SerializeField] private GameEventChannel gameEvents;

        [Header("Optional References")]
        [SerializeField] private HeroDatabase heroDatabase;

        private void Awake()
        {
            // Find HeroDatabase if not assigned
            if (heroDatabase == null && createStartingHeroes)
            {
                heroDatabase = FindFirstObjectByType<HeroDatabase>();
                if (heroDatabase == null)
                {
                    Debug.LogWarning("⚠️ HeroDatabase not found in scene. Heroes will not be created.");
                }
            }
        }

        private void Start()
        {
            if (initializeOnStart)
            {
                InitializeGame();
            }
        }

        /// <summary>
        /// Initializes the game state with players, resources, and heroes
        /// Can be called from Unity Editor or at runtime
        /// </summary>
        [ContextMenu("Initialize Game")]
        public void InitializeGame()
        {
            // Ensure GameStateManager exists
            if (GameStateManager.Instance == null)
            {
                var gameStateObj = new GameObject("GameStateManager");
                var gsm = gameStateObj.AddComponent<GameStateManager>();

                // Wire up event channel using reflection
                if (gameEvents != null)
                {
                    var field = typeof(GameStateManager).GetField("gameEvents",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    field?.SetValue(gsm, gameEvents);
                    Debug.Log("✓ Created GameStateManager singleton with gameEvents wired");
                }
                else
                {
                    Debug.LogWarning("⚠️ Created GameStateManager but gameEvents not assigned to GameInitializer!");
                }
            }

            var gameState = GameStateManager.Instance.State;

            Debug.Log("🎮 Initializing game state...");

            // Initialize game state with players
            var isHuman = new bool[numberOfPlayers];
            for (var i = 0; i < numberOfPlayers; i++)
            {
                isHuman[i] = true; // All players are human by default
            }

            gameState.Initialize(numberOfPlayers, isHuman);

            // Set starting resources for all players
            for (var i = 0; i < numberOfPlayers; i++)
            {
                var player = gameState.GetPlayer(i);
                if (player != null)
                {
                    player.Resources = new ResourceSet(
                        gold: startingGold,
                        wood: startingWood,
                        ore: startingOre,
                        mercury: 5,
                        sulfur: 5,
                        crystal: 5,
                        gems: 5
                    );
                    Debug.Log($"✓ Created Player {i} ({(PlayerColor)i}) with {startingGold} gold");
                }
            }

            // Create starting heroes if enabled
            if (createStartingHeroes)
            {
                CreateStartingHeroes(gameState);
            }

            Debug.Log($"✅ Game initialized: {numberOfPlayers} players, day {gameState.CurrentDay}");

            // Force movement refresh after initialization
            if (createStartingHeroes)
            {
                foreach (var heroId in gameState.GetPlayer(0)?.HeroIds ?? System.Linq.Enumerable.Empty<int>())
                {
                    var hero = gameState.GetHero(heroId);
                    if (hero != null)
                    {
                        hero.MaxMovement = 2000;
                        hero.Movement = 2000;
                    }
                }
                if (numberOfPlayers >= 2)
                {
                    foreach (var heroId in gameState.GetPlayer(1)?.HeroIds ?? System.Linq.Enumerable.Empty<int>())
                    {
                        var hero = gameState.GetHero(heroId);
                        if (hero != null)
                        {
                            hero.MaxMovement = 2000;
                            hero.Movement = 2000;
                        }
                    }
                }
            }
        }

        private void CreateStartingHeroes(GameState gameState)
        {
            // Check if HeroDatabase exists
            if (heroDatabase == null)
            {
                Debug.LogWarning("⚠️ HeroDatabase not assigned or found in scene. Cannot create heroes.");
                return;
            }

            var heroTypes = heroDatabase.GetAllHeroTypes().ToList();
            if (heroTypes.Count == 0)
            {
                Debug.LogWarning("⚠️ No hero types found in HeroDatabase. Run 'Generate Sample Data' first.");
                return;
            }

            // Create hero for Player 0
            if (numberOfPlayers >= 1)
            {
                var heroType1 = heroTypes[0];
                var hero1 = GameStateManager.Instance.CreateHero(heroType1.heroTypeId, 0, player1HeroPosition);

                // Initialize hero from type data
                hero1.CustomName = $"{heroType1.heroName} (P1)";
                hero1.Initialize(heroType1);
                hero1.MaxMovement = 2000;
                hero1.Movement = 2000; // Set AFTER MaxMovement

                Debug.Log($"Hero {hero1.CustomName} initialized: Movement={hero1.Movement}, MaxMovement={hero1.MaxMovement}");

                Debug.Log($"✓ Created hero '{hero1.CustomName}' at {player1HeroPosition} for Player 0");
            }

            // Create hero for Player 1 (if 2+ players)
            if (numberOfPlayers >= 2 && heroTypes.Count >= 1)
            {
                var heroType2 = heroTypes.Count > 1 ? heroTypes[1] : heroTypes[0];
                var hero2 = GameStateManager.Instance.CreateHero(heroType2.heroTypeId, 1, player2HeroPosition);

                // Initialize hero from type data
                hero2.CustomName = $"{heroType2.heroName} (P2)";
                hero2.Initialize(heroType2);
                hero2.MaxMovement = 2000;
                hero2.Movement = 2000;

                Debug.Log($"Hero {hero2.CustomName} initialized: Movement={hero2.Movement}, MaxMovement={hero2.MaxMovement}");

                Debug.Log($"✓ Created hero '{hero2.CustomName}' at {player2HeroPosition} for Player 1");
            }
        }
    }
}
