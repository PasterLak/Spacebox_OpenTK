
using Client;
using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Player;

namespace Spacebox.Scenes
{
    public class MultiplayerSpaceScene : BaseSpaceScene
    {
        ClientNetwork networkClient;
        Dictionary<int, RemoteAstronaut> remotePlayers = new Dictionary<int, RemoteAstronaut>();

        public MultiplayerSpaceScene(string[] args) : base(args)
        {
        }

        public override void LoadContent()
        {
            localPlayer = new LocalAstronaut(new Vector3(5, 5, 5));
            localPlayer.GameMode = GameMode.Creative;
            SceneGraph.AddRoot(localPlayer);
            networkClient = ClientNetwork.Instance;
            base.LoadContent();
        }

        public override void Update()
        {
            base.Update();
            networkClient.PollEvents();

            foreach (var remote in remotePlayers.Values)
            {
                remote.UpdateRemote(Time.Delta);
            }
        }

        public override void UnloadContent()
        {
            base.UnloadContent();
            networkClient.Disconnect("Scene unloaded");
        }

        public void UpdateRemotePlayer(int id, Vector3 pos, Vector3 rot, string name)
        {
            if (remotePlayers.ContainsKey(id))
            {
                var remote = remotePlayers[id];
                remote.LatestPosition = pos;
                remote.LatestRotation = rot;
            }
            else
            {
                var remote = new RemoteAstronaut
                {
                    LatestPosition = pos,
                    LatestRotation = rot,
                    PlayerName = name,
                    Position = pos
                };
                remotePlayers.Add(id, remote);
                SceneGraph.AddRoot(remote);
            }
        }
    }
}
