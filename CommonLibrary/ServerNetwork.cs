using SpaceNetwork;
using SpaceNetwork.Messages;
using System.Net;
using Lidgren.Network;


namespace ServerCommon
{
    public class ServerNetwork
    {
        private NetServer server;
        private readonly PlayerManager playerManager = new PlayerManager();
        public PlayerManager PlayerManager => playerManager;
        private readonly Dictionary<NetConnection, Player> connectionPlayers = new Dictionary<NetConnection, Player>();
        private bool _shouldStop;
        private float time;
        private readonly ILogger logger;
        private readonly string appKey;
        private readonly int maxConnections;
        private readonly int port;
        private MessageProcessor messageProcessor;

        public ServerNetwork(string appKey, int port, int maxConnections, ILogger logger)
        {
            this.appKey = appKey;
            this.port = port;
            this.maxConnections = maxConnections;
            this.logger = logger;
            BanManager.LoadBannedPlayers();
            InitializeServer();
            messageProcessor = new MessageProcessor(server, connectionPlayers, playerManager,
                                          (msg, type) => logger.Log(msg, type), this);
        }

        private void InitializeServer()
        {
            var config = new NetPeerConfiguration(appKey)
            {
                Port = port,
                MaximumConnections = maxConnections,
                PingInterval = Settings.PingInterval,
                ConnectionTimeout = Settings.ConnectionTimeout
            };
            config.LocalAddress = IPAddress.Any;
            config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
            server = new NetServer(config);
            server.Start();
            _shouldStop = false;
            logger.Log("<--------------------------->", LogType.Normal);
            logger.Log($"Server \"{Settings.Name}\" started on port {port}", LogType.Normal);
            logger.Log($"App key: {appKey}", LogType.Normal);
        }

        public void RunMainLoop()
        {
            var localIP = Dns.GetHostEntry(Dns.GetHostName())
                .AddressList
                .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip));
            logger.Log("Server IP (Local): " + (localIP?.ToString() ?? "Not found"), LogType.Normal);
            logger.Log("<--------------------------->", LogType.Normal);
            while (!_shouldStop)
            {
                Time.Update();
                if (time > Settings.TimeToCheckAfk)
                {
                    time = Settings.TimeToCheckAfk;
                    CheckAFKPlayers();
                }
                if (time < Settings.TimeToCheckAfk)
                    time += Time.Delta;
                ProcessMessages();
                Thread.Sleep(1);
            }
        }

        private void ProcessMessages()
        {
            NetIncomingMessage msg;
            while ((msg = server.ReadMessage()) != null)
            {
                if (msg.MessageType == NetIncomingMessageType.DiscoveryRequest)
                {
                    var response = server.CreateMessage();
                    response.Write("SpaceServer");
                    response.Write(server.Configuration.Port);
                    server.SendDiscoveryResponse(response, msg.SenderEndPoint);
                }
                else
                {
                    messageProcessor.Process(msg);
                }
                server.Recycle(msg);
            }
        }

        public string GetPlayerIp(int playerId)
        {

            var connection = connectionPlayers.FirstOrDefault(x => x.Value.ID == playerId).Key;
            return connection?.RemoteEndPoint?.Address?.ToString() ?? "Unknown";
        }


        public void Stop()
        {
            _shouldStop = true;
            server.Shutdown("Server stopped");
            logger.Log("Server stopped.", LogType.Success);
        }

        public void Restart()
        {
            logger.Log("Restarting server...", LogType.Info);
            Stop();
            time = 0;
            playerManager.Reset();
            Thread.Sleep(1000);
            InitializeServer();
            BanManager.LoadBannedPlayers();
            messageProcessor = new MessageProcessor(server, connectionPlayers, playerManager, logger.Log, this);
            new Thread(() => RunMainLoop()).Start();
            logger.Log("Server restarted.", LogType.Success);
        }

        private void CheckAFKPlayers()
        {
            if (playerManager.GetAll().Count == 0)
            {
                time = 0;
                return;
            }
            var players = playerManager.GetAll().Values.ToArray();
            foreach (var player in players)
            {
                if ((Environment.TickCount - player.LastTimeWasActive) > Settings.TimeCanBeAfkSec * 1000)
                {
                    KickPlayer(player.ID, "AFK Timeout");
                    time = 0;
                    return;
                }
            }
            time = 0;
        }

        public string KickPlayer(int playerId, string reason = "Kicked by server")
        {
            var targetConnection = GetConnectionByPlayerId(playerId);

            if (targetConnection != null && connectionPlayers.TryGetValue(targetConnection, out var player))
            {
                string playerName = player.Name;

                var km = new KickMessage { Reason = reason };
                var om = server.CreateMessage();
                km.Write(om);
                server.SendMessage(om, targetConnection, NetDeliveryMethod.ReliableOrdered);

                targetConnection.Disconnect(reason);

                string logMsg = $"Player [{playerId}]{playerName} was kicked. Reason: {reason}";
                logger.Log(logMsg, LogType.Warning);

                BroadcastChat(-1, logMsg);

                connectionPlayers.Remove(targetConnection);
                playerManager.RemovePlayer(playerId);

                BroadcastPlayers();

                return playerName;
            }

            return null;
        }

        public bool BanPlayer(int playerId, string reason)
        {
            var target = GetConnectionByPlayerId(playerId);
            if (target != null)
            {
                var banned = new PlayerBanned
                {
                    IDWhenWasBanned = playerId,
                    Name = connectionPlayers[target].Name,
                    Reason = reason,
                    IPAddress = target.RemoteEndPoint.Address.ToString(),
                    DeviceId = "",
                    BannedAt = DateTime.UtcNow
                };
                BanManager.AddBannedPlayer(banned);
                var km = new KickMessage { Reason = $"Banned: {reason}" };
                var om = server.CreateMessage();
                km.Write(om);
                server.SendMessage(om, target, NetDeliveryMethod.ReliableOrdered);
                logger.Log($"Player {playerId} ({banned.Name}) ({banned.IPAddress}) was banned. Reason: {reason}", LogType.Warning);
                BroadcastChat(-1, $"Player {playerId} ({connectionPlayers[target].Name}) was banned. Reason: {reason}");
                target.Disconnect("Banned");
                
                connectionPlayers.Remove(target);
                playerManager.RemovePlayer(playerId);
                BroadcastPlayers();
                return true;
            }
            return false;
        }

        private NetConnection GetConnectionByPlayerId(int playerId)
        {
            return connectionPlayers.FirstOrDefault(kvp => kvp.Value.ID == playerId).Key;
        }

        public void BroadcastPlayers()
        {
            var pm = new PlayersMessage { Players = playerManager.GetAll() };
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
        public int GetPlayerPing(int playerId)
        {
            var connection = GetConnectionByPlayerId(playerId);
            if (connection != null)
            {
                return (int)connection.AverageRoundtripTime;
            }
            return -1;
        }

        public void SendPositionUpdate(Player player)
        {

            BroadcastPlayers();
        }

        public void SendPrivateMessage(int targetPlayerId, string message)
        {
            var targetConnection = GetConnectionByPlayerId(targetPlayerId);
            if (targetConnection != null)
            {
                var cm = new ChatMessage(-1, "Server (Private)", message);
                var om = server.CreateMessage();
                cm.Write(om);
                server.SendMessage(om, targetConnection, NetDeliveryMethod.ReliableOrdered);
            }
            else
            {
                logger.Log($"Failed to send private message: Player {targetPlayerId} not found.", LogType.Warning);
            }
        }

        public NetServer GetServer() => server;

        public IEnumerable<Player> GetAllPlayers() => playerManager.GetAll().Values;
    }
}
