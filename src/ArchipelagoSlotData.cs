using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace CyberHookAP
{
    internal sealed class ArchipelagoSlotData
    {
        public int HighestRequiredRank = 0;
        public string GoalMode = string.Empty;
        public int GoalDiamondCount = 0;
        public HashSet<string> GoalLevelIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public List<int> WorldUnlockOrder = new List<int>();
        public bool DeathLinkEnabled = false;
        public int DeathLinkAmnesty = 0;

        internal static ArchipelagoSlotData FromToken(JToken slotDataToken, GeneratedApData data)
        {
            ArchipelagoSlotData result = new ArchipelagoSlotData();
            if (slotDataToken == null || slotDataToken.Type != JTokenType.Object)
            {
                return result;
            }

            JObject obj = (JObject)slotDataToken;
            result.HighestRequiredRank = ReadInt(obj, "highest_required_rank", "highestRequiredRank");
            result.GoalMode = ReadString(obj, "goal_mode", "goalMode", "goal_type", "goalType", "goal");
            result.GoalDiamondCount = ReadInt(obj, "goal_diamond_count", "goalDiamondCount", "diamond_goal", "diamondGoal");
            result.DeathLinkEnabled = ReadBool(obj, "death_link", "deathLink");
            result.DeathLinkAmnesty = ReadInt(obj, "death_link_amnesty", "deathLinkAmnesty");
            result.WorldUnlockOrder = ReadWorldOrder(
                obj,
                "world_order",
                "worldOrder",
                "world_unlock_order",
                "worldUnlockOrder",
                "shuffled_world_order",
                "shuffledWorldOrder");
            if (result.WorldUnlockOrder.Count == 0)
            {
                result.WorldUnlockOrder = ReadOrderedWorldFields(obj);
            }

            JToken goalLevelsToken = ReadToken(obj, "goal_levels", "goalLevels", "chosen_goal_levels", "chosenGoalLevels");
            if (goalLevelsToken != null && goalLevelsToken.Type == JTokenType.Array)
            {
                foreach (JToken entry in goalLevelsToken)
                {
                    string identifier = entry.Type == JTokenType.String ? (string)entry : entry.ToString();
                    string levelUniqueId;
                    if (data != null && data.TryResolveLevelIdentifier(identifier, out levelUniqueId))
                    {
                        result.GoalLevelIds.Add(levelUniqueId);
                    }
                }
            }

            return result;
        }

        internal int GetWorldUnlockPosition(int worldIndex)
        {
            if (WorldUnlockOrder == null || WorldUnlockOrder.Count == 0)
            {
                return worldIndex;
            }

            for (int i = 0; i < WorldUnlockOrder.Count; i++)
            {
                if (WorldUnlockOrder[i] == worldIndex)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int ReadInt(JObject obj, params string[] keys)
        {
            JToken token = ReadToken(obj, keys);
            return token != null && token.Type == JTokenType.Integer ? (int)token : 0;
        }

        private static string ReadString(JObject obj, params string[] keys)
        {
            JToken token = ReadToken(obj, keys);
            return token != null && token.Type == JTokenType.String ? (string)token : string.Empty;
        }

        private static bool ReadBool(JObject obj, params string[] keys)
        {
            JToken token = ReadToken(obj, keys);
            return token != null && token.Type == JTokenType.Boolean ? (bool)token : false;
        }

        private static List<int> ReadWorldOrder(JObject obj, params string[] keys)
        {
            List<int> result = new List<int>();
            JToken token = ReadToken(obj, keys);
            if (token == null || token.Type != JTokenType.Array)
            {
                return result;
            }

            foreach (JToken entry in token)
            {
                int worldIndex;
                if (TryResolveWorldIndex(entry, out worldIndex) && worldIndex >= 0 && worldIndex <= 6 && !result.Contains(worldIndex))
                {
                    result.Add(worldIndex);
                }
            }

            if (result.Count == 6)
            {
                AppendMissingWorld(result);
            }

            if (result.Count != 7)
            {
                result.Clear();
            }

            return result;
        }

        private static List<int> ReadOrderedWorldFields(JObject obj)
        {
            List<int> result = new List<int>();
            string[] keys =
            {
                "FirstWorld",
                "SecondWorld",
                "ThirdWorld",
                "FourthWorld",
                "FifthWorld",
                "SixthWorld"
            };

            for (int i = 0; i < keys.Length; i++)
            {
                JToken token = ReadToken(obj, keys[i]);
                int worldIndex;
                if (!TryResolveWorldIndex(token, out worldIndex) || result.Contains(worldIndex))
                {
                    result.Clear();
                    return result;
                }

                result.Add(worldIndex);
            }

            if (result.Count == 6)
            {
                AppendMissingWorld(result);
            }

            if (result.Count != 7)
            {
                result.Clear();
            }

            return result;
        }

        private static void AppendMissingWorld(List<int> result)
        {
            for (int worldIndex = 0; worldIndex <= 6; worldIndex++)
            {
                if (!result.Contains(worldIndex))
                {
                    result.Add(worldIndex);
                    return;
                }
            }
        }

        private static bool TryResolveWorldIndex(JToken token, out int worldIndex)
        {
            worldIndex = -1;
            if (token == null)
            {
                return false;
            }

            if (token.Type == JTokenType.Integer)
            {
                int value = (int)token;
                if (value >= 0 && value <= 6)
                {
                    worldIndex = value;
                    return true;
                }

                if (value >= 1 && value <= 7)
                {
                    worldIndex = value - 1;
                    return true;
                }

                return false;
            }

            string raw = token.Type == JTokenType.String ? (string)token : token.ToString();
            if (string.IsNullOrEmpty(raw))
            {
                return false;
            }

            string normalized = raw.Trim();
            if (normalized.StartsWith("world_", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring("world_".Length);
            }
            else if (normalized.StartsWith("world", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring("world".Length);
            }

            int parsed;
            if (!int.TryParse(normalized, out parsed))
            {
                return false;
            }

            if (parsed >= 0 && parsed <= 6)
            {
                worldIndex = parsed;
                return true;
            }

            if (parsed >= 1 && parsed <= 7)
            {
                worldIndex = parsed - 1;
                return true;
            }

            return false;
        }

        private static JToken ReadToken(JObject obj, params string[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                JToken token;
                if (obj.TryGetValue(keys[i], StringComparison.OrdinalIgnoreCase, out token))
                {
                    return token;
                }
            }

            return null;
        }
    }
}
