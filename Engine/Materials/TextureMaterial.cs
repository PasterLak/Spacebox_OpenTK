using OpenTK.Graphics.OpenGL4;

namespace Engine
{
    public class TextureMaterial : MaterialBase
    {
        private Texture2D? _mainTexture;

        public Texture2D? MainTexture
        {
            get => _mainTexture;
            set
            {
                _mainTexture = value;
                if (_mainTexture != null)
                {
                    ReplaceTexture("texture0", _mainTexture);
                }
            }
        }

        public TextureMaterial(Texture2D texture) : base(Resources.Load<Shader>("Resources/Shaders/textured"))
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
