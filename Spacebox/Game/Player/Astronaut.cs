using Engine;
using Engine.Components;
using Engine.Components.Debug;
using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Client;
using SpaceNetwork.Messages;


namespace Spacebox.Game.Player
{
    public abstract class Astronaut : Camera360Base, IDamageable
    {
     
        protected ModelRendererComponent AstBody;
        protected ModelRendererComponent AstHelmet;
        protected ModelRendererComponent AstTank;
        public Flashlight Flashlight { get; private set; }

        protected HandItemVisualizer HandVisualizer;
        protected ColliderComponent sphereCollision;
        protected NetworkIdentity rpc;
        public int SkinId { get; protected set; }

        public Astronaut(Vector3 position, bool isLocal) : base(position, isLocal)
        {
            Name = "Astronaut";
            Layer = CollisionLayer.Player;

            AttachComponent(new AxesDebugComponent());
            sphereCollision = AttachComponent(new SphereCollider());

        }

        public bool IsLocalAstronaut()
        {
            return this is LocalAstronaut;
        }

        public void ChangeItemInHand(Item? item)
        {

            HandVisualizer?.SetItem(item);

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

        [Rpc]
        protected void TestRpc()
        {
            Debug.Log("RPC message!");
        }
        [Rpc]
        protected void TestRpcArgs(int x, string message)
        {
            Debug.Log($"RPC message 2! {x} {message}");
        }

        [Rpc]
        protected void RpcColor()
        {
            SetSkinColor("Red");
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

        public void TakeDamage(Projectile projectile)
        {
            var local = this as LocalAstronaut;

            if (local != null)
            {
                string[] deathMessages =
                { "Wasted", "Hull breach detected", "Caught a bullet"};

                var random = new Random();
                string message = deathMessages[random.Next(deathMessages.Length)];

                if (projectile.HasOwer(out var owner))
                {

                }
                local.TakeDamage(projectile, new DeathCase(message));
            }
        }

        public bool CheckCollision(Ray ray, out float distance)
        {
            distance = 0;

            Vector3 rayDir = ray.Direction.Normalized();
            Vector3 playerLookDir = ForwardLocal.Normalized();
            Vector3 toRayStart = ray.Origin - Position;

            float dirDot = Vector3.Dot(rayDir, playerLookDir);
            float startPosDot = Vector3.Dot(toRayStart, playerLookDir);

            if (dirDot > 0 && startPosDot > -0.5f)
            {
                return false;
            }

            return ray.Intersects(sphereCollision, out distance);
        }
    }
}