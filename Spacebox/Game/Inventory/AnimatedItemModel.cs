using OpenTK.Mathematics;
using Engine;

using Engine.Animation;

namespace Spacebox.Game
{
    public class AnimatedItemModel : ItemModel
    {

        public Animator Animator { get; private set; }
        public AnimatedItemModel(Mesh mesh, Texture2D texture) : base(mesh, texture)
        {
            Animator = new Animator(this);

            //Animator.AddAnimation(new MoveAnimation(Position, Position + new Vector3(0.005f, 0, 0), 0.05f, true));

            SetAnimation(false);
        }

        public void SetAnimation(bool state)
        {
            if (state == Animator.IsActive) return;
            Animator.IsActive = state;
            Position = Vector3.Zero;
        }

        public override void Render()
        {
            Animator.Update();
            base.Render();
        }

        
    }
}
