using System;
using System.Collections.Generic;
using System.Linq;

namespace RealmsOfEldor.Core
{
    /// <summary>
    /// Creature stack in an army slot
    /// </summary>
    [Serializable]
    public class CreatureStack
    {
        public int CreatureId { get; set; }
        public int Count { get; set; }

        public CreatureStack()
        {
        }

        public CreatureStack(int creatureId, int count)
        {
            CreatureId = creatureId;
            Count = count;
        }

        public bool IsEmpty() => Count <= 0;
    }

    /// <summary>
    /// Army with 7 slots (HOMM3 pattern)
    /// Based on VCMI's TSlots
    /// </summary>
    [Serializable]
    public class Army
    {
        public const int MaxSlots = 7;

        private CreatureStack[] slots = new CreatureStack[MaxSlots];

        public Army()
        {
            for (var i = 0; i < MaxSlots; i++)
            {
                slots[i] = null;
            }
        }

        /// <summary>
        /// Get creature stack at slot index
        /// </summary>
        public CreatureStack GetSlot(int index)
        {
            return index is < 0 or >= MaxSlots ? null : slots[index];
        }

        /// <summary>
        /// Set creature stack at slot index
        /// </summary>
        public void SetSlot(int index, CreatureStack stack)
        {
            if (index is >= 0 and < MaxSlots)
            {
                slots[index] = stack;
            }
        }

        /// <summary>
        /// Add creatures to army - tries to merge with existing stack or find empty slot
        /// </summary>
        public bool AddCreatures(int creatureId, int count, int preferredSlot = -1)
        {
            if (count <= 0)
                return false;

            // Try preferred slot first if specified and empty
            if (preferredSlot is >= 0 and < MaxSlots && slots[preferredSlot] == null)
            {
                slots[preferredSlot] = new CreatureStack(creatureId, count);
                return true;
            }

            // Try to merge with existing stack of same creature
            for (var i = 0; i < MaxSlots; i++)
            {
                if (slots[i] == null || slots[i].CreatureId != creatureId) continue;
                slots[i].Count += count;
                return true;
            }

            // Find empty slot
            var emptySlot = FindEmptySlot();
            if (emptySlot < 0) return false; // Army is full
            slots[emptySlot] = new CreatureStack(creatureId, count);
            return true;

        }

        /// <summary>
        /// Remove creatures from a stack
        /// </summary>
        public bool RemoveCreatures(int slotIndex, int count)
        {
            if (slotIndex is < 0 or >= MaxSlots)
                return false;

            var stack = slots[slotIndex];
            if (stack == null || stack.Count < count)
                return false;

            stack.Count -= count;
            if (stack.Count <= 0)
            {
                slots[slotIndex] = null;
            }

            return true;
        }

        /// <summary>
        /// Find first empty slot
        /// </summary>
        public int FindEmptySlot()
        {
            for (var i = 0; i < MaxSlots; i++)
            {
                if (slots[i] == null || slots[i].IsEmpty())
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Get all non-empty stacks
        /// </summary>
        public IEnumerable<CreatureStack> GetStacks()
        {
            return slots.Where(s => s != null && !s.IsEmpty());
        }

        /// <summary>
        /// Get all stacks with their slot indices
        /// </summary>
        public IEnumerable<(int SlotIndex, CreatureStack Stack)> GetStacksWithIndices()
        {
            for (var i = 0; i < MaxSlots; i++)
            {
                if (slots[i] != null && !slots[i].IsEmpty())
                {
                    yield return (i, slots[i]);
                }
            }
        }

        /// <summary>
        /// Check if army is empty
        /// </summary>
        public bool IsEmpty()
        {
            return !GetStacks().Any();
        }

        /// <summary>
        /// Get total creature count across all stacks
        /// </summary>
        public int GetTotalCount()
        {
            return GetStacks().Sum(s => s.Count);
        }

        /// <summary>
        /// Get total army strength (requires creature database access)
        /// This is a placeholder - actual implementation will need CreatureDatabase
        /// </summary>
        public int GetTotalStrength()
        {
            // TODO: Calculate based on creature AI values from CreatureDatabase
            // For now, return simple count
            return GetTotalCount();
        }

        /// <summary>
        /// Clear all stacks
        /// </summary>
        public void Clear()
        {
            for (var i = 0; i < MaxSlots; i++)
            {
                slots[i] = null;
            }
        }

        /// <summary>
        /// Merge another army into this one
        /// </summary>
        public bool MergeArmy(Army other)
        {
            foreach (var (_, stack) in other.GetStacksWithIndices())
            {
                if (!AddCreatures(stack.CreatureId, stack.Count))
                    return false; // Failed to merge completely
            }
            return true;
        }
    }
}
