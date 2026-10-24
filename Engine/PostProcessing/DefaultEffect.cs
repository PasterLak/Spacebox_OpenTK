using Engine.Graphics.Rhi;
using OpenTK.Mathematics;


namespace Engine.PostProcessing
{
    public class DefaultEffect : PostProcessEffect
    {
        private Shader blackWhiteShader;
        public override bool IsRequired => true;
        public DefaultEffect(Shader shader)
        {
            
            blackWhiteShader = shader;
        }
        public override void Apply(int inputTexture, int outputFbo, Vector2i clientSize)
        {
        
            blackWhiteShader.Use();
            Gpu.Device.BindTexture(0, inputTexture);
            blackWhiteShader.SetInt("scene", 0);

           
        }
    }
}
