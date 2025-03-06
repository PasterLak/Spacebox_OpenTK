using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Lidgren.Network;
using SpaceNetwork;
using SpaceNetwork.Messages;

namespace ServerCommon
{
    public class MessageProcessor
    {
        private readonly NetServer server;
        private readonly Dictionary<NetConnection, Player> connectionPlayers;
        private readonly PlayerManager playerManager;
        private readonly Action<string, LogType> logCallback;
        private readonly ServerNetwork serverNetwork;

        public MessageProcessor(NetServer server, Dictionary<NetConnection, Player> connectionPlayers, PlayerManager playerManager, Action<string, LogType> logCallback, ServerNetwork serverNetwork)
        {
            this.server = server;
            this.connectionPlayers = connectionPlayers;
            this.playerManager = playerManager;
            this.logCallback = logCallback;
            this.serverNetwork = serverNetwork;
        }

        public void Process(NetIncomingMessage msg)
        {
            switch (msg.MessageType)
            {
                case NetIncomingMessageType.StatusChanged:
                    ProcessStatusChanged(msg);
                    break;
                case NetIncomingMessageType.Data:
                    ProcessData(msg);
                    break;
            }
        }

        private void ProcessStatusChanged(NetIncomingMessage msg)
        {
            var status = (NetConnectionStatus)msg.ReadByte();
         
            msg.ReadString();
            if (status == NetConnectionStatus.Connected)
            {
                var hail = msg.SenderConnection.RemoteHailMessage;
                var chosenName = hail.ReadString();
                string senderIp = msg.SenderConnection.RemoteEndPoint.Address.ToString();
                if (BanManager.IsBannedByName(chosenName) || BanManager.IsBannedByIp(senderIp))
                {
                    var km = new KickMessage { Reason = "You are banned." };
                    var om = server.CreateMessage();
                    km.Write(om);
                    server.SendMessage(om, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                    msg.SenderConnection.Disconnect("Banned");
                    return;
                }
                if (playerManager.IsNameUsed(chosenName))
                {
                    var km = new KickMessage { Reason = "Name in use. Choose another one." };
                    var om = server.CreateMessage();
                    km.Write(om);
                    server.SendMessage(om, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                    msg.SenderConnection.Disconnect("DuplicateName");
                    return;
                }
                var newPlayer = playerManager.AddNewPlayer(chosenName);
                connectionPlayers[msg.SenderConnection] = newPlayer;
                logCallback?.Invoke($"{newPlayer.Name}[{newPlayer.ID}] connected", LogType.Success);

                var initMsg = new InitMessage { Player = newPlayer };
                var omInit = server.CreateMessage();
                initMsg.Write(omInit);
                server.SendMessage(omInit, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered);

                var serverInfoMsg = new ServerInfoMessage
                {
                    Info = new ServerInfo
                    {
                        Name = Settings.Name,
                        Description = Settings.Description,
                        MaxPlayers = server.Configuration.MaximumConnections
                    }
                };
                var omServerInfo = server.CreateMessage();
                serverInfoMsg.Write(omServerInfo);
                server.SendMessage(omServerInfo, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered);

                serverNetwork.BroadcastPlayers();
                serverNetwork.BroadcastChat(-1, $"{newPlayer.Name}[{newPlayer.ID}] connected");

                var connection = msg.SenderConnection;
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    Thread.Sleep(1000);
                    if (connection != null && connection.Status == NetConnectionStatus.Connected)
                    {
                        SendZipToClient(connection);
                    }
                    else
                    {
                        logCallback?.Invoke("Recipient connection is null or not connected. Zip will not be sent.", LogType.Warning);
                    }
                });
            }
            else if (status == NetConnectionStatus.Disconnected)
            {
                if (connectionPlayers.TryGetValue(msg.SenderConnection, out var p))
                {
                    connectionPlayers.Remove(msg.SenderConnection);
                    playerManager.RemovePlayer(p.ID);
                    serverNetwork.BroadcastChat(-1, $"{p.Name}[{p.ID}] disconnected");
                    logCallback?.Invoke($"{p.Name}[{p.ID}] disconnected", LogType.Info);
                    serverNetwork.BroadcastPlayers();
                }
            }
        }

        private void SendZipToClient(NetConnection connection)
        {
            if (connection == null || connection.Status != NetConnectionStatus.Connected)
            {
                logCallback?.Invoke("Recipient connection is null or not connected. Zip will not be sent.", LogType.Warning);
                return;
            }
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string folderToZip = System.IO.Path.Combine(baseDir, "GameSet", Settings.GameSetFolder);
                if (!System.IO.Directory.Exists(folderToZip))
                {
                    logCallback?.Invoke("GameSet folder not found: " + folderToZip, LogType.Error);
                    return;
                }
                byte[] zipData = ZipManager.CreateZipFromFolder(folderToZip);
                var zipMsg = new ZipMessage
                {
                    ModName = Settings.GameSetFolder,
                    ZipData = zipData
                };
                var omZip = server.CreateMessage();
                zipMsg.Write(omZip);
                server.SendMessage(omZip, connection, NetDeliveryMethod.ReliableOrdered);
                logCallback?.Invoke("Zip sent to client: " + connection.RemoteEndPoint, LogType.Info);
            }
            catch (Exception ex)
            {
                logCallback?.Invoke("Error sending zip: " + ex.Message, LogType.Error);
            }
        }

        private void ProcessData(NetIncomingMessage msg)
        {
            var baseMsg = MessageFactory.CreateMessage(msg);
            if (baseMsg is Node3DMessage pm)
            {
                if (connectionPlayers.TryGetValue(msg.SenderConnection, out var p))
                {
                    p.Position = pm.Position;
                    p.Rotation = pm.Rotation;
                    p.LastTimeWasActive = Environment.TickCount;
                    serverNetwork.BroadcastPlayers();
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
                    logCallback?.Invoke($"> {p.Name}[{p.ID}]: {cm.Text}", LogType.Normal);
                }
            }
            else if (baseMsg is BlockDestroyedMessage || baseMsg is BlockPlaceMessage)
            {
                BroadcastRaw(msg);
            }
        }

        private void BroadcastRaw(NetIncomingMessage msg)
        {
            byte[] rawData = new byte[msg.LengthBytes];
            Array.Copy(msg.Data, 0, rawData, 0, msg.LengthBytes);
            var om = server.CreateMessage();
            om.Write(rawData);
            server.SendToAll(om, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered, 0);
        }
    }
}
