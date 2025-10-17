# Creature Data Import

This folder contains creature data for Realms of Eldor.

## JSON Creature Importer

The JSON Creature Importer tool allows bulk-creating CreatureData ScriptableObjects from JSON files.

### Usage

1. **Open the importer**: `Realms of Eldor > Tools > JSON Creature Importer`
2. **Select JSON file**: Browse to a creature JSON file (e.g., `castle_complete.json`)
3. **Choose output folder**: Select where to save the assets (default: `Assets/Data/Creatures`)
4. **Import**: Click "Import Creatures"

### JSON Format

The importer expects JSON in the following format:

```json
{
  "creatureName": {
    "index": 0,
    "level": 1,
    "faction": "castle",
    "attack": 4,
    "defense": 5,
    "minDamage": 1,
    "maxDamage": 3,
    "hitPoints": 10,
    "speed": 4,
    "shots": 0,
    "aiValue": 80,
    "growth": 14,
    "doubleWide": false,
    "cost": {
      "gold": 60,
      "wood": 0,
      "ore": 0,
      "crystal": 0,
      "gems": 0,
      "sulfur": 0,
      "mercury": 0
    },
    "abilities": {
      "abilityName": {
        "type": "ABILITY_TYPE"
      }
    }
  }
}
```

### Supported Fields

**Required:**
- `attack`, `defense`, `minDamage`, `maxDamage`, `hitPoints`, `speed`: Combat stats
- `aiValue`: AI strategic value
- `growth`: Weekly growth in town dwellings

**Optional:**
- `index`: Creature ID (auto-assigned if missing)
- `level`: Creature tier 1-7 (default: 1)
- `faction`: Faction name (castle, rampart, tower, etc.)
- `shots`: Number of shots for ranged units (0 = melee)
- `doubleWide`: Takes 2 hexes in battle (default: false)
- `cost`: Resource costs (gold, wood, ore, etc.)
- `abilities`: Special abilities (see below)

### Ability Types

The importer automatically recognizes these VCMI ability types:

- `FLYING`: Sets `isFlying = true`
- `SHOOTER`: Ranged attack (also inferred from `shots > 0`)
- `NO_MELEE_PENALTY`: Sets `canShootInMelee = true`
- `ADDITIONAL_ATTACK`: Sets `isDoubleAttack = true`
- `NO_RETALIATION` / `BLOCKS_RETALIATION`: Sets `noMeleeRetal = true`
- Other abilities are added as text descriptions

### Example Files

- **`castle_complete.json`**: Complete Castle faction with all 14 creatures (Heroes 3 stats)
- Use this as a template for other factions

### VCMI Compatibility

The JSON format is compatible with VCMI's creature definitions in `/config/creatures/*.json`, **but note:**

- **VCMI JSON files don't include creature stats** (attack, defense, HP, etc.)
- VCMI reads stats from the original Heroes 3 data files (CRTRAITS.TXT)
- To import from VCMI JSON, you must **manually add stats** to the JSON

### Creating Custom Creature JSONs

1. Copy VCMI's `/config/creatures/{faction}.json` file
2. Add stat fields to each creature:
   - `attack`, `defense`, `minDamage`, `maxDamage`, `hitPoints`, `speed`
   - `aiValue`, `growth`
   - `cost` object
3. Save and import via the tool

### Batch Operations

You can also use:
- `Realms of Eldor > Tools > Batch Import All VCMI Creatures` (requires stats in JSON)

---

## Manual Creation

To create creatures manually:
1. Right-click in Project window
2. `Create > Realms of Eldor > Creature`
3. Fill in stats in Inspector

---

## Creature Stats Reference (Heroes 3)

For reference, here are the official Heroes 3 stats:

### Castle
| Creature | Tier | ATK | DEF | DMG | HP | SPD | AI Value | Cost |
|----------|------|-----|-----|-----|----|----|----------|------|
| Pikeman | 1 | 4 | 5 | 1-3 | 10 | 4 | 80 | 60g |
| Halberdier | 1 | 6 | 5 | 2-3 | 10 | 5 | 115 | 75g |
| Archer | 2 | 6 | 3 | 2-3 | 10 | 4 | 126 | 100g, 5w |
| Marksman | 2 | 6 | 3 | 2-3 | 10 | 6 | 184 | 150g, 5w |
| Griffin | 3 | 8 | 8 | 3-6 | 25 | 6 | 351 | 200g |
| Royal Griffin | 3 | 9 | 9 | 3-6 | 25 | 9 | 448 | 240g |
| Swordsman | 4 | 10 | 12 | 6-9 | 35 | 5 | 445 | 300g |
| Crusader | 4 | 12 | 12 | 7-10 | 35 | 6 | 588 | 400g |
| Monk | 5 | 12 | 7 | 10-12 | 30 | 5 | 485 | 400g |
| Zealot | 5 | 12 | 10 | 10-12 | 30 | 7 | 750 | 450g |
| Cavalier | 6 | 15 | 15 | 15-25 | 100 | 7 | 1946 | 1000g |
| Champion | 6 | 16 | 16 | 20-25 | 100 | 9 | 2100 | 1200g |
| Angel | 7 | 20 | 20 | 50 | 200 | 12 | 5019 | 3000g |
| Archangel | 7 | 30 | 30 | 50 | 250 | 18 | 8776 | 5000g |

(Similar tables can be added for other factions)

---

*Last Updated: 2025-10-13*
