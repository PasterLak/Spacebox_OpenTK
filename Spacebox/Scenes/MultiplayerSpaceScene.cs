
using Client;
using Engine;

using OpenTK.Mathematics;
using Spacebox.Game.Player;
using System.Xml.Linq;

namespace Spacebox.Scenes
{
    public class MultiplayerSpaceScene : BaseSpaceScene
    {
        ClientNetwork networkClient;
        Dictionary<int, ClientPlayer> remotePlayers = new Dictionary<int, ClientPlayer>();
        private string mpAppKey;
        private string mpHost;
        private int mpPort;
        private string mpPlayerName;

        public MultiplayerSpaceScene(string[] args) : base(args) // name mod seed modfolder
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
           // GL.ClearColor(0f, 0f, 0f, 1f);
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

            if(ClientNetwork.Instance != null)
            {
              
                ClientNetwork.Instance.OnPlayerJoined += SpawnRemotePlayer;
            }
        }

        public override void Update()
        {
            base.Update();
            if (!networkClient.IsConnected)
            {
                Debug.Error("Lost connection to server. Returning to Multiplayer Menu.");
               // SceneManager.LoadScene(typeof(SpaceMenuScene));
              //  return;
            }
            networkClient.PollEvents();
            foreach (var remote in remotePlayers.Values)
            {
                remote.Update();
            }
        }

        public override void Render()
        {
            base.Render();
           
        }

        public override void UnloadContent()
        {
            base.UnloadContent();
            ClientNetwork.Instance.OnPlayerJoined -= SpawnRemotePlayer;
            networkClient.Disconnect("Scene unloaded");
        }

        public void SpawnRemotePlayer(ClientPlayer player)
        { 
            if (ClientNetwork.Instance == null)
            {

                Debug.Error("[] no ClientNetwork instance found!");
                return;
            }

            var id = player.NetworkPlayer.ID;

            if (ClientNetwork.Instance.LocalPlayerId == id)
            {
                Debug.Log($"same ip {id}, no  remote player");
                return;
            }

           
            if (remotePlayers.ContainsKey(id))
            {
                var remote = remotePlayers[id].InGamePlayer;
                remote.LatestPosition = player.NetworkPlayer.Position.ToOpenTKVector3() ;
                // remote.LatestRotation = rot;
                Debug.Log("Contains remote player");

            }
            else
            {
                var remote = new RemoteAstronaut(player.NetworkPlayer);

                remote.LatestPosition = Camera.Main.Position;
                remote.LatestRotation = Quaternion.Identity;
               
                remote.Position = Camera.Main.Position;
            
                
                remote.Position = Camera.Main.Position;
                player.InGamePlayer = remote;
                remotePlayers.Add(id, player);
                //SceneGraph.AddRoot(remote);
                OnRenderCenter += remote.Render;

                Debug.Log("added remote player + " + player.NetworkPlayer.ID);

            }
        }


    }
}
