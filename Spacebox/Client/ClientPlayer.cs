
using SpaceNetwork;
using Spacebox.Game.Player;
using Engine;

namespace Client
{
    public class ClientPlayer
    {
        public Player NetworkPlayer { get; private set; }
        public AstronautRemote RemotePlayer { get; set; } 

        public ClientPlayer(Player networkPlayer)
        {
            NetworkPlayer = networkPlayer;
        }

        public void UpdateFromNetwork(Player updated)
        {
            NetworkPlayer.Position = updated.Position;
            NetworkPlayer.Rotation = updated.Rotation;
            NetworkPlayer.DisplayedPosition = updated.DisplayedPosition;
            NetworkPlayer.LastTimeWasActive = updated.LastTimeWasActive;
            NetworkPlayer.Color = updated.Color;
            NetworkPlayer.Name = updated.Name;
        }

        public void Update()
        {
            if (RemotePlayer == null) return;

            RemotePlayer.LatestPosition = NetworkPlayer.Position.ToOpenTKVector3();
            RemotePlayer.LatestRotation = new OpenTK.Mathematics.Quaternion(NetworkPlayer.Rotation.X, NetworkPlayer.Rotation.Y, NetworkPlayer.Rotation.Z, NetworkPlayer.Rotation.W);
            RemotePlayer.UpdateRemote();
        }
    }
}

