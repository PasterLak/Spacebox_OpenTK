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
                return ForwardWorld;
            }
            set => _direction = value;
        }
        public bool GetDirectionFromNode = false;

        private float _cutOff = MathF.Cos(MathHelper.DegreesToRadians(12.5f));
        private float _outerCutOff = MathF.Cos(MathHelper.DegreesToRadians(17.5f));
        public float CutOff { get => _cutOff; set => MathF.Cos(MathHelper.DegreesToRadians(value)); }
        public float OuterCutOff { get => _outerCutOff; set => MathF.Cos(MathHelper.DegreesToRadians(value)); }

        public float Constant { get; set; } = 1.0f;
        public float Linear { get; set; } = 0.09f;
        public float Quadratic { get; set; } = 0.032f;

        private float _range = 10f;
        public float Range
        {
            get => _range;
            set
            {
                _range = value;
                Linear = 4.5f / _range;
                Quadratic = 75f / (_range * _range);
            }
        }


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
