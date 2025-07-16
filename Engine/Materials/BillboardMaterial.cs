
using OpenTK.Mathematics;

namespace Engine
{
    public class BillboardMaterial : MaterialBase
    {
        public Texture2D MainTexture { get; set; }
        public Vector2 Size { get; set; }

        public BillboardMaterial(Texture2D texture, Vector2 size)
            : base(Resources.Load<Shader>("Shaders/billboard"))
        {
            MainTexture = texture;
            Size = size;
            RenderFace = RenderFace.Front;
            RenderMode = RenderMode.Opaque;
            AddTexture("tex", MainTexture);
            TransposeMatrices = false;
        }

        public BillboardMaterial(Texture2D texture, Shader shader, Vector2 size)
            : base(shader)
        {
            MainTexture = texture;
            Size = size;
            RenderFace = RenderFace.Front;
            RenderMode = RenderMode.Opaque;
            AddTexture("tex", MainTexture);
        }

        protected override void UpdateDynamicUniforms()
        {
            Shader.SetVector2("size", Size);
        }
    }
}
