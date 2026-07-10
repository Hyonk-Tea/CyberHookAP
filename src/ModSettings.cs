namespace CyberHookAP
{
    internal sealed class ModSettings
    {
        public bool DiscoveryMode = false;
        public bool EnableGameplayGating = true;
        public bool EnableStatusHud = true;
        public bool AutoSweepEnabled = false;
        public bool AutoSweepIncludeHubs = true;
        public bool AutoSweepSkipExistingSceneDumps = true;
        public float AutoSweepDelaySeconds = 2.5f;
        public bool EnableRankChecks = true;
        public bool EnableAnchorChecks = true;
        public bool EnableCubeChecks = true;
        public bool OverrideWorldUnlocks = true;
        public int index_worlds_at = -1;
        public bool UseIsolatedSaveProfile = false;
        public bool FinalLevelAccessible = false;
        public bool EnableDebugOverrides = false;
        public int DebugDiamonds = 0;
        public bool DebugDoubleJump = false;
        public bool DebugWallSlide = false;
        public bool DebugHookPull = false;
        public bool DebugShoot = false;
        public bool DebugSlowmo = false;
        public int DebugSlowmoLevels = 0;
        public int DebugReachLevels = 0;
        public bool SkipSaveReset = true;
        public bool RestoreLatestBackupTrigger = false;
        public bool RestoreEarliestBackupTrigger = false;
        public bool AccessibleFont = false;
        public bool ClearedLevelsDeathLink = true;
        public ArchipelagoConnectionSettings Connection = new ArchipelagoConnectionSettings();
    }

    internal sealed class ArchipelagoConnectionSettings
    {
        public bool AutoConnect = false;
        public string Server = "localhost";
        public int Port = 38281;
        public string Slot = "";
        public string Password = "";
        public string ClientUuid = "";
    }
}
