using OpenTK.Mathematics;

namespace Engine
{
    public class MaterialOld : MaterialBase
    {
       
        public Texture2D Texture { get; private set; }

        public Vector2 Offset { get;  set; } = Vector2.Zero;
        public Vector2 Tiling { get;  set; } = Vector2.One;

        public MaterialOld() : base(null)
        {
            RenderFace = RenderFace.Front;
            RenderMode = RenderMode.Opaque;
           // Shader = Resources.Load<Shader>("Shaders/colored");

        }

        public MaterialOld(Shader shader) : base(shader)
        {
     
        }

        public MaterialOld(Shader shader, Texture2D texture) : base(shader)
        {
            Texture = texture;
        }

        /*public override void Use()
        {
            //GL.PolygonMode(MaterialFace.FrontAndBack, polygonMode);

            Shader.SetVector2("offset", Offset);
            Shader.SetVector2("tiling", Tiling);
            Shader.SetVector4("color", Color);
            Shader.Use();

            Texture?.Use();


            //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        }*/
    }
}
