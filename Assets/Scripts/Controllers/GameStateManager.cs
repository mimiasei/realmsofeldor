using UnityEngine;
using Newtonsoft.Json;
using Cysharp.Threading.Tasks;
using System.Threading;
using RealmsOfEldor.Core;
using RealmsOfEldor.Core.Events;

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
                Debug.Log($"🔔 Raising OnHeroCreated event for heroId={hero.Id}");
                gameEvents.RaiseHeroCreated(hero.Id);
            }
            else
            {
                Debug.LogError("⚠️ GameStateManager: gameEvents is NULL! Cannot raise OnHeroCreated.");
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
        /// Save game to JSON using Newtonsoft.Json
        /// Supports dictionaries, complex types, and better serialization than Unity's JsonUtility
        /// </summary>
        public void SaveGame(string filename)
        {
            try
            {
                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    TypeNameHandling = TypeNameHandling.Auto,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                };

                string json = JsonConvert.SerializeObject(gameState, settings);
                string path = System.IO.Path.Combine(Application.persistentDataPath, filename);
                System.IO.File.WriteAllText(path, json);

                Debug.Log($"Game saved to {path} ({json.Length} bytes)");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save game: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// Load game from JSON using Newtonsoft.Json
        /// Supports dictionaries, complex types, and better deserialization than Unity's JsonUtility
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

                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };

                gameState = JsonConvert.DeserializeObject<GameState>(json, settings);

                if (gameState == null)
                {
                    Debug.LogError("Failed to deserialize game state - result was null");
                    return false;
                }

                Debug.Log($"Game loaded from {path}: {gameState.GameName}, Day {gameState.CurrentDay}");

                if (gameEvents != null)
                {
                    gameEvents.RaiseGameLoaded();
                }

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load game: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Save game to JSON asynchronously using UniTask
        /// Non-blocking I/O operation for better performance
        /// </summary>
        public async UniTask SaveGameAsync(string filename, CancellationToken cancellationToken = default)
        {
            try
            {
                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    TypeNameHandling = TypeNameHandling.Auto,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                };

                // Serialize on background thread
                var json = await UniTask.RunOnThreadPool(() =>
                    JsonConvert.SerializeObject(gameState, settings),
                    cancellationToken: cancellationToken);

                string path = System.IO.Path.Combine(Application.persistentDataPath, filename);

                // Write to file asynchronously
                await UniTask.SwitchToThreadPool();
                await System.IO.File.WriteAllTextAsync(path, json, cancellationToken);
                await UniTask.SwitchToMainThread();

                Debug.Log($"Game saved async to {path} ({json.Length} bytes)");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save game async: {e.Message}\n{e.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Load game from JSON asynchronously using UniTask
        /// Non-blocking I/O operation for better performance
        /// </summary>
        public async UniTask<bool> LoadGameAsync(string filename, CancellationToken cancellationToken = default)
        {
            try
            {
                string path = System.IO.Path.Combine(Application.persistentDataPath, filename);
                if (!System.IO.File.Exists(path))
                {
                    Debug.LogError($"Save file not found: {path}");
                    return false;
                }

                // Read file asynchronously
                await UniTask.SwitchToThreadPool();
                var json = await System.IO.File.ReadAllTextAsync(path, cancellationToken);
                await UniTask.SwitchToMainThread();

                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };

                // Deserialize on background thread
                gameState = await UniTask.RunOnThreadPool(() =>
                    JsonConvert.DeserializeObject<GameState>(json, settings),
                    cancellationToken: cancellationToken);

                if (gameState == null)
                {
                    Debug.LogError("Failed to deserialize game state - result was null");
                    return false;
                }

                Debug.Log($"Game loaded async from {path}: {gameState.GameName}, Day {gameState.CurrentDay}");

                if (gameEvents != null)
                {
                    gameEvents.RaiseGameLoaded();
                }

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load game async: {e.Message}\n{e.StackTrace}");
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
