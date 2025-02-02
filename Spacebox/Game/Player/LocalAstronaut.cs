using Client;
using OpenTK.Mathematics;
using Spacebox.Game.Player;

namespace Spacebox.Game.Player
{
    public class LocalAstronaut : Astronaut
    {
        public LocalAstronaut(Vector3 position) : base(position)
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
