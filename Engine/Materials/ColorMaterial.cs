namespace Engine
{
    public class ColorMaterial : MaterialBase
    {
        public ColorMaterial() : base(Resources.Load<Shader>("Shaders/colored"))
        {
            RenderFace = RenderFace.Front;
            RenderMode = RenderMode.Opaque;
        }

        public ColorMaterial(Shader customShader) : base(customShader)
        {
            RenderFace = RenderFace.Front;
            RenderMode = RenderMode.Opaque;
        }

    }
}
