using Engine;
using Lidgren.Network;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using SpaceNetwork;
using SpaceNetwork.Messages;
using SpaceNetwork.Utilities;
using System.Collections.Concurrent;

namespace Client
{
    public class ClientNetwork
    {
        public static ClientNetwork Instance { get; set; }
        private NetClient _client;
        private NetConnection _serverConnection;
        private Dictionary<int, RemoteAstronaut> _remotePlayers = new Dictionary<int, RemoteAstronaut>();
        private int _localPlayerId = -1;

        private readonly Dictionary<Type, Action<BaseMessage>> _messageHandlers = new Dictionary<Type, Action<BaseMessage>>();

        private readonly ConcurrentQueue<(BaseMessage Message, NetDeliveryMethod Method)> _sendQueue = new ConcurrentQueue<(BaseMessage, NetDeliveryMethod)>();
        private Thread _senderThread;
        private volatile bool _isRunning;

        public bool IsInitialized { get; private set; }
        public bool NameInUse { get; private set; }
        public bool IsConnected { get; private set; }
        public bool IsKicked { get; private set; }
        public bool ZipDownloaded { get; private set; }
        public bool ServerInfoReceived { get; private set; }
        public ServerInfo ReceivedServerInfo { get; private set; }

        public event Action OnServerInfoReceived;
        public event Action OnZipDownloadStart;
        public event Action OnZipDownloadComplete;
        public Action<RemoteAstronaut> OnPlayerJoined;
        public Action<RemoteAstronaut> OnPlayerLeft;
        public event Action<int, int, int> OnBlockDestroyed;
        public event Action<BlockPlaceMessage> OnBlockPlaced;

        public int LocalPlayerId => _localPlayerId;

        public ClientNetwork(string appKey, string host, int port, string playerName)
        {
            Instance = this;
            var config = new NetPeerConfiguration(appKey);
            _client = new NetClient(config);
            _client.Start();

            var hail = _client.CreateMessage();
            hail.Write(playerName);
            _client.Connect(host, port, hail);

            RegisterHandlers();
            StartSenderThread();
            IsKicked = false;
        }

        private void RegisterHandlers()
        {
            _messageHandlers.Add(typeof(InitMessage), HandleInit);
            _messageHandlers.Add(typeof(PlayersMessage), HandlePlayers);
            _messageHandlers.Add(typeof(ServerInfoMessage), HandleServerInfo);
            _messageHandlers.Add(typeof(KickMessage), HandleKick);
            _messageHandlers.Add(typeof(SpaceNetwork.Messages.ChatMessage), HandleChat);
            _messageHandlers.Add(typeof(BlockDestroyedMessage), HandleBlockDestroyed);
            _messageHandlers.Add(typeof(BlockPlaceMessage), HandleBlockPlaced);
            _messageHandlers.Add(typeof(FlashlightMessage), HandleFlashlight);
            _messageHandlers.Add(typeof(ItemInHandMessage), HandleItemInHand);
            _messageHandlers.Add(typeof(ZipMessage), HandleZip);
        }

        private void StartSenderThread()
        {
            if (_senderThread != null) return;

            _isRunning = true;
            _senderThread = new Thread(SenderLoop)
            {
                IsBackground = true
            };
            _senderThread.Start();
        }

        private void SenderLoop()
        {
            while (_isRunning)
            {
                if (_sendQueue.TryDequeue(out var item))
                {
                    if (_serverConnection != null)
                    {
                        var om = _client.CreateMessage();
                        item.Message.Write(om);
                        _client.SendMessage(om, _serverConnection, item.Method);
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }

        public void Send<T>(T message, NetDeliveryMethod method) where T : BaseMessage
        {
            if (message == null) return;
            _sendQueue.Enqueue((message, method));
        }

        public void SendImmediate<T>(T message, NetDeliveryMethod method) where T : BaseMessage
        {
            if (_serverConnection == null) return;
            var om = _client.CreateMessage();
            message.Write(om);
            _client.SendMessage(om, _serverConnection, method);
        }

        public void PollEvents()
        {
            NetIncomingMessage msg;
            while ((msg = _client.ReadMessage()) != null)
            {
                switch (msg.MessageType)
                {
                    case NetIncomingMessageType.Data:
                        HandleData(msg);
                        break;
                    case NetIncomingMessageType.StatusChanged:
                        HandleStatusChanged(msg);
                        break;
                }
                _client.Recycle(msg);
            }
        }

        private void HandleStatusChanged(NetIncomingMessage msg)
        {
            var newStatus = (NetConnectionStatus)msg.ReadByte();
            string reason = msg.ReadString();

            if (newStatus == NetConnectionStatus.Connected)
            {
                _serverConnection = msg.SenderConnection;
                Debug.Log("Client connected to server.");
                IsConnected = true;
            }
            else if (newStatus == NetConnectionStatus.Disconnected)
            {
                _serverConnection = null;
                Debug.Log("Client disconnected from server. " + reason);

                if (reason != null && reason.Contains("DuplicateName"))
                    NameInUse = true;

                _isRunning = false;
                IsConnected = false;
            }
        }

        private void HandleData(NetIncomingMessage msg)
        {
            try
            {
                var baseMsg = MessageFactory.CreateMessage(msg);
                if (baseMsg != null && _messageHandlers.TryGetValue(baseMsg.GetType(), out var handler))
                {
                    handler(baseMsg);
                }
            }
            catch (Exception ex)
            {
                Debug.Error($"Error handling message: {ex.Message}");
            }
        }

        #region Message Handlers

        private void HandleInit(BaseMessage msg)
        {
            var im = (InitMessage)msg;
            _localPlayerId = im.Player.ID;
            AddOrUpdatePlayer(im.Player);
            IsInitialized = true;
        }

        private void HandlePlayers(BaseMessage msg)
        {
            var pm = (PlayersMessage)msg;
            foreach (var kvp in pm.Players)
            {
                if (kvp.Key == _localPlayerId) continue;
                AddOrUpdatePlayer(kvp.Value);
            }

            var removeList = _remotePlayers.Keys
                .Where(k => k != _localPlayerId && !pm.Players.ContainsKey(k))
                .ToList();

            foreach (var id in removeList)
            {
                if (_remotePlayers.TryGetValue(id, out var remote))
                {
                    OnPlayerLeft?.Invoke(remote);
                    _remotePlayers.Remove(id);
                }
            }
        }

        private void HandleServerInfo(BaseMessage msg)
        {
            var sim = (ServerInfoMessage)msg;
            ReceivedServerInfo = sim.Info;
            ServerInfoReceived = true;
            OnServerInfoReceived?.Invoke();
        }

        private void HandleKick(BaseMessage msg)
        {
            var km = (KickMessage)msg;
            Debug.Error(km.Reason);
            IsKicked = true;
            _client.Disconnect("Kicked");
        }

        private void HandleChat(BaseMessage msg)
        {
            var cm = (SpaceNetwork.Messages.ChatMessage)msg;
            string logMsg = cm.SenderId == -1
                ? $"[Server]: {cm.Text}"
                : $"> {cm.SenderName}[{cm.SenderId}]: {cm.Text}";

            Chat.Write(logMsg);
            Debug.Log(logMsg);
        }

        private void HandleBlockDestroyed(BaseMessage msg)
        {
            var bdm = (BlockDestroyedMessage)msg;
            if (bdm.senderID != _localPlayerId)
                OnBlockDestroyed?.Invoke(bdm.X, bdm.Y, bdm.Z);
        }

        private void HandleBlockPlaced(BaseMessage msg)
        {
            var bpm = (BlockPlaceMessage)msg;
            if (bpm.senderID != _localPlayerId)
                OnBlockPlaced?.Invoke(bpm);
        }

        private void HandleFlashlight(BaseMessage msg)
        {
            var message = (FlashlightMessage)msg;
            if (message.SenderId == _localPlayerId) return;

            if (_remotePlayers.TryGetValue(message.SenderId, out var remote))
            {
                if (remote.Flashlight != null)
                    remote.Flashlight.Enabled = message.State;
                else
                    Debug.Error($"Flashlight not initialized for player {message.SenderId}");
            }
        }

        private void HandleItemInHand(BaseMessage msg)
        {
            var message = (ItemInHandMessage)msg;
            if (message.SenderId == _localPlayerId) return;

            if (_remotePlayers.TryGetValue(message.SenderId, out var remote))
            {
                if (GameAssets.TryGetItemById(message.ItemId, out var item))
                    remote.ChangeItemInHand(item);
                else
                    remote.ChangeItemInHand(null);
            }
        }

        private void HandleZip(BaseMessage msg)
        {
            var zm = (ZipMessage)msg;
            ReceivedServerInfo.ModFolderName = zm.ModName;
            OnZipDownloadStart?.Invoke();

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string targetDir = Path.Combine(baseDir, "Server", ReceivedServerInfo?.Name ?? "Unknown", Path.Combine("GameSet", zm.ModName));

            if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

            string tempZip = Path.Combine(targetDir, "download.zip");
            File.WriteAllBytes(tempZip, zm.ZipData);
            ZipHelper.UnzipData(zm.ZipData, targetDir);
            File.Delete(tempZip);

            ZipDownloaded = true;
            OnZipDownloadComplete?.Invoke();
            Debug.Log("Zip received and unpacked.");
        }

        #endregion

        #region Public Senders

        public void SendMessage(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            Send(new SpaceNetwork.Messages.ChatMessage { SenderId = _localPlayerId, SenderName = "", Text = text }, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendBlockDestroyed(short x, short y, short z)
        {
            Send(new BlockDestroyedMessage(_localPlayerId, x, y, z), NetDeliveryMethod.ReliableOrdered);
        }

        public void SendBlockPlaced(Block block, short x, short y, short z)
        {
            Send(new BlockPlaceMessage(_localPlayerId, block.Id, (byte)block.Direction, (byte)block.Rotation, x, y, z), NetDeliveryMethod.ReliableOrdered);
        }

        public void SendFlashlightState(bool state)
        {
            Send(new FlashlightMessage { SenderId = _localPlayerId, State = state }, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendItemInHand(Item item)
        {
            Send(new ItemInHandMessage { SenderId = _localPlayerId, ItemId = (short)(item?.Id ?? 0) }, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendPosition(Vector3 pos, Quaternion rot)
        {
            var m = new Node3DMessage
            {
                Position = new System.Numerics.Vector3(pos.X, pos.Y, pos.Z),
                Rotation = new System.Numerics.Vector4(rot.X, rot.Y, rot.Z, rot.W)
            };
            SendImmediate(m, NetDeliveryMethod.Unreliable);
        }

        #endregion

        private void AddOrUpdatePlayer(Player p)
        {
            if (p.ID == _localPlayerId) return;

            if (_remotePlayers.TryGetValue(p.ID, out var remote))
            {
                remote.UpdateNetworkData(p);
            }
            else
            {
                remote = new RemoteAstronaut(p);
                _remotePlayers[p.ID] = remote;
                OnPlayerJoined?.Invoke(remote);
            }
        }

        public void Disconnect(string reason)
        {
            _client.Disconnect(reason);
            _isRunning = false;
            IsKicked = false;
            _localPlayerId = -1;
        }

        public List<RemoteAstronaut> GetRemotePlayers() => _remotePlayers.Values.ToList();
    }
}