
using System.Collections.Concurrent;

using Engine;
using Lidgren.Network;
using OpenTK.Mathematics;
using SpaceNetwork;
using SpaceNetwork.Messages;

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
        private volatile bool running;
        private ConcurrentQueue<string> chatQueue = new ConcurrentQueue<string>();
        public bool IsInitialized { get; private set; }
        public bool NameInUse { get; private set; }
        public bool IsConnected { get; private set; } = false;

        public Action<ClientPlayer> OnPlayerJoined;
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
        }

        public void SendMessage(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                chatQueue.Enqueue(message);
            }
              
        }

        private void StartChatThread()
        {
            if (chatThread != null)
                return;
            running = true;
            chatThread = new Thread(() =>
            {
                while (running)
                {
                    if (chatQueue.TryDequeue(out string chat))
                    {
                        if (serverConnection != null)
                        {
                            var m = new ChatMessage();
                            m.SenderId = localPlayerId;
                            m.SenderName = "";
                            m.Text = chat;
                            var om = client.CreateMessage();
                            m.Write(om);
                            client.SendMessage(om, serverConnection, NetDeliveryMethod.ReliableOrdered);
                        }
                    }
                    else
                    {
                        Thread.Sleep(50);
                    }
                }
            });
            chatThread.IsBackground = true;
            chatThread.Start();
        }

        public void StopChatThread()
        {
            running = false;
        }

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
                    Debug.Log($"[Server]: {cp.NetworkPlayer.Name}[{cp.NetworkPlayer.ID}] disconnected");
                    clientPlayers.Remove(id);
                }
            }
            else if (baseMsg is PositionMessage posMsg)
            {
               // posMsg.Read(msg);

            }
            else if (baseMsg is KickMessage km)
            {
                Debug.Log(km.Reason);
                client.Disconnect("Kicked");
            }
            else if (baseMsg is ChatMessage cm)
            {
                if (cm.SenderId == -1)
                    Debug.Log($"[Server]: {cm.Text}");
                else
                    Debug.Log($"> {cm.SenderName}[{cm.SenderId}]: {cm.Text}");
            }
        }

        private void AddOrUpdatePlayer(Player p)
        {
            if (clientPlayers.ContainsKey(p.ID))
            {
                clientPlayers[p.ID].UpdateFromNetwork(p);
            }
            else
            {
                Debug.Log("New player id" + p.ID);
                var cp = new ClientPlayer(p);
               
                clientPlayers[p.ID] = cp;
                OnPlayerJoined?.Invoke(clientPlayers[p.ID]);
            }
        }

        public void SendPosition(Vector3 pos, Vector3 rot)
        {
            if (serverConnection == null)
                return;
            var m = new PositionMessage();
            m.Position = new System.Numerics.Vector3(pos.X, pos.Y, pos.Z);
            m.Rotation = new System.Numerics.Vector3(rot.X, rot.Y, rot.Z);
            var om = client.CreateMessage();
            m.Write(om);
            client.SendMessage(om, serverConnection, NetDeliveryMethod.Unreliable);
        }

        public void Disconnect(string reason)
        {
            client.Disconnect(reason);
            StopChatThread();
        }

        public List<ClientPlayer> GetClientPlayers() => clientPlayers.Values.ToList();

        public int LocalPlayerId => localPlayerId;
    }
}
