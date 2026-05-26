using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace CyberHookAP
{
    internal sealed class DiscoveryExporter
    {
        private readonly string _dataDirectory;

        public DiscoveryExporter(string dataDirectory)
        {
            _dataDirectory = dataDirectory;
        }

        public void ExportLevelPack(Levelpack levelpack)
        {
            if (levelpack == null)
            {
                return;
            }

            LevelPackDump dump = new LevelPackDump();
            List<WorldDetail> worlds = levelpack.GetWorldListWithChildren;
            if (worlds != null)
            {
                foreach (WorldDetail world in worlds)
                {
                    if (world == null)
                    {
                        continue;
                    }

                    WorldDump worldDump = new WorldDump();
                    worldDump.Index = world.Index;
                    worldDump.NameKey = world.WorldNameKey;
                    worldDump.UnlockStarsThreshold = world.UnlockStarsThreshold;
                    object unlockMethod = AccessTools.Field(typeof(WorldDetail), "UnlockMethod").GetValue(world);
                    worldDump.UnlockMethod = unlockMethod != null ? unlockMethod.ToString() : string.Empty;

                    List<SO_Level> levels = AccessTools.Field(typeof(WorldDetail), "LevelList").GetValue(world) as List<SO_Level>;
                    if (levels != null)
                    {
                        foreach (SO_Level level in levels)
                        {
                            if (level == null)
                            {
                                continue;
                            }

                            LevelDump levelDump = new LevelDump();
                            levelDump.WorldIndex = world.Index;
                            levelDump.LevelUniqueId = level.LevelUniqueID;
                            levelDump.LevelNameKey = level.LevelNameKey;
                            levelDump.DisplayOnStageSelect = level.DisplayOnStageSelect;
                            levelDump.IsAlwaysUnlocked = level.IsAlwaysUnlocked;
                            levelDump.IsActionUnlockable = level.IsActionUnlockable;
                            levelDump.IsTimeUnlockable = level.IsTimeUnlockable;
                            levelDump.Stars = level.Stars ?? new float[0];
                            worldDump.Levels.Add(levelDump);
                        }
                    }

                    dump.Worlds.Add(worldDump);
                }
            }

            WriteJson("discovery_levels.json", dump);
        }

        public void ExportCurrentSceneObjects(SO_Level activeLevel)
        {
            if (activeLevel == null)
            {
                return;
            }

            SceneObjectDump dump = new SceneObjectDump();
            dump.LevelUniqueId = activeLevel.LevelUniqueID;
            dump.LevelNameKey = activeLevel.LevelNameKey;

            BreakableCube[] cubes = UnityEngine.Object.FindObjectsOfType<BreakableCube>();
            foreach (BreakableCube cube in cubes)
            {
                if (cube == null)
                {
                    continue;
                }

                dump.Cubes.Add(new SceneObjectEntry
                {
                    Path = SceneObjectId.BuildTransformPath(cube.transform),
                    Name = cube.name
                });
            }

            LevelCollectible[] collectibles = UnityEngine.Object.FindObjectsOfType<LevelCollectible>();
            foreach (LevelCollectible collectible in collectibles)
            {
                if (collectible == null)
                {
                    continue;
                }

                dump.Collectibles.Add(new CollectibleEntry
                {
                    Path = SceneObjectId.BuildTransformPath(collectible.transform),
                    Name = collectible.name,
                    LevelId = SafeCallLevelId(collectible)
                });
            }

            string fileName = "scene_" + SanitizeFileName(activeLevel.LevelUniqueID) + ".json";
            WriteJson(fileName, dump);
        }

        public void AppendLine(string fileName, string line)
        {
            Directory.CreateDirectory(_dataDirectory);
            File.AppendAllText(Path.Combine(_dataDirectory, fileName), line + Environment.NewLine);
        }

        private string SafeCallLevelId(LevelCollectible collectible)
        {
            try
            {
                object result = AccessTools.Method(typeof(LevelCollectible), "GetLevelID").Invoke(collectible, new object[0]);
                return result as string ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private void WriteJson(string fileName, object payload)
        {
            Directory.CreateDirectory(_dataDirectory);
            string path = Path.Combine(_dataDirectory, fileName);
            string json = JsonConvert.SerializeObject(payload, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "unknown";
            }

            char[] invalid = Path.GetInvalidFileNameChars();
            char[] chars = value.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                for (int j = 0; j < invalid.Length; j++)
                {
                    if (chars[i] == invalid[j])
                    {
                        chars[i] = '_';
                        break;
                    }
                }
            }

            return new string(chars);
        }

        private sealed class LevelPackDump
        {
            public List<WorldDump> Worlds = new List<WorldDump>();
        }

        private sealed class WorldDump
        {
            public int Index;
            public string NameKey;
            public int UnlockStarsThreshold;
            public string UnlockMethod;
            public List<LevelDump> Levels = new List<LevelDump>();
        }

        private sealed class LevelDump
        {
            public int WorldIndex;
            public string LevelUniqueId;
            public string LevelNameKey;
            public bool DisplayOnStageSelect;
            public bool IsAlwaysUnlocked;
            public bool IsActionUnlockable;
            public bool IsTimeUnlockable;
            public float[] Stars;
        }

        private sealed class SceneObjectDump
        {
            public string LevelUniqueId;
            public string LevelNameKey;
            public List<SceneObjectEntry> Cubes = new List<SceneObjectEntry>();
            public List<CollectibleEntry> Collectibles = new List<CollectibleEntry>();
        }

        private class SceneObjectEntry
        {
            public string Name;
            public string Path;
        }

        private sealed class CollectibleEntry : SceneObjectEntry
        {
            public string LevelId;
        }
    }
}
