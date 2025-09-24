using OpenTK.Graphics.OpenGL4;

namespace Engine
{
 
    public class FadeMaterial : TextureMaterial
    {
        public FadeMaterial(Texture2D texture)
            : base(texture, Resources.Load<Shader>("Resources/Shaders/fade"))  
        {
            RenderMode = RenderMode.Fade;    
            RenderFace = RenderFace.Front;   
        }

  
        protected override void ApplyRenderSettings()
        {
            base.ApplyRenderSettings();        
                     
        }
    }
}
