using OpenTK.Mathematics;

namespace Engine
{
    public class ColorMaterial : MaterialBase
    {
        public ColorMaterial() : base(ShaderManager.GetShader("Shaders/colored"))
        {
            RenderFace = RenderFace.Front;
            RenderMode = RenderMode.Opaque;
        }

    }
}
