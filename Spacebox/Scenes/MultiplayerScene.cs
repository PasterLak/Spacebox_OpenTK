using Client;
using Engine;
using Engine.SceneManagement;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Commands;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;


namespace Spacebox.Scenes
{
    public class MultiplayerScene : BaseSpaceScene
    {

        public MultiplayerScene()
        {

        }
        public MultiplayerScene(string[] args) 
        {

        }

        public override void LoadContent()
        {
            localPlayer = new AstronautMultiplayer(new Vector3(5, 5, 5));
            localPlayer.GameMode = GameMode.Creative;
            

            if (ClientNetwork.Instance == null)
            {
                Debug.Error("No existing connection. Returning to Multiplayer Menu.");
                SceneManager.Load<MenuScene>();
                return;
            }
            base.LoadContent();
            ClientNetwork.Instance.OnPlayerJoined += SpawnRemotePlayer;
            ClientNetwork.Instance.OnPlayerLeft += RemoveRemotePlayer;
            ClientNetwork.Instance.OnBlockDestroyed += OnBlockDestroyed;
            ClientNetwork.Instance.OnBlockPlaced += OnBlockPlaced;
            Debug.Success("[MultiplayerScene] ClientNetwork: Success");

            Debug.RegisterCommand(new TeleportToPlayerCommand(localPlayer));

            var players = ClientNetwork.Instance.GetClientPlayers();

            foreach ( var player in players )
            {
                SpawnRemotePlayer(player);
            }
        }

        public void OnBlockDestroyed(int x, int y, int z)
        {

            if (World.CurrentSector.TryGetNearestEntity(Camera.Main.Position, out var entity))
            {
                if (entity.RemoveBlockAtLocal(new Vector3(x, y, z), new Vector3SByte(0, 0, 0)))
                {

                }
            }
        }

        public void OnBlockPlaced(int playerId, short blockId, byte direction, short x, short y, short z)
        {

            Block block = GameAssets.CreateBlockFromId(blockId);
            if (direction <= 6)
            {
                block.Direction = (Direction)direction;
            }

            if (World.CurrentSector.TryGetNearestEntity(Camera.Main.Position, out var entity))
            {
                if (entity.TryPlaceBlockLocal(new Vector3(x, y, z), block))
                {

                }
            }
        }

        public override void Update()
        {
            base.Update();
            ClientNetwork.Instance.PollEvents();

            if (!ClientNetwork.Instance.IsConnected)
            {
                Debug.Error("Lost connection to server. Returning to Multiplayer Menu.");
                SceneManager.Load<MenuScene>();
                return;
            }

            foreach (var cp in ClientNetwork.Instance.GetClientPlayers())
            {
                if (cp.NetworkPlayer.ID == ClientNetwork.Instance.LocalPlayerId)
                    continue;

                cp.Update();
            }
        }
        public override void Render()
        {
            base.Render();
            if (ClientNetwork.Instance == null) return;

            foreach (var cp in ClientNetwork.Instance.GetClientPlayers())
            {
                if (cp.NetworkPlayer.ID == ClientNetwork.Instance.LocalPlayerId)
                    continue;
                cp.RemotePlayer?.Render();
            }
        }

        public override void UnloadContent()
        {
            base.UnloadContent();
            if (ClientNetwork.Instance != null)
            {
                ClientNetwork.Instance.OnPlayerJoined -= SpawnRemotePlayer;
                ClientNetwork.Instance.OnPlayerLeft -= RemoveRemotePlayer;
                ClientNetwork.Instance.Disconnect("Scene unloaded");
            }
        }

        public void SpawnRemotePlayer(ClientPlayer player)
        {
            if (ClientNetwork.Instance == null)
                return;
            if (ClientNetwork.Instance.LocalPlayerId == player.NetworkPlayer.ID)
                return;
            if (player.RemotePlayer == null)
            {
                var remote = new AstronautRemote(player.NetworkPlayer);
                remote.LatestPosition = Camera.Main.Position;
                remote.LatestRotation = Quaternion.Identity;
                remote.Position = Camera.Main.Position;
                player.RemotePlayer = remote;
                OnRenderCenter += remote.Render;
            }
        }

        public void RemoveRemotePlayer(ClientPlayer player)
        {
            if (player.RemotePlayer != null)
            {
                player.RemotePlayer.OnDisconnect();
                OnRenderCenter -= player.RemotePlayer.Render;
                player.RemotePlayer = null;
            }
        }
    }
}
