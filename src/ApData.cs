using System;
using System.Collections.Generic;

namespace CyberHookAP
{
    internal static class ApData
    {
        private static readonly HashSet<string> ExcludedLevelIds = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            "d653b614-6800-4a62-9f0b-886faf470279",
            "101e9920-1d6c-42d9-b6e8-6f7063e68e0f",
            "2752025a-35b3-4efd-ac5b-34ced9c4189f",
            "85c6cc94-f7f5-49cb-b21c-3af066b22995"
        };

        private static readonly float[] ReachMultipliers =
        {
            0.50f,
            0.75f,
            1.00f,
            1.33f,
            1.66f
        };

        private static readonly float[] SlowmoMultipliers =
        {
            0.50f,
            0.75f,
            1.00f,
            1.33f,
            1.66f
        };

        private static readonly int[] WorldUnlockThresholds =
        {
            0,
            15,
            30,
            55,
            80,
            110,
            140
        };

        internal const int MinProgressiveLevel = 0;
        internal const int MaxProgressiveLevel = 4;

        internal const int FinalWorldIndex = 6;
        internal const string OneMoreTimeLevelUniqueId = "4aab430c-3066-4207-9bf0-f266109d9978";

        internal static bool IsExcludedLevel(string levelUniqueId)
        {
            return !string.IsNullOrEmpty(levelUniqueId) && ExcludedLevelIds.Contains(levelUniqueId);
        }

        internal static int ClampProgressiveLevel(int level)
        {
            if (level < MinProgressiveLevel)
            {
                return MinProgressiveLevel;
            }

            if (level > MaxProgressiveLevel)
            {
                return MaxProgressiveLevel;
            }

            return level;
        }

        internal static float GetReachMultiplier(int level)
        {
            return ReachMultipliers[ClampProgressiveLevel(level)];
        }

        internal static float GetSlowmoMultiplier(int level)
        {
            return SlowmoMultipliers[ClampProgressiveLevel(level)];
        }

        internal static int GetWorldUnlockThresholdForPosition(int position)
        {
            if (position < 0)
            {
                return int.MaxValue;
            }

            if (position >= WorldUnlockThresholds.Length)
            {
                return WorldUnlockThresholds[WorldUnlockThresholds.Length - 1];
            }

            return WorldUnlockThresholds[position];
        }
    }
}
