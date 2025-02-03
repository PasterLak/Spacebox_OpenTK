using Client;
using Engine;
using Engine.SceneManagment;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Generation;
using Spacebox.Game.Player;
using System.Linq;

namespace Spacebox.Scenes
{
    public class MultiplayerSpaceScene : BaseSpaceScene
    {
        ClientNetwork networkClient;
        private string mpAppKey;
        private string mpHost;
        private int mpPort;
        private string mpPlayerName;

        public MultiplayerSpaceScene(string[] args) : base(args)
        {
            if (args.Length >= 8)
            {
                mpAppKey = args[4];
                mpHost = args[5];
                int.TryParse(args[6], out mpPort);
                mpPlayerName = args[7];
            }
        }

        public override void LoadContent()
        {
            localPlayer = new LocalAstronaut(new Vector3(5, 5, 5));
            localPlayer.GameMode = GameMode.Creative;
            SceneGraph.AddRoot(localPlayer);
            networkClient = ClientNetwork.Instance;
            if (networkClient == null)
            {
                Debug.Error("No existing connection. Creating new ClientNetwork instance...");
                networkClient = new ClientNetwork(mpAppKey, mpHost, mpPort, mpPlayerName);
                ClientNetwork.Instance = networkClient;
            }
            base.LoadContent();
            if (ClientNetwork.Instance != null)
            {
                ClientNetwork.Instance.OnPlayerJoined += SpawnRemotePlayer;
                ClientNetwork.Instance.OnPlayerLeft += RemoveRemotePlayer;

                ClientNetwork.Instance.OnBlockDestroyed += OnBlockDestroyed;
                ClientNetwork.Instance.OnBlockPlaced += OnBlockPlaced;

                Debug.Success("[MultiplayerSpaceScene] ClientNetwork: Success");
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

            Block block = GameBlocks.CreateBlockFromId(blockId);
            if(direction <= 6)
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
            if (!networkClient.IsConnected)
            {
                Debug.Error("Lost connection to server. Returning to Multiplayer Menu.");
                //SceneManager.LoadScene(typeof(SpaceMenuScene));
                return;
            }
            networkClient.PollEvents();
            foreach (var cp in ClientNetwork.Instance.GetClientPlayers())
            {
                if (cp.NetworkPlayer.ID == networkClient.LocalPlayerId)
                    continue;
                cp.Update();
            }
        }

        public override void Render()
        {
            base.Render();
            foreach (var cp in ClientNetwork.Instance.GetClientPlayers())
            {
                if (cp.NetworkPlayer.ID == networkClient.LocalPlayerId)
                    continue;
                cp.InGamePlayer?.Render();
            }
        }

        public override void UnloadContent()
        {
            base.UnloadContent();
            if (ClientNetwork.Instance != null)
            {
                ClientNetwork.Instance.OnPlayerJoined -= SpawnRemotePlayer;
                ClientNetwork.Instance.OnPlayerLeft -= RemoveRemotePlayer;
                networkClient.Disconnect("Scene unloaded");
            }
        }

        public void SpawnRemotePlayer(ClientPlayer player)
        {
            if (ClientNetwork.Instance == null)
                return;
            if (ClientNetwork.Instance.LocalPlayerId == player.NetworkPlayer.ID)
                return;
            if (player.InGamePlayer == null)
            {
                var remote = new RemoteAstronaut(player.NetworkPlayer);
                remote.LatestPosition = Camera.Main.Position;
                remote.LatestRotation = Quaternion.Identity;
                remote.Position = Camera.Main.Position;
                player.InGamePlayer = remote;
                OnRenderCenter += remote.Render;
            }
        }

        public void RemoveRemotePlayer(ClientPlayer player)
        {
            if (player.InGamePlayer != null)
            {
                player.InGamePlayer.OnDisconnect();
                OnRenderCenter -= player.InGamePlayer.Render;
                player.InGamePlayer = null;
            }
        }
    }
}
