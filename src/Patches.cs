using HarmonyLib;

namespace CyberHookAP
{
    [HarmonyPatch(typeof(StageSelectPanel), "CreateAllStageDivs")]
    internal static class StageSelectPanel_CreateAllStageDivs_Patch
    {
        private static void Postfix(StageSelectPanel __instance)
        {
            if (CyberHookApMod.Instance != null)
            {
                CyberHookApMod.Instance.OnStageSelectOpened(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(LevelManager), "LevelInit")]
    internal static class LevelManager_LevelInit_Patch
    {
        private static void Postfix(LevelManager __instance)
        {
            if (CyberHookApMod.Instance != null)
            {
                CyberHookApMod.Instance.OnLevelInitialized(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(PlayerController), "Update")]
    internal static class PlayerController_Update_Patch
    {
        private static void Postfix(PlayerController __instance)
        {
            if (CyberHookApMod.Instance != null)
            {
                CyberHookApMod.Instance.OnPlayerUpdate(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(PlayerController), "OnLevelComplete")]
    internal static class PlayerController_OnLevelComplete_Patch
    {
        private static void Postfix(PlayerController __instance)
        {
            if (CyberHookApMod.Instance != null)
            {
                CyberHookApMod.Instance.OnLevelCompleted(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(PauseManager), "OpenVictory")]
    internal static class PauseManager_OpenVictory_Patch
    {
        private static void Postfix(float finalTime)
        {
            if (CyberHookApMod.Instance != null)
            {
                CyberHookApMod.Instance.OnVictoryOpened(finalTime);
            }
        }
    }

    [HarmonyPatch(typeof(LevelCollectible), "OnTouch")]
    internal static class LevelCollectible_OnTouch_Patch
    {
        private static void Postfix(LevelCollectible __instance)
        {
            if (CyberHookApMod.Instance != null)
            {
                CyberHookApMod.Instance.OnCollectibleTouched(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(BreakableCube), "LevelInit")]
    internal static class BreakableCube_LevelInit_Patch
    {
        private static void Postfix(BreakableCube __instance)
        {
            if (CyberHookApMod.Instance != null)
            {
                CyberHookApMod.Instance.OnCubeSeen(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(BreakableCube), "CubeExplode")]
    internal static class BreakableCube_CubeExplode_Patch
    {
        private static void Postfix(BreakableCube __instance)
        {
            if (CyberHookApMod.Instance != null)
            {
                CyberHookApMod.Instance.OnCubeBroken(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(WorldDetail), "IsUnlockedWithStars", new System.Type[0])]
    internal static class WorldDetail_IsUnlockedWithStars_NoArg_Patch
    {
        private static bool Prefix(WorldDetail __instance, ref bool __result)
        {
            return !CyberHookApMod.Instance.TryOverrideWorldUnlock(__instance, ref __result);
        }
    }

    [HarmonyPatch(typeof(WorldDetail), "IsUnlockedWithStars", new[] { typeof(int) })]
    internal static class WorldDetail_IsUnlockedWithStars_Int_Patch
    {
        private static bool Prefix(WorldDetail __instance, ref bool __result)
        {
            return !CyberHookApMod.Instance.TryOverrideWorldUnlock(__instance, ref __result);
        }
    }

    [HarmonyPatch(typeof(WorldDetail), "IsUnlocked", new[] { typeof(bool) })]
    internal static class WorldDetail_IsUnlocked_Patch
    {
        private static bool Prefix(WorldDetail __instance, ref bool __result)
        {
            return !CyberHookApMod.Instance.TryOverrideWorldUnlock(__instance, ref __result);
        }
    }

    [HarmonyPatch(typeof(SO_Level), "get_IsUnlockable")]
    internal static class SO_Level_GetIsUnlockable_Patch
    {
        private static bool Prefix(SO_Level __instance, ref bool __result)
        {
            return !CyberHookApMod.Instance.TryOverrideLevelUnlock(__instance, ref __result);
        }
    }

    [HarmonyPatch(typeof(SO_Level), "IsUnlockableConditionMet")]
    internal static class SO_Level_IsUnlockableConditionMet_Patch
    {
        private static bool Prefix(SO_Level __instance, ref bool __result)
        {
            return !CyberHookApMod.Instance.TryOverrideLevelUnlock(__instance, ref __result);
        }
    }

    [HarmonyPatch(typeof(PlayerWallRideState), "CanWallride")]
    internal static class PlayerWallRideState_CanWallride_Patch
    {
        private static void Postfix(ref bool __result)
        {
            WallRidePatchUtility.RestrictWallRide(ref __result);
        }
    }

    [HarmonyPatch(typeof(PlayerState), "InputTestWallRideStart")]
    internal static class PlayerState_InputTestWallRideStart_Patch
    {
        private static void Postfix(ref bool __result)
        {
            WallRidePatchUtility.RestrictWallRide(ref __result);
        }
    }

    [HarmonyPatch(typeof(PlayerWallRideState), "Update")]
    internal static class PlayerWallRideState_Update_Patch
    {
        private static bool Prefix(PlayerWallRideState __instance)
        {
            return WallRidePatchUtility.EnsureWallRideAllowed(__instance);
        }
    }

    [HarmonyPatch(typeof(PlayerWallRideState), "FixedUpdate")]
    internal static class PlayerWallRideState_FixedUpdate_Patch
    {
        private static bool Prefix(PlayerWallRideState __instance)
        {
            return WallRidePatchUtility.EnsureWallRideAllowed(__instance);
        }
    }

    [HarmonyPatch(typeof(PlayerState), "InputTestPullStart")]
    internal static class PlayerState_InputTestPullStart_Patch
    {
        private static void Postfix(ref bool __result)
        {
            HookPullPatchUtility.RestrictHookPull(ref __result);
        }
    }

    [HarmonyPatch(typeof(PlayerState), "InputTestPulling")]
    internal static class PlayerState_InputTestPulling_Patch
    {
        private static void Postfix(ref bool __result)
        {
            HookPullPatchUtility.RestrictHookPull(ref __result);
        }
    }

    [HarmonyPatch(typeof(PlayerState), "InputTestPullStop")]
    internal static class PlayerState_InputTestPullStop_Patch
    {
        private static void Postfix(ref bool __result)
        {
            HookPullPatchUtility.RestrictHookPull(ref __result);
        }
    }

    [HarmonyPatch(typeof(PlayerState), "InputTestRetract")]
    internal static class PlayerState_InputTestRetract_Patch
    {
        private static void Postfix(ref bool __result)
        {
        }
    }

    [HarmonyPatch(typeof(PlayerState), "OnKill")]
    internal static class PlayerState_OnKill_Patch
    {
        private static void Postfix(PlayerState __instance, UnityEngine.GameObject killer)
        {
            if (CyberHookApMod.Instance != null)
            {
                CyberHookApMod.Instance.OnPlayerKilled(__instance, killer);
            }
        }
    }

    [HarmonyPatch(typeof(PauseManager), "QuickRestart")]
    internal static class PauseManager_QuickRestart_Patch
    {
        private static void Prefix()
        {
            if (CyberHookApMod.Instance != null)
            {
                CyberHookApMod.Instance.SuppressNextResetDeathLink();
            }
        }
    }

    [HarmonyPatch(typeof(PauseManager), "RestartLevel")]
    internal static class PauseManager_RestartLevel_Patch
    {
        private static void Prefix()
        {
            if (CyberHookApMod.Instance != null)
            {
                CyberHookApMod.Instance.SuppressNextResetDeathLink();
            }
        }
    }

    [HarmonyPatch(typeof(PauseManager), "MarathonRestart")]
    internal static class PauseManager_MarathonRestart_Patch
    {
        private static void Prefix()
        {
            if (CyberHookApMod.Instance != null)
            {
                CyberHookApMod.Instance.SuppressNextResetDeathLink();
            }
        }
    }

    internal static class HookPullPatchUtility
    {
        internal static void RestrictHookPull(ref bool result)
        {
            if (!result)
            {
                return;
            }

            if (CyberHookApMod.Instance != null && !CyberHookApMod.Instance.CanHookPull())
            {
                result = false;
            }
        }
    }

    internal static class WallRidePatchUtility
    {
        internal static void RestrictWallRide(ref bool result)
        {
            if (!result)
            {
                return;
            }

            if (CyberHookApMod.Instance != null && !CyberHookApMod.Instance.CanWallSlide())
            {
                result = false;
            }
        }

        internal static bool EnsureWallRideAllowed(PlayerWallRideState state)
        {
            if (CyberHookApMod.Instance == null || CyberHookApMod.Instance.CanWallSlide())
            {
                return true;
            }

            if (state != null)
            {
                state.ForceWallRideEnd();
            }

            return false;
        }
    }

}
