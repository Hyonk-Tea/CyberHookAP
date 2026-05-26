using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CyberHookAP
{
    internal sealed class GeneratedApData
    {
        private static string _gameName = "Cyber Hook";
        private static readonly Assembly RuntimeAssembly = typeof(GeneratedApData).Assembly;

        private const string ResourceApLocations = "CyberHookAP.Runtime.ap_locations.json";
        private const string ResourceApLevels = "CyberHookAP.Runtime.ap_levels.json";
        private const string ResourceCubeCounts = "CyberHookAP.Runtime.cube_counts_by_level.csv";
        private const string ResourceApRuntime = "CyberHookAP.Runtime.ap_runtime.json";

        private readonly Dictionary<string, long> _locationIdByCheckKey;
        private readonly Dictionary<long, string> _checkKeyByLocationId;
        private readonly Dictionary<string, GeneratedApLocation> _locationsByCheckKey;
        private readonly Dictionary<string, string> _checkKeyByGameId;
        private readonly Dictionary<long, GeneratedApItem> _itemsById;
        private readonly Dictionary<string, string> _levelUniqueIdByIdentifier;
        private readonly Dictionary<string, string> _displayNameByLevelUniqueId;

        internal static string GameName
        {
            get { return _gameName; }
        }

        private GeneratedApData(
            Dictionary<string, GeneratedApLocation> locationsByCheckKey,
            Dictionary<string, string> checkKeyByGameId,
            Dictionary<long, GeneratedApItem> itemsById,
            Dictionary<string, string> levelUniqueIdByIdentifier,
            Dictionary<string, string> displayNameByLevelUniqueId)
        {
            _locationIdByCheckKey = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            _checkKeyByLocationId = new Dictionary<long, string>();
            _locationsByCheckKey = locationsByCheckKey;
            _checkKeyByGameId = checkKeyByGameId;
            _itemsById = itemsById;
            _levelUniqueIdByIdentifier = levelUniqueIdByIdentifier;
            _displayNameByLevelUniqueId = displayNameByLevelUniqueId;
        }

        internal static GeneratedApData Load(string projectRoot)
        {
            string generatedRoot = Path.Combine(projectRoot, "generated");
            string helperLocationsPath = Path.Combine(generatedRoot, "ap_locations.json");
            string helperLevelsPath = Path.Combine(generatedRoot, "ap_levels.json");
            string localLevelsPath = Path.Combine(generatedRoot, "cube_counts_by_level.csv");
            string runtimePath = Path.Combine(generatedRoot, "ap_runtime.json");
            ApRuntimeData runtimeData = LoadJson<ApRuntimeData>(ReadTextResourceOrFile(ResourceApRuntime, runtimePath), ResourceApRuntime);
            _gameName = string.IsNullOrEmpty(runtimeData.GameName) ? "Cyber Hook" : runtimeData.GameName;

            Dictionary<string, ApRuntimeLevel> apWorldLevelsByDisplayName = ParseRuntimeLevels(runtimeData);
            Dictionary<string, GeneratedApLevel> generatedLevelsByNameKey = LoadGeneratedLevels(ReadTextResourceOrFile(ResourceApLevels, helperLevelsPath));
            Dictionary<string, LocalLevelInfo> localLevelsByUniqueId = ParseLocalLevels(
                ReadTextResourceOrFile(ResourceCubeCounts, localLevelsPath),
                generatedLevelsByNameKey);
            GeneratedApLocation[] helperLocations = LoadJson<GeneratedApLocation[]>(ReadTextResourceOrFile(ResourceApLocations, helperLocationsPath), ResourceApLocations);

            Dictionary<string, GeneratedApLocation> locationsByCheckKey = new Dictionary<string, GeneratedApLocation>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> checkKeyByGameId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> levelUniqueIdByIdentifier = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> displayNameByLevelUniqueId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, LocalLevelInfo> pair in localLevelsByUniqueId)
            {
                LocalLevelInfo localLevel = pair.Value;
                if (localLevel == null || string.IsNullOrEmpty(localLevel.DisplayName))
                {
                    continue;
                }

                ApRuntimeLevel apWorldLevel;
                if (!apWorldLevelsByDisplayName.TryGetValue(localLevel.DisplayName, out apWorldLevel) || apWorldLevel == null)
                {
                    continue;
                }

                levelUniqueIdByIdentifier[localLevel.LevelUniqueId] = localLevel.LevelUniqueId;
                levelUniqueIdByIdentifier[localLevel.DisplayName] = localLevel.LevelUniqueId;
                levelUniqueIdByIdentifier[localLevel.LevelNameKey] = localLevel.LevelUniqueId;
                levelUniqueIdByIdentifier[apWorldLevel.InternalLevelId] = localLevel.LevelUniqueId;
                displayNameByLevelUniqueId[localLevel.LevelUniqueId] = localLevel.DisplayName;
            }

            for (int i = 0; i < helperLocations.Length; i++)
            {
                GeneratedApLocation helperLocation = helperLocations[i];
                if (helperLocation == null || string.IsNullOrEmpty(helperLocation.LevelUniqueId))
                {
                    continue;
                }

                LocalLevelInfo localLevel;
                if (!localLevelsByUniqueId.TryGetValue(helperLocation.LevelUniqueId, out localLevel) || localLevel == null)
                {
                    continue;
                }

                ApRuntimeLevel apWorldLevel;
                if (!apWorldLevelsByDisplayName.TryGetValue(localLevel.DisplayName, out apWorldLevel) || apWorldLevel == null)
                {
                    continue;
                }

                string checkKey = BuildCheckKey(helperLocation);
                if (string.IsNullOrEmpty(checkKey))
                {
                    continue;
                }

                string locationName;
                string gameId;
                if (!TryBuildApWorldLocation(helperLocation, localLevel.DisplayName, apWorldLevel.InternalLevelId, out locationName, out gameId))
                {
                    continue;
                }

                GeneratedApLocation location = new GeneratedApLocation
                {
                    Name = locationName,
                    Kind = helperLocation.Kind,
                    WorldIndex = helperLocation.WorldIndex,
                    LevelUniqueId = helperLocation.LevelUniqueId,
                    LevelNameKey = helperLocation.LevelNameKey,
                    Rank = helperLocation.Rank,
                    ScenePath = helperLocation.ScenePath,
                    GameId = gameId
                };

                locationsByCheckKey[checkKey] = location;
                checkKeyByGameId[gameId] = checkKey;
            }

            Dictionary<long, GeneratedApItem> itemsById = ParseRuntimeItems(runtimeData);
            GeneratedApData data = new GeneratedApData(
                locationsByCheckKey,
                checkKeyByGameId,
                itemsById,
                levelUniqueIdByIdentifier,
                displayNameByLevelUniqueId);
            CyberHookApMod.SafeLog(
                "Generated AP data loaded: "
                + locationsByCheckKey.Count.ToString()
                + " location templates, "
                + itemsById.Count.ToString()
                + " items, "
                + levelUniqueIdByIdentifier.Count.ToString()
                + " level identifiers.");
            return data;
        }

        internal void UpdateLocationMappings(JToken slotLocationsToken)
        {
            _locationIdByCheckKey.Clear();
            _checkKeyByLocationId.Clear();

            JObject slotLocations = slotLocationsToken as JObject;
            if (slotLocations == null)
            {
                return;
            }

            foreach (JProperty property in slotLocations.Properties())
            {
                string gameId = property.Name;
                if (string.IsNullOrEmpty(gameId))
                {
                    continue;
                }

                long locationId;
                try
                {
                    locationId = property.Value.Value<long>();
                }
                catch
                {
                    continue;
                }

                string checkKey;
                if (!_checkKeyByGameId.TryGetValue(gameId, out checkKey))
                {
                    continue;
                }

                _locationIdByCheckKey[checkKey] = locationId;
                _checkKeyByLocationId[locationId] = checkKey;

                GeneratedApLocation location;
                if (_locationsByCheckKey.TryGetValue(checkKey, out location) && location != null)
                {
                    location.Id = locationId;
                }
            }

            CyberHookApMod.SafeLog(
                "Mapped "
                + _locationIdByCheckKey.Count.ToString()
                + " Archipelago location ids from slot data.");
        }

        internal bool TryGetLocationId(string checkKey, out long locationId)
        {
            return _locationIdByCheckKey.TryGetValue(checkKey, out locationId);
        }

        internal bool TryGetCheckKey(long locationId, out string checkKey)
        {
            return _checkKeyByLocationId.TryGetValue(locationId, out checkKey);
        }

        internal bool TryGetLocation(string checkKey, out GeneratedApLocation location)
        {
            return _locationsByCheckKey.TryGetValue(checkKey, out location);
        }

        internal bool TryGetItem(long itemId, out GeneratedApItem item)
        {
            return _itemsById.TryGetValue(itemId, out item);
        }

        internal bool TryResolveLevelIdentifier(string identifier, out string levelUniqueId)
        {
            levelUniqueId = string.Empty;
            if (string.IsNullOrEmpty(identifier))
            {
                return false;
            }

            return _levelUniqueIdByIdentifier.TryGetValue(identifier, out levelUniqueId);
        }

        internal bool TryGetLevelDisplayName(string levelUniqueId, out string displayName)
        {
            displayName = string.Empty;
            if (string.IsNullOrEmpty(levelUniqueId))
            {
                return false;
            }

            return _displayNameByLevelUniqueId.TryGetValue(levelUniqueId, out displayName);
        }

        private static bool TryBuildApWorldLocation(
            GeneratedApLocation helperLocation,
            string displayName,
            string internalLevelId,
            out string locationName,
            out string gameId)
        {
            locationName = string.Empty;
            gameId = string.Empty;

            if (helperLocation == null || string.IsNullOrEmpty(helperLocation.Kind) || string.IsNullOrEmpty(internalLevelId))
            {
                return false;
            }

            switch (helperLocation.Kind)
            {
                case "rank":
                    if (helperLocation.Rank.GetValueOrDefault() <= 0)
                    {
                        locationName = displayName + " Complete";
                        gameId = internalLevelId + "-c";
                        return true;
                    }

                    locationName = displayName + " Star " + helperLocation.Rank.Value.ToString();
                    gameId = internalLevelId + "-s" + helperLocation.Rank.Value.ToString();
                    return true;
                case "anchor":
                    locationName = displayName + " Numero Anchor";
                    gameId = internalLevelId + "-na";
                    return true;
                case "cube":
                    int cubeIndex = ExtractCubeIndex(helperLocation.Name);
                    if (cubeIndex <= 0)
                    {
                        return false;
                    }

                    locationName = displayName + " Cube " + cubeIndex.ToString();
                    gameId = internalLevelId + "-cu" + cubeIndex.ToString();
                    return true;
                default:
                    return false;
            }
        }

        private static string BuildCheckKey(GeneratedApLocation location)
        {
            if (location == null || string.IsNullOrEmpty(location.Kind) || string.IsNullOrEmpty(location.LevelUniqueId))
            {
                return string.Empty;
            }

            switch (location.Kind)
            {
                case "rank":
                    return location.LevelUniqueId + "::rank::" + location.Rank.GetValueOrDefault().ToString();
                case "anchor":
                    return location.LevelUniqueId + "::numero_anchor";
                case "cube":
                    return location.LevelUniqueId + "::cube::" + (location.ScenePath ?? string.Empty);
                default:
                    return string.Empty;
            }
        }

        private static int ExtractCubeIndex(string locationName)
        {
            if (string.IsNullOrEmpty(locationName))
            {
                return 0;
            }

            Match match = Regex.Match(locationName, @"Cube\s+(?<index>\d+)$", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return 0;
            }

            int cubeIndex;
            if (int.TryParse(match.Groups["index"].Value, out cubeIndex))
            {
                return cubeIndex;
            }

            return 0;
        }

        private static Dictionary<long, GeneratedApItem> ParseRuntimeItems(ApRuntimeData runtimeData)
        {
            Dictionary<long, GeneratedApItem> itemsById = new Dictionary<long, GeneratedApItem>();
            if (runtimeData == null || runtimeData.Items == null)
            {
                return itemsById;
            }

            for (int i = 0; i < runtimeData.Items.Length; i++)
            {
                GeneratedApItem item = runtimeData.Items[i];
                if (item == null || string.IsNullOrEmpty(item.Name))
                {
                    continue;
                }

                itemsById[item.Id] = item;
            }

            return itemsById;
        }

        private static Dictionary<string, ApRuntimeLevel> ParseRuntimeLevels(ApRuntimeData runtimeData)
        {
            Dictionary<string, ApRuntimeLevel> levelsByDisplayName = new Dictionary<string, ApRuntimeLevel>(StringComparer.OrdinalIgnoreCase);
            if (runtimeData == null || runtimeData.Levels == null)
            {
                return levelsByDisplayName;
            }

            for (int i = 0; i < runtimeData.Levels.Length; i++)
            {
                ApRuntimeLevel level = runtimeData.Levels[i];
                if (level == null || string.IsNullOrEmpty(level.DisplayName) || string.IsNullOrEmpty(level.InternalLevelId))
                {
                    continue;
                }

                levelsByDisplayName[level.DisplayName] = level;
            }

            return levelsByDisplayName;
        }

        private static Dictionary<string, GeneratedApLevel> LoadGeneratedLevels(string json)
        {
            GeneratedApLevel[] levels = LoadJson<GeneratedApLevel[]>(json, ResourceApLevels);
            Dictionary<string, GeneratedApLevel> byLevelNameKey = new Dictionary<string, GeneratedApLevel>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < levels.Length; i++)
            {
                GeneratedApLevel level = levels[i];
                if (level == null || string.IsNullOrEmpty(level.LevelNameKey))
                {
                    continue;
                }

                byLevelNameKey[level.LevelNameKey] = level;
            }

            return byLevelNameKey;
        }

        private static Dictionary<string, LocalLevelInfo> ParseLocalLevels(
            string csv,
            Dictionary<string, GeneratedApLevel> generatedLevelsByNameKey)
        {
            if (string.IsNullOrEmpty(csv))
            {
                throw new InvalidOperationException("Missing local cube count CSV data.");
            }

            Dictionary<string, LocalLevelInfo> levelsByUniqueId = new Dictionary<string, LocalLevelInfo>(StringComparer.OrdinalIgnoreCase);
            string[] lines = SplitLines(csv);
            if (lines.Length <= 1)
            {
                return levelsByUniqueId;
            }

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                string[] parts = SplitCsvLine(line);
                if (parts.Length < 7)
                {
                    continue;
                }

                string displayName = parts[3];
                if (string.IsNullOrEmpty(displayName))
                {
                    continue;
                }

                bool isExcluded = ParseBool(parts[6]);
                if (isExcluded)
                {
                    continue;
                }

                string levelNameKey = parts[2];
                GeneratedApLevel generatedLevel;
                if (string.IsNullOrEmpty(levelNameKey)
                    || !generatedLevelsByNameKey.TryGetValue(levelNameKey, out generatedLevel)
                    || generatedLevel == null
                    || string.IsNullOrEmpty(generatedLevel.LevelUniqueId))
                {
                    continue;
                }

                LocalLevelInfo localLevel = new LocalLevelInfo
                {
                    LevelUniqueId = generatedLevel.LevelUniqueId,
                    LevelNameKey = levelNameKey,
                    DisplayName = displayName
                };
                levelsByUniqueId[localLevel.LevelUniqueId] = localLevel;
            }

            return levelsByUniqueId;
        }

        private static bool ParseBool(string value)
        {
            return string.Equals(value, "True", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
        }

        private static string[] SplitCsvLine(string line)
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith("\"", StringComparison.Ordinal) && trimmed.EndsWith("\"", StringComparison.Ordinal) && trimmed.Length >= 2)
            {
                trimmed = trimmed.Substring(1, trimmed.Length - 2);
            }

            return trimmed.Split(new[] { "\",\"" }, StringSplitOptions.None);
        }

        private static string[] SplitLines(string content)
        {
            return content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        }

        private static string ReadTextResourceOrFile(string resourceName, string fallbackPath)
        {
            string resourceText = ReadEmbeddedResourceText(resourceName);
            if (!string.IsNullOrEmpty(resourceText))
            {
                return resourceText;
            }

            if (!string.IsNullOrEmpty(fallbackPath) && File.Exists(fallbackPath))
            {
                return File.ReadAllText(fallbackPath);
            }

            throw new FileNotFoundException("Missing generated AP runtime data.", fallbackPath ?? resourceName);
        }

        private static string ReadEmbeddedResourceText(string resourceName)
        {
            using (Stream stream = RuntimeAssembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    return null;
                }

                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private static T LoadJson<T>(string json, string sourceName)
        {
            T result = JsonConvert.DeserializeObject<T>(json);
            if (result == null)
            {
                throw new InvalidOperationException("Failed to deserialize generated AP data file: " + sourceName);
            }

            return result;
        }

        private sealed class ApRuntimeData
        {
            public string GameName;
            public GeneratedApItem[] Items;
            public ApRuntimeLevel[] Levels;
        }

        private sealed class ApRuntimeLevel
        {
            public string DisplayName;
            public string InternalLevelId;
        }

        private sealed class LocalLevelInfo
        {
            public string LevelUniqueId;
            public string LevelNameKey;
            public string DisplayName;
        }
    }

    internal sealed class GeneratedApLocation
    {
        public long Id;
        public string Name;
        public string Kind;
        public int WorldIndex;
        public string LevelUniqueId;
        public string LevelNameKey;
        public int? Rank;
        public string ScenePath;
        public string GameId;
    }

    internal sealed class GeneratedApItem
    {
        public long Id;
        public string Name;
    }

    internal sealed class GeneratedApLevel
    {
        public string LevelUniqueId;
        public string LevelNameKey;
        public int WorldIndex;
        public string WorldNameKey;
    }
}
