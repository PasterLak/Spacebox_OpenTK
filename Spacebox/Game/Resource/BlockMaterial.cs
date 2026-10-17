using Engine;


namespace Spacebox.Game.Resource
{
    public class BlockMaterial : TextureMaterial
    {
       

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

        public BlockMaterial(Texture2D texture, Texture2D emissionAtlas) : 
            base(texture, Resources.Load<Shader>("Resources/Shaders/block"))
        {
            RenderMode = RenderMode.Cutout;
            EmissionTexture = emissionAtlas;
        
        }

        protected override void UpdateDynamicUniforms()
        {
            base.UpdateDynamicUniforms();
           
        }
 
    }
}
