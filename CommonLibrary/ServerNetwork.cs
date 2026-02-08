using SpaceNetwork;
using SpaceNetwork.Messages;
using System.Net;
using Lidgren.Network;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System;

namespace ServerCommon
{
    public class ServerNetwork
    {
        private NetServer server;
        private readonly PlayerManager playerManager = new PlayerManager();
        public PlayerManager PlayerManager => playerManager;
        private readonly Dictionary<NetConnection, Player> connectionPlayers = new Dictionary<NetConnection, Player>();
        private readonly Dictionary<int, NetConnection> playerConnections = new Dictionary<int, NetConnection>();
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
           
            InitializeServer();
            messageProcessor = new MessageProcessor(server, connectionPlayers, playerManager,
                                    (msg, type) => logger.Log(msg, type), this);
        }

        private void InitializeServer()
        {

            if(!ServerPreparer.IsServerReady(logger))
            {
                return;
            }

            var config = new NetPeerConfiguration(appKey)
            {
                Port = port,
                MaximumConnections = maxConnections,
                PingInterval = Settings.PingInterval,
                ConnectionTimeout = Settings.ConnectionTimeout
            };
            config.LocalAddress = IPAddress.Any;
            config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

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
                switch (msg.MessageType)
                {
                    case NetIncomingMessageType.DiscoveryRequest:
                        var response = server.CreateMessage();
                        response.Write("SpaceServer");
                        response.Write(server.Configuration.Port);
                        server.SendDiscoveryResponse(response, msg.SenderEndPoint);
                        break;

                    case NetIncomingMessageType.ConnectionApproval:
                        msg.SenderConnection.Approve();
                        break;

                    case NetIncomingMessageType.Data:
                        messageProcessor.Process(msg);
                        break;

                    case NetIncomingMessageType.StatusChanged:
                        messageProcessor.Process(msg);
                        break;
                }
                server.Recycle(msg);
            }
        }

        public void KickConnection(NetConnection conn, string reason)
        {
            if (conn == null) return;

            if (connectionPlayers.TryGetValue(conn, out var player))
            {
                DisconnectAndRemovePlayer(conn, player, reason);
            }
            else
            {
                var km = new KickMessage { Reason = reason };
                var om = server.CreateMessage();
                km.Write(om);
                server.SendMessage(om, conn, NetDeliveryMethod.ReliableOrdered);
                server.FlushSendQueue();
                conn.Disconnect(reason);
            }
        }

        private void DisconnectAndRemovePlayer(NetConnection connection, Player player, string reason, bool isBan = false)
        {
            var km = new KickMessage { Reason = reason };
            var om = server.CreateMessage();
            km.Write(om);
            server.SendMessage(om, connection, NetDeliveryMethod.ReliableOrdered);
            server.FlushSendQueue();
            connection.Disconnect(reason);

            if (connectionPlayers.ContainsKey(connection))
            {
                connectionPlayers.Remove(connection);
                if (playerConnections.ContainsKey(player.ID))
                {
                    playerConnections.Remove(player.ID);
                }

                playerManager.RemovePlayer(player.ID);

                string action = isBan ? "banned" : "kicked";
                string logMsg = $"Player [{player.ID}]{player.Name} was {action}. Reason: {reason}";

                logger.Log(logMsg, LogType.Warning);
                BroadcastChat(-1, logMsg);

                BroadcastPlayers();
            }
        }

        public string KickPlayer(int playerId, string reason = "Kicked by server")
        {
            var targetConnection = GetConnectionByPlayerId(playerId);

            if (targetConnection != null && connectionPlayers.TryGetValue(targetConnection, out var player))
            {
                string playerName = player.Name;
                DisconnectAndRemovePlayer(targetConnection, player, reason, false);
                return playerName;
            }

            return null;
        }

        public bool BanPlayer(int playerId, string reason)
        {
            var target = GetConnectionByPlayerId(playerId);
            if (target != null && connectionPlayers.TryGetValue(target, out var player))
            {
                var banned = new PlayerBanned
                {
                    IDWhenWasBanned = playerId,
                    Name = player.Name,
                    Reason = reason,
                    IPAddress = target.RemoteEndPoint.Address.ToString(),
                    DeviceId = "",
                    BannedAt = DateTime.UtcNow
                };
                BanManager.AddBannedPlayer(banned);

                DisconnectAndRemovePlayer(target, player, $"Banned: {reason}", true);
                return true;
            }
            return false;
        }

        public string GetPlayerIp(int playerId)
        {
            var connection = connectionPlayers.FirstOrDefault(x => x.Value.ID == playerId).Key;
            return connection?.RemoteEndPoint?.Address?.ToString() ?? "Unknown";
        }

        public void Stop()
        {
            DisconnectAll("Server shutting down.");
            _shouldStop = true;
            server.Shutdown("Server stopped");
            logger.Log("Server stopped.", LogType.Success);
        }

        public void Restart()
        {
            logger.Log("Restarting server...", LogType.Info);
            DisconnectAll("Server restarting...");

            _shouldStop = true;
            server.Shutdown("Restarting");

            time = 0;
            playerManager.Reset();
            connectionPlayers.Clear();

            Thread.Sleep(1000);

            InitializeServer();
            BanManager.LoadBannedPlayers();
            messageProcessor = new MessageProcessor(server, connectionPlayers, playerManager, logger.Log, this);
            new Thread(() => RunMainLoop()).Start();
            logger.Log("Server restarted.", LogType.Success);
        }

        private void DisconnectAll(string reason)
        {
            var km = new KickMessage { Reason = reason };
            var om = server.CreateMessage();
            km.Write(om);

            if (server.ConnectionsCount > 0)
            {
                server.SendMessage(om, server.Connections, NetDeliveryMethod.ReliableOrdered, 0);
                server.FlushSendQueue();
                foreach (var conn in server.Connections)
                {
                    conn.Disconnect(reason);
                }
            }

            connectionPlayers.Clear();
            playerConnections.Clear();
            playerManager.Reset();
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

        public NetConnection GetConnectionByPlayerId(int playerId)
        {
            if (playerConnections.TryGetValue(playerId, out var conn))
            {
                return conn;
            }
            return null;
        }

        public void BroadcastPlayers()
        {
            var pm = new PlayersMessage { Players = playerManager.GetAll() };
            var outMsg = server.CreateMessage();
            pm.Write(outMsg);
            if (server.ConnectionsCount > 0)
                server.SendToAll(outMsg, NetDeliveryMethod.Unreliable);
        }

        public void BroadcastChat(int senderId, string text)
        {
            var name = senderId == -1 ? "Server" : "";
            var cm = new ChatMessage(senderId, name, text);
            var outMsg = server.CreateMessage();
            cm.Write(outMsg);
            if (server.ConnectionsCount > 0)
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
        public void RegisterPlayerConnection(NetConnection connection, Player player)
        {
            if (connection == null || player == null) return;

            connectionPlayers[connection] = player;

            if (playerConnections.ContainsKey(player.ID))
                playerConnections.Remove(player.ID);

            playerConnections[player.ID] = connection;
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