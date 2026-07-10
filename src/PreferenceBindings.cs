using MelonLoader;

namespace CyberHookAP
{
    internal sealed class PreferenceBindings
    {
        private readonly MelonPreferences_Category _mainCategory;
        private readonly MelonPreferences_Category _clientCategory;

        private readonly MelonPreferences_Entry<bool> _autoConnect;
        private readonly MelonPreferences_Entry<string> _server;
        private readonly MelonPreferences_Entry<int> _port;
        private readonly MelonPreferences_Entry<string> _slot;
        private readonly MelonPreferences_Entry<string> _password;
        private readonly MelonPreferences_Entry<bool> _accessibleFont;

        private readonly MelonPreferences_Entry<bool> _enableGameplayGating;
        private readonly MelonPreferences_Entry<bool> _overrideWorldUnlocks;
        private readonly MelonPreferences_Entry<bool> _finalLevelAccessible;
        private readonly MelonPreferences_Entry<bool> _clearedLevelsDeathLink;

        internal PreferenceBindings(ModSettings settings)
        {
            _mainCategory = MelonPreferences.CreateCategory("CyberHookAP", "CyberHookAP");
            _clientCategory = MelonPreferences.CreateCategory("CyberHookAP.Client", "CyberHookAP Advanced");

            _autoConnect = _mainCategory.CreateEntry(
                "AutoConnect",
                settings.Connection.AutoConnect,
                "Auto Connect",
                "Automatically connect to the configured Archipelago server at startup.",
                false,
                false,
                null);
            _server = _mainCategory.CreateEntry(
                "Server",
                settings.Connection.Server,
                "Server",
                "Archipelago server hostname or IP address.",
                false,
                false,
                null);
            _port = _mainCategory.CreateEntry(
                "Port",
                settings.Connection.Port,
                "Port",
                "Archipelago server port.",
                false,
                false,
                null);
            _slot = _mainCategory.CreateEntry(
                "Slot",
                settings.Connection.Slot,
                "Slot",
                "Archipelago slot name.",
                false,
                false,
                null);
            _password = _mainCategory.CreateEntry(
                "Password",
                settings.Connection.Password,
                "Password",
                "Archipelago room password. (Optional)",
                false,
                false,
                null);
            _accessibleFont = _mainCategory.CreateEntry(
                "AccessibleFont",
                settings.AccessibleFont,
                "Accessible Font",
                "Use the default Unity font instead of the terminal font. Should help with accessibility.",
                false,
                false,
                null);

            _enableGameplayGating = _clientCategory.CreateEntry(
                "EnableGameplayGating",
                settings.EnableGameplayGating,
                "Enable Gameplay Gating",
                "Apply received AP progression for abilities and stars.",
                true,
                false,
                null);
            _overrideWorldUnlocks = _clientCategory.CreateEntry(
                "OverrideWorldUnlocks",
                settings.OverrideWorldUnlocks,
                "Override World Unlocks",
                "Override the recieved AP progression for world unlocks.",
                true,
                false,
                null);
            _finalLevelAccessible = _clientCategory.CreateEntry(
                "FinalLevelAccessible",
                settings.FinalLevelAccessible,
                "Final Level Accessible",
                "Make the final level (One More Time) accessible regardless of which levels have been cleared in World 7.",
                true,
                false,
                null);
            _clearedLevelsDeathLink = _clientCategory.CreateEntry(
                "ClearedLevelsDeathLink",
                settings.ClearedLevelsDeathLink,
                "Cleared Levels DeathLink",
                "When enabled, deaths in cleared levels still count towards DeathLink. When disabled, only deaths in uncleared levels send DeathLink.",
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
            settings.AccessibleFont = _accessibleFont.Value;

            settings.EnableGameplayGating = _enableGameplayGating.Value;
            settings.OverrideWorldUnlocks = _overrideWorldUnlocks.Value;
            settings.FinalLevelAccessible = _finalLevelAccessible.Value;
            settings.ClearedLevelsDeathLink = _clearedLevelsDeathLink.Value;
        }
    }
}
