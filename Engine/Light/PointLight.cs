using OpenTK.Mathematics;


namespace Engine.Light
{
    public class PointLight : Light, IPoolable<PointLight>
    {

        private Vector3 _baseAmbient = new Vector3(0.2f);
        private Vector3 _baseDiffuse = new Vector3(0.5f);
        private Vector3 _baseSpecular = new Vector3(1.0f);

        private Vector3 _ambient;
        private Vector3 _diffuse;
        private Vector3 _specular;

        private float _range = 10f;
        private float _brightness = 1f;

        public Vector3 Position { get; set; }

        public float Constant { get; set; } = 1.0f;
        public float Linear { get; private set; } = 0.09f;
        public float Quadratic { get; private set; } = 0.032f;

        public float Range
        {
            get => _range;
            set
            {
                _range = value < 1f ? 1f : value;
                UpdateRangeAndBrightness();
            }
        }

        public float Brightness
        {
            get => _brightness;
            set
            {
                _brightness = value;
                UpdateRangeAndBrightness();
            }
        }
        public override Vector3 Ambient
        {
            get => _ambient;
            set
            {
                _baseAmbient = value;
                UpdateRangeAndBrightness();
            }
        }

        public override Vector3 Diffuse
        {
            get => _diffuse;
            set
            {
                _baseDiffuse = value;
                UpdateRangeAndBrightness();
            }
        }

        public override Vector3 Specular
        {
            get => _specular;
            set
            {
                _baseSpecular = value;
                UpdateRangeAndBrightness();
            }
        }

        private bool _isActive = false;
        public bool IsActive { get => _isActive; set => _isActive = value; }

        public PointLight() : base(null)
        {

        }
        public PointLight(Shader shader, Vector3 position, float range = 10f, float brightness = 1f)
            : base(shader)
        {
            Position = position;
            _range = range < 1f ? 1f : range;
            _brightness = brightness;
            UpdateRangeAndBrightness();
        }

        public override void Render(Camera camera)
        {
            base.Render(camera);

        }

        public void UpdateRangeAndBrightness()
        {

            Linear = 4.5f / _range;
            Quadratic = 75f / (_range * _range);


            _ambient = _baseAmbient * _brightness;
            _diffuse = _baseDiffuse * _brightness;
            _specular = _baseSpecular * _brightness;
        }

        public void SetShaderParams(int index, Camera camera)
        {
            if (Shader == null) return;


            string prefix = $"pointLights[{index}]";

            var pos = camera.CameraRelativeRender ? Position - camera.Position : Position;

            Shader.SetVector3(prefix + ".position", pos);
            Shader.SetFloat(prefix + ".constant", Constant);
            Shader.SetFloat(prefix + ".linear", Linear);
            Shader.SetFloat(prefix + ".quadratic", Quadratic);
            Shader.SetVector3(prefix + ".ambient", Ambient);
            Shader.SetVector3(prefix + ".diffuse", Diffuse);
            Shader.SetVector3(prefix + ".specular", Specular);

        }

        public PointLight CreateFromPool()
        {
            return new PointLight(null, Vector3.One, 10, 1);
        }

        public void Reset()
        {

        }
    }
}
