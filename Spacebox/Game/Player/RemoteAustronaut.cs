using OpenTK.Mathematics;
using Engine;
using Spacebox.Game.GUI;

using SharpNBT;
using Client;
using SpaceNetwork;


namespace Spacebox.Game.Player
{
    public class RemoteAstronaut : Node3D
    {
        private SpaceNetwork.Player playerData;
        public Vector3 LatestPosition { get; set; }
        public Vector3 LatestRotation { get; set; }

        private CubeRenderer cube;
        private Model spacer;

        private GUI.Tag tag;
        public RemoteAstronaut(SpaceNetwork.Player player)
        {

            playerData = player;
            cube = new CubeRenderer(LatestPosition);
            cube.Color = Color4.Green;
            cube.Enabled = true;

            var tex = TextureManager.GetTexture("Resources/Textures/spacer.png");
          //  tex.FlipY();
            tex.UpdateTexture(true);
            spacer = new Model("Resources/Models/spacer.obj",
                new Material(ShaderManager.GetShader("Shaders/textured"), tex));

            tag = new GUI.Tag($"[{playerData.ID}]{playerData.Name}", LatestPosition, new Color4(playerData.Color.X, playerData.Color.Y, playerData.Color.Z, 1));
           TagManager.RegisterTag(tag);
        }
        public void UpdateRemote()
        {
            Position = Vector3.Lerp(Position, LatestPosition, Time.Delta * 5f);
            Rotation = Vector3.Lerp(Rotation, LatestRotation, Time.Delta * 5f);

            cube.Position = Position;
            // cube.Rotation += new Vector3(10,0,0) * Time.Delta;
            spacer.Rotation = Rotation;
            spacer.Position = Position;
            //cube.Rotation = Rotation;
            tag.WorldPosition = Position + LocalToWorld(new Vector3(0,1,0), spacer);
        }

        public void Render()
        {
           // cube.Render();
            spacer.Draw(Camera.Main);
        }
    }
}
