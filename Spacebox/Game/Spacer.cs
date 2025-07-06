using Engine;
using Engine.Animation;
using Engine.Components;
using Engine.Components.Debug;
using OpenTK.Mathematics;
using Spacebox.Game.GUI;


namespace Spacebox.Game
{
    public class Spacer : Node3D
    {
        protected Animator animator;

        public ColliderComponent OBB => obb;
        private ColliderComponent obb;
        private Storage storage;
        public Spacer(Vector3 pos) {

            Position = pos;
            Texture2D spacerTex = Resources.Get<Texture2D>("Resources/Textures/spacer.png");
            spacerTex.FlipY();
            spacerTex.UpdateTexture(true);
            Name = "Spacer";

            storage = new Storage(4,4);

            obb = AttachComponent(new AABBCollider());
            AttachComponent(new MeshRendererComponent(
                Resources.Load<Mesh>("Resources/Models/spacer.obj"), 
                new Material(Resources.Load<Shader>("Shaders/player"), spacerTex)));

           
            animator = new Animator(this);
            animator.AddAnimation(new MoveAnimation(Position, Position + new Vector3(0, 0, 1000), 5000f, false));
            animator.AddAnimation(new RotateAnimation(Vector3.UnitX, 5f, 0f));
            animator.AddAnimation(new RotateAnimation(Vector3.UnitY, 5f, 0f));

        }

        public override void Update()
        {
            base.Update();
        
            animator.Update();
        }

        public override void Render()
        {
            base.Render();

        }
    }
}
