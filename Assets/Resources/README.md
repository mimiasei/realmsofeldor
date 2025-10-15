# Resources Folder

This folder contains assets that are loaded at runtime via Unity's `Resources.Load()` API.

## MapGenConfig.asset

**Purpose:** Default configuration for Random Map Generation (RMG).

**Used by:**
- `MapGenConfig.Instance` - Singleton accessor loads from this file
- `GameInitializer.cs` - Uses config for object placement budgets
- `MapGenBudget.cs` - Budget tracking based on config values

**Location:** Must be at `Assets/Resources/MapGenConfig.asset` for the Singleton to find it.

**How to Edit:**
1. Select `MapGenConfig.asset` in Project window
2. Edit values in Inspector
3. Changes apply immediately to all systems using `MapGenConfig.Instance`

**Default Values:**
- Treasure Budget: 10,000
- Mine Count: 7
- Dwelling Count: 4
- Resource Pile Count: 15
- Guard Placement: Enabled (min value 2000)

**Note:** If this file is missing, MapGenConfig will create a temporary instance with default values and log a warning.

---

*Last Updated: 2025-10-14*
