using Engine;
using OpenTK.Mathematics;
using Spacebox.Scenes.Test;

namespace Spacebox.Scenes.Test
{
    public class MoverComponent : Component
    {
        public Vector3 Velocity = new Vector3(0, 0, 1);
        public float Speed = 1f;

        public MoverComponent(Vector3 velocity, float speed)
        {
            Velocity = velocity; Speed = speed;
        }

        public override void Update()
        {
            Owner.Translate(Velocity * Speed * (float)Time.Delta);
        }
    }

    public class RotatorComponent : Component
    {
        public Vector3 EulerSpeed = new Vector3(0, 30, 0);

        public RotatorComponent(Vector3 eulerSpeed)
        {
            EulerSpeed = eulerSpeed;
        }

        public override void Update()
        {
            Owner.Rotate(EulerSpeed * (float)Time.Delta);
        }
    }
}
