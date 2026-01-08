using Engine;
using Engine.Components;
using Engine.Components.Debug;
using Engine.Physics;
using OpenTK.Mathematics;


namespace Spacebox.Game.Player
{
    public abstract class Astronaut : Camera360Base
    {
        protected ModelRendererComponent AstBody;
        protected ModelRendererComponent AstHelmet;
        protected ModelRendererComponent AstTank;
        public Flashlight Flashlight { get; private set; }

        protected ItemWorldModel _itemInHand;

        private static Dictionary<string, Texture2D> _astronautTextures = new Dictionary<string, Texture2D>();

        public int SkinId { get; protected set; }

        public Astronaut(Vector3 position, bool isLocal) : base(position, isLocal)
        {
            Name = "Astronaut";
            Layer = CollisionLayer.Player;

            AttachComponent(new AxesDebugComponent());
        }

        public bool IsLocalAstronaut()
        {
            return this is LocalAstronaut;
        }

        public void ChangeItemInHand(Item? item)
        {
            if (_itemInHand == null)
                return;
            if (item is BlockItem)
            {

                _itemInHand.Enabled = false;
                return;
            }

            if (item != null)
            {
                _itemInHand.ChangeModelTo(item);
                _itemInHand.Enabled = true;
            }
            else
            {
                _itemInHand.Enabled = false;
            }
        }

        protected void CreateModel(int id)
        {
            SkinId = id;
            var colors = new[] { "Yellow", "Orange", "Purple", "Blue", "Green", "Cyan", "Red", "White", "Black" };
            int index = Math.Abs(id) % colors.Length;
            var selectedColor = colors[index];
            Texture2D tex = GetAstronautTexture(selectedColor);
            var mat = new TextureMaterial(tex);

            var meshBody = Resources.Load<Engine.Mesh>("Resources/Models/Player/Astronaut_Body_Fly.obj");
            var meshHelmet = Resources.Load<Engine.Mesh>("Resources/Models/Player/Astronaut_Helmet_Closed.obj");
            var meshTank = Resources.Load<Engine.Mesh>("Resources/Models/Player/Astronaut_Tank_Fly.obj");

            AstBody = AttachComponent(new ModelRendererComponent(new Model(meshBody, mat)));
            AstHelmet = AttachComponent(new ModelRendererComponent(new Model(meshHelmet, mat)));
            AstTank = AttachComponent(new ModelRendererComponent(new Model(meshTank, mat)));

            _itemInHand = AddChild(new ItemWorldModel("default:titanium_drill", 0.1f));
            _itemInHand.Rotate(new Vector3(10, 90, -10));
            _itemInHand.SetScale(0.6f);
            _itemInHand.Position = new Vector3(0.35f, -0.47f, -0.25f);
            _itemInHand.Enabled = false;



            SetupFlashlight();
        }

        private void SetupFlashlight()
        {
            Flashlight = new Flashlight();
            AddChild(Flashlight);
            Flashlight.Position = Vector3.Zero;
            Flashlight.Diffuse = new Color3Byte(242, 211, 143).ToVector3();
            Flashlight.Specular = Vector3.Zero;
            Flashlight.CutOff = 15;
            Flashlight.OuterCutOff = 25;
        }

        private static Texture2D GetAstronautTexture(string color)
        {
            if (!_astronautTextures.TryGetValue(color, out Texture2D tex))
            {
                string texturePath = $"Resources/Textures/Skins/Astronaut_{color}.jpg";
                tex = Resources.Load<Texture2D>(texturePath);
                tex.FlipY();
                tex.FilterMode = FilterMode.Nearest;
                _astronautTextures[color] = tex;
            }
            return tex;
        }

        public override void Update()
        {
            base.Update();
            UpdateVisuals();
        }

        protected virtual void UpdateVisuals()
        {
            bool showModel = !IsMain;

            if (AstBody != null) AstBody.Enabled = showModel;
            if (AstHelmet != null) AstHelmet.Enabled = showModel;
            if (AstTank != null) AstTank.Enabled = showModel;
        }

    }
}