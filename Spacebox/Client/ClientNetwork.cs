using System.Collections.Concurrent;
using Engine;
using Lidgren.Network;
using OpenTK.Mathematics;
using SpaceNetwork;
using SpaceNetwork.Messages;
using SpaceNetwork.Utilities;
using Spacebox.Game.GUI;

namespace Client
{
    public class ClientNetwork
    {
        public static ClientNetwork Instance { get; set; }
        private NetClient client;
        private NetConnection serverConnection;
        private Dictionary<int, ClientPlayer> clientPlayers = new Dictionary<int, ClientPlayer>();
        private int localPlayerId = -1;
        private Thread chatThread;
        private Thread blockDeletionThread;
        private Thread blockPlaceThread;
        private volatile bool chatRunning;
        private volatile bool blockDeletionRunning;
        private volatile bool blockPlaceRunning;
        private ConcurrentQueue<string> chatQueue = new ConcurrentQueue<string>();
        private ConcurrentQueue<BlockDestroyedMessage> blockDeletionQueue = new ConcurrentQueue<BlockDestroyedMessage>();
        private ConcurrentQueue<BlockPlaceMessage> blockPlaceQueue = new ConcurrentQueue<BlockPlaceMessage>();
        public bool IsInitialized { get; private set; }
        public bool NameInUse { get; private set; }
        public bool IsConnected { get; private set; } = false;
        public bool IsKicked { get; private set; }
        public bool ZipDownloaded { get; private set; }
        public bool ServerInfoReceived { get; private set; }
        public ServerInfo ReceivedServerInfo { get; private set; }
        public event Action OnServerInfoReceived;
        public event Action OnZipDownloadStart;
        public event Action OnZipDownloadComplete;
        public Action<ClientPlayer> OnPlayerJoined;
        public Action<ClientPlayer> OnPlayerLeft;
        public event Action<int, int, int> OnBlockDestroyed;
        public event Action<int, short, byte, short, short, short> OnBlockPlaced;
        public ClientNetwork(string appKey, string host, int port, string playerName)
        {
            Instance = this;
            var config = new NetPeerConfiguration(appKey);
            client = new NetClient(config);
            client.Start();
            var hail = client.CreateMessage();
            hail.Write(playerName);
            client.Connect(host, port, hail);
            StartChatThread();
            StartBlockDeletionThread();
            StartBlockPlaceThread();
            IsKicked = false;
        }
        public void SendMessage(string message)
        {
            if (!string.IsNullOrEmpty(message))
                chatQueue.Enqueue(message);
        }
        public void SendBlockDestroyed(short x, short y, short z)
        {
            var msg = new BlockDestroyedMessage(localPlayerId, x, y, z);
            blockDeletionQueue.Enqueue(msg);
        }
        public void SendBlockPlaced(short blockID, byte direction, short x, short y, short z)
        {
            var msg = new BlockPlaceMessage(localPlayerId, blockID, direction, x, y, z);
            blockPlaceQueue.Enqueue(msg);
        }
        private void StartChatThread()
        {
            if (chatThread != null)
                return;
            chatRunning = true;
            chatThread = new Thread(() =>
            {
                while (chatRunning)
                {
                    if (chatQueue.TryDequeue(out string chat))
                    {
                        if (serverConnection != null)
                        {
                            var m = new SpaceNetwork.Messages.ChatMessage { SenderId = localPlayerId, SenderName = "", Text = chat };
                            var om = client.CreateMessage();
                            m.Write(om);
                            client.SendMessage(om, serverConnection, NetDeliveryMethod.ReliableOrdered);
                        }
                    }
                    else
                        Thread.Sleep(50);
                }
            });
            chatThread.IsBackground = true;
            chatThread.Start();
        }
        private void StartBlockDeletionThread()
        {
            if (blockDeletionThread != null)
                return;
            blockDeletionRunning = true;
            blockDeletionThread = new Thread(() =>
            {
                while (blockDeletionRunning)
                {
                    if (blockDeletionQueue.TryDequeue(out BlockDestroyedMessage bdm))
                    {
                        if (serverConnection != null)
                        {
                            var om = client.CreateMessage();
                            bdm.Write(om);
                            client.SendMessage(om, serverConnection, NetDeliveryMethod.ReliableOrdered);
                        }
                    }
                    else
                        Thread.Sleep(50);
                }
            });
            blockDeletionThread.IsBackground = true;
            blockDeletionThread.Start();
        }
        private void StartBlockPlaceThread()
        {
            if (blockPlaceThread != null)
                return;
            blockPlaceRunning = true;
            blockPlaceThread = new Thread(() =>
            {
                while (blockPlaceRunning)
                {
                    if (blockPlaceQueue.TryDequeue(out BlockPlaceMessage bpm))
                    {
                        if (serverConnection != null)
                        {
                            var om = client.CreateMessage();
                            bpm.Write(om);
                            client.SendMessage(om, serverConnection, NetDeliveryMethod.ReliableOrdered);
                        }
                    }
                    else
                        Thread.Sleep(50);
                }
            });
            blockPlaceThread.IsBackground = true;
            blockPlaceThread.Start();
        }
        public void StopChatThread() => chatRunning = false;
        public void StopBlockDeletionThread() => blockDeletionRunning = false;
        public void StopBlockPlaceThread() => blockPlaceRunning = false;
        public void PollEvents()
        {
            NetIncomingMessage msg;
            while ((msg = client.ReadMessage()) != null)
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
                client.Recycle(msg);
            }
        }
        private void HandleStatusChanged(NetIncomingMessage msg)
        {
            var newStatus = (NetConnectionStatus)msg.ReadByte();
            string reason = msg.ReadString();
            if (newStatus == NetConnectionStatus.Connected)
            {
                serverConnection = msg.SenderConnection;
                Debug.Log("Client connected to server.");
                
                IsConnected = true;
            }
            else if (newStatus == NetConnectionStatus.Disconnected)
            {
                serverConnection = null;
                Debug.Log("Client disconnected from server. " + reason);
                if (reason.Contains("DuplicateName"))
                    NameInUse = true;
                StopChatThread();
                StopBlockDeletionThread();
                StopBlockPlaceThread();
                IsConnected = false;
            }
        }
        private void HandleData(NetIncomingMessage msg)
        {
            var baseMsg = MessageFactory.CreateMessage(msg);
            if (baseMsg is InitMessage im)
            {
                localPlayerId = im.Player.ID;
                AddOrUpdatePlayer(im.Player);
                IsInitialized = true;
            }
            else if (baseMsg is ServerInfoMessage sim)
            {
                ReceivedServerInfo = sim.Info;
                ServerInfoReceived = true;
                OnServerInfoReceived?.Invoke();
            }
            else if (baseMsg is PlayersMessage pm)
            {
                foreach (var kvp in pm.Players)
                {
                    if (kvp.Key == localPlayerId)
                        continue;
                    AddOrUpdatePlayer(kvp.Value);
                }
                var removeList = clientPlayers.Keys.Where(k => k != localPlayerId && !pm.Players.ContainsKey(k)).ToList();
                foreach (var id in removeList)
                {
                    var cp = clientPlayers[id];
                    OnPlayerLeft?.Invoke(cp);
                    clientPlayers.Remove(id);
                }
            }
            else if (baseMsg is KickMessage km)
            {
                Debug.Error(km.Reason);
                IsKicked = true;
                client.Disconnect("Kicked");
            }
            else if (baseMsg is SpaceNetwork.Messages.ChatMessage cm)
            {
                if (cm.SenderId == -1)
                {
                    Debug.Log($"[Server]: {cm.Text}");
                    Chat.Write($"[Server]: {cm.Text}");
                }

                else
                {
                    Chat.Write($"> {cm.SenderName}[{cm.SenderId}]: {cm.Text}");
                    Debug.Log($"> {cm.SenderName}[{cm.SenderId}]: {cm.Text}");
                }
                  
            }
            else if (baseMsg is BlockDestroyedMessage bdm)
            {
                if (bdm.senderID != localPlayerId)
                    OnBlockDestroyed?.Invoke(bdm.X, bdm.Y, bdm.Z);
            }
            else if (baseMsg is BlockPlaceMessage bpm)
            {
                if (bpm.senderID != localPlayerId)
                    OnBlockPlaced?.Invoke(bpm.senderID, bpm.blockID, bpm.GetDirection(), bpm.GetX(), bpm.GetY(), bpm.GetZ());
            }
            else if (baseMsg is ZipMessage zm)
            {
                ReceivedServerInfo.ModFolderName = zm.ModName;
                OnZipDownloadStart?.Invoke();
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string targetDir = Path.Combine(baseDir, "Server", ReceivedServerInfo?.Name ?? "Unknown", Path.Combine("GameSet", zm.ModName));
                if (!Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);
                string tempZip = Path.Combine(targetDir, "download.zip");
                File.WriteAllBytes(tempZip, zm.ZipData);
                ZipHelper.UnzipData(zm.ZipData, targetDir);
                File.Delete(tempZip);
                ZipDownloaded = true;
                OnZipDownloadComplete?.Invoke();
                Debug.Log("Zip received and unpacked.");
            }
        }
        private void AddOrUpdatePlayer(Player p)
        {
            if (clientPlayers.ContainsKey(p.ID))
                clientPlayers[p.ID].UpdateFromNetwork(p);
            else
            {
                var cp = new ClientPlayer(p);
                clientPlayers[p.ID] = cp;
                OnPlayerJoined?.Invoke(cp);
            }
        }
        public void SendPosition(Vector3 pos, Quaternion rot)
        {
            if (serverConnection == null)
                return;
            var m = new Node3DMessage
            {
                Position = new System.Numerics.Vector3(pos.X, pos.Y, pos.Z),
                Rotation = new System.Numerics.Vector4(rot.X, rot.Y, rot.Z, rot.W)
            };
            var om = client.CreateMessage();
            m.Write(om);
            client.SendMessage(om, serverConnection, NetDeliveryMethod.Unreliable);
        }
        public void Disconnect(string reason)
        {
            client.Disconnect(reason);
            StopChatThread();
            StopBlockDeletionThread();
            StopBlockPlaceThread();
            IsKicked = false;
        }
        public List<ClientPlayer> GetClientPlayers() => clientPlayers.Values.ToList();
        public int LocalPlayerId => localPlayerId;
    }
}
