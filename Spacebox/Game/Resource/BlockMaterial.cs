using Engine;
using Spacebox.Game.Player;

namespace Spacebox.Game.Resource
{
    public class BlockMaterial : TextureMaterial
    {
        private Astronaut player;
        public Texture2D? EmissionTexture { get; set; }

        public BlockMaterial(Texture2D texture, Texture2D emissionAtlas, Astronaut player) : 
            base(texture, Resources.Load<Shader>("Shaders/block"))
        {
            RenderMode = RenderMode.Cutout;
            EmissionTexture = emissionAtlas;
            this.player = player;

            AddTexture("textureAtlas", EmissionTexture);
            Shader.Use();
            Shader.SetInt("texture0", 0);
            Shader.SetInt("textureAtlas", 1);

       

            
        }

        protected override void UpdateDynamicUniforms()
        {
            base.UpdateDynamicUniforms();
           
        }
 
    }
}
