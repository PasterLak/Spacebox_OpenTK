namespace Engine
{
    public class ColorMaterial : MaterialBase
    {
        public ColorMaterial() : base(Resources.Load<Shader>("Resources/Shaders/colored"))
        {
            RenderFace = RenderFace.Front;
            RenderMode = RenderMode.Opaque;
            WritesGBuffer = true;
        }

        public ColorMaterial(Shader customShader) : base(customShader)
        {
            RenderFace = RenderFace.Front;
            RenderMode = RenderMode.Opaque;
        }

    }
}
