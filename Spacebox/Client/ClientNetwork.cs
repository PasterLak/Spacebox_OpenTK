using Engine;
using Lidgren.Network;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using SpaceNetwork;
using SpaceNetwork.Messages;
using System.Collections.Concurrent;

namespace Client
{
    public class ClientNetwork
    {
        public static ClientNetwork Instance { get; set; }

        private NetClient _client;
        private NetConnection _serverConnection;
        private Thread _senderThread;
        private volatile bool _isRunning;
        private readonly ConcurrentQueue<(BaseMessage Message, NetDeliveryMethod Method)> _sendQueue = new ConcurrentQueue<(BaseMessage, NetDeliveryMethod)>();

        public NetworkPlayerRegistry Players { get; private set; }
        private ClientPacketHandler _packetHandler;

        public int LocalPlayerId { get; private set; } = -1;
        public bool IsInitialized { get; private set; }
        public bool IsConnected { get; private set; }
        public bool IsKicked { get; private set; }
        public bool NameInUse { get; private set; }
        public ServerInfo ReceivedServerInfo { get; private set; }
        public bool ServerInfoReceived { get; private set; }
        public bool ZipDownloaded { get; private set; } = false;

        public event Action OnServerInfoReceived;
        public event Action OnZipDownloadStart;
        public event Action OnZipDownloadComplete;
        public event Action<int, int, int> OnBlockDestroyed;
        public event Action<BlockPlaceMessage> OnBlockPlaced;

        public ClientNetwork(string appKey, string host, int port, string playerName)
        {
            Instance = this;
            var config = new NetPeerConfiguration(appKey);
            _client = new NetClient(config);
            _client.Start();

            var hail = _client.CreateMessage();
            hail.Write(playerName);
            _client.Connect(host, port, hail);

            Players = new NetworkPlayerRegistry(LocalPlayerId);
            _packetHandler = new ClientPacketHandler(this, Players);

            StartSenderThread();
            IsKicked = false;
        }

        private void StartSenderThread()
        {
            if (_senderThread != null) return;
            _isRunning = true;
            _senderThread = new Thread(SenderLoop) { IsBackground = true };
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

        public void PollEvents()
        {
            NetIncomingMessage msg;
            while ((msg = _client.ReadMessage()) != null)
            {
                switch (msg.MessageType)
                {
                    case NetIncomingMessageType.Data:
                        try
                        {
                            var baseMsg = MessageFactory.CreateMessage(msg);
                            if (baseMsg != null) _packetHandler.Handle(baseMsg);
                        }
                        catch (Exception ex)
                        {
                            Debug.Error($"Error processing message: {ex.Message}");
                        }
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
                if (reason != null && reason.Contains("DuplicateName")) NameInUse = true;
                _isRunning = false;
                IsConnected = false;
            }
        }

        #region Internal Methods for Handler

        public void SetLocalPlayerId(int id)
        {
            LocalPlayerId = id;
            Players = new NetworkPlayerRegistry(LocalPlayerId);
            _packetHandler = new ClientPacketHandler(this, Players);
        }
        public void MarkInitialized() => IsInitialized = true;
        public void SetServerInfo(ServerInfo info) { ReceivedServerInfo = info; ServerInfoReceived = true; OnServerInfoReceived?.Invoke(); }
        public void TriggerKick() => IsKicked = true;
        public void InvokeBlockDestroyed(int x, int y, int z) => OnBlockDestroyed?.Invoke(x, y, z);
        public void InvokeBlockPlaced(BlockPlaceMessage msg) => OnBlockPlaced?.Invoke(msg);
        public void InvokeZipStart()
        {
            ZipDownloaded = false;
            OnZipDownloadStart?.Invoke();
        }
        public void InvokeZipComplete()
        {
            ZipDownloaded = true;
            OnZipDownloadComplete?.Invoke();
        }

        #endregion

        #region Public API

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

        public void SendMessage(string text) => Send(new SpaceNetwork.Messages.ChatMessage { SenderId = LocalPlayerId, SenderName = "", Text = text }, NetDeliveryMethod.ReliableOrdered);
        public void SendBlockDestroyed(short x, short y, short z) => Send(new BlockDestroyedMessage(LocalPlayerId, x, y, z), NetDeliveryMethod.ReliableOrdered);
        public void SendBlockPlaced(Block block, short x, short y, short z) => Send(new BlockPlaceMessage(LocalPlayerId, block.Id, (byte)block.Direction, (byte)block.Rotation, x, y, z), NetDeliveryMethod.ReliableOrdered);
        public void SendFlashlightState(bool state) => Send(new FlashlightMessage { SenderId = LocalPlayerId, State = state }, NetDeliveryMethod.ReliableOrdered);
        public void SendItemInHand(ItemSlot item)
        {
            var itemInHand = item.HasItem ? item.Item : null;

            Send(new ItemInHandMessage { SenderId = LocalPlayerId, ItemId = (short)(itemInHand?.Id ?? 0) }, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendZipRequest()
        {
            Send(new RequestZipMessage(), NetDeliveryMethod.ReliableOrdered);
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

        public void Disconnect(string reason)
        {
            _client.Disconnect(reason);
            _isRunning = false;
            IsKicked = false;
            LocalPlayerId = -1;
        }

        public List<RemoteAstronaut> GetRemotePlayers() => Players.GetAll();

        #endregion
    }
}