using OpenTK.Graphics.OpenGL4;

namespace Engine
{
    public class TextureMaterial : MaterialBase
    {
        public Texture2D? MainTexture { get; set; }

        public TextureMaterial(Texture2D texture) : base(Resources.Load<Shader>("Shaders/textured"))
        {
            MainTexture = texture;
            RenderFace = RenderFace.Front;
            RenderMode = RenderMode.Cutout;
        }

        public TextureMaterial(Texture2D texture, Shader shader) : 
            base(shader)
        {
            MainTexture = texture;
            RenderFace = RenderFace.Front;
            RenderMode = RenderMode.Cutout;
        }

        public static MeshBuffer GetMeshBuffer()
        {
            var attrs = new BufferAttribute[]
          {
                new BufferAttribute { Name = "aPos",    Size = 3 },
                new BufferAttribute { Name = "aNormal", Size = 3 },
                 new BufferAttribute { Name = "aTexCoords", Size = 2 }
          };

          return new MeshBuffer(attrs);
        }

        protected override void SetMaterialProperties()
        {
            base.SetMaterialProperties();

            UseTexture(MainTexture, "texture0");
        }

    }
}
