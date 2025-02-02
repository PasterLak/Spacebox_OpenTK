using OpenTK.Mathematics;
using Engine;
using Spacebox.Game.GUI;

using SharpNBT;
using Client;
using SpaceNetwork;
using static System.Net.Mime.MediaTypeNames;


namespace Spacebox.Game.Player
{
    public class RemoteAstronaut : Node3D
    {
        private SpaceNetwork.Player playerData;
        public Vector3 LatestPosition { get; set; }
        public Quaternion LatestRotation { get; set; }

        private CubeRenderer cube;
        private Model spacer;

        private GUI.Tag tag;
        public RemoteAstronaut(SpaceNetwork.Player player)
        {

            playerData = player;
            cube = new CubeRenderer(LatestPosition);
            cube.Color = Color4.Green;
            cube.Enabled = true;

            var tex = TextureManager.GetTexture("Resources/Textures/Astronaut.jpg");
              tex.FlipY();
            tex.UpdateTexture(true);
            spacer = new Model("Resources/Models/Astronaut.obj",
                new Material(ShaderManager.GetShader("Shaders/textured"), tex));

            tag = new GUI.Tag($"[{playerData.ID}]{playerData.Name}", LatestPosition, new Color4(playerData.Color.X, playerData.Color.Y, playerData.Color.Z, 1));
            TagManager.RegisterTag(tag);
        }

        /*
         *  protected override void UpdateVectors()
        {
            _front = Vector3.Transform(-Vector3.UnitZ, _rotation);  //  old
            _up = Vector3.Transform(Vector3.UnitY, _rotation);
            _right = Vector3.Transform(Vector3.UnitX, _rotation);

          

            //Rotation = _rotation.ToEulerAngles() * 360f;
        }

         * */


        public static Quaternion MirrorQuaternion(Quaternion original)
        {
            Quaternion flip = Quaternion.FromAxisAngle(Vector3.UnitY, MathHelper.DegreesToRadians(180));
            return original * flip;
        }

        public void UpdateRemote()
        {
            Position = Vector3.Lerp(Position, LatestPosition, Time.Delta * 5f);
            //  Rotation = Vector4.Lerp(Rotation, LatestRotation, Time.Delta * 5f);
            Rotation = Node3D.QuaternionToEulerDegrees(LatestRotation);
            cube.Position = Position;
            // cube.Rotation += new Vector3(10,0,0) * Time.Delta;
            spacer.Rotation = Rotation;
            spacer.Position = Position;
            //cube.Rotation = Rotation;

            var up = Vector3.Transform(Vector3.UnitY, LatestRotation);
            tag.WorldPosition = spacer.Position + up *  2.3f;
        }

        public void Render()
        {
            // cube.Render();
            spacer.Draw(Camera.Main);
        }
    }
}
