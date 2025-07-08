using OpenTK.Mathematics;

namespace Engine.Light
{
    public abstract class LightBase : Node3D
    {
        internal abstract LightKind Kind { get; }
     
        public float Intensity { get; set; } = 1f;
        public Vector3 Diffuse { get; set; } = new(0.4f);
        public Vector3 Specular { get; set; } = new(0.5f);

        protected LightBase()
        {
            LightSystem.Register(this);
          
        }

        public override void Destroy()
        {
            LightSystem.Unregister(this);
            base.Destroy();

           
        }
    }
}
