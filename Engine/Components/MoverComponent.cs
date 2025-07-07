using Engine.Components;
using OpenTK.Mathematics;

namespace Engine
{
    public class MoverComponent : Component
    {
        public Vector3 Velocity = new Vector3(0, 0, 1);
        public float Speed = 1f;

        public MoverComponent(Vector3 velocity, float speed)
        {
            Velocity = velocity; Speed = speed;
        }

        public override void OnUpdate()
        {
            Owner.Translate(Velocity * (Speed * (float)Time.Delta));
        }
    }

}
