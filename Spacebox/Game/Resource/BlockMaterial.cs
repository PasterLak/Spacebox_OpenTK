using Engine;
using Spacebox.Game.Player;

namespace Spacebox.Game.Resource
{
    public class BlockMaterial : TextureMaterial
    {
        private Astronaut player;

        private Texture2D? _emission;

        public Texture2D? EmissionTexture
        {
            get => _emission;
            set
            {
                _emission = value;
                if (_emission != null)
                {
                    ReplaceTexture("textureAtlas", _emission);
                }
            }
        }

        public BlockMaterial(Texture2D texture, Texture2D emissionAtlas, Astronaut player) : 
            base(texture, Resources.Load<Shader>("Shaders/block"))
        {
            RenderMode = RenderMode.Cutout;
            EmissionTexture = emissionAtlas;
            this.player = player;

            
        }

        protected override void UpdateDynamicUniforms()
        {
            base.UpdateDynamicUniforms();
           
        }
 
    }
}
