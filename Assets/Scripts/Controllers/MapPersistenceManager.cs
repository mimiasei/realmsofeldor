using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using Cysharp.Threading.Tasks;
using System.Threading;
using RealmsOfEldor.Core;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Manages saving and loading of generated maps.
    /// Maps are stored with metadata for display in map selection UI.
    /// Based on VCMI's map file management pattern.
    /// </summary>
    public class MapPersistenceManager : MonoBehaviour
    {
        public static MapPersistenceManager Instance { get; private set; }

        private const string MAPS_FOLDER = "Maps";
        private const string METADATA_FILE = "map_metadata.json";
        private const string MAP_FILE_EXTENSION = ".remap"; // Realms of Eldor Map

        private string mapsDirectory;
        private Dictionary<string, MapMetadata> mapMetadataCache;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            mapsDirectory = Path.Combine(Application.persistentDataPath, MAPS_FOLDER);
            mapMetadataCache = new Dictionary<string, MapMetadata>();

            // Ensure maps directory exists
            if (!Directory.Exists(mapsDirectory))
            {
                Directory.CreateDirectory(mapsDirectory);
                Debug.Log($"Created maps directory: {mapsDirectory}");
            }

            LoadMapMetadataCache();

            Debug.Log($"MapPersistenceManager initialized. Maps directory: {mapsDirectory}");
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #region Save/Load Maps

        /// <summary>
        /// Saves a generated map with metadata.
        /// </summary>
        public bool SaveMap(GameMap map, MapMetadata metadata)
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

                var mapFilePath = GetMapFilePath(metadata.Id);
                var json = JsonConvert.SerializeObject(map, settings);
                File.WriteAllText(mapFilePath, json);

                // Update metadata
                metadata.LastModifiedDate = DateTime.Now;
                mapMetadataCache[metadata.Id] = metadata;
                SaveMapMetadataCache();

                Debug.Log($"Map saved: {metadata.Name} ({mapFilePath}, {json.Length} bytes)");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save map: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Saves a map asynchronously using UniTask.
        /// </summary>
        public async UniTask<bool> SaveMapAsync(GameMap map, MapMetadata metadata, CancellationToken cancellationToken = default)
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
                    JsonConvert.SerializeObject(map, settings),
                    cancellationToken: cancellationToken);

                var mapFilePath = GetMapFilePath(metadata.Id);

                // Write to file asynchronously
                await UniTask.SwitchToThreadPool();
                await File.WriteAllTextAsync(mapFilePath, json, cancellationToken);
                await UniTask.SwitchToMainThread();

                // Update metadata
                metadata.LastModifiedDate = DateTime.Now;
                mapMetadataCache[metadata.Id] = metadata;
                await SaveMapMetadataCacheAsync(cancellationToken);

                Debug.Log($"Map saved async: {metadata.Name} ({mapFilePath}, {json.Length} bytes)");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save map async: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Loads a map by metadata ID.
        /// </summary>
        public GameMap LoadMap(string mapId)
        {
            try
            {
                var mapFilePath = GetMapFilePath(mapId);
                if (!File.Exists(mapFilePath))
                {
                    Debug.LogError($"Map file not found: {mapFilePath}");
                    return null;
                }

                var json = File.ReadAllText(mapFilePath);
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };

                var map = JsonConvert.DeserializeObject<GameMap>(json, settings);

                if (map == null)
                {
                    Debug.LogError($"Failed to deserialize map: {mapId}");
                    return null;
                }

                Debug.Log($"Map loaded: {mapId} ({map.Width}x{map.Height})");
                return map;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load map: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Loads a map asynchronously using UniTask.
        /// </summary>
        public async UniTask<GameMap> LoadMapAsync(string mapId, CancellationToken cancellationToken = default)
        {
            try
            {
                var mapFilePath = GetMapFilePath(mapId);
                if (!File.Exists(mapFilePath))
                {
                    Debug.LogError($"Map file not found: {mapFilePath}");
                    return null;
                }

                // Read file asynchronously
                await UniTask.SwitchToThreadPool();
                var json = await File.ReadAllTextAsync(mapFilePath, cancellationToken);
                await UniTask.SwitchToMainThread();

                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };

                // Deserialize on background thread
                var map = await UniTask.RunOnThreadPool(() =>
                    JsonConvert.DeserializeObject<GameMap>(json, settings),
                    cancellationToken: cancellationToken);

                if (map == null)
                {
                    Debug.LogError($"Failed to deserialize map: {mapId}");
                    return null;
                }

                Debug.Log($"Map loaded async: {mapId} ({map.Width}x{map.Height})");
                return map;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load map async: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Deletes a map and its metadata.
        /// </summary>
        public bool DeleteMap(string mapId)
        {
            try
            {
                var mapFilePath = GetMapFilePath(mapId);
                if (File.Exists(mapFilePath))
                {
                    File.Delete(mapFilePath);
                }

                if (mapMetadataCache.ContainsKey(mapId))
                {
                    mapMetadataCache.Remove(mapId);
                    SaveMapMetadataCache();
                }

                Debug.Log($"Map deleted: {mapId}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete map: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        #endregion

        #region Metadata Management

        /// <summary>
        /// Gets all available map metadata.
        /// </summary>
        public List<MapMetadata> GetAllMapMetadata()
        {
            return mapMetadataCache.Values.OrderByDescending(m => m.LastModifiedDate).ToList();
        }

        /// <summary>
        /// Gets metadata for a specific map.
        /// </summary>
        public MapMetadata GetMapMetadata(string mapId)
        {
            return mapMetadataCache.TryGetValue(mapId, out var metadata) ? metadata : null;
        }

        /// <summary>
        /// Checks if a map exists.
        /// </summary>
        public bool MapExists(string mapId)
        {
            return mapMetadataCache.ContainsKey(mapId) && File.Exists(GetMapFilePath(mapId));
        }

        #endregion

        #region Private Helpers

        private string GetMapFilePath(string mapId)
        {
            return Path.Combine(mapsDirectory, $"{mapId}{MAP_FILE_EXTENSION}");
        }

        private string GetMetadataFilePath()
        {
            return Path.Combine(mapsDirectory, METADATA_FILE);
        }

        private void LoadMapMetadataCache()
        {
            try
            {
                var metadataFilePath = GetMetadataFilePath();
                if (!File.Exists(metadataFilePath))
                {
                    Debug.Log("No map metadata file found. Starting with empty cache.");
                    mapMetadataCache = new Dictionary<string, MapMetadata>();
                    return;
                }

                var json = File.ReadAllText(metadataFilePath);
                var metadataList = JsonConvert.DeserializeObject<List<MapMetadata>>(json);

                mapMetadataCache = metadataList.ToDictionary(m => m.Id);
                Debug.Log($"Loaded {mapMetadataCache.Count} map metadata entries");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load map metadata cache: {e.Message}");
                mapMetadataCache = new Dictionary<string, MapMetadata>();
            }
        }

        private void SaveMapMetadataCache()
        {
            try
            {
                var metadataFilePath = GetMetadataFilePath();
                var metadataList = mapMetadataCache.Values.ToList();
                var json = JsonConvert.SerializeObject(metadataList, Formatting.Indented);
                File.WriteAllText(metadataFilePath, json);

                Debug.Log($"Saved {metadataList.Count} map metadata entries");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save map metadata cache: {e.Message}");
            }
        }

        private async UniTask SaveMapMetadataCacheAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var metadataFilePath = GetMetadataFilePath();
                var metadataList = mapMetadataCache.Values.ToList();

                var json = await UniTask.RunOnThreadPool(() =>
                    JsonConvert.SerializeObject(metadataList, Formatting.Indented),
                    cancellationToken: cancellationToken);

                await UniTask.SwitchToThreadPool();
                await File.WriteAllTextAsync(metadataFilePath, json, cancellationToken);
                await UniTask.SwitchToMainThread();

                Debug.Log($"Saved {metadataList.Count} map metadata entries async");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save map metadata cache async: {e.Message}");
            }
        }

        #endregion

        #region Debug Helpers

#if UNITY_EDITOR
        [ContextMenu("Print All Maps")]
        private void PrintAllMaps()
        {
            var maps = GetAllMapMetadata();
            Debug.Log($"=== Available Maps ({maps.Count}) ===");
            foreach (var map in maps)
            {
                Debug.Log($"- {map.GetDisplayName()} [ID: {map.Id}]");
            }
        }

        [ContextMenu("Clear All Maps")]
        private void ClearAllMaps()
        {
            if (Directory.Exists(mapsDirectory))
            {
                Directory.Delete(mapsDirectory, true);
                Directory.CreateDirectory(mapsDirectory);
            }
            mapMetadataCache.Clear();
            Debug.Log("All maps cleared");
        }
#endif

        #endregion
    }
}
