using OpenTK.Mathematics;
using Spacebox.common.Animation;
using Spacebox.Common;
using Spacebox.Common.Animation;

namespace Spacebox.Game
{
    public class AnimatedItemModel : ItemModel
    {

        private Animator animator;
        public AnimatedItemModel(Mesh mesh, Texture2D texture) : base(mesh, texture)
        {
            animator = new Animator(this);
            animator.AddAnimation(new MoveAnimation(Position, Position + new Vector3(0.005f, 0, 0), 0.05f, true));

            SetAnimation(false);
        }

        public void SetAnimation(bool state)
        {
            if (state == animator.IsActive) return;
            animator.IsActive = state;
            Position = Vector3.Zero;
        }

        public override void Draw(Shader shader)
        {
            animator.Update(Time.Delta);
            base.Draw(shader);
        }

        
    }
}
