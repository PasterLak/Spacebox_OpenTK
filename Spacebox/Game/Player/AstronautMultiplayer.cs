using Client;
using OpenTK.Mathematics;


namespace Spacebox.Game.Player
{
    public class AstronautMultiplayer : Astronaut
    {
        public AstronautMultiplayer(Vector3 position) : base(position)
        {

        }

        public override void Update()
        {
            base.Update();
            if (ClientNetwork.Instance != null && ClientNetwork.Instance.IsConnected)
            {
                ClientNetwork.Instance.SendPosition(Position,GetRotation());
            }
        }
    }
}
