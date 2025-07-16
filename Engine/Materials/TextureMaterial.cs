using OpenTK.Graphics.OpenGL4;

namespace Engine
{
    public class TextureMaterial : MaterialBase
    {
        public Texture2D? MainTexture { get; set; }

        public TextureMaterial(Texture2D texture) : base(Resources.Load<Shader>("Shaders/textured"))
        {
            Init(texture);
        }

        public TextureMaterial(Texture2D texture, Shader shader) : 
            base(shader)
        {
            Init(texture);
        }

        public TextureMaterial( Shader shader, Texture2D texture) :
            base(shader)
        {
            Init(texture);
        }

        private void Init(Texture2D texture)
        {
            MainTexture = texture;
            RenderFace = RenderFace.Front;
            RenderMode = RenderMode.Cutout;
            AddTexture("texture0", MainTexture);
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


    }
}
