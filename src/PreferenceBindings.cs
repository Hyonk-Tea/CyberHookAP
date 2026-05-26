using MelonLoader;

namespace CyberHookAP
{
    internal sealed class PreferenceBindings
    {
        private readonly MelonPreferences_Category _connectionCategory;
        private readonly MelonPreferences_Category _clientCategory;
        private readonly MelonPreferences_Category _debugCategory;

        private readonly MelonPreferences_Entry<bool> _autoConnect;
        private readonly MelonPreferences_Entry<string> _server;
        private readonly MelonPreferences_Entry<int> _port;
        private readonly MelonPreferences_Entry<string> _slot;
        private readonly MelonPreferences_Entry<string> _password;

        private readonly MelonPreferences_Entry<bool> _discoveryMode;
        private readonly MelonPreferences_Entry<bool> _enableGameplayGating;
        private readonly MelonPreferences_Entry<bool> _enableStatusHud;
        private readonly MelonPreferences_Entry<bool> _enableRankChecks;
        private readonly MelonPreferences_Entry<bool> _enableAnchorChecks;
        private readonly MelonPreferences_Entry<bool> _enableCubeChecks;
        private readonly MelonPreferences_Entry<bool> _overrideWorldUnlocks;
        private readonly MelonPreferences_Entry<bool> _autoSweepEnabled;
        private readonly MelonPreferences_Entry<bool> _autoSweepIncludeHubs;
        private readonly MelonPreferences_Entry<bool> _autoSweepSkipExistingSceneDumps;
        private readonly MelonPreferences_Entry<float> _autoSweepDelaySeconds;

        private readonly MelonPreferences_Entry<int> _debugDiamonds;
        private readonly MelonPreferences_Entry<bool> _debugDoubleJump;
        private readonly MelonPreferences_Entry<bool> _debugWallSlide;
        private readonly MelonPreferences_Entry<bool> _debugHookPull;
        private readonly MelonPreferences_Entry<bool> _debugShoot;
        private readonly MelonPreferences_Entry<bool> _debugSlowmo;
        private readonly MelonPreferences_Entry<int> _debugSlowmoLevels;
        private readonly MelonPreferences_Entry<int> _debugReachLevels;

        internal PreferenceBindings(ModSettings settings)
        {
            _connectionCategory = MelonPreferences.CreateCategory("CyberHookAP.Connection", "CyberHookAP Connection");
            _clientCategory = MelonPreferences.CreateCategory("CyberHookAP.Client", "CyberHookAP Client");
            _debugCategory = MelonPreferences.CreateCategory("CyberHookAP.Debug", "CyberHookAP Debug", true, false);

            _autoConnect = _connectionCategory.CreateEntry(
                "AutoConnect",
                settings.Connection.AutoConnect,
                "Auto Connect",
                "Automatically connect to the configured Archipelago server at startup.",
                false,
                false,
                null);
            _server = _connectionCategory.CreateEntry(
                "Server",
                settings.Connection.Server,
                "Server",
                "Archipelago server hostname or IP address.",
                false,
                false,
                null);
            _port = _connectionCategory.CreateEntry(
                "Port",
                settings.Connection.Port,
                "Port",
                "Archipelago websocket port.",
                false,
                false,
                null);
            _slot = _connectionCategory.CreateEntry(
                "Slot",
                settings.Connection.Slot,
                "Slot",
                "Slot or player name used when connecting.",
                false,
                false,
                null);
            _password = _connectionCategory.CreateEntry(
                "Password",
                settings.Connection.Password,
                "Password",
                "Optional Archipelago room password.",
                false,
                false,
                null);

            _discoveryMode = _clientCategory.CreateEntry(
                "DiscoveryMode",
                settings.DiscoveryMode,
                "Discovery Mode",
                "When enabled, the mod logs metadata and scene contents instead of acting like a release client.",
                true,
                false,
                null);
            _enableGameplayGating = _clientCategory.CreateEntry(
                "EnableGameplayGating",
                settings.EnableGameplayGating,
                "Enable Gameplay Gating",
                "Applies received AP progression to abilities and world unlocks.",
                true,
                false,
                null);
            _enableStatusHud = _clientCategory.CreateEntry(
                "EnableStatusHud",
                settings.EnableStatusHud,
                "Enable Status HUD",
                "Shows the development status HUD in the top-left corner.",
                true,
                false,
                null);
            _enableRankChecks = _clientCategory.CreateEntry(
                "EnableRankChecks",
                settings.EnableRankChecks,
                "Enable Rank Checks",
                "Sends clear and diamond-rank checks on results screen.",
                true,
                false,
                null);
            _enableAnchorChecks = _clientCategory.CreateEntry(
                "EnableAnchorChecks",
                settings.EnableAnchorChecks,
                "Enable Anchor Checks",
                "Sends numero anchor checks when anchors are touched.",
                true,
                false,
                null);
            _enableCubeChecks = _clientCategory.CreateEntry(
                "EnableCubeChecks",
                settings.EnableCubeChecks,
                "Enable Cube Checks",
                "Sends cube checks when breakable cubes are destroyed.",
                true,
                false,
                null);
            _overrideWorldUnlocks = _clientCategory.CreateEntry(
                "OverrideWorldUnlocks",
                settings.OverrideWorldUnlocks,
                "Override World Unlocks",
                "Uses received AP diamonds instead of vanilla local progression for world access.",
                true,
                false,
                null);
            _autoSweepEnabled = _clientCategory.CreateEntry(
                "AutoSweepEnabled",
                settings.AutoSweepEnabled,
                "Auto Sweep Enabled",
                "Automatically loads undiscovered levels in discovery mode.",
                true,
                false,
                null);
            _autoSweepIncludeHubs = _clientCategory.CreateEntry(
                "AutoSweepIncludeHubs",
                settings.AutoSweepIncludeHubs,
                "Auto Sweep Include Hubs",
                "Whether discovery auto-sweep should also visit hub scenes.",
                true,
                false,
                null);
            _autoSweepSkipExistingSceneDumps = _clientCategory.CreateEntry(
                "AutoSweepSkipExistingSceneDumps",
                settings.AutoSweepSkipExistingSceneDumps,
                "Auto Sweep Skip Existing Dumps",
                "Skips levels that already have a scene dump during discovery sweep.",
                true,
                false,
                null);
            _autoSweepDelaySeconds = _clientCategory.CreateEntry(
                "AutoSweepDelaySeconds",
                settings.AutoSweepDelaySeconds,
                "Auto Sweep Delay Seconds",
                "Time to wait after each auto-swept level loads.",
                true,
                false,
                null);

            _debugDiamonds = _debugCategory.CreateEntry(
                "DebugDiamonds",
                settings.DebugDiamonds,
                "Debug Diamonds",
                "Local development override for received diamond count.",
                true,
                false,
                null);
            _debugDoubleJump = _debugCategory.CreateEntry(
                "DebugDoubleJump",
                settings.DebugDoubleJump,
                "Debug Double Jump",
                "Local development override for air dash ownership.",
                true,
                false,
                null);
            _debugWallSlide = _debugCategory.CreateEntry(
                "DebugWallSlide",
                settings.DebugWallSlide,
                "Debug Wall Slide",
                "Local development override for wall ride ownership.",
                true,
                false,
                null);
            _debugHookPull = _debugCategory.CreateEntry(
                "DebugHookPull",
                settings.DebugHookPull,
                "Debug Hook Pull",
                "Local development override for hook pull ownership.",
                true,
                false,
                null);
            _debugShoot = _debugCategory.CreateEntry(
                "DebugShoot",
                settings.DebugShoot,
                "Debug Shoot",
                "Local development override for shooting ownership.",
                true,
                false,
                null);
            _debugSlowmo = _debugCategory.CreateEntry(
                "DebugSlowmo",
                settings.DebugSlowmo,
                "Debug Slowmo",
                "Local development override for slowmo ownership.",
                true,
                false,
                null);
            _debugSlowmoLevels = _debugCategory.CreateEntry(
                "DebugSlowmoLevels",
                settings.DebugSlowmoLevels,
                "Debug Slowmo Levels",
                "Local development override for progressive slowmo level.",
                true,
                false,
                null);
            _debugReachLevels = _debugCategory.CreateEntry(
                "DebugReachLevels",
                settings.DebugReachLevels,
                "Debug Reach Levels",
                "Local development override for progressive reach level.",
                true,
                false,
                null);
        }

        internal void ApplyTo(ModSettings settings)
        {
            settings.Connection.AutoConnect = _autoConnect.Value;
            settings.Connection.Server = _server.Value ?? string.Empty;
            settings.Connection.Port = _port.Value;
            settings.Connection.Slot = _slot.Value ?? string.Empty;
            settings.Connection.Password = _password.Value ?? string.Empty;

            settings.DiscoveryMode = _discoveryMode.Value;
            settings.EnableGameplayGating = _enableGameplayGating.Value;
            settings.EnableStatusHud = _enableStatusHud.Value;
            settings.EnableRankChecks = _enableRankChecks.Value;
            settings.EnableAnchorChecks = _enableAnchorChecks.Value;
            settings.EnableCubeChecks = _enableCubeChecks.Value;
            settings.OverrideWorldUnlocks = _overrideWorldUnlocks.Value;
            settings.UseIsolatedSaveProfile = false;
            settings.AutoSweepEnabled = _autoSweepEnabled.Value;
            settings.AutoSweepIncludeHubs = _autoSweepIncludeHubs.Value;
            settings.AutoSweepSkipExistingSceneDumps = _autoSweepSkipExistingSceneDumps.Value;
            settings.AutoSweepDelaySeconds = _autoSweepDelaySeconds.Value;

            settings.DebugDiamonds = _debugDiamonds.Value;
            settings.DebugDoubleJump = _debugDoubleJump.Value;
            settings.DebugWallSlide = _debugWallSlide.Value;
            settings.DebugHookPull = _debugHookPull.Value;
            settings.DebugShoot = _debugShoot.Value;
            settings.DebugSlowmo = _debugSlowmo.Value;
            settings.DebugSlowmoLevels = ApData.ClampProgressiveLevel(_debugSlowmoLevels.Value);
            settings.DebugReachLevels = ApData.ClampProgressiveLevel(_debugReachLevels.Value);
        }
    }
}
