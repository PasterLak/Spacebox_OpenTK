using OpenTK.Graphics.OpenGL4;

namespace Engine
{
    public class ParticleMaterial : MaterialBase
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
                    ReplaceTexture("particleTexture", _mainTexture);
                }
            }
        }

        public ParticleMaterial(Texture2D texture) : base(Resources.Load<Shader>("Shaders/particle"))
        {
           
            RenderFace = RenderFace.Both;
            RenderMode = RenderMode.Fade;

            MainTexture = texture;
        }

        public ParticleMaterial(Texture2D texture, Shader shader) :
            base(shader)
        {
            
            RenderFace = RenderFace.Both;
            RenderMode = RenderMode.Fade;

            MainTexture = texture;
        }


    }
}
