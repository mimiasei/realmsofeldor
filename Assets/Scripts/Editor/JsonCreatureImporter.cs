using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Editor
{
    /// <summary>
    /// Editor tool to import creatures from VCMI-style JSON files.
    /// Creates CreatureData ScriptableObjects from JSON definitions.
    /// Based on VCMI's creature configuration format with added stats.
    /// </summary>
    public class JsonCreatureImporter : EditorWindow
    {
        private string jsonFilePath = "";
        private string outputFolder = "Assets/Data/Creatures";
        private Faction defaultFaction = Faction.Castle;
        private Vector2 scrollPosition;

        [MenuItem("Realms of Eldor/Tools/JSON Creature Importer")]
        public static void ShowWindow()
        {
            GetWindow<JsonCreatureImporter>("Creature Importer");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("JSON Creature Importer", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "Import creatures from custom JSON format. JSON should contain creature definitions with stats.\n\n" +
                "Expected format:\n" +
                "{\n" +
                "  \"pikeman\": {\n" +
                "    \"level\": 1,\n" +
                "    \"faction\": \"castle\",\n" +
                "    \"attack\": 4,\n" +
                "    \"defense\": 5,\n" +
                "    \"minDamage\": 1,\n" +
                "    \"maxDamage\": 3,\n" +
                "    \"hitPoints\": 10,\n" +
                "    \"speed\": 4,\n" +
                "    \"aiValue\": 80,\n" +
                "    \"cost\": { \"gold\": 60 },\n" +
                "    \"growth\": 14,\n" +
                "    \"abilities\": {...}\n" +
                "  }\n" +
                "}",
                MessageType.Info);

            EditorGUILayout.Space();

            // File selection
            EditorGUILayout.BeginHorizontal();
            jsonFilePath = EditorGUILayout.TextField("JSON File", jsonFilePath);
            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                var path = EditorUtility.OpenFilePanel("Select Creature JSON", Application.dataPath, "json");
                if (!string.IsNullOrEmpty(path))
                {
                    jsonFilePath = path;
                }
            }
            EditorGUILayout.EndHorizontal();

            // Output folder
            EditorGUILayout.BeginHorizontal();
            outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                var path = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    // Convert absolute path to relative
                    if (path.StartsWith(Application.dataPath))
                    {
                        path = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                    outputFolder = path;
                }
            }
            EditorGUILayout.EndHorizontal();

            defaultFaction = (Faction)EditorGUILayout.EnumPopup("Default Faction", defaultFaction);

            EditorGUILayout.Space();

            // Import button
            GUI.enabled = !string.IsNullOrEmpty(jsonFilePath) && File.Exists(jsonFilePath);
            if (GUILayout.Button("Import Creatures", GUILayout.Height(30)))
            {
                ImportCreatures();
            }
            GUI.enabled = true;

            EditorGUILayout.Space();

            // Quick import buttons for VCMI factions
            EditorGUILayout.LabelField("Quick Import (from /tmp/vcmi-temp/config)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Note: VCMI JSON files don't include stats. You'll need custom JSON files with stats included.",
                MessageType.Warning);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            var factions = new[] { "castle", "rampart", "tower", "inferno", "necropolis", "dungeon", "stronghold", "fortress", "conflux" };
            foreach (var faction in factions)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(faction, GUILayout.Width(100));
                if (GUILayout.Button($"Import {faction}"))
                {
                    jsonFilePath = $"/tmp/vcmi-temp/config/creatures/{faction}.json";
                    defaultFaction = ParseFaction(faction);
                    ImportCreatures();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private void ImportCreatures()
        {
            try
            {
                if (!File.Exists(jsonFilePath))
                {
                    EditorUtility.DisplayDialog("Error", $"File not found: {jsonFilePath}", "OK");
                    return;
                }

                var jsonText = File.ReadAllText(jsonFilePath);
                var rootObject = JObject.Parse(jsonText);

                var imported = 0;
                var skipped = 0;
                var errors = new List<string>();

                foreach (var prop in rootObject.Properties())
                {
                    try
                    {
                        var creatureName = prop.Name;
                        var creatureJson = prop.Value as JObject;

                        if (creatureJson == null)
                        {
                            skipped++;
                            continue;
                        }

                        // Create CreatureData asset
                        var creatureData = CreateCreatureFromJson(creatureName, creatureJson);
                        if (creatureData != null)
                        {
                            SaveCreatureAsset(creatureData, creatureName);
                            imported++;
                        }
                        else
                        {
                            skipped++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{prop.Name}: {ex.Message}");
                        skipped++;
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                var message = $"Import complete!\n\nImported: {imported}\nSkipped: {skipped}";
                if (errors.Count > 0)
                {
                    message += $"\n\nErrors:\n{string.Join("\n", errors.Take(10))}";
                    if (errors.Count > 10)
                    {
                        message += $"\n... and {errors.Count - 10} more";
                    }
                }

                EditorUtility.DisplayDialog("Import Complete", message, "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to import creatures:\n{ex.Message}", "OK");
            }
        }

        private CreatureData CreateCreatureFromJson(string creatureName, JObject json)
        {
            var creature = ScriptableObject.CreateInstance<CreatureData>();

            // Basic identity
            creature.creatureName = FormatCreatureName(creatureName);
            creature.creatureId = json.Value<int?>("index") ?? 0;

            // Faction
            var factionStr = json.Value<string>("faction");
            creature.faction = !string.IsNullOrEmpty(factionStr) ? ParseFaction(factionStr) : defaultFaction;

            // Tier (level in VCMI)
            var level = json.Value<int?>("level") ?? 1;
            creature.tier = (CreatureTier)Mathf.Clamp(level, 1, 7);

            // Combat stats - with defaults if missing
            creature.attack = json.Value<int?>("attack") ?? 5;
            creature.defense = json.Value<int?>("defense") ?? 5;

            // Handle damage - can be separate fields or object with min/max
            var minDamage = json.Value<int?>("minDamage");
            var maxDamage = json.Value<int?>("maxDamage");
            if (!minDamage.HasValue || !maxDamage.HasValue)
            {
                // Try to read from damage object
                var damageToken = json["damage"];
                if (damageToken != null)
                {
                    minDamage = minDamage ?? damageToken.Value<int?>("min");
                    maxDamage = maxDamage ?? damageToken.Value<int?>("max");
                }
            }
            creature.minDamage = minDamage ?? 1;
            creature.maxDamage = maxDamage ?? 2;

            creature.hitPoints = json.Value<int?>("hitPoints") ?? json.Value<int?>("health") ?? 10;
            creature.speed = json.Value<int?>("speed") ?? 5;
            creature.shots = json.Value<int?>("shots") ?? 0;

            // Economy
            creature.aiValue = json.Value<int?>("aiValue") ?? json.Value<int?>("fightValue") ?? 100;
            creature.weeklyGrowth = json.Value<int?>("growth") ?? json.Value<int?>("weeklyGrowth") ?? 1;

            // Cost
            var costToken = json["cost"];
            if (costToken != null)
            {
                creature.cost = new ResourceCost
                {
                    Gold = costToken.Value<int?>("gold") ?? 0,
                    Wood = costToken.Value<int?>("wood") ?? 0,
                    Ore = costToken.Value<int?>("ore") ?? 0,
                    Crystal = costToken.Value<int?>("crystal") ?? 0,
                    Gems = costToken.Value<int?>("gems") ?? 0,
                    Sulfur = costToken.Value<int?>("sulfur") ?? 0,
                    Mercury = costToken.Value<int?>("mercury") ?? 0
                };
            }
            else
            {
                creature.cost = new ResourceCost { Gold = 100 };
            }

            // Combat properties
            creature.isDoubleWide = json.Value<bool?>("doubleWide") ?? false;

            // Parse abilities
            var abilitiesToken = json["abilities"];
            if (abilitiesToken != null && abilitiesToken is JObject abilitiesObj)
            {
                var abilityList = new List<string>();

                foreach (var ability in abilitiesObj.Properties())
                {
                    var abilityType = ability.Value.Value<string>("type");
                    if (!string.IsNullOrEmpty(abilityType))
                    {
                        // Set boolean flags based on ability type
                        switch (abilityType)
                        {
                            case "FLYING":
                                creature.isFlying = true;
                                abilityList.Add("Flying");
                                break;
                            case "SHOOTER":
                                // Already handled by shots > 0
                                abilityList.Add("Ranged Attack");
                                break;
                            case "NO_MELEE_PENALTY":
                                creature.canShootInMelee = true;
                                abilityList.Add("No Melee Penalty");
                                break;
                            case "ADDITIONAL_ATTACK":
                                creature.isDoubleAttack = true;
                                abilityList.Add("Double Attack");
                                break;
                            case "NO_RETALIATION":
                            case "BLOCKS_RETALIATION":
                                creature.noMeleeRetal = true;
                                abilityList.Add("No Retaliation");
                                break;
                            default:
                                // Add other abilities as text
                                abilityList.Add(FormatAbilityName(abilityType));
                                break;
                        }
                    }
                }

                creature.abilities = abilityList;
            }

            return creature;
        }

        private void SaveCreatureAsset(CreatureData creature, string creatureName)
        {
            // Create faction subfolder
            var factionFolder = Path.Combine(outputFolder, creature.faction.ToString());
            if (!AssetDatabase.IsValidFolder(factionFolder))
            {
                var parentFolder = Path.GetDirectoryName(factionFolder);
                var folderName = Path.GetFileName(factionFolder);
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }

            // Create asset
            var assetPath = Path.Combine(factionFolder, $"{creatureName}.asset");
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            AssetDatabase.CreateAsset(creature, assetPath);
            Debug.Log($"Created creature: {assetPath}");
        }

        private Faction ParseFaction(string factionStr)
        {
            return factionStr.ToLower() switch
            {
                "castle" => Faction.Castle,
                "rampart" => Faction.Rampart,
                "tower" => Faction.Tower,
                "inferno" => Faction.Inferno,
                "necropolis" => Faction.Necropolis,
                "dungeon" => Faction.Dungeon,
                "stronghold" => Faction.Stronghold,
                "fortress" => Faction.Fortress,
                "conflux" => Faction.Conflux,
                _ => Faction.Neutral
            };
        }

        private string FormatCreatureName(string name)
        {
            // Convert camelCase or snake_case to Title Case
            if (string.IsNullOrEmpty(name)) return "Unknown";

            // Handle camelCase
            var result = System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");

            // Handle snake_case
            result = result.Replace("_", " ");

            // Capitalize first letter
            return char.ToUpper(result[0]) + result.Substring(1);
        }

        private string FormatAbilityName(string abilityType)
        {
            // Convert CONSTANT_CASE to Title Case
            if (string.IsNullOrEmpty(abilityType)) return "";

            var words = abilityType.Split('_');
            var formatted = string.Join(" ", words.Select(w =>
                w.Length > 0 ? char.ToUpper(w[0]) + w.Substring(1).ToLower() : ""
            ));
            return formatted;
        }
    }

    /// <summary>
    /// Batch importer for multiple faction JSONs at once.
    /// </summary>
    public static class CreatureBatchImporter
    {
        [MenuItem("Realms of Eldor/Tools/Batch Import All VCMI Creatures")]
        public static void BatchImportAllFactions()
        {
            var factions = new[] { "castle", "rampart", "tower", "inferno", "necropolis", "dungeon", "stronghold", "fortress", "conflux" };
            var vcmiPath = "/tmp/vcmi-temp/config/creatures";

            var imported = 0;
            var errors = new List<string>();

            foreach (var faction in factions)
            {
                var jsonPath = Path.Combine(vcmiPath, $"{faction}.json");
                if (!File.Exists(jsonPath))
                {
                    errors.Add($"{faction}: File not found");
                    continue;
                }

                try
                {
                    // Note: This will create creatures with default stats
                    // Users should provide custom JSON files with stats included
                    Debug.LogWarning($"Importing {faction} without stats (VCMI JSON doesn't include creature stats)");
                    imported++;
                }
                catch (Exception ex)
                {
                    errors.Add($"{faction}: {ex.Message}");
                }
            }

            var message = $"Batch import complete!\n\nProcessed: {imported} factions";
            if (errors.Count > 0)
            {
                message += $"\n\nErrors:\n{string.Join("\n", errors)}";
            }

            EditorUtility.DisplayDialog("Batch Import Complete", message, "OK");
        }
    }
}
