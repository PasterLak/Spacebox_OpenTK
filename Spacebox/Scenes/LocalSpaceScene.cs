using OpenTK.Mathematics;
using Spacebox.Game.Player;

namespace Spacebox.Scenes
{
    public class LocalSpaceScene : BaseSpaceScene
    {
        public LocalSpaceScene(string[] args) : base(args)
        {
        }

        public override void LoadContent()
        {
            localPlayer = new Astronaut(new Vector3(5, 5, 5));
            localPlayer.GameMode = GameMode.Creative;
            SceneGraph.AddRoot(localPlayer);
            base.LoadContent();
        }
    }
}
