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
using SpaceNetwork.Messages;

namespace Spacebox.Scenes
{
    public class MultiplayerScene : BaseSpaceScene
    {
       
        public override void LoadContent()
        {
            localPlayer = new LocalAstronaut(new Vector3(5, 5, 5));
            localPlayer.GameMode = GameMode.Creative;

          
            if (ClientNetwork.Instance == null)
            {
                Debug.Error("No existing connection. Returning to Multiplayer Menu.");
                SceneManager.Load<MenuScene>();
                return;
            }

            base.LoadContent();

            ClientNetwork.Instance.Players.OnPlayerJoined += AddRemotePlayerToScene;
            ClientNetwork.Instance.Players.OnPlayerLeft += RemoveRemotePlayerFromScene;
            ClientNetwork.Instance.OnBlockDestroyed += OnBlockDestroyed;
            ClientNetwork.Instance.OnBlockPlaced += OnBlockPlaced;

            Debug.Success("[MultiplayerScene] ClientNetwork: Success");
            Debug.RegisterCommand(new TeleportToPlayerCommand(localPlayer));

            
        }

        public void OnBlockDestroyed(int x, int y, int z)
        {
            if (World.CurrentSector.TryGetNearestEntity(Camera.Main.Position, out var entity))
            {
                entity.RemoveBlockAtLocal(new Vector3(x, y, z), new Vector3SByte(0, 0, 0));
            }
        }

        public void OnBlockPlaced(BlockPlaceMessage message)
        {
            Block block = GameAssets.CreateBlockFromId(message.blockID);
            if (message.GetDirection() <= 6)
            {
                block.Direction = (Direction)message.GetDirection();
                block.Rotation = (Rotation)message.GetRotation();
            }

            if (World.CurrentSector.TryGetNearestEntity(Camera.Main.Position, out var entity))
            {
                entity.TryPlaceBlockLocal(new Vector3(message.GetX(), message.GetY(), message.GetZ()), block);
            }
        }

        public override void Start()
        {
            base.Start();
            var players = ClientNetwork.Instance.GetRemotePlayers();
            foreach (var remote in players)
            {
                AddRemotePlayerToScene(remote);

            }

            localPlayer.Flashlight.OnEnabledChanged += enabled =>
            {
              
                ClientNetwork.Instance.SendFlashlightState(enabled);
            };
            ClientNetwork.Instance.SendFlashlightState(localPlayer.Flashlight.Enabled);
        }

        public override void Update()
        {
            base.Update();

            ClientNetwork.Instance.PollEvents();

        }

        public override void UnloadContent()
        {
            base.UnloadContent();
            if (ClientNetwork.Instance != null)
            {
                ClientNetwork.Instance.Players.OnPlayerJoined -= AddRemotePlayerToScene;
                ClientNetwork.Instance.Players.OnPlayerLeft -= RemoveRemotePlayerFromScene;
                if(ClientNetwork.Instance.IsConnected)
                ClientNetwork.Instance.Disconnect("Scene unloaded");
            }

            
        }

        private void AddRemotePlayerToScene(RemoteAstronaut remote)
        {
            if (remote == null) return;
            if(ClientNetwork.Instance != null && remote.NetworkData.ID == ClientNetwork.Instance.LocalPlayerId)
            {
                Debug.Error($"Tried to add remote player with same ID as local player: {remote.Name}");
                return;
            }

            AddChild(remote);
      
            Debug.Log($"[MultiplayerScene] Adding remote player: {remote.NetworkData.Name}{remote.NetworkData.ID} with skin color: {remote.NetworkData.SkinColor}");
            remote.CreatePlayerVisuals(remote.NetworkData.SkinColor);
           

            if (localPlayer != null)
            {
               // ClientNetwork.Instance.SendFlashlightState(localPlayer.Flashlight.Enabled);
               
            }
   
        }

        private void RemoveRemotePlayerFromScene(RemoteAstronaut remote)
        {
            if (remote == null) return;

            remote.OnDisconnect();
            RemoveChild(remote);
    
        }
    }
}