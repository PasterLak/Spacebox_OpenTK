using Engine;
using Engine.Components;
using Engine.Components.Debug;
using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Resource;


namespace Spacebox.Game.Player
{
    public abstract class Astronaut : Camera360Base
    {
        private readonly string[] colors = new[] { "Yellow", "Orange", "Purple", "Blue", "Green", "Cyan", "Red", "White", "Black" };
        protected ModelRendererComponent AstBody;
        protected ModelRendererComponent AstHelmet;
        protected ModelRendererComponent AstTank;
        public Flashlight Flashlight { get; private set; }

        protected HandItemVisualizer HandVisualizer;


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
            if (HandVisualizer != null)
            {
                Debug.Log($"[Astronaut] Changing item in hand to: {(item != null ? item.Name : "None")}");
                HandVisualizer.SetItem(item);
            }
        }

        protected void CreateModel(int id, string color)
        {

            Texture2D tex = GetAstronautTexture(color);

            var mat = new TextureMaterial(tex);

            var meshBody = GameAssets.LoadResource<Engine.Mesh>("Resources/Models/Player/Astronaut_Body_Fly.obj");
            var meshHelmet = GameAssets.LoadResource<Engine.Mesh>("Resources/Models/Player/Astronaut_Helmet_Closed.obj");
            var meshTank = GameAssets.LoadResource<Engine.Mesh>("Resources/Models/Player/Astronaut_Tank_Fly.obj");

            AstBody = AttachComponent(new ModelRendererComponent(new Model(meshBody, mat)));
            AstHelmet = AttachComponent(new ModelRendererComponent(new Model(meshHelmet, mat)));
            AstTank = AttachComponent(new ModelRendererComponent(new Model(meshTank, mat)));

            HandVisualizer = new HandItemVisualizer(this);
            AddChild(HandVisualizer);

            SetupFlashlight();
        }
        public void SetSkinColor(string color)
        {
            if (string.IsNullOrEmpty(color)) return;

            Texture2D tex = GetAstronautTexture(color);
            if (tex == null) return;

            if (AstBody != null && AstBody.Model != null && AstBody.Model.Material is TextureMaterial bodyMat)
                bodyMat.MainTexture = tex;

            if (AstHelmet != null && AstHelmet.Model != null && AstHelmet.Model.Material is TextureMaterial helmetMat)
                helmetMat.MainTexture = tex;

            if (AstTank != null && AstTank.Model != null && AstTank.Model.Material is TextureMaterial tankMat)
                tankMat.MainTexture = tex;
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

        public static Texture2D GetAstronautTexture(string color)
        {
            Texture2D tex = null;

            string texturePath = $"Resources/Textures/Skins/Astronaut_{color}.jpg";
           
            tex = GameAssets.LoadResource<Texture2D>(texturePath);
            if (!tex.YWasFlipped)
                tex.FlipY();
            tex.FilterMode = FilterMode.Nearest;


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