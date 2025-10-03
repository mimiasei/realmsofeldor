using UnityEditor;
using UnityEngine;
using RealmsOfEldor.Core;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Editor
{
    /// <summary>
    /// Editor utility to generate sample game data
    /// </summary>
    public class SampleDataGenerator : EditorWindow
    {
        [MenuItem("Realms of Eldor/Generate Sample Data")]
        public static void ShowWindow()
        {
            GetWindow<SampleDataGenerator>("Sample Data Generator");
        }

        void OnGUI()
        {
            GUILayout.Label("Generate Sample Game Data", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (GUILayout.Button("Generate Sample Creatures (Castle)", GUILayout.Height(40)))
            {
                GenerateCastleCreatures();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate Sample Heroes (2)", GUILayout.Height(40)))
            {
                GenerateSampleHeroes();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate Sample Spells (5)", GUILayout.Height(40)))
            {
                GenerateSampleSpells();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("This will create sample ScriptableObject assets in Assets/Data/ folders.", MessageType.Info);
        }

        private static void GenerateCastleCreatures()
        {
            // Castle Tier 1 - Peasant
            CreateCreature(1, "Peasant", Faction.Castle, CreatureTier.Tier1,
                attack: 1, defense: 1, minDamage: 1, maxDamage: 1, hitPoints: 1, speed: 4,
                growth: 12, cost: new ResourceSet(gold: 15), aiValue: 15);

            // Castle Tier 1 Upgraded - Halberdier
            CreateCreature(2, "Halberdier", Faction.Castle, CreatureTier.Tier1,
                attack: 2, defense: 3, minDamage: 2, maxDamage: 3, hitPoints: 10, speed: 5,
                growth: 12, cost: new ResourceSet(gold: 75), aiValue: 75);

            // Castle Tier 2 - Archer
            CreateCreature(3, "Archer", Faction.Castle, CreatureTier.Tier2,
                attack: 6, defense: 3, minDamage: 2, maxDamage: 3, hitPoints: 10, speed: 4,
                shots: 12, growth: 9, cost: new ResourceSet(wood: 5, gold: 150), aiValue: 126);

            // Castle Tier 2 Upgraded - Marksman
            CreateCreature(4, "Marksman", Faction.Castle, CreatureTier.Tier2,
                attack: 6, defense: 3, minDamage: 2, maxDamage: 3, hitPoints: 10, speed: 6,
                shots: 24, growth: 9, cost: new ResourceSet(wood: 5, gold: 150), aiValue: 115,
                abilities: new[] { "Double shot" });

            // Castle Tier 3 - Griffin
            CreateCreature(5, "Griffin", Faction.Castle, CreatureTier.Tier3,
                attack: 8, defense: 8, minDamage: 3, maxDamage: 6, hitPoints: 25, speed: 6,
                growth: 7, cost: new ResourceSet(gold: 200), aiValue: 324,
                flying: true, noMeleeRetal: true, abilities: new[] { "Flying", "No enemy retaliation" });

            // Castle Tier 3 Upgraded - Royal Griffin
            CreateCreature(6, "Royal Griffin", Faction.Castle, CreatureTier.Tier3,
                attack: 9, defense: 9, minDamage: 3, maxDamage: 6, hitPoints: 25, speed: 9,
                growth: 7, cost: new ResourceSet(gold: 240), aiValue: 364,
                flying: true, noMeleeRetal: true, abilities: new[] { "Flying", "No enemy retaliation" });

            // Rampart Tier 1 - Centaur
            CreateCreature(20, "Centaur", Faction.Rampart, CreatureTier.Tier1,
                attack: 3, defense: 1, minDamage: 2, maxDamage: 3, hitPoints: 8, speed: 6,
                growth: 10, cost: new ResourceSet(gold: 70), aiValue: 100);

            // Rampart Tier 2 - Dwarf
            CreateCreature(21, "Dwarf", Faction.Rampart, CreatureTier.Tier2,
                attack: 5, defense: 7, minDamage: 2, maxDamage: 4, hitPoints: 20, speed: 3,
                growth: 8, cost: new ResourceSet(gold: 150), aiValue: 138,
                abilities: new[] { "Resistant to magic (20%)" });

            // Tower Tier 1 - Gremlin
            CreateCreature(40, "Gremlin", Faction.Tower, CreatureTier.Tier1,
                attack: 3, defense: 3, minDamage: 1, maxDamage: 2, hitPoints: 4, speed: 4,
                growth: 16, cost: new ResourceSet(gold: 30), aiValue: 44);

            // Neutral - Gold Golem
            CreateCreature(100, "Gold Golem", Faction.Neutral, CreatureTier.Tier4,
                attack: 11, defense: 12, minDamage: 8, maxDamage: 10, hitPoints: 50, speed: 5,
                growth: 0, cost: new ResourceSet(gold: 500), aiValue: 600,
                abilities: new[] { "Immune to spells" });

            Debug.Log("Generated 10 sample creatures!");
        }

        private static void CreateCreature(int id, string name, Faction faction, CreatureTier tier,
            int attack, int defense, int minDamage, int maxDamage, int hitPoints, int speed,
            int shots = 0, int growth = 1, ResourceSet cost = default, int aiValue = 100,
            bool flying = false, bool noMeleeRetal = false, string[] abilities = null)
        {
            var creature = ScriptableObject.CreateInstance<CreatureData>();
            creature.creatureId = id;
            creature.creatureName = name;
            creature.faction = faction;
            creature.tier = tier;
            creature.attack = attack;
            creature.defense = defense;
            creature.minDamage = minDamage;
            creature.maxDamage = maxDamage;
            creature.hitPoints = hitPoints;
            creature.speed = speed;
            creature.shots = shots;
            creature.weeklyGrowth = growth;
            creature.cost = cost;
            creature.aiValue = aiValue;
            creature.isFlying = flying;
            creature.noMeleeRetal = noMeleeRetal;

            if (abilities != null)
            {
                creature.abilities.AddRange(abilities);
            }

            string factionFolder = $"Assets/Data/Creatures/{faction}";
            if (!AssetDatabase.IsValidFolder(factionFolder))
            {
                AssetDatabase.CreateFolder("Assets/Data/Creatures", faction.ToString());
            }

            string path = $"{factionFolder}/{name}.asset";
            AssetDatabase.CreateAsset(creature, path);
            Debug.Log($"Created creature: {path}");
        }

        private static void GenerateSampleHeroes()
        {
            // Knight Hero
            var knight = ScriptableObject.CreateInstance<HeroTypeData>();
            knight.heroTypeId = 1;
            knight.heroName = "Sir Roland";
            knight.heroClass = HeroClass.Knight;
            knight.faction = Faction.Castle;
            knight.startAttack = 2;
            knight.startDefense = 2;
            knight.startSpellPower = 1;
            knight.startKnowledge = 1;
            knight.statGrowth = new PrimaryStatGrowth
            {
                attackChance = 35,
                defenseChance = 35,
                spellPowerChance = 15,
                knowledgeChance = 15
            };
            knight.specialtyName = "Swordsmen";
            knight.specialtyDescription = "+5% attack and defense to Swordsmen";
            knight.startsWithSpellbook = false;

            string knightPath = "Assets/Data/Heroes/SirRoland.asset";
            AssetDatabase.CreateAsset(knight, knightPath);
            Debug.Log($"Created hero: {knightPath}");

            // Wizard Hero
            var wizard = ScriptableObject.CreateInstance<HeroTypeData>();
            wizard.heroTypeId = 2;
            wizard.heroName = "Solmyr";
            wizard.heroClass = HeroClass.Wizard;
            wizard.faction = Faction.Tower;
            wizard.startAttack = 0;
            wizard.startDefense = 0;
            wizard.startSpellPower = 2;
            wizard.startKnowledge = 3;
            wizard.statGrowth = new PrimaryStatGrowth
            {
                attackChance = 10,
                defenseChance = 10,
                spellPowerChance = 35,
                knowledgeChance = 45
            };
            wizard.specialtyName = "Chain Lightning";
            wizard.specialtyDescription = "Casts Chain Lightning with increased effect";
            wizard.startsWithSpellbook = true;

            string wizardPath = "Assets/Data/Heroes/Solmyr.asset";
            AssetDatabase.CreateAsset(wizard, wizardPath);
            Debug.Log($"Created hero: {wizardPath}");
        }

        private static void GenerateSampleSpells()
        {
            // Magic Arrow - Level 1 damage spell
            CreateSpell(1, "Magic Arrow", SpellSchool.All, level: 1, manaCost: 5,
                target: SpellTarget.SingleEnemy, canBattle: true, canAdventure: false,
                effectType: SpellEffectType.Damage, basePower: 10, scaling: 10f,
                description: "Deals 10 + 10 x spell power damage to target unit.");

            // Haste - Level 1 buff spell
            CreateSpell(2, "Haste", SpellSchool.Air, level: 1, manaCost: 6,
                target: SpellTarget.SingleAlly, canBattle: true, canAdventure: false,
                effectType: SpellEffectType.Buff, basePower: 3, scaling: 0f,
                description: "Increases target's speed for duration of battle.");

            // Cure - Level 2 heal spell
            CreateSpell(3, "Cure", SpellSchool.Water, level: 2, manaCost: 6,
                target: SpellTarget.SingleAlly, canBattle: true, canAdventure: false,
                effectType: SpellEffectType.Heal, basePower: 10, scaling: 5f,
                description: "Restores 10 + 5 x spell power health to target unit.");

            // Fireball - Level 3 damage spell
            CreateSpell(4, "Fireball", SpellSchool.Fire, level: 3, manaCost: 15,
                target: SpellTarget.SingleEnemy, canBattle: true, canAdventure: false,
                effectType: SpellEffectType.Damage, basePower: 15, scaling: 10f,
                description: "Deals 15 + 10 x spell power fire damage to target.");

            // Town Portal - Level 5 adventure spell
            CreateSpell(5, "Town Portal", SpellSchool.All, level: 5, manaCost: 20,
                target: SpellTarget.Self, canBattle: false, canAdventure: true,
                effectType: SpellEffectType.Teleport, basePower: 0, scaling: 0f,
                description: "Teleports hero to any owned town.");

            Debug.Log("Generated 5 sample spells!");
        }

        private static void CreateSpell(int id, string name, SpellSchool school, int level, int manaCost,
            SpellTarget target, bool canBattle, bool canAdventure,
            SpellEffectType effectType, int basePower, float scaling, string description)
        {
            var spell = ScriptableObject.CreateInstance<SpellData>();
            spell.spellId = id;
            spell.spellName = name;
            spell.school = school;
            spell.level = level;
            spell.manaCost = manaCost;
            spell.targetType = target;
            spell.canCastInBattle = canBattle;
            spell.canCastOnAdventureMap = canAdventure;
            spell.description = description;

            spell.effects.Add(new SpellEffect
            {
                effectType = effectType,
                basePower = basePower,
                spellPowerScaling = scaling,
                duration = effectType == SpellEffectType.Buff ? 10 : 0
            });

            string schoolFolder = $"Assets/Data/Spells/{school}";
            if (!AssetDatabase.IsValidFolder(schoolFolder))
            {
                AssetDatabase.CreateFolder("Assets/Data/Spells", school.ToString());
            }

            string path = $"{schoolFolder}/{name}.asset";
            AssetDatabase.CreateAsset(spell, path);
            Debug.Log($"Created spell: {path}");
        }
    }
}
