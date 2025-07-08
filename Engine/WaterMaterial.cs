using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine
{
    public class WaterMaterial : MaterialBase
    {
        public Texture2D Noise { get; }
        public float Shininess { get; set; } = 48f;

        public WaterMaterial(Texture2D noise) : base(Resources.Load<Shader>("Shaders/water"))
        {
            Noise = noise;
            RenderMode = RenderMode.Transparent;
            RenderFace = RenderFace.Both;
            AddTexture("noiseMap", Noise);
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
