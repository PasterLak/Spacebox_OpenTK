// File: ClientNetwork.cs
using System.Collections.Concurrent;
using Engine;
using Lidgren.Network;
using OpenTK.Mathematics;
using SpaceNetwork;
using SpaceNetwork.Messages;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;

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
        private Thread blockThread;
        private volatile bool running;
        private volatile bool blockRunning;
        private ConcurrentQueue<string> chatQueue = new ConcurrentQueue<string>();
        private ConcurrentQueue<BlockDestroyedMessage> blockQueue = new ConcurrentQueue<BlockDestroyedMessage>();
        public bool IsInitialized { get; private set; }
        public bool NameInUse { get; private set; }
        public bool IsConnected { get; private set; } = false;
        public Action<ClientPlayer> OnPlayerJoined;
        public Action<ClientPlayer> OnPlayerLeft;
        public event Action<int, int, int> OnBlockDestroyed;
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
            StartBlockThread();
        }
        public void SendMessage(string message)
        {
            if (!string.IsNullOrEmpty(message))
                chatQueue.Enqueue(message);
        }
        public void SendBlockDestroyed(int x, int y, int z)
        {
            var msg = new BlockDestroyedMessage();
            msg.senderID = localPlayerId;
            msg.X = x;
            msg.Y = y;
            msg.Z = z;

            Debug.Log($"block destroyed {x},{y},{z} id{localPlayerId}");
            blockQueue.Enqueue(msg);
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
                        Thread.Sleep(50);
                }
            });
            chatThread.IsBackground = true;
            chatThread.Start();
        }
        private void StartBlockThread()
        {
            if (blockThread != null)
                return;
            blockRunning = true;
            blockThread = new Thread(() =>
            {
                while (blockRunning)
                {
                    if (blockQueue.TryDequeue(out BlockDestroyedMessage bdm))
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
            blockThread.IsBackground = true;
            blockThread.Start();
        }
        public void StopChatThread()
        {
            running = false;
        }
        public void StopBlockThread()
        {
            blockRunning = false;
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
                StopBlockThread();
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
                    OnPlayerLeft?.Invoke(cp);
                    clientPlayers.Remove(id);
                }
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
            else if (baseMsg is BlockDestroyedMessage bdm)
            {
                if (bdm.senderID != localPlayerId) 
                OnBlockDestroyed?.Invoke(bdm.X, bdm.Y, bdm.Z);
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
            var m = new Node3DMessage();
            m.Position = new System.Numerics.Vector3(pos.X, pos.Y, pos.Z);
            m.Rotation = new System.Numerics.Vector4(rot.X, rot.Y, rot.Z, rot.W);
            var om = client.CreateMessage();
            m.Write(om);
            client.SendMessage(om, serverConnection, NetDeliveryMethod.Unreliable);
        }
        public void Disconnect(string reason)
        {
            client.Disconnect(reason);
            StopChatThread();
            StopBlockThread();
        }
        public List<ClientPlayer> GetClientPlayers() => clientPlayers.Values.ToList();
        public int LocalPlayerId => localPlayerId;
    }
}
