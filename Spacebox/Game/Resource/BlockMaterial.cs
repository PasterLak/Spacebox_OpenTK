using Engine;
using OpenTK.Graphics.OpenGL4;
using Spacebox.Game.Player;

namespace Spacebox.Game.Resource
{
    public class BlockMaterial : TextureMaterial
    {
        private Astronaut player;
        public Texture2D? EmissionTexture { get; set; }

        public BlockMaterial(Texture2D texture, Texture2D emissionAtlas, Astronaut player) : 
            base(texture, ShaderManager.GetShader("Shaders/block"))
        {
            RenderMode = RenderMode.Cutout;
            EmissionTexture = emissionAtlas;
            this.player = player;
            Shader.Use();
            Shader.SetInt("texture0", 0);
            Shader.SetInt("textureAtlas", 1);

            Shader.SetFloat("fogDensity", Lighting.FogDensity);
            Shader.SetVector3("fogColor", Lighting.FogColor);
            Shader.SetVector3("ambientColor", Lighting.AmbientColor);
        }

        protected override void SetMaterialProperties()
        {
            base.SetMaterialProperties();

            if (EmissionTexture != null)
            {
                GL.ActiveTexture(TextureUnit.Texture1);
                EmissionTexture.Use();
                Shader.SetInt("textureAtlas", 1);
            }

            Shader.SetVector3("cameraPosition", player.Position);
            Shader.SetVector3("ambientColor", Lighting.AmbientColor);
        }
    }
}
