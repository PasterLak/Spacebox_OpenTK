using SpaceNetwork;
using SpaceNetwork.Messages;
using System.Net;
using System.Threading;
using System.Linq;
using Lidgren.Network;
using System;
using System.Collections.Generic;

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

        public void Stop()
        {
            _shouldStop = true;
            server.Shutdown("Server stopped");
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
                    KickPlayer(player.ID, true);
                    time = 0;
                    return;
                }
            }
            time = 0;
        }

        public bool KickPlayer(int playerId, bool wasAFK = false)
        {
            var target = GetConnectionByPlayerId(playerId);
            if (target != null)
            {
                var km = new KickMessage { Reason = "Was Kicked" + (wasAFK ? " because of AFK" : "") };
                var om = server.CreateMessage();
                km.Write(om);
                server.SendMessage(om, target, NetDeliveryMethod.ReliableOrdered);
                target.Disconnect("Kicked");
                logger.Log($"Player {playerId} was kicked.", LogType.Warning);
                BroadcastChat(-1, $"Player {playerId} was kicked.");
                connectionPlayers.Remove(target);
                playerManager.RemovePlayer(playerId);
                BroadcastPlayers();
                return true;
            }
            return false;
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

        public NetServer GetServer() => server;

        public IEnumerable<Player> GetAllPlayers() => playerManager.GetAll().Values;
    }
}
