using OpenTK.Mathematics;


namespace Engine.Light
{
    public class SpotLight : Light
    {
        private Vector3 _ambient = new Vector3(0.15f);
        private Vector3 _diffuse = new Vector3(1.0f, 1.0f, 0.9f);
        private Vector3 _specular = new Vector3(1.0f);

        public override Vector3 Ambient
        {
            get => _ambient;
            set => _ambient = value;
        }
        public override Vector3 Diffuse
        {
            get => _diffuse;
            set => _diffuse = value;
        }
        public override Vector3 Specular
        {
            get => _specular;
            set => _specular = value;
        }

        public bool IsActive = false;
        public bool UseSpecular = true;

        public Vector3 Direction { get; set; }
        public float CutOffRadians { get; set; } = MathF.Cos(MathHelper.DegreesToRadians(12.5f));
        public float OuterCutOffRadians { get; set; } = MathF.Cos(MathHelper.DegreesToRadians(20.5f));
        public float Constant { get; set; } = 1.0f;
        public float Linear { get; set; } = 0.09f;
        public float Quadratic { get; set; } = 0.032f;

        public SpotLight(Shader shader, Vector3 direction) : base(shader)
        {
            Direction = direction;
        }

        public override void Draw(Camera camera)
        {
            base.Draw(camera);
            var position = camera.CameraRelativeRender ? Vector3.Zero : camera.Position;
            Shader.SetVector3("spotLight.position", position);
            Shader.SetVector3("spotLight.direction", camera.Front);
            Shader.SetFloat("material_shininess", 32.0f);
            if (!IsActive)
            {
                Shader.SetVector3("spotLight.ambient", Vector3.Zero);
                Shader.SetVector3("spotLight.diffuse", Vector3.Zero);
                if (UseSpecular) Shader.SetVector3("spotLight.specular", Vector3.Zero);
            }
            else
            {
                Shader.SetVector3("spotLight.ambient", Ambient);
                Shader.SetVector3("spotLight.diffuse", Diffuse);
                if (UseSpecular) Shader.SetVector3("spotLight.specular", Specular);
            }
            Shader.SetFloat("spotLight.constant", Constant);
            Shader.SetFloat("spotLight.linear", Linear);
            Shader.SetFloat("spotLight.quadratic", Quadratic);
            Shader.SetFloat("spotLight.cutOff", CutOffRadians);
            Shader.SetFloat("spotLight.outerCutOff", OuterCutOffRadians);
        }
    }
}
