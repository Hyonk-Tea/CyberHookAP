using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using MelonLoader;
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json.Linq;

[assembly: MelonInfo(typeof(CyberHookAP.CyberHookApMod), "CyberHookAP", "0.1.0", "CyberHookAP")]
[assembly: MelonGame(null, "Cyber Hook")]

namespace CyberHookAP
{
    public sealed class CyberHookApMod : MelonMod
    {
        private static readonly FieldInfo AllowAirDashField =
            AccessTools.Field(typeof(SO_PlayerData), "_allowAirDash");

        private static readonly FieldInfo CanAirDashField =
            AccessTools.Field(typeof(SO_PlayerData), "_canAirDash");

        private static readonly FieldInfo CanSlowTimeField =
            AccessTools.Field(typeof(SO_PlayerData), "_canSlowTime");

        private static readonly FieldInfo ActiveEnergyField =
            AccessTools.Field(typeof(SO_PlayerData), "_energy");

        private static readonly MethodInfo ChangeGravityMethod =
            AccessTools.Method(typeof(PlayerController), "ChangeGravity");

        private static readonly MethodInfo RemoveGravityModifiersMethod =
            AccessTools.Method(typeof(PlayerController), "RemoveGravityModifiers");

        private static readonly PropertyInfo ActivePlayerStateProperty =
            AccessTools.Property(typeof(PlayerController), "ActivePlayerState");

        private static readonly PropertyInfo PlayerStateGravityProperty =
            AccessTools.Property(typeof(PlayerState), "Gravity");

        private static readonly FieldInfo HandRootField =
            AccessTools.Field(typeof(PlayerController), "HandRoot");

        private static readonly FieldInfo GunAnimatorField =
            AccessTools.Field(typeof(PlayerController), "GunAnimator");

        private static readonly FieldInfo AimTransformField =
            AccessTools.Field(typeof(PlayerController), "AimTransform");

        private static readonly FieldInfo FPSRenderersField =
            AccessTools.Field(typeof(PlayerController), "FPSRenderers");

        private static readonly MethodInfo PlayerControllerResetHandPositionMethod =
            AccessTools.Method(typeof(PlayerController), "ResetHandPosition");

        private static readonly MethodInfo PlayerStateOnKillMethod =
            AccessTools.Method(typeof(PlayerState), "OnKill");

        private static readonly MethodInfo PlayerStateOnShootMethod =
            AccessTools.Method(typeof(PlayerState), "OnShoot");

        private static readonly MethodInfo PlayerStateActionShootMethod =
            AccessTools.Method(typeof(PlayerState), "ActionShoot");

        private enum TrapKind
        {
            None,
            RandomGravityDirection,
            IncreasedGravity
        }

        private sealed class TrapRuntime
        {
            public TrapKind Kind;
            public string Name;
            public float RemainingSeconds;
            public float MagnitudeScale;
            public Vector3 Direction;
            public Vector3 RestoreGravity;
            public bool HasRestoreGravity;
            public bool IsApplied;
            public PlayerController AppliedPlayer;
        }

        private static CyberHookApMod _instance;
        private JsonFileStore<ModSettings> _settingsStore;
        private JsonFileStore<ArchipelagoState> _stateStore;
        private PreferenceBindings _preferences;
        private GeneratedApData _generatedData;
        private ArchipelagoClient _apClient;
        private ArchipelagoSlotData _slotData;
        private SaveProfileManager _saveProfiles;
        private EventPopupUi _popupUi;
        private Levelpack _cachedLevelpack;
        private float _baseEnergyMax;
        private float _baseHookRange;
        private float _baseTimeWarpChainBonus;
        private bool _capturedBaseValues;
        private PlayerController _lastAppliedPlayer;
        private string _lastAppliedSignature = string.Empty;
        private bool _autoSweepActive;
        private bool _autoSweepWaitingForLevelInit;
        private object _autoSweepCoroutine;
        private string _autoSweepCurrentLevelId = string.Empty;
        private GUIStyle _hudBoxStyle;
        private GUIStyle _hudLabelStyle;
        private GUIStyle _hudValueStyle;
        private GUIStyle _hudButtonStyle;
        private bool _showDebugMenu;
        private TrapRuntime _activeTrap;
        private TrapRuntime _queuedTrap;
        private int _deathLinkAmnestyCounter;
        private bool _suppressOutgoingDeathLink;
        private int _suppressedResetDeathLinks;
        private bool _deathLinkSequenceActive;
        private bool _pendingIncomingDeathLink;
        private string _pendingDeathLinkMessage = string.Empty;
        private bool _shootPoseCaptureActive;
        private float _shootPoseCaptureRemainingSeconds;
        private Quaternion _shootPoseCaptureOrigin = Quaternion.identity;
        private Quaternion _shootPoseCaptureBest = Quaternion.identity;
        private Vector3 _shootPoseCaptureBestPosition = Vector3.zero;
        private Quaternion _shootPoseCaptureBestAimRotation = Quaternion.identity;
        private Vector3 _shootPoseCaptureBestAimPosition = Vector3.zero;
        private Quaternion _shootPoseCaptureBestHandParentRotation = Quaternion.identity;
        private Vector3 _shootPoseCaptureBestHandParentPosition = Vector3.zero;
        private float _shootPoseCaptureBestAngle;
        private bool _hasCapturedShootPose;
        private Quaternion _capturedShootPose = Quaternion.identity;
        private Vector3 _capturedShootPosition = Vector3.zero;
        private Quaternion _capturedAimRotation = Quaternion.identity;
        private Vector3 _capturedAimPosition = Vector3.zero;
        private Quaternion _capturedHandParentRotation = Quaternion.identity;
        private Vector3 _capturedHandParentPosition = Vector3.zero;
        private bool _hasCapturedGunAnimatorState;
        private int _capturedGunAnimatorStateHash;
        private float _capturedGunAnimatorNormalizedTime;
        private bool _deathLinkPoseOverrideActive;
        private PlayerController _deathLinkPosePlayer;
        private Quaternion _deathLinkPoseLocalRotation = Quaternion.identity;
        private Vector3 _deathLinkPoseLocalPosition = Vector3.zero;
        private Quaternion _deathLinkAimLocalRotation = Quaternion.identity;
        private Vector3 _deathLinkAimLocalPosition = Vector3.zero;
        private Quaternion _deathLinkHandParentLocalRotation = Quaternion.identity;
        private Vector3 _deathLinkHandParentLocalPosition = Vector3.zero;
        private readonly Queue<string> _powerupAnimationQueue = new Queue<string>();
        private bool _powerupAnimationActive;

        internal static CyberHookApMod Instance
        {
            get { return _instance; }
        }

        internal static string DataDirectory
        {
            get { return Path.Combine(Environment.CurrentDirectory, "UserData", "CyberHookAP"); }
        }

        internal static string ProjectDirectory
        {
            get { return Path.Combine(Environment.CurrentDirectory, "CyberHookAP"); }
        }

        internal ModSettings Settings { get; private set; }

        internal ArchipelagoState State { get; private set; }

        internal DiscoveryExporter Discovery { get; private set; }

        public override void OnInitializeMelon()
        {
            _instance = this;
            Directory.CreateDirectory(DataDirectory);

            _settingsStore = new JsonFileStore<ModSettings>(Path.Combine(DataDirectory, "settings.json"));
            _stateStore = new JsonFileStore<ArchipelagoState>(Path.Combine(DataDirectory, "state.json"));
            Settings = _settingsStore.LoadOrCreate();
            State = _stateStore.LoadOrCreate();
            Discovery = new DiscoveryExporter(DataDirectory);
            _preferences = new PreferenceBindings(Settings);
            SyncSettingsFromPreferences();
            _generatedData = GeneratedApData.Load(ProjectDirectory);
            _saveProfiles = new SaveProfileManager(DataDirectory);
            _saveProfiles.Initialize(Settings);
            InitializeArchipelagoClient();
            _settingsStore.Save(Settings);
            _stateStore.Save(State);

            SafeLog("CyberHookAP initialized.");
            SafeLog(
                "DiscoveryMode=" + Settings.DiscoveryMode
                + ", EnableGameplayGating=" + Settings.EnableGameplayGating
                + ", AutoSweepEnabled=" + Settings.AutoSweepEnabled
                + ", OverrideWorldUnlocks=" + Settings.OverrideWorldUnlocks);
        }

        public override void OnApplicationQuit()
        {
            if (_apClient != null)
            {
                _apClient.Disconnect(true, string.Empty);
            }

            PersistState();
        }

        public override void OnUpdate()
        {
            EnsurePopupUi();

            if (_apClient != null)
            {
                _apClient.Tick();
                _apClient.EnsureConnected(Settings);
            }

            if (_saveProfiles != null)
            {
                _saveProfiles.Tick();
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.F2))
            {
                TriggerLocalDeathLinkPreview();
            }

            UpdateTrapRuntime();
            UpdateDeathLinkRuntime();
            UpdatePowerupAnimationRuntime();
            TryReportGoalCompletion();
        }

        public override void OnPreferencesLoaded()
        {
            SyncSettingsFromPreferences();
        }

        public override void OnPreferencesSaved()
        {
            SyncSettingsFromPreferences();
        }

        public override void OnGUI()
        {
            if (Event.current != null
                && Event.current.type == EventType.KeyDown
                && Event.current.keyCode == KeyCode.BackQuote)
            {
                _showDebugMenu = !_showDebugMenu;
                Event.current.Use();
            }

            if (Settings == null || !_showDebugMenu)
            {
                return;
            }

            EnsureHudStyles();

            ArchipelagoState effective = GetEffectiveState(); 
            float x = 16f;
            float y = 16f;
            float width = 292f;
            float height = 448f;

            GUI.Box(new Rect(x, y, width, height), string.Empty, _hudBoxStyle);

            float lineY = y + 10f;
            DrawHudLine(x + 12f, ref lineY, "CyberHookAP", "DEBUG");
            DrawHudLine(x + 12f, ref lineY, "AP", _apClient != null ? _apClient.StatusText : "OFF");
            DrawHudLine(x + 12f, ref lineY, "Gating", ShouldGateGameplay() ? "ON" : "OFF");
            DrawHudLine(x + 12f, ref lineY, "Trap", GetTrapSummary());
            DrawHudToggleRow(x + 12f, ref lineY, "Air Dash", effective.HasDoubleJump, "DoubleJump");
            DrawHudToggleRow(x + 12f, ref lineY, "Wall Slide", effective.HasWallSlide, "WallSlide");
            DrawHudToggleRow(x + 12f, ref lineY, "Hook Pull", effective.HasHookPull, "HookPull");
            DrawHudToggleRow(x + 12f, ref lineY, "Finger Gun", effective.HasShoot, "Shoot");
            DrawHudToggleRow(x + 12f, ref lineY, "Time Warp", effective.HasSlowmo, "Slowmo");
            DrawHudAdjustRow(x + 12f, ref lineY, "Time Warp Lv", effective.ProgressiveSlowmoTime.ToString(), "SlowmoLevels", -1, +1);
            DrawHudAdjustRow(x + 12f, ref lineY, "Hook Reach Lv", effective.ProgressiveReach.ToString(), "ReachLevels", -1, +1);

            if (GUI.Button(new Rect(x + 12f, lineY, 120f, 24f), "Reset Debug", _hudButtonStyle))
            {
                ResetDebugOverrides();
            }

            lineY += 32f;
            DrawHudAdjustRow(x + 12f, ref lineY, "Stars", effective.ReceivedDiamonds.ToString(), "Diamonds", -1, +1);

            lineY += 12f;
            DrawCurrentLevelStarTimes(x + 12f, ref lineY);

            lineY += 8f;
            GUI.Label(new Rect(x + 12f, lineY, 180f, 20f), "Trap Testing", _hudLabelStyle);
            lineY += 22f;
            DrawTrapButtonRow(x + 12f, ref lineY, "Gravity Dir", TrapKind.RandomGravityDirection);
            DrawTrapButtonRow(x + 12f, ref lineY, "Gravity Up", TrapKind.IncreasedGravity);
            if (GUI.Button(new Rect(x + 12f, lineY - 1f, 120f, 22f), "Clear Trap", _hudButtonStyle))
            {
                ClearTrapState(true);
            }
            lineY += 30f;

            GUI.Label(new Rect(x + 12f, lineY, 180f, 20f), "DeathLink", _hudLabelStyle);
            lineY += 22f;
            if (GUI.Button(new Rect(x + 12f, lineY - 1f, 120f, 22f), "Test DeathLink", _hudButtonStyle))
            {
                TriggerLocalDeathLinkPreview();
            }
            lineY += 30f;
        }

        internal static void SafeLog(string message)
        {
            MelonLogger.Msg("[CyberHookAP] " + message);
        }

        internal void PersistState()
        {
            if (_stateStore != null && State != null)
            {
                _stateStore.Save(State);
            }
        }

        internal ArchipelagoState GetEffectiveState()
        {
            ArchipelagoState effective = State.Clone();

            if (Settings.DebugDiamonds > effective.ReceivedDiamonds)
            {
                effective.ReceivedDiamonds = Settings.DebugDiamonds;
            }

            effective.HasDoubleJump = effective.HasDoubleJump || Settings.DebugDoubleJump;
            effective.HasWallSlide = effective.HasWallSlide || Settings.DebugWallSlide;
            effective.HasHookPull = effective.HasHookPull || Settings.DebugHookPull;
            effective.HasShoot = effective.HasShoot || Settings.DebugShoot;
            effective.HasSlowmo = effective.HasSlowmo || Settings.DebugSlowmo;

            if (Settings.DebugSlowmoLevels > effective.ProgressiveSlowmoTime)
            {
                effective.ProgressiveSlowmoTime = Settings.DebugSlowmoLevels;
            }

            if (Settings.DebugReachLevels > effective.ProgressiveReach)
            {
                effective.ProgressiveReach = Settings.DebugReachLevels;
            }

            return effective;
        }

        private void UpdateTrapRuntime()
        {
            if (_activeTrap == null)
            {
                TryActivateQueuedTrap();
                return;
            }

            PlayerController player;
            if (!TryGetPlayablePlayer(out player))
            {
                _activeTrap.IsApplied = false;
                _activeTrap.AppliedPlayer = null;
                return;
            }

            if (!_activeTrap.IsApplied || !ReferenceEquals(_activeTrap.AppliedPlayer, player))
            {
                ApplyTrapToPlayer(_activeTrap, player);
            }

            _activeTrap.RemainingSeconds -= Time.unscaledDeltaTime;
            if (_activeTrap.RemainingSeconds <= 0f)
            {
                ClearTrapState(true);
                TryActivateQueuedTrap();
            }
        }

        private void UpdateDeathLinkRuntime()
        {
            if (_deathLinkSequenceActive || !_pendingIncomingDeathLink)
            {
                return;
            }

            PlayerController player;
            if (!TryGetPlayablePlayer(out player))
            {
                return;
            }

            _pendingIncomingDeathLink = false;
            MelonCoroutines.Start(DeathLinkSequenceCoroutine(player, _pendingDeathLinkMessage));
        }

        private void TriggerLocalDeathLinkPreview()
        {
            if (_deathLinkSequenceActive)
            {
                SafeLog("DeathLink preview ignored because a DeathLink sequence is already active.");
                return;
            }

            PlayerController player;
            if (!TryGetPlayablePlayer(out player))
            {
                SafeLog("DeathLink preview is only available in an active non-hub level.");
                return;
            }

            _pendingIncomingDeathLink = false;
            _pendingDeathLinkMessage = "Local DeathLink preview.";
            MelonCoroutines.Start(DeathLinkSequenceCoroutine(player, _pendingDeathLinkMessage));
        }

        private void TryActivateQueuedTrap()
        {
            if (_queuedTrap == null)
            {
                return;
            }

            PlayerController player;
            if (!TryGetPlayablePlayer(out player))
            {
                return;
            }

            _activeTrap = _queuedTrap;
            _queuedTrap = null;
            ApplyTrapToPlayer(_activeTrap, player);
            SafeLog(
                "Activated trap: "
                + _activeTrap.Name
                + " ("
                + _activeTrap.RemainingSeconds.ToString("0.0")
                + "s)");
        }

        private void QueueTrap(TrapKind kind, string itemName)
        {
            TrapRuntime trap = BuildTrap(kind, itemName);
            if (trap == null)
            {
                return;
            }

            if (_activeTrap == null)
            {
                _queuedTrap = trap;
                TryActivateQueuedTrap();
                return;
            }

            _queuedTrap = trap;
            SafeLog("Queued trap: " + trap.Name);
        }

        private TrapRuntime BuildTrap(TrapKind kind, string itemName)
        {
            TrapRuntime trap = new TrapRuntime();
            trap.Kind = kind;
            trap.Name = itemName ?? kind.ToString();
            trap.Direction = Vector3.zero;
            trap.MagnitudeScale = 1f;

            switch (kind)
            {
                case TrapKind.RandomGravityDirection:
                    trap.RemainingSeconds = UnityEngine.Random.Range(2f, 10f);
                    trap.MagnitudeScale = 1f;
                    break;
                case TrapKind.IncreasedGravity:
                    trap.RemainingSeconds = UnityEngine.Random.Range(2f, 10f);
                    trap.MagnitudeScale = UnityEngine.Random.Range(0.25f, 2.5f);
                    break;
                default:
                    return null;
            }

            return trap;
        }

        private void ApplyTrapToPlayer(TrapRuntime trap, PlayerController player)
        {
            if (trap == null || player == null)
            {
                return;
            }

            if (trap.IsApplied && trap.AppliedPlayer != null && !ReferenceEquals(trap.AppliedPlayer, player))
            {
                RestoreTrapGravity(trap);
            }

            Vector3 baselineGravity = trap.HasRestoreGravity
                ? trap.RestoreGravity
                : ReadCurrentGravity(player);
            if (baselineGravity.sqrMagnitude < 0.001f)
            {
                baselineGravity = Physics.gravity.sqrMagnitude > 0.001f ? Physics.gravity : new Vector3(0f, -30f, 0f);
            }

            trap.RestoreGravity = baselineGravity;
            trap.HasRestoreGravity = true;
            trap.Direction = ResolveTrapDirection(trap.Kind, baselineGravity);

            float baseMagnitude = GetVanillaGravityMagnitude(baselineGravity);
            float magnitude = baseMagnitude * trap.MagnitudeScale;
            if (magnitude < 0.1f)
            {
                magnitude = Mathf.Max(baseMagnitude, 30f);
            }

            Vector3 appliedGravity = trap.Direction.normalized * magnitude;
            ApplyGravity(player, appliedGravity);
            trap.IsApplied = true;
            trap.AppliedPlayer = player;
        }

        private Vector3 ResolveTrapDirection(TrapKind kind, Vector3 baselineGravity)
        {
            if (kind == TrapKind.IncreasedGravity)
            {
                return baselineGravity.normalized;
            }

            Vector3 direction = UnityEngine.Random.onUnitSphere;
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = baselineGravity.sqrMagnitude > 0.001f
                    ? baselineGravity.normalized
                    : Vector3.down;
            }

            return direction.normalized;
        }

        private float GetVanillaGravityMagnitude(Vector3 baselineGravity)
        {
            float physicsMagnitude = Physics.gravity.magnitude;
            if (physicsMagnitude > 0.1f && physicsMagnitude < 1000f)
            {
                return physicsMagnitude;
            }

            float baselineMagnitude = baselineGravity.magnitude;
            if (baselineMagnitude > 0.1f && baselineMagnitude < 1000f)
            {
                return baselineMagnitude;
            }

            return 30f;
        }

        private void ClearTrapState(bool restoreGravity)
        {
            if (restoreGravity && _activeTrap != null)
            {
                RestoreTrapGravity(_activeTrap);
            }

            _activeTrap = null;
            _queuedTrap = null;
        }

        private void RestoreTrapGravity(TrapRuntime trap)
        {
            if (trap == null || !trap.IsApplied)
            {
                return;
            }

            if (trap.AppliedPlayer != null)
            {
                if (trap.HasRestoreGravity)
                {
                    ApplyGravity(trap.AppliedPlayer, trap.RestoreGravity);
                }
                else if (RemoveGravityModifiersMethod != null)
                {
                    try
                    {
                        RemoveGravityModifiersMethod.Invoke(trap.AppliedPlayer, null);
                    }
                    catch
                    {
                    }
                }
            }

            trap.IsApplied = false;
            trap.AppliedPlayer = null;
            trap.HasRestoreGravity = false;
        }

        private bool TryGetPlayablePlayer(out PlayerController player)
        {
            player = null;
            LevelManager levelManager = LevelManager.Instance;
            if (levelManager == null || levelManager.Player == null || levelManager.LevelDataSO == null)
            {
                return false;
            }

            if (IsHubLikeLevel(levelManager.LevelDataSO))
            {
                return false;
            }

            player = levelManager.Player;
            return true;
        }

        private bool TryGetLoadedPlayer(out PlayerController player)
        {
            player = null;
            LevelManager levelManager = LevelManager.Instance;
            if (levelManager == null || levelManager.Player == null)
            {
                return false;
            }

            player = levelManager.Player;
            return true;
        }

        private Vector3 ReadCurrentGravity(PlayerController player)
        {
            if (player == null || ActivePlayerStateProperty == null || PlayerStateGravityProperty == null)
            {
                return Vector3.zero;
            }

            try
            {
                object playerState = ActivePlayerStateProperty.GetValue(player, null);
                if (playerState == null)
                {
                    return Vector3.zero;
                }

                object gravityObj = PlayerStateGravityProperty.GetValue(playerState, null);
                return gravityObj is Vector3 ? (Vector3)gravityObj : Vector3.zero;
            }
            catch
            {
                return Vector3.zero;
            }
        }

        private void ApplyGravity(PlayerController player, Vector3 gravity)
        {
            if (player == null || ChangeGravityMethod == null)
            {
                return;
            }

            Vector3 applied = gravity.sqrMagnitude > 0.001f ? gravity : new Vector3(0f, -30f, 0f);
            Vector3 up = -applied.normalized;

            try
            {
                ChangeGravityMethod.Invoke(player, new object[] { applied, up });
            }
            catch (Exception ex)
            {
                SafeLog("Failed to apply trap gravity: " + ex.Message);
            }
        }

        private IEnumerator DeathLinkSequenceCoroutine(PlayerController player, string message)
        {
            if (player == null)
            {
                yield break;
            }

            _deathLinkSequenceActive = true;
            _pendingIncomingDeathLink = false;

            if (_popupUi != null && !string.IsNullOrEmpty(message))
            {
                _popupUi.ShowInfo("DeathLink", message);
            }

            TriggerDeathLinkKill(player);
            yield return null;
            _deathLinkSequenceActive = false;
        }

        private void UpdatePowerupAnimationRuntime()
        {
            if (_powerupAnimationActive || _deathLinkSequenceActive || _powerupAnimationQueue.Count == 0)
            {
                return;
            }

            PlayerController player;
            if (!TryGetLoadedPlayer(out player))
            {
                return;
            }

            string triggerName = _powerupAnimationQueue.Dequeue();
            if (string.IsNullOrEmpty(triggerName))
            {
                return;
            }

            MelonCoroutines.Start(PowerupAnimationCoroutine(player, triggerName));
        }

        private IEnumerator PowerupAnimationCoroutine(PlayerController player, string triggerName)
        {
            _powerupAnimationActive = true;
            Animator gunAnimator = GunAnimatorField != null ? GunAnimatorField.GetValue(player) as Animator : null;
            float originalIsOut = 1f;

            if (gunAnimator != null)
            {
                try
                {
                    originalIsOut = gunAnimator.GetFloat("IsOut");
                    gunAnimator.SetFloat("IsOut", 0f);
                    gunAnimator.ResetTrigger("Enter");
                    gunAnimator.ResetTrigger("GetGun");
                    gunAnimator.ResetTrigger("Shoot");
                    if (string.Equals(triggerName, "Enter", StringComparison.Ordinal))
                    {
                        gunAnimator.SetFloat("Enter_Random", 0f);
                    }

                    gunAnimator.SetTrigger(triggerName);
                    gunAnimator.Update(0f);
                }
                catch (Exception ex)
                {
                    SafeLog("Powerup animation failed to start: " + ex.Message);
                }
            }

            float duration = string.Equals(triggerName, "GetGun", StringComparison.Ordinal) ? 0.9f : 0.75f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (gunAnimator != null)
            {
                try
                {
                    gunAnimator.SetFloat("IsOut", originalIsOut);
                }
                catch (Exception ex)
                {
                    SafeLog("Powerup animation failed to restore: " + ex.Message);
                }
            }

            _powerupAnimationActive = false;
        }

        private void QueuePowerupAnimationForItem(string itemName, bool allowTransientEffects)
        {
            if (!allowTransientEffects || string.IsNullOrEmpty(itemName))
            {
                return;
            }

            string triggerName = null;
            switch (itemName)
            {
                case "Hook Pull":
                case "Progressive Hook Reach":
                    triggerName = "GetGun";
                    break;
                case "Air Dash":
                case "Wall Slide":
                case "Finger Gun":
                case "Progressive Time Warp":
                    triggerName = "Enter";
                    break;
            }

            if (!string.IsNullOrEmpty(triggerName))
            {
                _powerupAnimationQueue.Enqueue(triggerName);
            }
        }

        private void FireDeathLinkShot(PlayerController player)
        {
            if (player == null)
            {
                return;
            }

            try
            {
                player.SetAllowShoot(true);
                player.SetAllowShootAnim(1);

                PlayerState state = ActivePlayerStateProperty != null
                    ? ActivePlayerStateProperty.GetValue(player, null) as PlayerState
                    : null;
                if (state != null)
                {
                    if (PlayerStateActionShootMethod != null)
                    {
                        PlayerStateActionShootMethod.Invoke(state, null);
                    }
                    else if (PlayerStateOnShootMethod != null)
                    {
                        PlayerStateOnShootMethod.Invoke(state, null);
                    }
                }
            }
            catch (Exception ex)
            {
                SafeLog("DeathLink fire failed: " + ex.Message);
            }
        }

        private void PrepareDeathLinkPresentation(
            PlayerController player,
            out bool[] originalFpsRendererStates,
            out bool? originalHookMeshState)
        {
            originalFpsRendererStates = null;
            originalHookMeshState = null;

            if (player == null)
            {
                return;
            }

            try
            {
                player.SetAllowShoot(true);
                player.SetAllowShootAnim(1);

                GameObject[] fpsRenderers = FPSRenderersField != null ? FPSRenderersField.GetValue(player) as GameObject[] : null;
                if (fpsRenderers != null)
                {
                    originalFpsRendererStates = new bool[fpsRenderers.Length];
                    for (int i = 0; i < fpsRenderers.Length; i++)
                    {
                        GameObject fpsRenderer = fpsRenderers[i];
                        if (fpsRenderer == null)
                        {
                            continue;
                        }

                        originalFpsRendererStates[i] = fpsRenderer.activeSelf;
                        fpsRenderer.SetActive(true);
                    }
                }
            }
            catch (Exception ex)
            {
                SafeLog("Failed to prepare DeathLink presentation: " + ex.Message);
            }
        }

        private void RestoreDeathLinkPresentation(
            PlayerController player,
            bool hadShoot,
            bool[] originalFpsRendererStates,
            bool? originalHookMeshState)
        {
            if (player == null)
            {
                return;
            }

            try
            {
                if (!hadShoot)
                {
                    player.SetAllowShoot(false);
                    player.SetAllowShootAnim(0);
                    if (PlayerControllerResetHandPositionMethod != null)
                    {
                        PlayerControllerResetHandPositionMethod.Invoke(player, null);
                    }
                }

                GameObject[] fpsRenderers = FPSRenderersField != null ? FPSRenderersField.GetValue(player) as GameObject[] : null;
                if (fpsRenderers != null && originalFpsRendererStates != null)
                {
                    int count = Mathf.Min(fpsRenderers.Length, originalFpsRendererStates.Length);
                    for (int i = 0; i < count; i++)
                    {
                        GameObject fpsRenderer = fpsRenderers[i];
                        if (fpsRenderer != null)
                        {
                            fpsRenderer.SetActive(originalFpsRendererStates[i]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SafeLog("Failed to restore DeathLink presentation: " + ex.Message);
            }
        }

        private void UpdateShootPoseCapture(PlayerController player)
        {
            if (!_shootPoseCaptureActive || player == null)
            {
                return;
            }

            Transform handRoot = HandRootField != null ? HandRootField.GetValue(player) as Transform : null;
            if (handRoot == null)
            {
                _shootPoseCaptureActive = false;
                return;
            }

            Transform aimTransform = AimTransformField != null ? AimTransformField.GetValue(player) as Transform : null;
            Transform handParent = handRoot.parent;

            float angle = Quaternion.Angle(_shootPoseCaptureOrigin, handRoot.localRotation);
            if (angle > _shootPoseCaptureBestAngle)
            {
                _shootPoseCaptureBestAngle = angle;
                _shootPoseCaptureBest = handRoot.localRotation;
                _shootPoseCaptureBestPosition = handRoot.localPosition;
                _shootPoseCaptureBestAimRotation = aimTransform != null ? aimTransform.localRotation : Quaternion.identity;
                _shootPoseCaptureBestAimPosition = aimTransform != null ? aimTransform.localPosition : Vector3.zero;
                _shootPoseCaptureBestHandParentRotation = handParent != null ? handParent.localRotation : Quaternion.identity;
                _shootPoseCaptureBestHandParentPosition = handParent != null ? handParent.localPosition : Vector3.zero;

                Animator gunAnimator = GunAnimatorField != null ? GunAnimatorField.GetValue(player) as Animator : null;
                if (gunAnimator != null)
                {
                    try
                    {
                        AnimatorStateInfo stateInfo = gunAnimator.GetCurrentAnimatorStateInfo(0);
                        if (stateInfo.fullPathHash != 0)
                        {
                            _capturedGunAnimatorStateHash = stateInfo.fullPathHash;
                            _capturedGunAnimatorNormalizedTime = Mathf.Repeat(stateInfo.normalizedTime, 1f);
                            _hasCapturedGunAnimatorState = true;
                        }
                    }
                    catch
                    {
                    }
                }
            }

            _shootPoseCaptureRemainingSeconds -= Time.unscaledDeltaTime;
            if (_shootPoseCaptureRemainingSeconds > 0f)
            {
                return;
            }

            _shootPoseCaptureActive = false;
            if (_shootPoseCaptureBestAngle >= 1f)
            {
                _capturedShootPose = _shootPoseCaptureBest;
                _capturedShootPosition = _shootPoseCaptureBestPosition;
                _capturedAimRotation = _shootPoseCaptureBestAimRotation;
                _capturedAimPosition = _shootPoseCaptureBestAimPosition;
                _capturedHandParentRotation = _shootPoseCaptureBestHandParentRotation;
                _capturedHandParentPosition = _shootPoseCaptureBestHandParentPosition;
                _hasCapturedShootPose = true;
            }
        }

        private void TriggerDeathLinkKill(PlayerController player)
        {
            if (player == null)
            {
                return;
            }

            try
            {
                PlayerState state = ActivePlayerStateProperty != null
                    ? ActivePlayerStateProperty.GetValue(player, null) as PlayerState
                    : null;
                if (state == null || PlayerStateOnKillMethod == null)
                {
                    return;
                }

                _suppressOutgoingDeathLink = true;
                PlayerStateOnKillMethod.Invoke(state, new object[] { player.gameObject });
            }
            catch (Exception ex)
            {
                _suppressOutgoingDeathLink = false;
                SafeLog("DeathLink kill failed: " + ex.Message);
            }
        }

        private string GetTrapSummary()
        {
            if (_activeTrap != null)
            {
                return _activeTrap.Name + " " + _activeTrap.RemainingSeconds.ToString("0.0") + "s";
            }

            if (_queuedTrap != null)
            {
                return "Queued: " + _queuedTrap.Name;
            }

            return "NONE";
        }

        internal void OnStageSelectOpened(StageSelectPanel panel)
        {
            if (panel == null)
            {
                return;
            }

            Levelpack levelpack = AccessTools.Field(typeof(StageSelectPanel), "_levelPackRef").GetValue(panel) as Levelpack;
            _cachedLevelpack = levelpack;
            ApplyWorldLevelUnlockOverrides(levelpack);
            if (Settings.DiscoveryMode)
            {
                Discovery.ExportLevelPack(levelpack);
                SafeLog("Exported stage select level metadata.");
            }

            if (Settings.DiscoveryMode && Settings.AutoSweepEnabled)
            {
                TryStartAutoSweep(levelpack);
            }
        }

        internal void OnLevelInitialized(LevelManager levelManager)
        {
            if (levelManager == null)
            {
                return;
            }

            if (levelManager.LevelDataSO != null)
            {
                ApplyWorldLevelUnlockOverride(levelManager.LevelDataSO);
            }

            if (Settings.DiscoveryMode)
            {
                Discovery.ExportCurrentSceneObjects(levelManager.LevelDataSO);
            }

            if (ShouldGateGameplay())
            {
                ApplyGameplayState(levelManager.Player);
            }

            if (_autoSweepActive && levelManager.LevelDataSO != null)
            {
                string activeLevelId = levelManager.LevelDataSO.LevelUniqueID ?? string.Empty;
                if (string.Equals(activeLevelId, _autoSweepCurrentLevelId, StringComparison.OrdinalIgnoreCase))
                {
                    _autoSweepWaitingForLevelInit = false;
                    SafeLog("Auto-sweep captured scene for " + activeLevelId + ".");
                }
            }
        }

        internal void OnPlayerUpdate(PlayerController player)
        {
            if (ShouldGateGameplay())
            {
                ApplyGameplayState(player);
            }
        }

        internal void OnPlayerLateUpdate(PlayerController player)
        {
        }

        internal void OnLevelCompleted(PlayerController player)
        {
            ClearTrapState(true);
        }

        internal void OnVictoryOpened(float finalTime)
        {
            SO_Level level = LevelManager.Instance != null ? LevelManager.Instance.LevelDataSO : null;
            if (level == null)
            {
                return;
            }

            int starsUnlocked = CalculateStarsForRun(level, finalTime);

            if (Settings.EnableRankChecks && IsEligibleApLevel(level))
            {
                RegisterCheck(level.LevelUniqueID + "::rank::0", "Clear check");
                for (int rank = 1; rank <= 3; rank++)
                {
                    if (starsUnlocked >= rank)
                    {
                        RegisterCheck(level.LevelUniqueID + "::rank::" + rank.ToString(), "Rank check");
                    }
                }
            }

            Discovery.AppendLine(
                "completions.log",
                DateTime.UtcNow.ToString("o")
                + "\t" + level.LevelUniqueID
                + "\ttime=" + finalTime.ToString("0.000")
                + "\tstars=" + starsUnlocked.ToString());
        }

        private int CalculateStarsForRun(SO_Level level, float finalTime)
        {
            if (level == null || finalTime <= 0f)
            {
                return 0;
            }

            float[] stars = level.Stars ?? new float[0];
            if (stars.Length == 0)
            {
                return 0;
            }

            int earned = 0;
            if (stars.Length >= 4)
            {
                for (int rank = 1; rank <= 3; rank++)
                {
                    if (stars[rank] > 0f && finalTime <= stars[rank])
                    {
                        earned = rank;
                    }
                }

                return earned;
            }

            int maxStars = Mathf.Min(3, stars.Length);
            for (int i = 0; i < maxStars; i++)
            {
                if (stars[i] > 0f && finalTime <= stars[i])
                {
                    earned = i + 1;
                }
            }

            return earned;
        }

        internal void OnCollectibleTouched(LevelCollectible collectible)
        {
            if (collectible == null)
            {
                return;
            }

            SO_Level level = LevelManager.Instance != null ? LevelManager.Instance.LevelDataSO : null;
            if (level == null)
            {
                return;
            }

            object levelIdObj = AccessTools.Method(typeof(LevelCollectible), "GetLevelID").Invoke(collectible, new object[0]);
            string cubeFreeId = levelIdObj as string ?? string.Empty;
            string path = SceneObjectId.BuildTransformPath(collectible.transform);
            string discoveryKey = level.LevelUniqueID + "::collectible::" + cubeFreeId + "::" + path;

            if (State.KnownCollectibleIds.Add(discoveryKey))
            {
                Discovery.AppendLine(
                    "collectibles.log",
                    DateTime.UtcNow.ToString("o") + "\t" + discoveryKey);
                PersistState();
            }

            if (Settings.EnableAnchorChecks
                && IsEligibleApLevel(level)
                && level.TargetWorld != null
                && level.TargetWorld.Index >= 0
                && level.TargetWorld.Index < 4)
            {
                RegisterCheck(level.LevelUniqueID + "::numero_anchor", "Numero anchor check");
            }
        }

        internal void OnCubeSeen(BreakableCube cube)
        {
            if (!Settings.DiscoveryMode || cube == null)
            {
                return;
            }

            SO_Level level = LevelManager.Instance != null ? LevelManager.Instance.LevelDataSO : null;
            if (level == null)
            {
                return;
            }

            string cubeId = level.LevelUniqueID + "::cube::" + SceneObjectId.BuildTransformPath(cube.transform);
            if (State.KnownCubeIds.Add(cubeId))
            {
                Discovery.AppendLine("cubes.log", DateTime.UtcNow.ToString("o") + "\tseen\t" + cubeId);
                PersistState();
            }
        }

        internal void OnCubeBroken(BreakableCube cube)
        {
            if (cube == null)
            {
                return;
            }

            SO_Level level = LevelManager.Instance != null ? LevelManager.Instance.LevelDataSO : null;
            if (level == null)
            {
                return;
            }

            string cubeId = level.LevelUniqueID + "::cube::" + SceneObjectId.BuildTransformPath(cube.transform);
            if (Settings.EnableCubeChecks && IsEligibleApLevel(level))
            {
                RegisterCheck(cubeId, "Cube check");
            }

            Discovery.AppendLine("cubes.log", DateTime.UtcNow.ToString("o") + "\tbroken\t" + cubeId);
        }

        internal void OnPlayerKilled(PlayerState playerState, GameObject killer)
        {
            if (_suppressOutgoingDeathLink)
            {
                _suppressOutgoingDeathLink = false;
                return;
            }

            if (_suppressedResetDeathLinks > 0)
            {
                _suppressedResetDeathLinks--;
                return;
            }

            if (_slotData == null || !_slotData.DeathLinkEnabled || _apClient == null || !_apClient.IsAuthenticated)
            {
                return;
            }

            int amnesty = Mathf.Max(1, _slotData.DeathLinkAmnesty);
            _deathLinkAmnestyCounter++;
            if (_deathLinkAmnestyCounter < amnesty)
            {
                return;
            }

            _deathLinkAmnestyCounter = 0;

            string source = !string.IsNullOrEmpty(State.SessionSlotName)
                ? State.SessionSlotName
                : (Settings != null && Settings.Connection != null ? Settings.Connection.Slot : string.Empty);
            if (string.IsNullOrEmpty(source))
            {
                source = "Cyber Hook";
            }

            _apClient.SendDeathLink(source, source + " died.");
            SafeLog("Sent DeathLink.");
        }

        internal void OnPlayerShot(PlayerState playerState)
        {
        }

        internal void SuppressNextResetDeathLink()
        {
            _suppressedResetDeathLinks++;
        }

        internal bool TryOverrideWorldUnlock(WorldDetail world, ref bool result)
        {
            if (world == null || !Settings.OverrideWorldUnlocks || !ShouldGateGameplay())
            {
                return false;
            }

            if (world.Index < 0)
            {
                result = false;
                return true;
            }

            ArchipelagoState effective = GetEffectiveState();
            int unlockThreshold = world.UnlockStarsThreshold;
            if (_slotData != null && _slotData.WorldUnlockOrder != null && _slotData.WorldUnlockOrder.Count > 0)
            {
                int worldPosition = _slotData.GetWorldUnlockPosition(world.Index);
                if (worldPosition < 0)
                {
                    result = false;
                    return true;
                }

                unlockThreshold = ApData.GetWorldUnlockThresholdForPosition(worldPosition);
            }

            result = effective.ReceivedDiamonds >= unlockThreshold;
            return true;
        }

        internal bool TryOverrideLevelUnlock(SO_Level level, ref bool result)
        {
            if (level == null || !Settings.OverrideWorldUnlocks || !ShouldGateGameplay())
            {
                return false;
            }

            if (level.IsAlwaysUnlocked)
            {
                result = true;
                return true;
            }

            if (level.TargetWorld == null)
            {
                return false;
            }

            bool worldUnlocked = false;
            if (!TryOverrideWorldUnlock(level.TargetWorld, ref worldUnlocked))
            {
                return false;
            }

            result = worldUnlocked;
            if (worldUnlocked)
            {
                TryMarkLevelUnlocked(level);
            }
            return true;
        }

        internal bool CanWallSlide()
        {
            if (!ShouldGateGameplay())
            {
                return true;
            }

            return GetEffectiveState().HasWallSlide;
        }

        internal bool CanHookPull()
        {
            if (!ShouldGateGameplay())
            {
                return true;
            }

            return GetEffectiveState().HasHookPull;
        }

        private void ApplyWorldLevelUnlockOverrides(Levelpack levelpack)
        {
            if (levelpack == null || !Settings.OverrideWorldUnlocks || !ShouldGateGameplay())
            {
                return;
            }

            List<SO_Level> levels = levelpack.GetLevelListWithChildren;
            if (levels == null)
            {
                return;
            }

            for (int i = 0; i < levels.Count; i++)
            {
                ApplyWorldLevelUnlockOverride(levels[i]);
            }
        }

        private void ApplyWorldLevelUnlockOverride(SO_Level level)
        {
            if (level == null || !Settings.OverrideWorldUnlocks || !ShouldGateGameplay())
            {
                return;
            }

            bool worldUnlocked = false;
            if (!TryOverrideLevelUnlock(level, ref worldUnlocked) || !worldUnlocked)
            {
                return;
            }

            TryMarkLevelUnlocked(level);
        }

        private void TryMarkLevelUnlocked(SO_Level level)
        {
            if (level == null)
            {
                return;
            }

            try
            {
                LevelData data = level.SerializedData;
                if (data != null)
                {
                    data.IsUnlocked = true;
                }
            }
            catch (Exception ex)
            {
                SafeLog("Failed to mark level unlocked: " + ex.Message);
            }
        }

        private SO_Level ResolveLevelByUniqueId(string levelUniqueId)
        {
            if (string.IsNullOrEmpty(levelUniqueId))
            {
                return null;
            }

            if (_cachedLevelpack != null)
            {
                try
                {
                    SO_Level cached = _cachedLevelpack.GetLevel(levelUniqueId);
                    if (cached != null)
                    {
                        return cached;
                    }
                }
                catch
                {
                }
            }

            SO_Level[] loadedLevels = Resources.FindObjectsOfTypeAll<SO_Level>();
            for (int i = 0; i < loadedLevels.Length; i++)
            {
                SO_Level level = loadedLevels[i];
                if (level != null && string.Equals(level.LevelUniqueID, levelUniqueId, StringComparison.OrdinalIgnoreCase))
                {
                    return level;
                }
            }

            return null;
        }

        private bool ShouldGateGameplay()
        {
            return !Settings.DiscoveryMode && Settings.EnableGameplayGating;
        }

        private void TryStartAutoSweep(Levelpack levelpack)
        {
            if (_autoSweepActive || levelpack == null)
            {
                return;
            }

            List<SO_Level> queue = BuildAutoSweepQueue(levelpack);
            if (queue.Count == 0)
            {
                SafeLog("Auto-sweep found no missing level scenes.");
                return;
            }

            _autoSweepActive = true;
            _autoSweepCoroutine = MelonCoroutines.Start(AutoSweepCoroutine(queue));
            SafeLog("Auto-sweep queued " + queue.Count.ToString() + " levels.");
        }

        private List<SO_Level> BuildAutoSweepQueue(Levelpack levelpack)
        {
            Dictionary<string, SO_Level> deduped = new Dictionary<string, SO_Level>(StringComparer.OrdinalIgnoreCase);
            List<SO_Level> levels = levelpack.GetLevelListWithChildren;
            if (levels == null)
            {
                return new List<SO_Level>();
            }

            for (int i = 0; i < levels.Count; i++)
            {
                SO_Level level = levels[i];
                if (level == null || string.IsNullOrEmpty(level.LevelUniqueID))
                {
                    continue;
                }

                if (!Settings.AutoSweepIncludeHubs && IsHubLikeLevel(level))
                {
                    continue;
                }

                if (Settings.AutoSweepSkipExistingSceneDumps && HasExistingSceneDump(level.LevelUniqueID))
                {
                    continue;
                }

                if (!deduped.ContainsKey(level.LevelUniqueID))
                {
                    deduped.Add(level.LevelUniqueID, level);
                }
            }

            return new List<SO_Level>(deduped.Values);
        }

        private IEnumerator AutoSweepCoroutine(List<SO_Level> queue)
        {
            yield return new WaitForSeconds(1f);

            for (int i = 0; i < queue.Count; i++)
            {
                SO_Level level = queue[i];
                if (level == null)
                {
                    continue;
                }

                while (IsMainManagerLoading())
                {
                    yield return null;
                }

                _autoSweepCurrentLevelId = level.LevelUniqueID ?? string.Empty;
                _autoSweepWaitingForLevelInit = true;
                SafeLog(
                    "Auto-sweep loading "
                    + (i + 1).ToString()
                    + "/"
                    + queue.Count.ToString()
                    + ": "
                    + level.LevelNameKey
                    + " ("
                    + _autoSweepCurrentLevelId
                    + ")");

                bool launchAccepted = false;
                try
                {
                    launchAccepted = level.Launch(true, true);
                }
                catch (Exception ex)
                {
                    SafeLog("Auto-sweep launch failed for " + level.LevelUniqueID + ": " + ex.Message);
                }

                if (!launchAccepted)
                {
                    _autoSweepWaitingForLevelInit = false;
                    continue;
                }

                float timeoutAt = Time.realtimeSinceStartup + 30f;
                while (_autoSweepWaitingForLevelInit && Time.realtimeSinceStartup < timeoutAt)
                {
                    yield return null;
                }

                if (_autoSweepWaitingForLevelInit)
                {
                    SafeLog("Auto-sweep timed out waiting for " + level.LevelUniqueID + ".");
                    _autoSweepWaitingForLevelInit = false;
                }

                yield return new WaitForSeconds(Settings.AutoSweepDelaySeconds);
            }

            _autoSweepActive = false;
            _autoSweepWaitingForLevelInit = false;
            _autoSweepCurrentLevelId = string.Empty;
            _autoSweepCoroutine = null;
            SafeLog("Auto-sweep complete.");
        }

        private bool HasExistingSceneDump(string levelId)
        {
            if (string.IsNullOrEmpty(levelId))
            {
                return false;
            }

            string path = Path.Combine(DataDirectory, "scene_" + levelId + ".json");
            return File.Exists(path);
        }

        private bool IsHubLikeLevel(SO_Level level)
        {
            string name = level.LevelNameKey ?? string.Empty;
            return name.IndexOf("hub", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool IsEligibleApLevel(SO_Level level)
        {
            return level != null
                && !IsHubLikeLevel(level)
                && !ApData.IsExcludedLevel(level.LevelUniqueID);
        }

        private bool IsMainManagerLoading()
        {
            MainManager manager = UnityEngine.Object.FindObjectOfType<MainManager>();
            return manager != null && manager.IsLoadingLevel;
        }

        private void EnsureHudStyles()
        {
            if (_hudBoxStyle == null)
            {
                _hudBoxStyle = new GUIStyle(GUI.skin.box);
                _hudBoxStyle.alignment = TextAnchor.UpperLeft;
                _hudBoxStyle.fontSize = 13;
                _hudBoxStyle.padding = new RectOffset(10, 10, 10, 10);
                _hudBoxStyle.normal.textColor = Color.white;
            }

            if (_hudLabelStyle == null)
            {
                _hudLabelStyle = new GUIStyle(GUI.skin.label);
                _hudLabelStyle.fontSize = 13;
                _hudLabelStyle.alignment = TextAnchor.UpperLeft;
                _hudLabelStyle.normal.textColor = new Color(0.72f, 0.85f, 1f, 1f);
            }

            if (_hudValueStyle == null)
            {
                _hudValueStyle = new GUIStyle(GUI.skin.label);
                _hudValueStyle.fontSize = 13;
                _hudValueStyle.alignment = TextAnchor.UpperRight;
                _hudValueStyle.normal.textColor = Color.white;
            }

            if (_hudButtonStyle == null)
            {
                _hudButtonStyle = new GUIStyle(GUI.skin.button);
                _hudButtonStyle.fontSize = 12;
                _hudButtonStyle.alignment = TextAnchor.MiddleCenter;
            }
        }

        private void DrawHudLine(float x, ref float y, string label, string value)
        {
            GUI.Label(new Rect(x, y, 128f, 20f), label, _hudLabelStyle);
            GUI.Label(new Rect(x + 128f, y, 140f, 20f), value, _hudValueStyle);
            y += 18f;
        }

        private void DrawHudToggleRow(float x, ref float y, string label, bool value, string toggleKey)
        {
            GUI.Label(new Rect(x, y, 128f, 22f), label, _hudLabelStyle);
            if (GUI.Button(new Rect(x + 188f, y - 1f, 80f, 22f), FormatOnOff(value), _hudButtonStyle))
            {
                ToggleDebugSetting(toggleKey);
            }

            y += 24f;
        }

        private void DrawHudAdjustRow(float x, ref float y, string label, string value, string adjustKey, int minusAmount, int plusAmount)
        {
            GUI.Label(new Rect(x, y, 128f, 22f), label, _hudLabelStyle);
            GUI.Label(new Rect(x + 168f, y, 24f, 22f), value, _hudValueStyle);

            if (GUI.Button(new Rect(x + 204f, y - 1f, 28f, 22f), "-", _hudButtonStyle))
            {
                AdjustDebugValue(adjustKey, minusAmount);
            }

            if (GUI.Button(new Rect(x + 240f, y - 1f, 28f, 22f), "+", _hudButtonStyle))
            {
                AdjustDebugValue(adjustKey, plusAmount);
            }

            y += 24f;
        }

        private void DrawTrapButtonRow(float x, ref float y, string buttonLabel, TrapKind trapKind)
        {
            if (GUI.Button(new Rect(x, y - 1f, 120f, 22f), buttonLabel, _hudButtonStyle))
            {
                QueueTrap(trapKind, GetTrapName(trapKind));
            }

            y += 24f;
        }

        private void DrawCurrentLevelStarTimes(float x, ref float y)
        {
            SO_Level level = LevelManager.Instance != null ? LevelManager.Instance.LevelDataSO : null;
            if (level == null || IsHubLikeLevel(level))
            {
                return;
            }

            string levelName = level.LevelNameKey ?? "Current Level";
            if (_generatedData != null && !string.IsNullOrEmpty(level.LevelUniqueID))
            {
                string displayName;
                if (_generatedData.TryGetLevelDisplayName(level.LevelUniqueID, out displayName) && !string.IsNullOrEmpty(displayName))
                {
                    levelName = displayName;
                }
            }

            float[] stars = level.Stars ?? new float[0];
            if (stars.Length == 0)
            {
                return;
            }

            GUI.Label(new Rect(x, y, 240f, 20f), levelName, _hudLabelStyle);
            y += 22f;

            if (stars.Length >= 4)
            {
                DrawHudLine(x, ref y, "1 Star", FormatTimeSeconds(stars[1]));
                DrawHudLine(x, ref y, "2 Star", FormatTimeSeconds(stars[2]));
                DrawHudLine(x, ref y, "3 Star", FormatTimeSeconds(stars[3]));
                return;
            }

            int maxStars = Mathf.Min(3, stars.Length);
            for (int i = 0; i < maxStars; i++)
            {
                DrawHudLine(x, ref y, (i + 1).ToString() + " Star", FormatTimeSeconds(stars[i]));
            }
        }

        private string GetTrapName(TrapKind trapKind)
        {
            switch (trapKind)
            {
                case TrapKind.RandomGravityDirection:
                    return "Random Gravity Direction Trap";
                case TrapKind.IncreasedGravity:
                    return "Increased Gravity Trap";
                default:
                    return "Trap";
            }
        }

        private string FormatOnOff(bool value)
        {
            return value ? "ON" : "OFF";
        }

        private string FormatTimeSeconds(float seconds)
        {
            if (seconds <= 0f)
            {
                return "--:--.--";
            }

            TimeSpan span = TimeSpan.FromSeconds(seconds);
            int minutes = (int)span.TotalMinutes;
            int secs = span.Seconds;
            int hundredths = Mathf.Clamp(Mathf.RoundToInt((seconds - Mathf.Floor(seconds)) * 100f), 0, 99);
            return minutes.ToString("00") + ":" + secs.ToString("00") + "." + hundredths.ToString("00");
        }

        private void ToggleDebugSetting(string toggleKey)
        {
            switch (toggleKey)
            {
                case "DoubleJump":
                    Settings.DebugDoubleJump = !Settings.DebugDoubleJump;
                    break;
                case "WallSlide":
                    Settings.DebugWallSlide = !Settings.DebugWallSlide;
                    break;
                case "HookPull":
                    Settings.DebugHookPull = !Settings.DebugHookPull;
                    break;
                case "Shoot":
                    Settings.DebugShoot = !Settings.DebugShoot;
                    break;
                case "Slowmo":
                    Settings.DebugSlowmo = !Settings.DebugSlowmo;
                    break;
            }

            PersistSettings();
        }

        private void AdjustDebugValue(string adjustKey, int amount)
        {
            switch (adjustKey)
            {
                case "Diamonds":
                    Settings.DebugDiamonds = Mathf.Max(0, Settings.DebugDiamonds + amount);
                    break;
                case "SlowmoLevels":
                    Settings.DebugSlowmoLevels = ApData.ClampProgressiveLevel(Settings.DebugSlowmoLevels + amount);
                    break;
                case "ReachLevels":
                    Settings.DebugReachLevels = ApData.ClampProgressiveLevel(Settings.DebugReachLevels + amount);
                    break;
            }

            PersistSettings();
        }

        private void ResetDebugOverrides()
        {
            Settings.DebugDiamonds = 0;
            Settings.DebugDoubleJump = false;
            Settings.DebugWallSlide = false;
            Settings.DebugHookPull = false;
            Settings.DebugShoot = false;
            Settings.DebugSlowmo = false;
            Settings.DebugSlowmoLevels = 0;
            Settings.DebugReachLevels = 0;
            PersistSettings();
        }

        private void PersistSettings()
        {
            if (_settingsStore != null && Settings != null)
            {
                _settingsStore.Save(Settings);
            }
        }

        private void SyncSettingsFromPreferences()
        {
            if (_preferences == null || Settings == null)
            {
                return;
            }

            _preferences.ApplyTo(Settings);
            EnsureConnectionUuid();
            Settings.DebugSlowmoLevels = ApData.ClampProgressiveLevel(Settings.DebugSlowmoLevels);
            Settings.DebugReachLevels = ApData.ClampProgressiveLevel(Settings.DebugReachLevels);
            _lastAppliedPlayer = null;
            _lastAppliedSignature = string.Empty;
            if (_saveProfiles != null)
            {
                _saveProfiles.OnSettingsChanged(Settings);
            }
            PersistSettings();
        }

        private void InitializeArchipelagoClient()
        {
            _apClient = new ArchipelagoClient();
            _apClient.Connected += OnArchipelagoConnected;
            _apClient.ReceivedItems += OnArchipelagoReceivedItems;
            _apClient.CheckedLocationsUpdated += OnArchipelagoCheckedLocationsUpdated;
            _apClient.TextMessage += OnArchipelagoTextMessage;
            _apClient.StatusChanged += OnArchipelagoStatusChanged;
            _apClient.DeathLinkReceived += OnArchipelagoDeathLinkReceived;
            _slotData = new ArchipelagoSlotData();
        }

        private void EnsurePopupUi()
        {
            if (_popupUi == null)
            {
                _popupUi = EventPopupUi.Ensure();
                if (_popupUi != null && _apClient != null)
                {
                    _popupUi.SetStatus(_apClient.StatusText);
                    _popupUi.SetCommandHandler(SendArchipelagoText);
                }
            }

            if (_popupUi != null)
            {
                _popupUi.gameObject.SetActive(Settings == null || Settings.EnableStatusHud);
            }
        }

        private void SendArchipelagoText(string text)
        {
            if (_apClient == null || string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            _apClient.SendText(text.Trim());
        }

        private void EnsureConnectionUuid()
        {
            if (Settings == null || Settings.Connection == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(Settings.Connection.ClientUuid))
            {
                Settings.Connection.ClientUuid = Guid.NewGuid().ToString("N");
            }
        }

        private void TryLaunchLevelById(string levelId)
        {
            if (string.IsNullOrEmpty(levelId))
            {
                return;
            }

            if (_cachedLevelpack == null)
            {
                SafeLog("Open stage select once before launching hidden levels.");
                return;
            }

            SO_Level level = null;
            try
            {
                level = _cachedLevelpack.GetLevel(levelId);
            }
            catch (Exception ex)
            {
                SafeLog("Failed to resolve level " + levelId + ": " + ex.Message);
                return;
            }

            if (level == null)
            {
                SafeLog("Hidden level not found in cached levelpack: " + levelId);
                return;
            }

            try
            {
                bool started = level.Launch(true, false);
                SafeLog("Launch " + level.LevelNameKey + ": " + (started ? "started" : "rejected"));
            }
            catch (Exception ex)
            {
                SafeLog("Launch failed for " + levelId + ": " + ex.Message);
            }
        }

        private void OnArchipelagoConnected(ArchipelagoConnectedPayload payload)
        {
            ClearTrapState(true);
            string seedName = payload != null ? payload.SeedName ?? string.Empty : string.Empty;
            string slotName = payload != null ? payload.SlotName ?? string.Empty : string.Empty;
            bool isNewSession =
                !string.Equals(State.SessionSeedName, seedName, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(State.SessionSlotName, slotName, StringComparison.OrdinalIgnoreCase);

            if (isNewSession)
            {
                State.ResetForNewSession(seedName, slotName);
                SafeLog("Started new Archipelago session: seed=" + seedName + ", slot=" + slotName);
                if (_popupUi != null)
                {
                    _popupUi.ShowInfo("Session Started", string.IsNullOrEmpty(seedName) ? slotName : seedName);
                }
            }

            JToken slotDataToken = payload != null ? payload.SlotData : null;
            JToken locationMapToken = slotDataToken != null ? slotDataToken["locations"] : null;
            if (_generatedData != null)
            {
                _generatedData.UpdateLocationMappings(locationMapToken);
            }

            _slotData = ArchipelagoSlotData.FromToken(slotDataToken, _generatedData);
            _deathLinkAmnestyCounter = 0;
            if (_apClient != null)
            {
                _apClient.SetDeathLinkEnabled(_slotData.DeathLinkEnabled);
            }
            if (_saveProfiles != null)
            {
                _saveProfiles.OnArchipelagoConnected(seedName, slotName);
            }
            MergeCheckedLocationsFromServer(payload != null ? payload.CheckedLocations : null);
            PersistState();

            if (_apClient != null)
            {
                _apClient.SendPlayingStatus();
                _apClient.SendLocationChecks(GetCompletedLocationIds());
            }

            TryReportGoalCompletion();
        }

        private void OnArchipelagoReceivedItems(int startIndex, List<ArchipelagoNetworkItem> items)
        {
            if (startIndex == 0)
            {
                ClearTrapState(true);
                State.ResetProgression();
                ApplyReceivedItems(items, false);
                ApplyWorldLevelUnlockOverrides(_cachedLevelpack);
                State.LastReceivedItemIndex = items != null ? items.Count : 0;
                PersistState();
                SafeLog("Archipelago item sync applied from index 0.");
                TryReportGoalCompletion();
                return;
            }

            if (startIndex != State.LastReceivedItemIndex)
            {
                SafeLog(
                    "Archipelago item desync detected. Expected "
                    + State.LastReceivedItemIndex.ToString()
                    + " but received "
                    + startIndex.ToString()
                    + ". Requesting Sync.");
                if (_apClient != null)
                {
                    _apClient.RequestSync(GetCompletedLocationIds());
                }

                return;
            }

            ApplyReceivedItems(items, true);
            ApplyWorldLevelUnlockOverrides(_cachedLevelpack);
            State.LastReceivedItemIndex += items != null ? items.Count : 0;
            PersistState();
            TryReportGoalCompletion();
        }

        private void OnArchipelagoCheckedLocationsUpdated(List<long> locationIds)
        {
            MergeCheckedLocationsFromServer(locationIds);
            PersistState();
            TryReportGoalCompletion();
        }

        private void OnArchipelagoTextMessage(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                SafeLog(message);
                if (_popupUi != null)
                {
                    _popupUi.ShowInfo("Archipelago", message);
                }
            }
        }

        private void OnArchipelagoStatusChanged(string status)
        {
            SafeLog("Archipelago status: " + status);
            if (_popupUi != null)
            {
                _popupUi.SetStatus(status);
            }
        }

        private void OnArchipelagoDeathLinkReceived(ArchipelagoDeathLinkPayload payload)
        {
            if (payload == null || !_slotData.DeathLinkEnabled)
            {
                return;
            }

            string message = !string.IsNullOrEmpty(payload.Cause)
                ? payload.Cause
                : (payload.Source + " sent a DeathLink.");
            _pendingDeathLinkMessage = message;
            _pendingIncomingDeathLink = true;
            SafeLog("Received DeathLink: " + message);
            if (_popupUi != null)
            {
                _popupUi.ShowInfo("DeathLink", message);
            }
        }

        private void ApplyReceivedItems(List<ArchipelagoNetworkItem> items, bool allowTransientEffects)
        {
            if (items == null)
            {
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                ApplyReceivedItem(items[i], allowTransientEffects);
            }
        }

        private void ApplyReceivedItem(ArchipelagoNetworkItem networkItem, bool allowTransientEffects)
        {
            if (networkItem == null || _generatedData == null)
            {
                return;
            }

            GeneratedApItem item;
            if (!_generatedData.TryGetItem(networkItem.Item, out item) || item == null || string.IsNullOrEmpty(item.Name))
            {
                SafeLog("Received unknown Archipelago item id: " + networkItem.Item.ToString());
                return;
            }

            switch (item.Name)
            {
                case "Star":
                    State.ReceivedDiamonds++;
                    ShowPopup(PopupKind.Item, item.Name);
                    break;
                case "Air Dash":
                    State.HasDoubleJump = true;
                    ShowPopup(PopupKind.Item, item.Name);
                    QueuePowerupAnimationForItem(item.Name, allowTransientEffects);
                    break;
                case "Wall Slide":
                    State.HasWallSlide = true;
                    ShowPopup(PopupKind.Item, item.Name);
                    QueuePowerupAnimationForItem(item.Name, allowTransientEffects);
                    break;
                case "Hook Pull":
                    State.HasHookPull = true;
                    ShowPopup(PopupKind.Item, item.Name);
                    QueuePowerupAnimationForItem(item.Name, allowTransientEffects);
                    break;
                case "Finger Gun":
                    State.HasShoot = true;
                    ShowPopup(PopupKind.Item, item.Name);
                    QueuePowerupAnimationForItem(item.Name, allowTransientEffects);
                    break;
                case "Progressive Time Warp":
                    if (!State.HasSlowmo)
                    {
                        State.HasSlowmo = true;
                    }
                    else
                    {
                        State.ProgressiveSlowmoTime = ApData.ClampProgressiveLevel(State.ProgressiveSlowmoTime + 1);
                    }

                    ShowPopup(PopupKind.Item, item.Name + " Lv " + State.ProgressiveSlowmoTime.ToString());
                    QueuePowerupAnimationForItem(item.Name, allowTransientEffects);
                    break;
                case "Progressive Hook Reach":
                    State.ProgressiveReach = ApData.ClampProgressiveLevel(State.ProgressiveReach + 1);
                    ShowPopup(PopupKind.Item, item.Name + " Lv " + State.ProgressiveReach.ToString());
                    QueuePowerupAnimationForItem(item.Name, allowTransientEffects);
                    break;
                case "Random Gravity Direction Trap":
                    if (allowTransientEffects)
                    {
                        QueueTrap(TrapKind.RandomGravityDirection, item.Name);
                    }
                    break;
                case "Increased Gravity Trap":
                    if (allowTransientEffects)
                    {
                        QueueTrap(TrapKind.IncreasedGravity, item.Name);
                    }
                    break;
                default:
                    SafeLog("Received Archipelago item without gameplay handling yet: " + item.Name);
                    ShowPopup(PopupKind.Info, item.Name);
                    break;
            }

            SafeLog("Received Archipelago item: " + item.Name);
            _lastAppliedPlayer = null;
            _lastAppliedSignature = string.Empty;
        }

        private void MergeCheckedLocationsFromServer(List<long> locationIds)
        {
            if (locationIds == null || _generatedData == null)
            {
                return;
            }

            for (int i = 0; i < locationIds.Count; i++)
            {
                string checkKey;
                if (_generatedData.TryGetCheckKey(locationIds[i], out checkKey))
                {
                    State.CompletedChecks.Add(checkKey);
                }
            }
        }

        private List<long> GetCompletedLocationIds()
        {
            List<long> ids = new List<long>();
            if (_generatedData == null)
            {
                return ids;
            }

            foreach (string checkId in State.CompletedChecks)
            {
                long locationId;
                if (_generatedData.TryGetLocationId(checkId, out locationId))
                {
                    ids.Add(locationId);
                }
            }

            return ids;
        }

        private void TrySendCheckToArchipelago(string checkId)
        {
            if (_apClient == null || !_apClient.IsAuthenticated || _generatedData == null || string.IsNullOrEmpty(checkId))
            {
                return;
            }

            long locationId;
            if (_generatedData.TryGetLocationId(checkId, out locationId))
            {
                List<long> ids = new List<long>(1);
                ids.Add(locationId);
                _apClient.SendLocationChecks(ids);
            }
        }

        private void TryReportGoalCompletion()
        {
            if (_apClient == null || !_apClient.IsAuthenticated || State.GoalReported || _slotData == null)
            {
                return;
            }

            if (!IsGoalSatisfied())
            {
                return;
            }

            _apClient.SendGoalStatus();
            State.GoalReported = true;
            PersistState();
            SafeLog("Reported Archipelago goal completion.");
            if (_popupUi != null)
            {
                _popupUi.ShowGoal("Goal requirements fulfilled.");
            }
        }

        private bool IsGoalSatisfied()
        {
            string goalMode = _slotData.GoalMode ?? string.Empty;
            if (goalMode.Length == 0)
            {
                return false;
            }

            if (string.Equals(goalMode, "chosen_goal_levels", StringComparison.OrdinalIgnoreCase)
                || string.Equals(goalMode, "goal_levels", StringComparison.OrdinalIgnoreCase))
            {
                if (_slotData.GoalLevelIds == null || _slotData.GoalLevelIds.Count == 0)
                {
                    return false;
                }

                int requiredRank = Mathf.Clamp(_slotData.HighestRequiredRank, 0, 3);
                foreach (string levelId in _slotData.GoalLevelIds)
                {
                    string requiredCheckId = levelId + "::rank::" + requiredRank.ToString();
                    if (!State.CompletedChecks.Contains(requiredCheckId))
                    {
                        return false;
                    }
                }

                return true;
            }

            if (string.Equals(goalMode, "set_diamond_count", StringComparison.OrdinalIgnoreCase)
                || string.Equals(goalMode, "diamond_count", StringComparison.OrdinalIgnoreCase))
            {
                return State.ReceivedDiamonds >= _slotData.GoalDiamondCount && _slotData.GoalDiamondCount > 0;
            }

            return false;
        }

        private void RegisterCheck(string checkId, string label)
        {
            if (State.CompletedChecks.Add(checkId))
            {
                Discovery.AppendLine("checks.log", DateTime.UtcNow.ToString("o") + "\t" + checkId);
                SafeLog(label + " completed: " + checkId);
                ShowCheckPopup(checkId);
                TrySendCheckToArchipelago(checkId);
                PersistState();
            }
        }

        private void ShowCheckPopup(string checkId)
        {
            if (_popupUi == null || _generatedData == null || string.IsNullOrEmpty(checkId))
            {
                return;
            }

            GeneratedApLocation location;
            if (_generatedData.TryGetLocation(checkId, out location) && location != null && !string.IsNullOrEmpty(location.Name))
            {
                _popupUi.ShowCheck(location.Name);
            }
            else
            {
                _popupUi.ShowCheck(checkId);
            }
        }

        private void ShowPopup(PopupKind kind, string message)
        {
            if (_popupUi == null || string.IsNullOrEmpty(message))
            {
                return;
            }

            switch (kind)
            {
                case PopupKind.Item:
                    _popupUi.ShowItem(message);
                    break;
                case PopupKind.Trap:
                    _popupUi.ShowTrap(message);
                    break;
                default:
                    _popupUi.ShowInfo("Archipelago", message);
                    break;
            }
        }

        private void ApplyGameplayState(PlayerController player)
        {
            if (player == null || player.PlayerData == null || player.Hook == null)
            {
                return;
            }

            CaptureBaseValues(player);

            ArchipelagoState effective = GetEffectiveState();
            string signature = effective.BuildSignature();
            if (ReferenceEquals(player, _lastAppliedPlayer) && signature == _lastAppliedSignature)
            {
                return;
            }

            SO_PlayerData playerData = player.PlayerData;
            AllowAirDashField.SetValue(playerData, effective.HasDoubleJump);
            CanAirDashField.SetValue(playerData, effective.HasDoubleJump);
            player.SetAllowShoot(effective.HasShoot);
            player.SetAllowShootAnim(effective.HasShoot ? 1 : 0);
            player.SetAllowTimeWarp(effective.HasSlowmo);
            player.SetAllowTimeWarpAnim(effective.HasSlowmo ? 1 : 0);
            CanSlowTimeField.SetValue(playerData, effective.HasSlowmo);

            float slowmoMultiplier = ApData.GetSlowmoMultiplier(effective.ProgressiveSlowmoTime);
            float reachMultiplier = ApData.GetReachMultiplier(effective.ProgressiveReach);

            playerData.EnergyMax = _baseEnergyMax * slowmoMultiplier;
            float currentEnergy = (float)ActiveEnergyField.GetValue(playerData);
            if (currentEnergy > playerData.EnergyMax)
            {
                ActiveEnergyField.SetValue(playerData, playerData.EnergyMax);
            }

            player.Hook.MaxChainLength = _baseHookRange * reachMultiplier;
            player.Hook.TimeWarpChainBonus = _baseTimeWarpChainBonus * reachMultiplier;

            _lastAppliedPlayer = player;
            _lastAppliedSignature = signature;
        }

        private void CaptureBaseValues(PlayerController player)
        {
            if (_capturedBaseValues)
            {
                return;
            }

            _baseEnergyMax = player.PlayerData.EnergyMax;
            _baseHookRange = player.Hook.MaxChainLength;
            _baseTimeWarpChainBonus = player.Hook.TimeWarpChainBonus;
            _capturedBaseValues = true;
        }
    }
}
