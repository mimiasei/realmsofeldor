using System;
using UnityEngine;


namespace RealmsOfEldor.Core.Events
{
    /// <summary>
    /// ScriptableObject-based event channel for game-wide events
    /// Provides decoupled communication between systems
    /// Based on VCMI observer pattern adapted for Unity
    /// </summary>
    [CreateAssetMenu(fileName = "GameEventChannel", menuName = "Realms of Eldor/Event Channels/Game Events")]
    public class GameEventChannel : ScriptableObject
    {
        #region Game Lifecycle Events

        /// <summary>
        /// Raised when a new game is started
        /// </summary>
        public event Action OnGameStarted;

        public void RaiseGameStarted()
        {
            OnGameStarted?.Invoke();
        }

        /// <summary>
        /// Raised when a game is loaded
        /// </summary>
        public event Action OnGameLoaded;

        public void RaiseGameLoaded()
        {
            OnGameLoaded?.Invoke();
        }

        /// <summary>
        /// Raised when game ends (victory/defeat)
        /// </summary>
        public event Action<int> OnGameEnded; // winnerId

        public void RaiseGameEnded(int winnerId)
        {
            OnGameEnded?.Invoke(winnerId);
        }

        #endregion

        #region Turn Events

        /// <summary>
        /// Raised when a new day starts
        /// </summary>
        public event Action<int> OnDayAdvanced; // day number

        public void RaiseDayAdvanced(int day)
        {
            OnDayAdvanced?.Invoke(day);
        }

        /// <summary>
        /// Raised when turn changes to another player
        /// </summary>
        public event Action<int> OnTurnChanged; // player ID

        public void RaiseTurnChanged(int playerId)
        {
            OnTurnChanged?.Invoke(playerId);
        }

        #endregion

        #region Hero Events

        /// <summary>
        /// Raised when a hero is created
        /// </summary>
        public event Action<int> OnHeroCreated; // hero ID

        public void RaiseHeroCreated(int heroId)
        {
            OnHeroCreated?.Invoke(heroId);
        }

        /// <summary>
        /// Raised when a hero moves
        /// </summary>
        public event Action<int, Position> OnHeroMoved; // hero ID, new position

        public void RaiseHeroMoved(int heroId, Position position)
        {
            OnHeroMoved?.Invoke(heroId, position);
        }

        /// <summary>
        /// Raised when a hero levels up
        /// </summary>
        public event Action<int, int> OnHeroLeveledUp; // hero ID, new level

        public void RaiseHeroLeveledUp(int heroId, int newLevel)
        {
            OnHeroLeveledUp?.Invoke(heroId, newLevel);
        }

        /// <summary>
        /// Raised when a hero is defeated/removed
        /// </summary>
        public event Action<int> OnHeroDefeated; // hero ID

        public void RaiseHeroDefeated(int heroId)
        {
            OnHeroDefeated?.Invoke(heroId);
        }

        #endregion

        #region Resource Events

        /// <summary>
        /// Raised when a player's resources change
        /// </summary>
        public event Action<int, ResourceSet> OnResourcesChanged; // player ID, new resources

        public void RaiseResourcesChanged(int playerId, ResourceSet resources)
        {
            OnResourcesChanged?.Invoke(playerId, resources);
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Clear all event subscriptions (useful for scene transitions)
        /// </summary>
        public void ClearAllListeners()
        {
            OnGameStarted = null;
            OnGameLoaded = null;
            OnGameEnded = null;
            OnDayAdvanced = null;
            OnTurnChanged = null;
            OnHeroCreated = null;
            OnHeroMoved = null;
            OnHeroLeveledUp = null;
            OnHeroDefeated = null;
            OnResourcesChanged = null;
        }

        #endregion
    }
}
