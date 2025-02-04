using SpaceNetwork;
using SpaceNetwork.Messages;
using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ServerCommon
{
    public class MessageProcessor
    {
        private readonly NetServer server;
        private readonly Dictionary<NetConnection, Player> connectionPlayers;
        private readonly PlayerManager playerManager;
        private readonly Action<string> logCallback;
        private readonly ServerNetwork serverNetwork;

        public MessageProcessor(NetServer server, Dictionary<NetConnection, Player> connectionPlayers, PlayerManager playerManager, Action<string> logCallback, ServerNetwork serverNetwork)
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
            msg.ReadString(); // Чтение дополнительного текста
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
                logCallback?.Invoke($"{newPlayer.Name}[{newPlayer.ID}] connected");

                var initMsg = new InitMessage { Player = newPlayer };
                var omInit = server.CreateMessage();
                initMsg.Write(omInit);
                server.SendMessage(omInit, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered);

                // Отправка ServerInfoMessage
                var serverInfoMsg = new ServerInfoMessage
                {
                    Info = new SpaceNetwork.ServerInfo
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

                // Захватим соединение в локальную переменную
                var connection = msg.SenderConnection;
                // Ждем 1 секунду, затем отправляем zip, если соединение всё ещё активно
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    Thread.Sleep(1000);
                    if (connection != null && connection.Status == NetConnectionStatus.Connected)
                    {
                        SendZipToClient(connection);
                    }
                    else
                    {
                        logCallback?.Invoke("Recipient connection is null or not connected. Zip will not be sent.");
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
                    logCallback?.Invoke($"{p.Name}[{p.ID}] disconnected");
                    serverNetwork.BroadcastPlayers();
                }
            }
        }



        private void SendZipToClient(NetConnection connection)
        {
            if (connection == null || connection.Status != NetConnectionStatus.Connected)
            {
                logCallback?.Invoke("Recipient connection is null or not connected. Zip will not be sent.");
                return;
            }
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string folderToZip = System.IO.Path.Combine(baseDir, "GameSet", Settings.GameSetFolder);
                if (!System.IO.Directory.Exists(folderToZip))
                {
                    logCallback?.Invoke("GameSet folder not found: " + folderToZip);
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
                logCallback?.Invoke("Zip sent to client: " + connection.RemoteEndPoint);
            }
            catch (Exception ex)
            {
                logCallback?.Invoke("Error sending zip: " + ex.Message);
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
                    logCallback?.Invoke($"> {p.Name}[{p.ID}]: {cm.Text}");
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
