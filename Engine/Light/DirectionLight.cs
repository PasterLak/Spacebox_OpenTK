using OpenTK.Mathematics;

namespace Engine.Light
{
    public sealed class DirectionalLight : LightBase
    {
        internal override LightKind Kind => LightKind.Directional;

        public Vector3 Direction { get; set; } = new(-0.2f, -1.0f, -0.3f);

        public DirectionalLight()
        {
            Diffuse = new(0.4f);
            Specular = new(0.5f);
            Name = nameof(DirectionalLight);
        }

    }
}
