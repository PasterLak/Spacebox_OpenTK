using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine
{
    public class WaterMaterial : MaterialBase
    {
        private Texture2D? _noise;

        public Texture2D? Noise
        {
            get => _noise;
            set
            {
                _noise = value;
                if (_noise != null)
                {
                    ReplaceTexture("noiseMap", _noise);
                }
            }
        }
        public float Shininess { get; set; } = 48f;

        public WaterMaterial(Texture2D noise) : base(Resources.Load<Shader>("Shaders/water"))
        {
            Noise = noise;
            RenderMode = RenderMode.Opaque;
            RenderFace = RenderFace.Both;
            AddTexture("noiseMap", Noise);
        }

        protected override void ApplyRenderSettings()
        {
            base.ApplyRenderSettings();


        }
        protected override void UpdateDynamicUniforms()
        {
       
            Shader.SetFloat("shininess", Shininess);
        }

        public static MeshBuffer GetMeshBuffer()
        {
            var attrs = new BufferAttribute[]
            {
                new BufferAttribute { Name = "vertexPosition", Size = 3 },
                new BufferAttribute { Name = "vertexNormal",   Size = 3 },
                new BufferAttribute { Name = "vertexUV",       Size = 2 }
            };
            return new MeshBuffer(attrs);
        }
    }
}
