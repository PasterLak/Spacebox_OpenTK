
using SpaceNetwork;
using Spacebox.Game.Player;

namespace Client
{
    public class ClientPlayer
    {
        public Player NetworkPlayer { get; private set; }
        public RemoteAstronaut InGamePlayer { get; set; } 

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
    }
}

