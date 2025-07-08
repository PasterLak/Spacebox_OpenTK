using OpenTK.Mathematics;

namespace Engine.Light
{
    public sealed class SpotLight : LightBase
    {
        internal override LightKind Kind => LightKind.Spot;

        public Vector3 Direction { get; set; } = -Vector3.UnitZ;

        public float CutOff { get; set; } = MathF.Cos(MathHelper.DegreesToRadians(12.5f));
        public float OuterCutOff { get; set; } = MathF.Cos(MathHelper.DegreesToRadians(17.5f));

        public float Constant { get; set; } = 1.0f;
        public float Linear { get; set; } = 0.09f;
        public float Quadratic { get; set; } = 0.032f;


        public SpotLight(Vector3 direction) 
        {
            Direction = direction;

            Diffuse = new(1f);
            Specular = new(1f);
            Name = nameof(SpotLight); 
        }
        public SpotLight()
        {
            Diffuse = new(1f);
            Specular = new(1f);
            Name = nameof(SpotLight);
        }

        

    }
}
