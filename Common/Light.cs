using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public abstract class Light
    {
        public Vector3 Ambient { get; set; } = new Vector3(0.2f);
        public Vector3 Diffuse { get; set; } = new Vector3(0.5f);
        public Vector3 Specular { get; set; } = new Vector3(1.0f);
    }

    public class DirectionalLight : Light
    {
        public Vector3 Direction { get; set; } = new Vector3(-0.2f, -1.0f, -0.3f);
    }

    public class PointLight : Light
    {
        public Vector3 Position { get; set; }
        public float Constant { get; set; } = 1.0f;
        public float Linear { get; set; } = 0.09f;
        public float Quadratic { get; set; } = 0.032f;

        public PointLight(Vector3 position)
        {
            Position = position;
        }
    }

    public class SpotLight : Light
    {
        public Vector3 Position { get; set; }
        public Vector3 Direction { get; set; }
        public float CutOff { get; set; } = MathF.Cos(MathHelper.DegreesToRadians(12.5f));
        public float OuterCutOff { get; set; } = MathF.Cos(MathHelper.DegreesToRadians(17.5f));
        public float Constant { get; set; } = 1.0f;
        public float Linear { get; set; } = 0.09f;
        public float Quadratic { get; set; } = 0.032f;

        public SpotLight(Vector3 position, Vector3 direction)
        {
            Position = position;
            Direction = direction;
        }
    }
}
