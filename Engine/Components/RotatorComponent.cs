using Engine.Components;
using OpenTK.Mathematics;

namespace Engine
{
    public class RotatorComponent : Component
    {
        public Vector3 EulerSpeed = new Vector3(0, 30, 0);

        public RotatorComponent(Vector3 eulerSpeed)
        {
            EulerSpeed = eulerSpeed;
        }

        public override void OnUpdate()
        {
            Owner.Rotate(EulerSpeed * (float)Time.Delta);
        }
    }
}