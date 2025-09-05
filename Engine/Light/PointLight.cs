
namespace Engine.Light
{
    public sealed class PointLight : LightBase
    {
        internal override LightKind Kind => LightKind.Point;

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

        public float Constant { get; set; } = 1.0f;
        public float Linear { get; set; } = 0.09f;
        public float Quadratic { get; set; } = 0.032f;

        public PointLight()
        {
            Diffuse = new(0.8f);
            Specular = new(1f);
            Range = 10f;
            Name = nameof(PointLight);
        }
 
    }
}
