using System.Net;
using Lidgren.Network;
using SpaceNetwork;
using SpaceNetwork.Messages;


namespace SpaceServer
{
    public class ServerNetwork
    {
        private NetServer server;
        private PlayerManager playerManager = new PlayerManager();
        private Dictionary<NetConnection, Player> connectionPlayers = new Dictionary<NetConnection, Player>();
        private bool _shouldStop;
        private float time;
        private readonly Action<string> _logCallback;

        static ServerNetwork()
        {

        }

        public ServerNetwork(string appKey, int port, int maxConnections, Action<string> logCallback)
        {
            _logCallback = logCallback;
            var config = new NetPeerConfiguration(appKey)
            {
                Port = port,
                MaximumConnections = maxConnections,
                PingInterval = Settings.PingInterval,
                ConnectionTimeout = Settings.ConnectionTimeout
            };
            config.LocalAddress = IPAddress.Any;
            server = new NetServer(config);
            server.Start();
            _logCallback?.Invoke($"<--------------------------->");
            _logCallback?.Invoke($"Server started on port {port}");
        }

        public void RunMainLoop()
        {
            var localIP = Dns.GetHostEntry(Dns.GetHostName())
                .AddressList
                .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip));
            _logCallback?.Invoke("Server IP (Local): " + (localIP?.ToString() ?? "Not found"));
            _logCallback?.Invoke($"<--------------------------->");
            while (!_shouldStop)
            {
                Time.Update();
                if (time > Settings.TimeToCheckAfk) { time = Settings.TimeToCheckAfk; CheckAFKPlayers(); }
                if (time < Settings.TimeToCheckAfk) time += Time.Delta;
                NetIncomingMessage msg;
                while ((msg = server.ReadMessage()) != null)
                {
                    switch (msg.MessageType)
                    {
                        case NetIncomingMessageType.StatusChanged:
                            HandleStatusChanged(msg);
                            break;
                        case NetIncomingMessageType.Data:
                            HandleData(msg);
                            break;
                    }
                    server.Recycle(msg);
                }
                Thread.Sleep(1);
            }
        }

        public void Stop()
        {
            _shouldStop = true;
            server.Shutdown("Server stopped");
        }

        private void CheckAFKPlayers()
        {
            if (playerManager.GetAll().Count == 0) { time = 0; return; }
            var players = playerManager.GetAll().Values.ToArray();
            foreach (var player in players)
            {
                if ((Environment.TickCount - player.LastTimeWasActive) > Settings.TimeCanBeAfkSec * 1000)
                {
                    KickPlayer(player.ID, true);
                    time = 0;
                    return;
                }
            }
            time = 0;
        }

        private void HandleStatusChanged(NetIncomingMessage msg)
        {
            var status = (NetConnectionStatus)msg.ReadByte();
            msg.ReadString();
            if (status == NetConnectionStatus.Connected)
            {
                var hail = msg.SenderConnection.RemoteHailMessage;
                var chosenName = hail.ReadString();
                if (playerManager.IsNameUsed(chosenName))
                {
                    var km = new KickMessage();
                    km.Reason = "Name in use. Choose another one.";
                    var om = server.CreateMessage();
                    km.Write(om);
                    server.SendMessage(om, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                    msg.SenderConnection.Disconnect("DuplicateName");
                    return;
                }
                var newPlayer = playerManager.AddNewPlayer(chosenName);
                connectionPlayers[msg.SenderConnection] = newPlayer;
                _logCallback?.Invoke($"{newPlayer.Name}[{newPlayer.ID}] connected");
                var initMsg = new InitMessage();
                initMsg.Player = newPlayer;
                var outMsg = server.CreateMessage();
                initMsg.Write(outMsg);
                server.SendMessage(outMsg, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                BroadcastPlayers();
                BroadcastChat(-1, $"{newPlayer.Name}[{newPlayer.ID}] connected");
            }
            else if (status == NetConnectionStatus.Disconnected)
            {
                if (connectionPlayers.TryGetValue(msg.SenderConnection, out var p))
                {
                    connectionPlayers.Remove(msg.SenderConnection);
                    playerManager.RemovePlayer(p.ID);
                    _logCallback?.Invoke($"{p.Name}[{p.ID}] disconnected");
                    BroadcastPlayers();
                }
            }
        }

        private void HandleData(NetIncomingMessage msg)
        {
            var baseMsg = MessageFactory.CreateMessage(msg);
            if (baseMsg is PositionMessage um)
            {
                if (connectionPlayers.TryGetValue(msg.SenderConnection, out var p))
                {
                    p.Position = um.Position;
                    p.LastTimeWasActive = Environment.TickCount;
                    BroadcastPlayers();
                }
            }
            else if (baseMsg is ChatMessage cm)
            {
                if (connectionPlayers.TryGetValue(msg.SenderConnection, out var p))
                {
                    var broadcast = new ChatMessage(p.ID, p.Name, cm.Text);

                    var om = server.CreateMessage();
                    broadcast.Write(om);
                    server.SendToAll(om, NetDeliveryMethod.ReliableOrdered);
                    _logCallback?.Invoke($"> {p.Name}[{p.ID}]: {cm.Text}");
                }
            }
        }

        private void BroadcastPlayers()
        {
            var pm = new PlayersMessage();
            pm.Players = playerManager.GetAll();
            var outMsg = server.CreateMessage();
            pm.Write(outMsg);
            server.SendToAll(outMsg, NetDeliveryMethod.Unreliable);
        }

        public void BroadcastChat(int senderId, string text)
        {
            var name = senderId == -1 ? "Server" : "";
            var cm = new ChatMessage(senderId, name, text);

            var outMsg = server.CreateMessage();
            cm.Write(outMsg);
            server.SendToAll(outMsg, NetDeliveryMethod.ReliableOrdered);
        }

        public bool KickPlayer(int playerId, bool wasAFK = false)
        {
            NetConnection target = null;
            foreach (var kvp in connectionPlayers)
            {
                if (kvp.Value.ID == playerId) { target = kvp.Key; break; }
            }
            if (target != null)
            {
                var km = new KickMessage();
                km.Reason = "Was Kicked" + (wasAFK ? " because of AFK" : "");
                var om = server.CreateMessage();
                km.Write(om);
                server.SendMessage(om, target, NetDeliveryMethod.ReliableOrdered);
                target.Disconnect("Kicked");
                _logCallback?.Invoke($"Player {playerId} was kicked.");
                connectionPlayers.Remove(target);
                playerManager.RemovePlayer(playerId);
                BroadcastPlayers();
                return true;
            }
            else
            {
                return false;
            }
        }

        public IEnumerable<Player> GetAllPlayers() => playerManager.GetAll().Values;
    }
}
