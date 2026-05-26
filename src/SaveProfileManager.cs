namespace CyberHookAP
{
    // Save/profile isolation caused most of the instability during early AP work.
    // For now we keep this as a deliberate no-op so the mod always runs against
    // the normal Cyber Hook save until the AP client loop is stable.
    internal sealed class SaveProfileManager
    {
        internal SaveProfileManager(string dataDirectory)
        {
        }

        internal void Initialize(ModSettings settings)
        {
        }

        internal void OnSettingsChanged(ModSettings settings)
        {
        }

        internal void OnArchipelagoConnected(string seedName, string slotName)
        {
        }

        internal bool ConsumeInitialHubRedirect(string levelUniqueId)
        {
            return false;
        }

        internal bool PeekInitialHubRedirect(string levelUniqueId)
        {
            return false;
        }

        internal void Tick()
        {
        }

        internal bool IsUsingApProfile
        {
            get { return false; }
        }

        internal bool ShouldApplyApBootstrap
        {
            get { return false; }
        }

        internal bool TryOverridePath(SavePathKind kind, ref string result)
        {
            return false;
        }

        internal bool ApplyTutorialBypass(GameData gameData)
        {
            return false;
        }
    }

    internal enum SavePathKind
    {
        GameData,
        LevelFolder,
        ReplayRoot,
        ReplayFolder,
        MarathonFolder
    }
}
