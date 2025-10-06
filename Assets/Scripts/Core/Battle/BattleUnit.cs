using System;
using System.Collections.Generic;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Core.Battle
{
    /// <summary>
    /// Battle unit (creature stack) in combat.
    /// Based on VCMI's battle::Unit and CUnitState.
    /// Represents a stack of creatures fighting in battle.
    /// </summary>
    [Serializable]
    public class BattleUnit
    {
        // ===== Identity =====
        public int UnitId { get; set; }
        public CreatureData CreatureType { get; private set; }
        public BattleSide Side { get; private set; }
        public int SlotIndex { get; private set; }  // Original army slot (0-6)

        // ===== Position =====
        public BattleHex Position { get; set; }
        public bool IsDoubleWide => CreatureType != null && CreatureType.isDoubleWide;

        // ===== Health & Stack Count =====
        public int Count { get; private set; }           // Number of creatures alive
        public int FirstUnitHP { get; private set; }     // HP of first (damaged) creature
        public int MaxHealth => CreatureType?.hitPoints ?? 1;
        public int TotalHealth => (Count - 1) * MaxHealth + FirstUnitHP;
        public bool IsAlive => Count > 0;

        // ===== Base Stats (from CreatureData) =====
        public int BaseAttack => CreatureType?.attack ?? 0;
        public int BaseDefense => CreatureType?.defense ?? 0;
        public int MinDamage => CreatureType?.minDamage ?? 1;
        public int MaxDamage => CreatureType?.maxDamage ?? 1;
        public int BaseSpeed => CreatureType?.speed ?? 5;

        // ===== Effective Stats (with buffs/debuffs) =====
        public int Attack => GetEffectiveStat(StatType.Attack);
        public int Defense => GetEffectiveStat(StatType.Defense);
        public int Speed => GetEffectiveStat(StatType.Speed);
        public int Initiative => Speed;  // Base initiative = speed

        // ===== Combat State (reset each turn) =====
        public bool HasMoved { get; set; }
        public bool HasRetaliatedThisTurn { get; set; }
        public bool IsDefending { get; set; }
        public bool HasWaited { get; set; }
        public bool HadMoraleThisTurn { get; set; }

        // ===== Ranged Combat =====
        public int ShotsRemaining { get; private set; }
        public bool CanShoot => CreatureType != null && CreatureType.shots > 0 && ShotsRemaining > 0;
        public bool IsRanged => CreatureType != null && CreatureType.shots > 0;

        // ===== Retaliation =====
        public int RetaliationsRemaining { get; private set; }
        public bool CanRetaliate => RetaliationsRemaining > 0 && !HasRetaliatedThisTurn;

        // ===== Special Abilities =====
        public bool IsFlying => CreatureType != null && CreatureType.isFlying;
        public bool HasDoubleAttack => CreatureType != null && CreatureType.isDoubleAttack;
        public bool CanShootInMelee => CreatureType != null && CreatureType.canShootInMelee;
        public bool NoMeleeRetaliation => CreatureType != null && CreatureType.noMeleeRetal;

        // ===== Status Effects =====
        private readonly List<StatusEffect> activeEffects = new();

        // ===== Constructor =====
        public BattleUnit(int unitId, CreatureData creatureType, int count, BattleSide side, int slotIndex, BattleHex position)
        {
            UnitId = unitId;
            CreatureType = creatureType ?? throw new ArgumentNullException(nameof(creatureType));
            Count = count;
            Side = side;
            SlotIndex = slotIndex;
            Position = position;

            // Initialize health
            FirstUnitHP = MaxHealth;

            // Initialize combat resources
            ShotsRemaining = creatureType.shots;
            RetaliationsRemaining = 1; // HOMM3: 1 retaliation per turn by default
        }

        // ===== Damage & Healing =====

        /// <summary>
        /// Apply damage to this unit stack.
        /// Kills creatures from the back of the stack.
        /// Based on VCMI's damage calculation.
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (damage <= 0 || !IsAlive)
                return;

            var remaining = damage;

            // Damage first (partially damaged) creature
            if (FirstUnitHP <= remaining)
            {
                remaining -= FirstUnitHP;
                Count--;
                FirstUnitHP = MaxHealth;
            }
            else
            {
                FirstUnitHP -= remaining;
                return;
            }

            // Kill complete creatures
            while (remaining >= MaxHealth && Count > 0)
            {
                remaining -= MaxHealth;
                Count--;
            }

            // Apply remaining damage to next creature
            if (Count > 0 && remaining > 0)
            {
                FirstUnitHP -= remaining;
                if (FirstUnitHP <= 0)
                {
                    Count--;
                    FirstUnitHP = MaxHealth;
                }
            }

            // Ensure count doesn't go below 0
            if (Count < 0)
                Count = 0;
        }

        /// <summary>
        /// Heal damage to this stack.
        /// Heals from the first (damaged) creature.
        /// </summary>
        public void Heal(int healAmount)
        {
            if (healAmount <= 0 || !IsAlive)
                return;

            FirstUnitHP += healAmount;

            // Don't overheal beyond max HP
            if (FirstUnitHP > MaxHealth)
                FirstUnitHP = MaxHealth;
        }

        /// <summary>
        /// Resurrect dead creatures in stack.
        /// </summary>
        public void Resurrect(int healthToRestore, int maxCount)
        {
            if (healthToRestore <= 0)
                return;

            var creaturesRestored = healthToRestore / MaxHealth;
            creaturesRestored = Math.Min(creaturesRestored, maxCount - Count);

            Count += creaturesRestored;
            var remainingHealth = healthToRestore % MaxHealth;

            if (remainingHealth > 0 && Count > 0)
            {
                FirstUnitHP = Math.Min(FirstUnitHP + remainingHealth, MaxHealth);
            }
        }

        // ===== Turn Management =====

        /// <summary>
        /// Reset turn-specific flags at start of unit's turn.
        /// </summary>
        public void StartTurn()
        {
            HasMoved = false;
            HasRetaliatedThisTurn = false;
            IsDefending = false;
            HadMoraleThisTurn = false;
            RetaliationsRemaining = 1;  // Reset retaliation counter
        }

        /// <summary>
        /// Mark turn as ended (unit has acted).
        /// </summary>
        public void EndTurn()
        {
            HasMoved = true;
        }

        /// <summary>
        /// Use one shot (for ranged attacks).
        /// </summary>
        public void UseShot()
        {
            if (ShotsRemaining > 0)
                ShotsRemaining--;
        }

        /// <summary>
        /// Use retaliation.
        /// </summary>
        public void UseRetaliation()
        {
            if (RetaliationsRemaining > 0)
                RetaliationsRemaining--;
            HasRetaliatedThisTurn = true;
        }

        // ===== Status Effects & Buffs =====

        public enum StatType
        {
            Attack,
            Defense,
            Speed
        }

        /// <summary>
        /// Get effective stat value including buffs/debuffs.
        /// </summary>
        private int GetEffectiveStat(StatType statType)
        {
            var baseStat = statType switch
            {
                StatType.Attack => BaseAttack,
                StatType.Defense => BaseDefense,
                StatType.Speed => BaseSpeed,
                _ => 0
            };

            var total = baseStat;

            // Apply status effects
            foreach (var effect in activeEffects)
            {
                total += effect.GetStatModifier(statType);
            }

            // Defense bonus when defending
            if (statType == StatType.Defense && IsDefending)
            {
                total += baseStat / 2;  // +50% defense when defending
            }

            return Math.Max(0, total);  // Stats can't go below 0
        }

        /// <summary>
        /// Add a status effect to this unit.
        /// </summary>
        public void AddStatusEffect(StatusEffect effect)
        {
            if (effect != null)
                activeEffects.Add(effect);
        }

        /// <summary>
        /// Remove a status effect.
        /// </summary>
        public void RemoveStatusEffect(StatusEffect effect)
        {
            activeEffects.Remove(effect);
        }

        /// <summary>
        /// Clear all status effects.
        /// </summary>
        public void ClearStatusEffects()
        {
            activeEffects.Clear();
        }

        /// <summary>
        /// Update status effects (called each round).
        /// </summary>
        public void UpdateStatusEffects()
        {
            for (var i = activeEffects.Count - 1; i >= 0; i--)
            {
                activeEffects[i].DecrementDuration();
                if (activeEffects[i].IsExpired)
                {
                    activeEffects.RemoveAt(i);
                }
            }
        }

        // ===== Query Methods =====

        /// <summary>
        /// Check if this unit can act this turn.
        /// </summary>
        public bool CanAct()
        {
            return IsAlive && !HasMoved;
        }

        /// <summary>
        /// Get movement range in hexes.
        /// </summary>
        public int GetMovementRange()
        {
            // VCMI: Speed determines how many hexes unit can move
            // Flying units can move freely, ground units navigate around obstacles
            return Speed;
        }

        public override string ToString()
        {
            var name = CreatureType?.creatureName ?? "Unknown";
            return $"{Count} {name} (HP: {TotalHealth}/{Count * MaxHealth}) [Hex {Position.Value}]";
        }
    }

    /// <summary>
    /// Battle side enum.
    /// </summary>
    public enum BattleSide
    {
        Attacker = 0,
        Defender = 1
    }

    /// <summary>
    /// Status effect (buff/debuff) applied to a battle unit.
    /// Placeholder for future spell/ability effects.
    /// </summary>
    [Serializable]
    public class StatusEffect
    {
        public string EffectName { get; set; }
        public int Duration { get; private set; }  // Rounds remaining
        public bool IsExpired => Duration <= 0;

        private int attackModifier;
        private int defenseModifier;
        private int speedModifier;

        public StatusEffect(string name, int duration, int attack = 0, int defense = 0, int speed = 0)
        {
            EffectName = name;
            Duration = duration;
            attackModifier = attack;
            defenseModifier = defense;
            speedModifier = speed;
        }

        public void DecrementDuration()
        {
            if (Duration > 0)
                Duration--;
        }

        public int GetStatModifier(BattleUnit.StatType statType)
        {
            return statType switch
            {
                BattleUnit.StatType.Attack => attackModifier,
                BattleUnit.StatType.Defense => defenseModifier,
                BattleUnit.StatType.Speed => speedModifier,
                _ => 0
            };
        }
    }
}
