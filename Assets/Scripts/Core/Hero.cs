using System;
using System.Collections.Generic;

namespace RealmsOfEldor.Core
{
    /// <summary>
    /// Hero runtime instance
    /// Based on VCMI's CGHeroInstance
    /// Plain C# class (no MonoBehaviour) - represents hero data/state
    /// </summary>
    [Serializable]
    public class Hero
    {
        // Identity
        public int Id { get; set; }
        public int TypeId { get; set; }  // Reference to HeroTypeData
        public string CustomName { get; set; }
        public int Owner { get; set; }   // Player ID

        // Position and movement
        public Position Position { get; set; }
        public int Movement { get; set; }
        public int MaxMovement { get; set; } = 1500;  // HOMM3 base movement

        // Experience and level
        public int Experience { get; set; }
        public int Level { get; set; } = 1;

        // Primary stats
        public int Attack { get; set; } = 1;
        public int Defense { get; set; } = 1;
        public int SpellPower { get; set; } = 1;
        public int Knowledge { get; set; } = 1;

        // Mana
        public int Mana { get; set; }
        public int MaxMana { get; set; } = 10;

        // Secondary skills (skill -> level)
        public Dictionary<SecondarySkillType, SkillLevel> SecondarySkills { get; set; }
            = new Dictionary<SecondarySkillType, SkillLevel>();

        // Army
        public Army Army { get; set; } = new Army();

        // Spellbook
        public HashSet<int> KnownSpells { get; set; } = new HashSet<int>();
        public bool HasSpellbook { get; set; } = false;

        // Artifacts (slot -> artifact ID)
        public Dictionary<ArtifactSlot, int> EquippedArtifacts { get; set; }
            = new Dictionary<ArtifactSlot, int>();
        public List<int> Backpack { get; set; } = new List<int>();

        // State flags
        public bool IsActive { get; set; } = true;

        #region Experience and Leveling

        /// <summary>
        /// Gain experience points
        /// </summary>
        public void GainExperience(int exp)
        {
            if (exp <= 0)
                return;

            Experience += exp;

            // Check for level ups
            while (Experience >= GetExperienceForNextLevel() && Level < 100)
            {
                LevelUp();
            }
        }

        /// <summary>
        /// Get experience required for next level
        /// HOMM3 formula: 1000 * current_level
        /// </summary>
        public int GetExperienceForNextLevel()
        {
            return 1000 * Level;
        }

        /// <summary>
        /// Level up the hero
        /// </summary>
        private void LevelUp()
        {
            Level++;

            // Increase primary stats
            // TODO: This should be based on hero class growth rates
            // For now, simple random increase
            var random = new Random();
            int stat = random.Next(4);
            switch (stat)
            {
                case 0: Attack++; break;
                case 1: Defense++; break;
                case 2: SpellPower++; break;
                case 3: Knowledge++; break;
            }

            // Recalculate max mana based on knowledge
            MaxMana = 10 * Knowledge;
            Mana = MaxMana;

            // TODO: Trigger event for secondary skill selection
        }

        #endregion

        #region Movement

        /// <summary>
        /// Refresh movement points (called at start of turn)
        /// </summary>
        public void RefreshMovement()
        {
            Movement = MaxMovement;
            // TODO: Apply bonuses from artifacts, skills, etc.
        }

        /// <summary>
        /// Check if hero can move (has movement points)
        /// </summary>
        public bool CanMove(int cost)
        {
            return Movement >= cost;
        }

        /// <summary>
        /// Spend movement points
        /// </summary>
        public void SpendMovement(int cost)
        {
            Movement = Math.Max(0, Movement - cost);
        }

        /// <summary>
        /// Move hero to new position
        /// </summary>
        public bool MoveTo(Position newPosition, int movementCost)
        {
            if (!CanMove(movementCost))
                return false;

            Position = newPosition;
            SpendMovement(movementCost);
            return true;
        }

        #endregion

        #region Mana

        /// <summary>
        /// Refresh mana (called daily)
        /// </summary>
        public void RefreshMana()
        {
            Mana = MaxMana;
        }

        /// <summary>
        /// Check if hero can cast spell
        /// </summary>
        public bool CanCastSpell(int manaCost)
        {
            return HasSpellbook && Mana >= manaCost;
        }

        /// <summary>
        /// Spend mana
        /// </summary>
        public bool SpendMana(int cost)
        {
            if (Mana < cost)
                return false;

            Mana -= cost;
            return true;
        }

        #endregion

        #region Secondary Skills

        /// <summary>
        /// Add or upgrade a secondary skill
        /// </summary>
        public void AddSecondarySkill(SecondarySkillType skill, SkillLevel level)
        {
            if (SecondarySkills.ContainsKey(skill))
            {
                // Upgrade existing skill
                if (level > SecondarySkills[skill])
                {
                    SecondarySkills[skill] = level;
                }
            }
            else
            {
                // Add new skill
                SecondarySkills[skill] = level;
            }
        }

        /// <summary>
        /// Get secondary skill level
        /// </summary>
        public SkillLevel GetSecondarySkillLevel(SecondarySkillType skill)
        {
            return SecondarySkills.TryGetValue(skill, out var level) ? level : SkillLevel.None;
        }

        /// <summary>
        /// Check if hero has a secondary skill
        /// </summary>
        public bool HasSecondarySkill(SecondarySkillType skill)
        {
            return SecondarySkills.ContainsKey(skill) && SecondarySkills[skill] > SkillLevel.None;
        }

        #endregion

        #region Spells

        /// <summary>
        /// Learn a spell
        /// </summary>
        public bool LearnSpell(int spellId)
        {
            if (!HasSpellbook)
                return false;

            KnownSpells.Add(spellId);
            return true;
        }

        /// <summary>
        /// Check if hero knows a spell
        /// </summary>
        public bool KnowsSpell(int spellId)
        {
            return HasSpellbook && KnownSpells.Contains(spellId);
        }

        #endregion

        #region Artifacts

        /// <summary>
        /// Equip an artifact
        /// </summary>
        public bool EquipArtifact(ArtifactSlot slot, int artifactId)
        {
            // TODO: Check if slot is valid for artifact type
            EquippedArtifacts[slot] = artifactId;
            return true;
        }

        /// <summary>
        /// Unequip artifact from slot
        /// </summary>
        public int? UnequipArtifact(ArtifactSlot slot)
        {
            if (EquippedArtifacts.TryGetValue(slot, out var artifactId))
            {
                EquippedArtifacts.Remove(slot);
                return artifactId;
            }
            return null;
        }

        /// <summary>
        /// Add artifact to backpack
        /// </summary>
        public void AddToBackpack(int artifactId)
        {
            Backpack.Add(artifactId);
        }

        #endregion

        #region Combat Stats

        /// <summary>
        /// Get total attack value (including bonuses from artifacts, skills, etc.)
        /// </summary>
        public int GetTotalAttack()
        {
            // TODO: Add bonuses from artifacts, skills, etc.
            return Attack;
        }

        /// <summary>
        /// Get total defense value
        /// </summary>
        public int GetTotalDefense()
        {
            // TODO: Add bonuses from artifacts, skills, etc.
            return Defense;
        }

        /// <summary>
        /// Get total spell power
        /// </summary>
        public int GetTotalSpellPower()
        {
            // TODO: Add bonuses from artifacts, skills, etc.
            return SpellPower;
        }

        #endregion

        public override string ToString()
        {
            return $"Hero {Id} (Lvl {Level}) - {Position}";
        }
    }
}
