using System;
using System.Collections.Generic;

namespace CyberHookAP
{
    internal sealed class ArchipelagoState
    {
        public int ReceivedDiamonds = 0;
        public bool HasDoubleJump = false;
        public bool HasWallSlide = false;
        public bool HasHookPull = false;
        public bool HasShoot = false;
        public bool HasSlowmo = false;
        public int ProgressiveSlowmoTime = 0;
        public int ProgressiveReach = 0;
        public int LastReceivedItemIndex = 0;
        public string SessionSeedName = string.Empty;
        public string SessionSlotName = string.Empty;
        public bool GoalReported = false;
        public bool HasFinalLevelComplete = false;
        public Dictionary<string, float> LevelBestTimes = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> CompletedChecks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> KnownCubeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> KnownCollectibleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public ArchipelagoState Clone()
        {
            return new ArchipelagoState
            {
                ReceivedDiamonds = ReceivedDiamonds,
                HasDoubleJump = HasDoubleJump,
                HasWallSlide = HasWallSlide,
                HasHookPull = HasHookPull,
                HasShoot = HasShoot,
                HasSlowmo = HasSlowmo,
                ProgressiveSlowmoTime = ProgressiveSlowmoTime,
                ProgressiveReach = ProgressiveReach,
                LastReceivedItemIndex = LastReceivedItemIndex,
                SessionSeedName = SessionSeedName,
                SessionSlotName = SessionSlotName,
                GoalReported = GoalReported,
                HasFinalLevelComplete = HasFinalLevelComplete,
                LevelBestTimes = new Dictionary<string, float>(LevelBestTimes, StringComparer.OrdinalIgnoreCase),
                CompletedChecks = new HashSet<string>(CompletedChecks, StringComparer.OrdinalIgnoreCase),
                KnownCubeIds = new HashSet<string>(KnownCubeIds, StringComparer.OrdinalIgnoreCase),
                KnownCollectibleIds = new HashSet<string>(KnownCollectibleIds, StringComparer.OrdinalIgnoreCase)
            };
        }

        public string BuildSignature()
        {
            return string.Join(
                "|",
                ReceivedDiamonds.ToString(),
                HasDoubleJump ? "1" : "0",
                HasWallSlide ? "1" : "0",
                HasHookPull ? "1" : "0",
                HasShoot ? "1" : "0",
                HasSlowmo ? "1" : "0",
                ProgressiveSlowmoTime.ToString(),
                ProgressiveReach.ToString());
        }

        public void ResetProgression()
        {
            ReceivedDiamonds = 0;
            HasDoubleJump = false;
            HasWallSlide = false;
            HasHookPull = false;
            HasShoot = false;
            HasSlowmo = false;
            ProgressiveSlowmoTime = 0;
            ProgressiveReach = 0;
            LastReceivedItemIndex = 0;
            GoalReported = false;
            HasFinalLevelComplete = false;
            LevelBestTimes.Clear();
        }

        public void ResetForNewSession(string seedName, string slotName)
        {
            ResetProgression();
            SessionSeedName = seedName ?? string.Empty;
            SessionSlotName = slotName ?? string.Empty;
            CompletedChecks.Clear();
        }
    }
}
