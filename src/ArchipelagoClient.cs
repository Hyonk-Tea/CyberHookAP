using System;
using System.Collections.Generic;
using System.Threading;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Newtonsoft.Json.Linq;

namespace CyberHookAP
{
    internal sealed class ArchipelagoClient
    {
        private readonly Queue<ClientEvent> _pendingEvents = new Queue<ClientEvent>();
        private readonly object _pendingEventsLock = new object();
        private readonly object _connectionLock = new object();

        private ArchipelagoSession _session;
        private DeathLinkService _deathLinkService;
        private Thread _connectThread;
        private bool _manualDisconnect;
        private bool _connectPending;
        private DateTime _lastConnectAttemptUtc = DateTime.MinValue;
        private string _configuredConnectionSignature = string.Empty;
        private string _configuredUrl = string.Empty;
        private string _configuredSlot = string.Empty;
        private string _configuredPassword = string.Empty;
        private string _configuredUuid = string.Empty;

        internal bool IsAuthenticated { get; private set; }
        internal bool IsConnecting { get; private set; }
        internal string StatusText { get; private set; }

        internal event Action<ArchipelagoConnectedPayload> Connected;
        internal event Action<int, List<ArchipelagoNetworkItem>> ReceivedItems;
        internal event Action<List<long>> CheckedLocationsUpdated;
        internal event Action<string> TextMessage;
        internal event Action<string> StatusChanged;
        internal event Action<ArchipelagoDeathLinkPayload> DeathLinkReceived;

        internal ArchipelagoClient()
        {
            StatusText = "Disconnected";
        }

        internal void EnsureConnected(ModSettings settings)
        {
            if (settings == null || settings.Connection == null)
            {
                return;
            }

            if (!settings.Connection.AutoConnect || string.IsNullOrEmpty(settings.Connection.Slot))
            {
                if (_session != null || IsConnecting || IsAuthenticated)
                {
                    Disconnect(true, "Autoconnect disabled.");
                }

                return;
            }

            string url = BuildWebSocketUrl(settings.Connection.Server, settings.Connection.Port);
            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            string nextConfiguredUrl = url;
            string nextConfiguredSlot = settings.Connection.Slot ?? string.Empty;
            string nextConfiguredPassword = settings.Connection.Password ?? string.Empty;
            string nextConfiguredUuid = settings.Connection.ClientUuid ?? string.Empty;
            string nextConfiguredConnectionSignature = string.Join(
                "|",
                nextConfiguredUrl,
                nextConfiguredSlot,
                nextConfiguredPassword,
                nextConfiguredUuid);

            if (_configuredConnectionSignature.Length > 0
                && !string.Equals(_configuredConnectionSignature, nextConfiguredConnectionSignature, StringComparison.Ordinal)
                && (_session != null || IsConnecting || IsAuthenticated))
            {
                _configuredUrl = nextConfiguredUrl;
                _configuredSlot = nextConfiguredSlot;
                _configuredPassword = nextConfiguredPassword;
                _configuredUuid = nextConfiguredUuid;
                _configuredConnectionSignature = nextConfiguredConnectionSignature;
                Disconnect(false, "Connection settings changed.");
                return;
            }

            _configuredUrl = nextConfiguredUrl;
            _configuredSlot = nextConfiguredSlot;
            _configuredPassword = nextConfiguredPassword;
            _configuredUuid = nextConfiguredUuid;
            _configuredConnectionSignature = nextConfiguredConnectionSignature;

            if (_session != null || IsConnecting || IsAuthenticated || _connectPending)
            {
                return;
            }

            if ((DateTime.UtcNow - _lastConnectAttemptUtc).TotalSeconds < 3)
            {
                return;
            }

            Uri uri;
            if (!TryCreateUri(_configuredUrl, out uri))
            {
                RaiseTextMessage("Archipelago connect failed: invalid websocket URL.");
                return;
            }

            _lastConnectAttemptUtc = DateTime.UtcNow;
            _manualDisconnect = false;
            _connectPending = true;
            IsConnecting = true;
            IsAuthenticated = false;
            StatusText = "Connecting";
            RaiseStatusChanged(StatusText);

            _connectThread = new Thread(ConnectWorker);
            _connectThread.IsBackground = true;
            _connectThread.Name = "CyberHookAP-AP-Connect";
            _connectThread.Start(uri);
        }

        internal void Disconnect(bool manual, string reason)
        {
            _manualDisconnect = manual;
            _connectPending = false;

            ArchipelagoSession session;
            lock (_connectionLock)
            {
                session = _session;
                _session = null;
                _deathLinkService = null;
            }

            if (session != null)
            {
                try
                {
                    session.Socket.Disconnect();
                }
                catch
                {
                }
            }

            IsConnecting = false;
            IsAuthenticated = false;
            StatusText = "Disconnected";
            RaiseStatusChanged(StatusText);

            if (!string.IsNullOrEmpty(reason))
            {
                RaiseTextMessage(reason);
            }
        }

        internal void Tick()
        {
            ClientEvent evt;
            while (TryDequeue(out evt))
            {
                switch (evt.Type)
                {
                    case ClientEventType.Opened:
                        _connectPending = false;
                        StatusText = "Handshake";
                        RaiseStatusChanged(StatusText);
                        break;
                    case ClientEventType.Authenticating:
                        StatusText = "Authenticating";
                        RaiseStatusChanged(StatusText);
                        break;
                    case ClientEventType.Closed:
                        HandleClosed(evt.Message);
                        break;
                    case ClientEventType.Error:
                        HandleError(evt.Message);
                        break;
                    case ClientEventType.TextMessage:
                        RaiseTextMessage(evt.Message);
                        break;
                    case ClientEventType.Connected:
                        HandleConnected((ArchipelagoConnectedPayload)evt.Payload);
                        break;
                    case ClientEventType.ReceivedItems:
                        HandleReceivedItems((ReceivedItemsPayload)evt.Payload);
                        break;
                    case ClientEventType.CheckedLocationsUpdated:
                        HandleCheckedLocationsUpdated((List<long>)evt.Payload);
                        break;
                    case ClientEventType.DeathLinkReceived:
                        HandleDeathLinkReceived((ArchipelagoDeathLinkPayload)evt.Payload);
                        break;
                }
            }
        }

        internal void SendLocationChecks(List<long> locationIds)
        {
            if (!IsAuthenticated || locationIds == null || locationIds.Count == 0)
            {
                return;
            }

            ArchipelagoSession session = GetActiveSession();
            if (session == null)
            {
                return;
            }

            try
            {
                session.Locations.CompleteLocationChecks(locationIds.ToArray());
            }
            catch (Exception ex)
            {
                RaiseTextMessage("Archipelago send failed: " + ex.Message);
            }
        }

        internal void RequestSync(List<long> locationIds)
        {
            if (!IsAuthenticated)
            {
                return;
            }

            ArchipelagoSession session = GetActiveSession();
            if (session == null)
            {
                return;
            }

            try
            {
                session.Socket.SendPacket(new SyncPacket());
                if (locationIds != null && locationIds.Count > 0)
                {
                    session.Locations.CompleteLocationChecks(locationIds.ToArray());
                }
            }
            catch (Exception ex)
            {
                RaiseTextMessage("Archipelago sync failed: " + ex.Message);
            }
        }

        internal void SendPlayingStatus()
        {
            if (!IsAuthenticated)
            {
                return;
            }

            ArchipelagoSession session = GetActiveSession();
            if (session == null)
            {
                return;
            }

            try
            {
                session.SetClientState(ArchipelagoClientState.ClientPlaying);
            }
            catch (Exception ex)
            {
                RaiseTextMessage("Archipelago status update failed: " + ex.Message);
            }
        }

        internal void SendGoalStatus()
        {
            if (!IsAuthenticated)
            {
                return;
            }

            ArchipelagoSession session = GetActiveSession();
            if (session == null)
            {
                return;
            }

            try
            {
                session.SetGoalAchieved();
            }
            catch (Exception ex)
            {
                RaiseTextMessage("Archipelago goal report failed: " + ex.Message);
            }
        }

        internal void SendText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            if (!IsAuthenticated)
            {
                RaiseTextMessage("Archipelago send failed: not connected.");
                return;
            }

            ArchipelagoSession session = GetActiveSession();
            if (session == null)
            {
                RaiseTextMessage("Archipelago send failed: no active session.");
                return;
            }

            try
            {
                session.Say(text);
            }
            catch (Exception ex)
            {
                RaiseTextMessage("Archipelago send failed: " + ex.Message);
            }
        }

        internal void SetDeathLinkEnabled(bool enabled)
        {
            if (_deathLinkService == null)
            {
                return;
            }

            try
            {
                if (enabled)
                {
                    _deathLinkService.EnableDeathLink();
                }
                else
                {
                    _deathLinkService.DisableDeathLink();
                }
            }
            catch (Exception ex)
            {
                RaiseTextMessage("Archipelago deathlink update failed: " + ex.Message);
            }
        }

        internal void SendDeathLink(string source, string cause)
        {
            if (!IsAuthenticated || _deathLinkService == null || string.IsNullOrEmpty(source))
            {
                return;
            }

            try
            {
                _deathLinkService.SendDeathLink(new DeathLink(source, cause));
            }
            catch (Exception ex)
            {
                RaiseTextMessage("Archipelago deathlink send failed: " + ex.Message);
            }
        }

        private void ConnectWorker(object state)
        {
            Uri uri = state as Uri;
            if (uri == null)
            {
                Enqueue(new ClientEvent(ClientEventType.Error, "Invalid websocket URI."));
                return;
            }

            try
            {
                ArchipelagoSession session = ArchipelagoSessionFactory.CreateSession(uri);
                WireSessionEvents(session);

                lock (_connectionLock)
                {
                    _session = session;
                    _deathLinkService = session.CreateDeathLinkService();
                    _deathLinkService.OnDeathLinkReceived += OnDeathLinkReceivedInternal;
                }

                LoginResult result = session.TryConnectAndLogin(
                    GeneratedApData.GameName,
                    _configuredSlot,
                    ItemsHandlingFlags.AllItems,
                    new Version(0, 6, 0),
                    new[] { "AP" },
                    _configuredUuid,
                    string.IsNullOrEmpty(_configuredPassword) ? null : _configuredPassword,
                    true);

                LoginSuccessful success = result as LoginSuccessful;
                if (success != null)
                {
                    ArchipelagoConnectedPayload payload = new ArchipelagoConnectedPayload();
                    payload.Team = success.Team;
                    payload.Slot = success.Slot;
                    payload.CheckedLocations = new List<long>(session.Locations.AllLocationsChecked);
                    payload.MissingLocations = new List<long>(session.Locations.AllMissingLocations);
                    payload.SlotData = success.SlotData != null ? JToken.FromObject(success.SlotData) : null;
                    payload.SlotName = _configuredSlot;
                    payload.SeedName = session.RoomState.Seed ?? string.Empty;
                    Enqueue(new ClientEvent(ClientEventType.Connected, payload));

                    if (session.Items.AllItemsReceived.Count > 0)
                    {
                        List<ArchipelagoNetworkItem> items = ConvertItems(session.Items.AllItemsReceived);
                        ReceivedItemsPayload itemsPayload = new ReceivedItemsPayload();
                        itemsPayload.StartIndex = 0;
                        itemsPayload.Items = items;
                        Enqueue(new ClientEvent(ClientEventType.ReceivedItems, itemsPayload));
                    }

                    return;
                }

                LoginFailure failure = result as LoginFailure;
                string message = "Connection failed.";
                if (failure != null)
                {
                    message = "Archipelago refused connection: " + string.Join(", ", failure.Errors ?? new string[0]);
                }

                Enqueue(new ClientEvent(ClientEventType.Error, message));
                SafeDisconnectSession(session);
                Enqueue(new ClientEvent(ClientEventType.Closed, string.Empty));
            }
            catch (Exception ex)
            {
                SafeDisconnectSession(GetActiveSession());
                Enqueue(new ClientEvent(ClientEventType.Error, ex.Message));
                Enqueue(new ClientEvent(ClientEventType.Closed, string.Empty));
            }
        }

        private void WireSessionEvents(ArchipelagoSession session)
        {
            if (session == null)
            {
                return;
            }

            session.Socket.SocketOpened += OnSocketOpened;
            session.Socket.SocketClosed += OnSocketClosed;
            session.Socket.ErrorReceived += OnSocketError;
            session.Socket.PacketReceived += OnSocketPacketReceived;
            session.Items.ItemReceived += OnItemReceived;
            session.Locations.CheckedLocationsUpdated += OnCheckedLocationsUpdated;
            session.MessageLog.OnMessageReceived += OnMessageReceived;
        }

        private void OnSocketOpened()
        {
            Enqueue(new ClientEvent(ClientEventType.Opened, string.Empty));
        }

        private void OnSocketClosed(string reason)
        {
            Enqueue(new ClientEvent(ClientEventType.Closed, reason ?? string.Empty));
        }

        private void OnSocketError(Exception exception, string message)
        {
            string error = !string.IsNullOrEmpty(message)
                ? message
                : (exception != null ? exception.Message : "Unknown socket error.");
            Enqueue(new ClientEvent(ClientEventType.Error, error));
        }

        private void OnSocketPacketReceived(ArchipelagoPacketBase packet)
        {
            if (packet is RoomInfoPacket)
            {
                Enqueue(new ClientEvent(ClientEventType.Authenticating, string.Empty));
            }
        }

        private void OnItemReceived(ReceivedItemsHelper helper)
        {
            if (helper == null || !helper.Any())
            {
                return;
            }

            ItemInfo item = helper.DequeueItem();
            if (item == null)
            {
                return;
            }

            List<ArchipelagoNetworkItem> items = new List<ArchipelagoNetworkItem>(1);
            items.Add(ConvertItem(item));

            ReceivedItemsPayload payload = new ReceivedItemsPayload();
            payload.StartIndex = Math.Max(0, helper.Index - 1);
            payload.Items = items;
            Enqueue(new ClientEvent(ClientEventType.ReceivedItems, payload));
        }

        private void OnCheckedLocationsUpdated(System.Collections.ObjectModel.ReadOnlyCollection<long> newCheckedLocations)
        {
            if (newCheckedLocations == null || newCheckedLocations.Count == 0)
            {
                return;
            }

            Enqueue(new ClientEvent(ClientEventType.CheckedLocationsUpdated, new List<long>(newCheckedLocations)));
        }

        private void OnMessageReceived(Archipelago.MultiClient.Net.MessageLog.Messages.LogMessage message)
        {
            if (message == null)
            {
                return;
            }

            string text = message.ToString();
            if (!string.IsNullOrEmpty(text))
            {
                Enqueue(new ClientEvent(ClientEventType.TextMessage, text));
            }
        }

        private void OnDeathLinkReceivedInternal(DeathLink deathLink)
        {
            if (deathLink == null)
            {
                return;
            }

            ArchipelagoDeathLinkPayload payload = new ArchipelagoDeathLinkPayload();
            payload.Source = deathLink.Source ?? string.Empty;
            payload.Cause = deathLink.Cause ?? string.Empty;
            payload.TimestampUtc = deathLink.Timestamp;
            Enqueue(new ClientEvent(ClientEventType.DeathLinkReceived, payload));
        }

        private void HandleConnected(ArchipelagoConnectedPayload payload)
        {
            _connectPending = false;
            IsConnecting = false;
            IsAuthenticated = true;
            StatusText = "Connected";
            RaiseStatusChanged(StatusText);

            Action<ArchipelagoConnectedPayload> connected = Connected;
            if (connected != null)
            {
                connected(payload);
            }
        }

        private void HandleReceivedItems(ReceivedItemsPayload payload)
        {
            if (payload == null)
            {
                return;
            }

            Action<int, List<ArchipelagoNetworkItem>> receivedItems = ReceivedItems;
            if (receivedItems != null)
            {
                receivedItems(payload.StartIndex, payload.Items);
            }
        }

        private void HandleCheckedLocationsUpdated(List<long> locationIds)
        {
            Action<List<long>> checkedLocationsUpdated = CheckedLocationsUpdated;
            if (checkedLocationsUpdated != null)
            {
                checkedLocationsUpdated(locationIds);
            }
        }

        private void HandleDeathLinkReceived(ArchipelagoDeathLinkPayload payload)
        {
            Action<ArchipelagoDeathLinkPayload> deathLinkReceived = DeathLinkReceived;
            if (deathLinkReceived != null)
            {
                deathLinkReceived(payload);
            }
        }

        private void HandleClosed(string reason)
        {
            ArchipelagoSession session;
            lock (_connectionLock)
            {
                session = _session;
                _session = null;
            }

            SafeDisconnectSession(session);

            bool wasManual = _manualDisconnect;
            _manualDisconnect = false;
            _connectPending = false;
            IsConnecting = false;
            IsAuthenticated = false;
            StatusText = "Disconnected";
            RaiseStatusChanged(StatusText);

            if (!string.IsNullOrEmpty(reason))
            {
                RaiseTextMessage("Archipelago disconnected: " + reason);
            }
            else if (!wasManual)
            {
                RaiseTextMessage("Archipelago disconnected.");
            }
        }

        private void HandleError(string error)
        {
            if (!string.IsNullOrEmpty(error))
            {
                RaiseTextMessage("Archipelago socket error: " + error);
            }
        }

        private static List<ArchipelagoNetworkItem> ConvertItems(System.Collections.ObjectModel.ReadOnlyCollection<ItemInfo> allItems)
        {
            List<ArchipelagoNetworkItem> items = new List<ArchipelagoNetworkItem>();
            if (allItems == null)
            {
                return items;
            }

            for (int i = 0; i < allItems.Count; i++)
            {
                ItemInfo item = allItems[i];
                if (item != null)
                {
                    items.Add(ConvertItem(item));
                }
            }

            return items;
        }

        private static ArchipelagoNetworkItem ConvertItem(ItemInfo item)
        {
            ArchipelagoNetworkItem networkItem = new ArchipelagoNetworkItem();
            networkItem.Item = item.ItemId;
            networkItem.Location = item.LocationId;
            networkItem.Player = item.Player != null ? item.Player.Slot : 0;
            networkItem.Flags = (int)item.Flags;
            return networkItem;
        }

        private ArchipelagoSession GetActiveSession()
        {
            lock (_connectionLock)
            {
                return _session;
            }
        }

        private static void SafeDisconnectSession(ArchipelagoSession session)
        {
            if (session == null)
            {
                return;
            }

            try
            {
                session.Socket.Disconnect();
            }
            catch
            {
            }
        }

        private static bool TryCreateUri(string url, out Uri uri)
        {
            try
            {
                uri = new Uri(url);
                return true;
            }
            catch
            {
                uri = null;
                return false;
            }
        }

        private static string BuildWebSocketUrl(string server, int port)
        {
            if (string.IsNullOrEmpty(server))
            {
                return string.Empty;
            }

            string normalizedServer = server.Trim();

            if (normalizedServer.StartsWith("archipelago://", StringComparison.OrdinalIgnoreCase))
            {
                string hostPart = normalizedServer.Substring("archipelago://".Length);
                return IsSecureWebHost(hostPart) ? "wss://" + hostPart : "ws://" + hostPart;
            }

            if (normalizedServer.StartsWith("ws://", StringComparison.OrdinalIgnoreCase))
            {
                return normalizedServer;
            }

            if (normalizedServer.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
            {
                return normalizedServer;
            }

            return (IsSecureWebHost(normalizedServer) ? "wss://" : "ws://") + normalizedServer + ":" + port.ToString();
        }

        private static bool IsSecureWebHost(string server)
        {
            if (string.IsNullOrEmpty(server))
            {
                return false;
            }

            string host = server.Trim();
            int slashIndex = host.IndexOf('/');
            if (slashIndex >= 0)
            {
                host = host.Substring(0, slashIndex);
            }

            int colonIndex = host.IndexOf(':');
            if (colonIndex >= 0)
            {
                host = host.Substring(0, colonIndex);
            }

            return host.EndsWith("archipelago.gg", StringComparison.OrdinalIgnoreCase);
        }

        private void Enqueue(ClientEvent evt)
        {
            lock (_pendingEventsLock)
            {
                _pendingEvents.Enqueue(evt);
            }
        }

        private bool TryDequeue(out ClientEvent evt)
        {
            lock (_pendingEventsLock)
            {
                if (_pendingEvents.Count > 0)
                {
                    evt = _pendingEvents.Dequeue();
                    return true;
                }
            }

            evt = default(ClientEvent);
            return false;
        }

        private void RaiseTextMessage(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                Action<string> textMessage = TextMessage;
                if (textMessage != null)
                {
                    textMessage(message);
                }
            }
        }

        private void RaiseStatusChanged(string message)
        {
            Action<string> statusChanged = StatusChanged;
            if (statusChanged != null)
            {
                statusChanged(message);
            }
        }

        private struct ClientEvent
        {
            public readonly ClientEventType Type;
            public readonly string Message;
            public readonly object Payload;

            public ClientEvent(ClientEventType type, string message)
            {
                Type = type;
                Message = message ?? string.Empty;
                Payload = null;
            }

            public ClientEvent(ClientEventType type, object payload)
            {
                Type = type;
                Message = string.Empty;
                Payload = payload;
            }
        }

        private enum ClientEventType
        {
            Opened,
            Authenticating,
            Closed,
            Error,
            TextMessage,
            Connected,
            ReceivedItems,
            CheckedLocationsUpdated,
            DeathLinkReceived
        }

        private sealed class ReceivedItemsPayload
        {
            public int StartIndex;
            public List<ArchipelagoNetworkItem> Items;
        }
    }

    internal sealed class ArchipelagoConnectedPayload
    {
        public int Team;
        public int Slot;
        public string SeedName;
        public string SlotName;
        public List<long> CheckedLocations;
        public List<long> MissingLocations;
        public JToken SlotData;
    }

    internal sealed class ArchipelagoNetworkItem
    {
        public long Item;
        public long Location;
        public int Player;
        public int Flags;
    }

    internal sealed class ArchipelagoDeathLinkPayload
    {
        public string Source;
        public string Cause;
        public DateTime TimestampUtc;
    }
}
