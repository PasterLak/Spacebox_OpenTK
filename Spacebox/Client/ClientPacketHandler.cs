using Engine;
using Spacebox.Game;
using Spacebox.Game.GUI;
using SpaceNetwork.Messages;
using SpaceNetwork.Utilities;


namespace Client
{
    public class ClientPacketHandler
    {
        private readonly ClientNetwork _network;
        private readonly NetworkPlayerRegistry _playerRegistry;
        private readonly Dictionary<Type, Action<BaseMessage>> _handlers;

        public ClientPacketHandler(ClientNetwork network, NetworkPlayerRegistry registry)
        {
            _network = network;
            _playerRegistry = registry;
            _handlers = new Dictionary<Type, Action<BaseMessage>>
            {
                { typeof(InitMessage), HandleInit },
                { typeof(PlayersMessage), HandlePlayers },
                { typeof(ServerInfoMessage), HandleServerInfo },
                { typeof(KickMessage), HandleKick },
                { typeof(SpaceNetwork.Messages.ChatMessage), HandleChat },
                { typeof(BlockDestroyedMessage), HandleBlockDestroyed },
                { typeof(BlockPlaceMessage), HandleBlockPlaced },
                { typeof(FlashlightMessage), HandleFlashlight },
                { typeof(ItemInHandMessage), HandleItemInHand },
                { typeof(ZipMessage), HandleZip }
            };
        }

        public void Handle(BaseMessage message)
        {
            if (_handlers.TryGetValue(message.GetType(), out var handler))
            {
                handler(message);
            }
        }

        private void HandleInit(BaseMessage msg)
        {
            var im = (InitMessage)msg;
            _network.SetLocalPlayerId(im.Player.ID);
            _playerRegistry.AddOrUpdate(im.Player);
            _network.MarkInitialized();
        }

        private void HandlePlayers(BaseMessage msg)
        {
            var pm = (PlayersMessage)msg;
            _playerRegistry.SyncFromList(pm.Players);
        }

        private void HandleServerInfo(BaseMessage msg)
        {
            var sim = (ServerInfoMessage)msg;
            _network.SetServerInfo(sim.Info);
        }

        private void HandleKick(BaseMessage msg)
        {
            var km = (KickMessage)msg;

            _network.TriggerKick();
            string message = "Kicked from server.";
            if (!string.IsNullOrEmpty(km.Reason))
                message += " Reason: " + km.Reason;
            _network.Disconnect(message);
        }

        private void HandleChat(BaseMessage msg)
        {
            var cm = (SpaceNetwork.Messages.ChatMessage)msg;
            string logMsg = cm.SenderId == -1
                ? $"[Server]: {cm.Text}"
                : $"> {cm.SenderName}[{cm.SenderId}]: {cm.Text}";

            Chat.Write(logMsg);
            Debug.Log(logMsg);
        }

        private void HandleBlockDestroyed(BaseMessage msg)
        {
            var bdm = (BlockDestroyedMessage)msg;
            if (bdm.senderID != _network.LocalPlayerId)
                _network.InvokeBlockDestroyed(bdm.X, bdm.Y, bdm.Z);
        }

        private void HandleBlockPlaced(BaseMessage msg)
        {
            var bpm = (BlockPlaceMessage)msg;
            if (bpm.senderID != _network.LocalPlayerId)
                _network.InvokeBlockPlaced(bpm);
        }

        private void HandleFlashlight(BaseMessage msg)
        {
            var message = (FlashlightMessage)msg;
            if (message.SenderId == _network.LocalPlayerId) return;

            if (_playerRegistry.TryGet(message.SenderId, out var remote))
            {
                if (remote.Flashlight != null)
                    remote.Flashlight.Enabled = message.State;
            }
        }

        private void HandleItemInHand(BaseMessage msg)
        {
            var message = (ItemInHandMessage)msg;
            if (message.SenderId == _network.LocalPlayerId) return;

            if (message.ItemId <= 0)
            {
                // No item in hand
                if (_playerRegistry.TryGet(message.SenderId, out var remote1))
                {
                    remote1.ChangeItemInHand(null);
                }
                return;
            }

            if (_playerRegistry.TryGet(message.SenderId, out var remote))
            {
                if (GameAssets.TryGetItemById(message.ItemId, out var item))
                    remote.ChangeItemInHand(item);
                else
                    remote.ChangeItemInHand(null);
            }
        }

        private void HandleZip(BaseMessage msg)
        {
            var zm = (ZipMessage)msg;
            _network.ReceivedServerInfo.ModFolderName = zm.ModName;
            _network.InvokeZipStart();

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string targetDir = Path.Combine(baseDir, "Server", _network.ReceivedServerInfo?.Name ?? "Unknown", Path.Combine("GameSet", zm.ModName));

            if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

            string tempZip = Path.Combine(targetDir, "download.zip");
            File.WriteAllBytes(tempZip, zm.ZipData);
            ZipHelper.UnzipData(zm.ZipData, targetDir);
            File.Delete(tempZip);

            Debug.Log("Zip received and unpacked.");

            _network.InvokeZipComplete();

        }
    }
}