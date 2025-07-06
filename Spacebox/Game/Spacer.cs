using Engine;
using Engine.Animation;
using Engine.Components;
using Engine.Components.Debug;
using OpenTK.Mathematics;


namespace Spacebox.Game
{
    public class Spacer : Node3D
    {
        protected Animator animator;
        Axes axes;
        private OBBCollider obb;
        public Spacer(Vector3 pos) {

            Position = pos;
            Texture2D spacerTex = Resources.Get<Texture2D>("Resources/Textures/spacer.png");
            spacerTex.FlipY();
            spacerTex.UpdateTexture(true);
     
            Name = "Spacer";
            axes = new Axes(pos, 2);
          
            obb = AttachComponent(new OBBCollider()) as OBBCollider;
            AttachComponent(new MeshRendererComponent(
                Resources.Load<Engine.Mesh>("Resources/Models/spacer.obj"), 
                new Material(Resources.Load<Shader>("Shaders/player"), spacerTex)));
            AttachComponent(new AxesComponent(1));
            animator = new Animator(this);
            animator.AddAnimation(new MoveAnimation(Position, Position + new Vector3(0, 0, 1000), 5000f, false));
            animator.AddAnimation(new RotateAnimation(Vector3.UnitX, 5f, 0f));
            animator.AddAnimation(new RotateAnimation(Vector3.UnitY, 5f, 0f));

        }

        public override void Update()
        {
            base.Update();
            //axes.Position = Position;
            //axes.Update();

            if(obb.Volume.Intersects(Camera.Main.BoundingVolume))
            {
                Debug.Log("hit");
            }
            //VisualDebug.DrawPosition(Position, Color4.Green);
            // Debug.Log(Position.ToString());
            animator.Update();
        }

        public override void Render()
        {
            base.Render();

           // axes.Render();
          
        }
    }
}
