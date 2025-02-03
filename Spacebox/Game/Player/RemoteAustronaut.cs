using OpenTK.Mathematics;
using Engine;
using Spacebox.Game.GUI;
using SharpNBT;
using Client;
using SpaceNetwork;
using static System.Net.Mime.MediaTypeNames;
using Engine.Physics;

namespace Spacebox.Game.Player
{
    public class RemoteAstronaut : Node3D
    {
        private SpaceNetwork.Player playerData;
        public Vector3 LatestPosition { get; set; }
        public Quaternion LatestRotation { get; set; }

        private CubeRenderer cube;
        private Model spacerOld;
        private Model spacer;
        private GUI.Tag tag;
        private ItemModel itemModel;
        private Quaternion currentRotation = Quaternion.Identity;
        private Shader itemModelShader;
        static bool wasFlipped = false;
        private Shader playerShader;
        public void OnDisconnect()
        {
            if(tag != null)
            {
                TagManager.UnregisterTag(tag);
            }
        }
        public RemoteAstronaut(SpaceNetwork.Player player)
        {
            playerData = player;
            cube = new CubeRenderer(LatestPosition);
            cube.Color = Color4.Green;
            cube.Enabled = true;
            var texold = TextureManager.GetTexture("Resources/Textures/spacer.png");
            texold.UpdateTexture(true);

            playerShader = ShaderManager.GetShader("Shaders/player");
            spacerOld = new Model("Resources/Models/spacer.obj", new Material(playerShader, texold));
            var tex = TextureManager.GetTexture("Resources/Textures/Astronaut.jpg", true, true);
            tex.FlipY();
            tex.UpdateTexture(true);
            spacer = new Model("Resources/Models/Astronaut_klein.obj", new Material(playerShader, tex));
            tag = new GUI.Tag($"[{playerData.ID}]{playerData.Name}", LatestPosition, new Color4(playerData.Color.X, playerData.Color.Y, playerData.Color.Z, 1));
            TagManager.RegisterTag(tag);
            itemModelShader = ShaderManager.GetShader("Shaders/itemModel");
            var uvIndex = GameBlocks.AtlasItems.GetUVIndexByName("drill1");
            itemModel  = ItemModelGenerator.GenerateModel(
                GameBlocks.ItemsTexture, uvIndex.X, uvIndex.Y, 0.1f, 300f / 500f * 2f, false, false);
            itemModel.UseMainCamera = true;
            itemModel.offset = Vector3.Zero;
        }

        public static Quaternion MirrorQuaternion(Quaternion original)
        {
            Quaternion flip = Quaternion.FromAxisAngle(Vector3.UnitY, MathHelper.DegreesToRadians(180));
            return original * flip;
        }

        public void UpdateRemote()
        {
            Position = Vector3.Lerp(Position, LatestPosition, Time.Delta * 5f);
            currentRotation = Quaternion.Slerp(currentRotation, LatestRotation, Time.Delta * 5f);
            Rotation = Node3D.QuaternionToEulerDegrees(currentRotation);
            cube.Position = Position;
            var up = Vector3.Transform(Vector3.UnitY, currentRotation);

           
            if (playerData.Name == "alconaut")
            {
                spacerOld.Rotation = Rotation;
                spacerOld.Position = Position;
                tag.WorldPosition = Position + up * 1f;
               
            }
            else
            {
                spacer.Rotation = Rotation;
                spacer.Position = Position;
                tag.WorldPosition = Position + up * 1f;
            }
            itemModel.Position = Position;

        }

        public void Render()
        {
            if (VisualDebug.Enabled)
            {
                BoundingSphere sphere = new BoundingSphere(spacer.Position, 0.5f);
                VisualDebug.DrawBoundingSphere(sphere, Color4.Yellow);
            }

            itemModelShader.SetVector4("ambient", new Vector4(Lighting.AmbientColor,1));
            if (playerData.Name == "alconaut")
            {
                spacerOld.Draw(Camera.Main);
                return;
            }
            itemModel.Draw(itemModelShader);
            spacer.Draw(Camera.Main);
        }
    }
}
