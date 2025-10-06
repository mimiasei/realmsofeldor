using System;
using UnityEngine;


namespace RealmsOfEldor.Data
{
    /// <summary>
    /// ScriptableObject-based event channel for battle events
    /// Decouples battle logic from UI/visual feedback
    /// Based on VCMI's IBattleEventsReceiver pattern
    /// </summary>
    [CreateAssetMenu(fileName = "BattleEventChannel", menuName = "Realms of Eldor/Event Channels/Battle Events")]
    public class BattleEventChannel : ScriptableObject
    {
        #region Battle Lifecycle Events

        /// <summary>
        /// Raised when battle starts
        /// </summary>
        public event Action OnBattleStarted;

        public void RaiseBattleStarted()
        {
            OnBattleStarted?.Invoke();
        }

        /// <summary>
        /// Raised when battle ends
        /// </summary>
        public event Action<bool> OnBattleEnded; // attackerWon

        public void RaiseBattleEnded(bool attackerWon)
        {
            OnBattleEnded?.Invoke(attackerWon);
        }

        /// <summary>
        /// Raised when a new round starts
        /// </summary>
        public event Action<int> OnRoundStarted; // round number

        public void RaiseRoundStarted(int round)
        {
            OnRoundStarted?.Invoke(round);
        }

        #endregion

        #region Combat Events

        /// <summary>
        /// Raised when a unit attacks another
        /// </summary>
        public event Action<int, int> OnUnitAttacking; // attacker ID, target ID

        public void RaiseUnitAttacking(int attackerId, int targetId)
        {
            OnUnitAttacking?.Invoke(attackerId, targetId);
        }

        /// <summary>
        /// Raised when damage is dealt
        /// </summary>
        public event Action<int, int, int> OnDamageDealt; // attacker ID, target ID, damage

        public void RaiseDamageDealt(int attackerId, int targetId, int damage)
        {
            OnDamageDealt?.Invoke(attackerId, targetId, damage);
        }

        /// <summary>
        /// Raised when a unit dies
        /// </summary>
        public event Action<int> OnUnitDied; // unit ID

        public void RaiseUnitDied(int unitId)
        {
            OnUnitDied?.Invoke(unitId);
        }

        /// <summary>
        /// Raised when a unit moves
        /// </summary>
        public event Action<int, Position> OnUnitMoved; // unit ID, new position

        public void RaiseUnitMoved(int unitId, Position position)
        {
            OnUnitMoved?.Invoke(unitId, position);
        }

        #endregion

        #region Spell Events

        /// <summary>
        /// Raised when a spell is cast
        /// </summary>
        public event Action<int, int> OnSpellCast; // caster hero ID, spell ID

        public void RaiseSpellCast(int casterId, int spellId)
        {
            OnSpellCast?.Invoke(casterId, spellId);
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Clear all event subscriptions
        /// </summary>
        public void ClearAllListeners()
        {
            OnBattleStarted = null;
            OnBattleEnded = null;
            OnRoundStarted = null;
            OnUnitAttacking = null;
            OnDamageDealt = null;
            OnUnitDied = null;
            OnUnitMoved = null;
            OnSpellCast = null;
        }

        #endregion
    }
}
