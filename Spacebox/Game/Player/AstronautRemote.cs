using OpenTK.Mathematics;
using Engine;
using Spacebox.Game.GUI;

using Engine.Physics;
using Engine.Light;

namespace Spacebox.Game.Player
{
    public class AstronautRemote : Node3D
    {
        private SpaceNetwork.Player playerData;
        public Vector3 LatestPosition { get; set; }
        public Quaternion LatestRotation { get; set; }
        private CubeRenderer cube;
        private Model spacerOld;
        private Model spacer;
        private GUI.Tag tagg;
        private ItemModel itemModel;
        private Quaternion currentRotation = Quaternion.Identity;
        
        static bool wasFlipped = false;
        private Shader playerShader;
        private SpotLight spotLight;
        private Model astBody;
        private Model astHelmet;
        private Model astTank;
        private static Dictionary<string, Texture2D> astronautTextures = new Dictionary<string, Texture2D>();
        
        public void OnDisconnect()
        {
            if (tagg != null)
            {
                TagManager.UnregisterTag(tagg);
            }
        }

        public AstronautRemote(SpaceNetwork.Player player)
        {
            playerData = player;
            cube = new CubeRenderer(LatestPosition);
            cube.Color = Color4.Green;
            cube.Enabled = true;
            var texold = Engine.Resources.Get<Texture2D>("Resources/Textures/spacer.png");
            texold.UpdateTexture(true);
            playerShader = Resources.Load<Shader>("Shaders/player");
            spacerOld = new Model(Resources.Load<Engine.Mesh>("Resources/Models/spacer.obj"), new TextureMaterial(playerShader, texold));
            var tex = Resources.Load<Texture2D>("Resources/Textures/astronaut2.jpg");
            tex.FlipY();
            tex.FilterMode = FilterMode.Nearest;
            spacer = new Model(Resources.Load<Engine.Mesh>("Resources/Models/astronaut2.obj"), new TextureMaterial(playerShader, tex));
            tagg = new GUI.Tag($"[{playerData.ID}]{playerData.Name}", LatestPosition, new Color4(playerData.Color.X, playerData.Color.Y, playerData.Color.Z, 1));
            tagg.TextAlignment = GUI.Tag.Alignment.Center;
            GUI.Tag.CalculateFontSize(100);
            TagManager.RegisterTag(tagg);
          
            var uvIndex = GameAssets.AtlasItems.GetUVIndexByName("drill1");
            itemModel = ItemModelGenerator.GenerateModel(GameAssets.ItemsTexture, uvIndex.X, uvIndex.Y, 0.1f, 300f / 500f * 2f, false, false);
            itemModel.UseMainCamera = true;
            itemModel.offset = Vector3.Zero;
            spotLight = new SpotLight(playerShader, Camera.Main.Front);
            spotLight.UseSpecular = false;
            spotLight.IsActive = true;
            Create(player.ID);
        }

        private static Texture2D GetAstronautTexture(string color)
        {
            if (!astronautTextures.TryGetValue(color, out Texture2D tex))
            {
                string texturePath = $"Resources/Textures/Player/Astronaut_{color}.jpg";
                tex = Resources.Load<Texture2D>(texturePath);
                tex.FlipY();
                tex.FilterMode = FilterMode.Nearest;
             
                astronautTextures[color] = tex;
            }
            return tex;
        }

        private void Create(int id)
        {
            var colors = new[] { "Yellow", "Orange", "Purple", "Blue", "Green", "Cyan", "Red", "White", "Black" };
            int index = Math.Abs(id) % colors.Length;
            var selectedColor = colors[index];
            Texture2D tex = GetAstronautTexture(selectedColor);
            var mat = new TextureMaterial(playerShader, tex);

            var mesh1 = Resources.Load<Engine.Mesh>("Resources/Models/Player/Astronaut_Body_Fly.obj");
            var mesh2 = Resources.Load<Engine.Mesh>("Resources/Models/Player/Astronaut_Helmet_Closed.obj");
            var mesh3 = Resources.Load<Engine.Mesh>("Resources/Models/Player/Astronaut_Tank_Fly.obj");

            astBody = new Model(mesh1, mat);
            astHelmet = new Model(mesh2, mat);
            astTank = new Model(mesh3, mat);
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
                tagg.WorldPosition = Position + up * 1f;
            }
            else
            {
             
                astBody.Rotation = Rotation;
                astBody.Position = Position;
                astHelmet.Rotation = Rotation;
                astHelmet.Position = Position;
                astTank.Rotation = Rotation;
                astTank.Position = Position;
                tagg.WorldPosition = Position + up * 1f;
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
            playerShader.Use();
            playerShader.SetVector3("cameraPosition", Camera.Main.Position);
            playerShader.SetVector3("viewPos", Camera.Main.Position);
            playerShader.SetFloat("material_shininess", 32.0f);
            playerShader.SetVector3("fogColor", new Vector3(0.05f, 0.05f, 0.05f));
            playerShader.SetVector4("color", new Vector4(1, 1, 1, 1));
            Vector3 camPos = Camera.Main.Position;
            Vector3 camFront = Camera.Main.Front;
            playerShader.SetVector3("spotLight.position", camPos);
            playerShader.SetVector3("spotLight.direction", camFront);
            playerShader.SetVector3("spotLight.ambient", new Vector3(0.2f, 0.2f, 0.2f));
            playerShader.SetVector3("spotLight.diffuse", new Vector3(0.8f, 0.8f, 0.8f));
            playerShader.SetVector3("spotLight.specular", new Vector3(1.0f, 1.0f, 1.0f));
            playerShader.SetFloat("spotLight.constant", 1.0f);
            playerShader.SetFloat("spotLight.linear", 0.09f);
            playerShader.SetFloat("spotLight.quadratic", 0.032f);
            playerShader.SetFloat("spotLight.cutOff", MathF.Cos(MathHelper.DegreesToRadians(12.5f)));
            playerShader.SetFloat("spotLight.outerCutOff", MathF.Cos(MathHelper.DegreesToRadians(17.5f)));
            if (playerData.Name == "alconaut")
            {
                spacerOld.Render(Camera.Main);
                return;
            }
            spotLight.Render(Camera.Main);
            itemModel.Render();
            astBody.Render(Camera.Main);
            astHelmet.Render(Camera.Main);
            astTank.Render(Camera.Main);
        }
    }
}
