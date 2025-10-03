using UnityEngine;
using RealmsOfEldor.Core;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Unity MonoBehaviour wrapper for GameState
    /// Provides singleton access and Unity lifecycle integration
    /// Based on hybrid architecture pattern from VCMI research
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        // The actual game state (plain C# class)
        private GameState gameState = new GameState();

        // Event channels (will be assigned in Inspector)
        [Header("Event Channels")]
        [SerializeField] private GameEventChannel gameEvents;

        // Public access to game state
        public GameState State => gameState;

        #region Unity Lifecycle

        void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log("GameStateManager initialized");
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region Game Management

        /// <summary>
        /// Start a new game
        /// </summary>
        public void StartNewGame(int playerCount, bool[] isHuman, string gameName = "New Game")
        {
            gameState = new GameState
            {
                GameName = gameName
            };
            gameState.Initialize(playerCount, isHuman);

            Debug.Log($"Started new game: {gameName} with {playerCount} players");

            // Raise event
            if (gameEvents != null)
            {
                gameEvents.RaiseGameStarted();
            }
        }

        /// <summary>
        /// End current player's turn and advance to next
        /// </summary>
        public void EndTurn()
        {
            var currentPlayer = gameState.GetCurrentPlayer();
            var currentDay = gameState.CurrentDay;

            gameState.EndTurn();

            var newPlayer = gameState.GetCurrentPlayer();
            var newDay = gameState.CurrentDay;

            Debug.Log($"Turn ended. Now: Day {newDay}, Player {newPlayer?.Id}");

            // Raise events
            if (gameEvents != null)
            {
                if (newDay > currentDay)
                {
                    gameEvents.RaiseDayAdvanced(newDay);
                }
                gameEvents.RaiseTurnChanged(newPlayer?.Id ?? 0);
            }

            // Check for game over
            if (gameState.IsGameOver())
            {
                var winner = gameState.GetWinner();
                Debug.Log($"Game Over! Winner: Player {winner?.Id}");

                if (gameEvents != null)
                {
                    gameEvents.RaiseGameEnded(winner?.Id ?? -1);
                }
            }
        }

        #endregion

        #region Hero Management

        /// <summary>
        /// Create a new hero
        /// </summary>
        public Hero CreateHero(int heroTypeId, int ownerId, Position position)
        {
            var hero = gameState.AddHero(heroTypeId, ownerId, position);

            Debug.Log($"Created hero {hero.Id} for player {ownerId} at {position}");

            if (gameEvents != null)
            {
                gameEvents.RaiseHeroCreated(hero.Id);
            }

            return hero;
        }

        /// <summary>
        /// Move a hero
        /// </summary>
        public bool MoveHero(int heroId, Position newPosition, int movementCost)
        {
            var hero = gameState.GetHero(heroId);
            if (hero == null)
                return false;

            if (!hero.MoveTo(newPosition, movementCost))
                return false;

            Debug.Log($"Hero {heroId} moved to {newPosition}");

            if (gameEvents != null)
            {
                gameEvents.RaiseHeroMoved(heroId, newPosition);
            }

            return true;
        }

        /// <summary>
        /// Hero gains experience
        /// </summary>
        public void HeroGainExperience(int heroId, int experience)
        {
            var hero = gameState.GetHero(heroId);
            if (hero == null)
                return;

            int oldLevel = hero.Level;
            hero.GainExperience(experience);
            int newLevel = hero.Level;

            Debug.Log($"Hero {heroId} gained {experience} exp");

            if (newLevel > oldLevel)
            {
                Debug.Log($"Hero {heroId} leveled up to {newLevel}!");

                if (gameEvents != null)
                {
                    gameEvents.RaiseHeroLeveledUp(heroId, newLevel);
                }
            }
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// Save game to JSON
        /// </summary>
        public void SaveGame(string filename)
        {
            try
            {
                string json = JsonUtility.ToJson(gameState, true);
                string path = System.IO.Path.Combine(Application.persistentDataPath, filename);
                System.IO.File.WriteAllText(path, json);

                Debug.Log($"Game saved to {path}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save game: {e.Message}");
            }
        }

        /// <summary>
        /// Load game from JSON
        /// </summary>
        public bool LoadGame(string filename)
        {
            try
            {
                string path = System.IO.Path.Combine(Application.persistentDataPath, filename);
                if (!System.IO.File.Exists(path))
                {
                    Debug.LogError($"Save file not found: {path}");
                    return false;
                }

                string json = System.IO.File.ReadAllText(path);
                gameState = JsonUtility.FromJson<GameState>(json);

                Debug.Log($"Game loaded from {path}");

                if (gameEvents != null)
                {
                    gameEvents.RaiseGameLoaded();
                }

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load game: {e.Message}");
                return false;
            }
        }

        #endregion

        #region Debug Helpers

        /// <summary>
        /// Get game state summary for debugging
        /// </summary>
        public string GetStateSummary()
        {
            return gameState.GetStateSummary();
        }

        #endregion
    }
}
