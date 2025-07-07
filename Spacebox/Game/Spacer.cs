using Engine;
using Engine.Animation;
using Engine.Components;
using Engine.Components.Debug;
using Engine.Physics;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.GUI;


namespace Spacebox.Game
{
    public class Spacer : Node3D
    {
        protected Animator animator;
        private StatsBarData Health { get;  set; }
        public ColliderComponent OBB => obb;
        private ColliderComponent obb;
        public Storage Storage { get; private set; }
        public Spacer(Vector3 pos) {

            Position = pos;
            Texture2D spacerTex = Resources.Get<Texture2D>("Resources/Textures/spacer.png");
            spacerTex.FlipY();
            spacerTex.UpdateTexture(true);
            Name = "Spacer";
            Health = new StatsBarData();
            Health.MaxCount = 100;
            Health.Count = 100;
            Health.DataChanged += OnHit;
            Health.OnEqualZero += OnKilled;
            Storage = new Storage(3,3);
            Storage.Name = Name;

            obb = AttachComponent(new OBBCollider());

            Model spacerModel = new Model(Resources.Load<Mesh>("Resources/Models/spacer.obj"), 
                new TextureMaterial(spacerTex));
            AttachComponent(new ModelRendererComponent(spacerModel));
            AttachComponent(new AxesDebugComponent());

            animator = new Animator(this);
            animator.AddAnimation(new MoveAnimation(Position, Position + new Vector3(0, 0, 1000), 5000f, false));
            animator.AddAnimation(new RotateAnimation(Vector3.UnitX, 5f, 0f));
            animator.AddAnimation(new RotateAnimation(Vector3.UnitY, 5f, 0f));

            AddItems(Storage);

            var mo = AddChild(new ItemWorldModel("Resources/Textures/Old/drill6.png", 0.1f));
            mo.Rotate(new Vector3(0,90,0));
            mo.SetScale(0.5f);
            mo.Position = new Vector3(0.3f,-0.2f,-0.2f);


        }

        public void Hit(Projectile projectile)
        {
            Health.Decrement(projectile.Parameters.Damage);
        }
        
        private void OnHit()
        {
           
        }
        
        private void OnKilled()
        {
           
            Storage.Clear();
            Parent?.RemoveChild(this);
        }
        private void AddItems(Storage storage) // hard coded
        {
            var item = GameAssets.GetItemByName("Iron Ingot");
            if (item != null)
            {
                storage.TryAddItem(item, 6);
            }
           
        }

        public override void Update()
        {
            base.Update();
          
            animator.Update();

            var cam = Camera.Main as Astronaut;

            if (cam == null) return;

            Ray ray = new Ray(cam.Position, cam.Front, 5f);

            if (ray.Intersects(OBB, out float distance))
            {
              
                if (distance < 3)
                {
                    CenteredText.SetText("Press RMB to open");
                    CenteredText.Show();

                    if (Input.IsMouseButtonDown(MouseButton.Right))
                    {
                        
                        StorageUI.OpenStorage(Storage, cam );
                    }
                }
                
            }
        }

        public override void Render()
        {
            base.Render();
           
        }

    }
}
