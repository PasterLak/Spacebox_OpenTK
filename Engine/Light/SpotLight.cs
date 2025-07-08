using OpenTK.Mathematics;

namespace Engine.Light
{
    public class SpotLight : LightBase
    {
        internal override LightKind Kind => LightKind.Spot;

        private Vector3 _direction = -Vector3.UnitZ;
        public Vector3 Direction
        {
            get
            {
                if (!GetDirectionFromNode) return _direction;
                return WorldForward;
            }
            set => _direction = value;
        }
        public bool GetDirectionFromNode = false;
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
