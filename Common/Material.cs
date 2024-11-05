

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Spacebox.Common
{
    public class Material
    {
        public Shader Shader { get; private set; }
        public Texture2D Texture { get; private set; }
        public TextureCube CubeTexture { get; private set; }


        public Vector4 Color { get; set; } = Vector4.One;
        public Vector2 Offset { get;  set; } = Vector2.Zero;
        public Vector2 Tiling { get;  set; } = Vector2.One;

        public Material()
        {
            Shader = ShaderManager.GetShader("Shaders/colored");

        }

        public Material(Shader shader)
        {
            Shader = shader;
           
        }

        public Material(Shader shader, Texture2D texture)
        {
            Shader = shader;
            Texture = texture;
        }

        public Material(Shader shader, TextureCube cubeTexture)
        {
            Shader = shader;
            CubeTexture = cubeTexture;
        }
        
        public void Use()
        {
            

            //GL.PolygonMode(MaterialFace.FrontAndBack, polygonMode);

            Shader.SetVector2("offset", Offset);
            Shader.SetVector2("tiling", Tiling);
            Shader.SetVector4("color", Color);
            Shader.Use();
            
            Texture?.Use();
            CubeTexture?.Use();

            //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        }
    }
}
