using Lidgren.Network;
using OpenTK.Mathematics;
using SpaceNetwork;
using SpaceNetwork.Messages;

namespace Client
{
    public class ClientNetwork
    {
        private NetClient client;
        private NetConnection serverConnection;
        private Dictionary<int, Player> players = new Dictionary<int, Player>();
        private int localPlayerId = -1;
        private Thread chatThread;
        private volatile bool running;
        public bool IsInitialized { get; private set; }
        public bool NameInUse { get; private set; }

        static ClientNetwork()
        {
         
        }

        public ClientNetwork(string appKey, string host, int port, string playerName)
        {
            var config = new NetPeerConfiguration(appKey);
            client = new NetClient(config);
            client.Start();
            var hail = client.CreateMessage();
            hail.Write(playerName);
            client.Connect(host, port, hail);
        }

        public void StartChatThread()
        {
            if (chatThread != null)
                return;
            running = true;
            chatThread = new Thread(() =>
            {
                while (running)
                {
                    string chat = Console.ReadLine() ?? "";
                    if (!string.IsNullOrEmpty(chat) && serverConnection != null)
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
                Console.Clear();
                Console.WriteLine("Client connected to server.");
            }
            else if (newStatus == NetConnectionStatus.Disconnected)
            {
                serverConnection = null;
                Console.WriteLine("Client disconnected from server. " + reason);
               // MultiplayerClient.Game.Instance?.Close();
                if (reason.Contains("DuplicateName"))
                    NameInUse = true;
                StopChatThread();
            }
        }

        private void HandleData(NetIncomingMessage msg)
        {
            var baseMsg = MessageFactory.CreateMessage(msg);
            if (baseMsg is InitMessage im)
            {
                localPlayerId = im.Player.ID;
                players[localPlayerId] = im.Player;
                IsInitialized = true;
            }
            else if (baseMsg is PlayersMessage pm)
            {
                foreach (var kvp in pm.Players)
                {
                    if (kvp.Key == localPlayerId)
                        continue;
                    players[kvp.Key] = kvp.Value;
                }
                var removeList = players.Keys.Where(k => k != localPlayerId && !pm.Players.ContainsKey(k)).ToList();
                foreach (var id in removeList)
                {
                    var p = players[id];
                    Console.WriteLine($"[Server]: {p.Name}[{p.ID}] disconnected");
                    players.Remove(id);
                }
            }
            else if (baseMsg is PositionMessage um)
            {
            }
            else if (baseMsg is KickMessage km)
            {
                Console.WriteLine(km.Reason);
               // MultiplayerClient.Game.Instance?.SetTitle(km.Reason);
                client.Disconnect("Kicked");
            }
            else if (baseMsg is ChatMessage cm)
            {
                if (cm.SenderId == -1)
                    Console.WriteLine($"[Server]: {cm.Text}");
                else
                    Console.WriteLine($"> {cm.SenderName}[{cm.SenderId}]: {cm.Text}");
            }
        }

        public void SendPosition(Vector3 pos)
        {
            if (serverConnection == null)
                return;
            var m = new PositionMessage();
            m.Position = new System.Numerics.Vector3(pos.X, pos.Y, pos.Z);
            var om = client.CreateMessage();
            m.Write(om);
            client.SendMessage(om, serverConnection, NetDeliveryMethod.Unreliable);
        }

        public void Disconnect(string reason)
        {
            client.Disconnect(reason);
            StopChatThread();
        }

        public Dictionary<int, Player> GetPlayers() => players;
        public int LocalPlayerId => localPlayerId;
    }
}
