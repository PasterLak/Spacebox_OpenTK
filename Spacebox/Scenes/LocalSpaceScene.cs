using OpenTK.Mathematics;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;

namespace Spacebox.Scenes
{
    public class LocalSpaceScene : BaseSpaceScene
    {


        public override void LoadContent()
        {
            localPlayer = new LocalAstronaut(new Vector3(5, 5, 5));
            localPlayer.GameMode = GameMode.Creative;
         

            base.LoadContent();
        }
    }
}
