using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace CyberHookAP
{
    internal sealed class ArchipelagoSlotData
    {
        private const int FinalWorldIndex = 6;

        public int HighestRequiredRank = 0;
        public string GoalMode = string.Empty;
        public int GoalDiamondCount = 0;
        public HashSet<string> GoalLevelIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public List<int> WorldUnlockOrder = new List<int>();
        public bool DeathLinkEnabled = false;
        public int DeathLinkAmnesty = 0;

        internal static ArchipelagoSlotData FromToken(JToken slotDataToken, GeneratedApData data, int indexWorldsAt)
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
                indexWorldsAt,
                obj,
                "world_order",
                "worldOrder",
                "world_unlock_order",
                "worldUnlockOrder",
                "shuffled_world_order",
                "shuffledWorldOrder");
            if (result.WorldUnlockOrder.Count == 0)
            {
                result.WorldUnlockOrder = ReadOrderedWorldFields(obj, indexWorldsAt);
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

        private static List<int> ReadWorldOrder(int indexWorldsAt, JObject obj, params string[] keys)
        {
            JToken token = ReadToken(obj, keys);
            if (token == null || token.Type != JTokenType.Array)
            {
                return new List<int>();
            }

            List<int> rawValues = new List<int>();
            foreach (JToken entry in token)
            {
                int rawValue;
                if (TryReadRawWorldNumber(entry, out rawValue))
                {
                    rawValues.Add(rawValue);
                }
            }

            return NormalizeWorldOrder(rawValues, indexWorldsAt);
        }

        private static List<int> ReadOrderedWorldFields(JObject obj, int indexWorldsAt)
        {
            List<int> rawValues = new List<int>();
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
                int rawValue;
                if (!TryReadRawWorldNumber(token, out rawValue))
                {
                    return new List<int>();
                }

                rawValues.Add(rawValue);
            }

            return NormalizeWorldOrder(rawValues, indexWorldsAt);
        }

        private static List<int> NormalizeWorldOrder(List<int> rawValues, int indexWorldsAt)
        {
            List<int> result = new List<int>();
            if (rawValues == null || rawValues.Count == 0)
            {
                return result;
            }

            bool oneBased = DetermineOneBased(rawValues, indexWorldsAt);
            for (int i = 0; i < rawValues.Count; i++)
            {
                int worldIndex = oneBased ? rawValues[i] - 1 : rawValues[i];
                if (worldIndex < 0 || worldIndex > FinalWorldIndex || result.Contains(worldIndex))
                {
                    return new List<int>();
                }

                result.Add(worldIndex);
            }

            if (result.Count == 6)
            {
                AppendMissingWorld(result);
            }

            if (result.Count != 7)
            {
                return new List<int>();
            }

            ForceFinalWorldLast(result);
            return result;
        }

        private static bool DetermineOneBased(List<int> rawValues, int indexWorldsAt)
        {
            if (indexWorldsAt == 0)
            {
                return false;
            }

            if (indexWorldsAt > 0)
            {
                return true;
            }

            bool containsZero = false;
            bool containsSeven = false;
            for (int i = 0; i < rawValues.Count; i++)
            {
                containsZero = containsZero || rawValues[i] == 0;
                containsSeven = containsSeven || rawValues[i] == 7;
            }

            if (containsZero)
            {
                return false;
            }

            if (containsSeven)
            {
                return true;
            }

            return true;
        }

        private static void AppendMissingWorld(List<int> result)
        {
            for (int worldIndex = 0; worldIndex <= FinalWorldIndex; worldIndex++)
            {
                if (worldIndex == FinalWorldIndex)
                {
                    continue;
                }

                if (!result.Contains(worldIndex))
                {
                    result.Add(worldIndex);
                    return;
                }
            }

            if (!result.Contains(FinalWorldIndex))
            {
                result.Add(FinalWorldIndex);
            }
        }

        private static void ForceFinalWorldLast(List<int> result)
        {
            if (result == null)
            {
                return;
            }

            result.Remove(FinalWorldIndex);
            result.Add(FinalWorldIndex);
        }

        private static bool TryReadRawWorldNumber(JToken token, out int rawValue)
        {
            rawValue = -1;
            if (token == null)
            {
                return false;
            }

            if (token.Type == JTokenType.Integer)
            {
                rawValue = (int)token;
                return true;
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

            return int.TryParse(normalized, out rawValue);
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
