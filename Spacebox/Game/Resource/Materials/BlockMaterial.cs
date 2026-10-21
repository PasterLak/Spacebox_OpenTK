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
            WritesGBuffer = true;
            EmissionTexture = emissionAtlas;
            AddCrackTexture();
        
        }

        private const string CrackTexturePath = "Resources/Textures/Effects/blockCracks.png";

        private void AddCrackTexture()
        {
            var cracks = Resources.Load<Texture2D>(CrackTexturePath);
            if (cracks == null) return;
            cracks.FilterMode = FilterMode.Nearest;
            AddTexture("crackTexture", cracks);
        }

                protected override void UpdateDynamicUniforms()
        {
            base.UpdateDynamicUniforms();
           
        }
 
    }
}
