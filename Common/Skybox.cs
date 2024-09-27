using System;
using System.Drawing;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public class Skybox
    {
        private readonly int _vao;
        private readonly int _vbo;
        private readonly int _texture;
        private readonly Shader _shader;
        private readonly Camera _camera;
        private readonly float _size;

        // Define the vertices for a cube
        private readonly float[] _vertices = {
            // positions          
            -1.0f,  1.0f, -1.0f,
            -1.0f, -1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,

            -1.0f, -1.0f,  1.0f,
            -1.0f, -1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f,  1.0f,
            -1.0f,  1.0f, -1.0f,
            -1.0f, -1.0f, -1.0f,

             1.0f, -1.0f, -1.0f,
             1.0f, -1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,

            -1.0f, -1.0f,  1.0f,
            -1.0f,  1.0f,  1.0f,
             1.0f,  1.0f, -1.0f,
             1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,
            -1.0f, -1.0f,  1.0f,

            -1.0f,  1.0f, -1.0f,
             1.0f,  1.0f, -1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
            -1.0f,  1.0f,  1.0f,
            -1.0f,  1.0f, -1.0f,

            -1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f,  1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f,  1.0f,
             1.0f, -1.0f,  1.0f
        };

        public Skybox(Camera camera, float size, string texturePath)
        {
            _camera = camera;
            _size = size;

            // Load the shader for the skybox
            _shader = new Shader("Shaders/shader");

            // Generate VAO and VBO for the cube
            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();

            GL.BindVertexArray(_vao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

            GL.BindVertexArray(0);

            // Load the texture
            _texture = LoadTexture(texturePath);
        }

        private int LoadTexture(string path)
        {
            int texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.TextureCubeMap, texture);

            // Load texture data for all six faces
            for (int i = 0; i < 6; i++)
            {
                using (var image = new Bitmap(path))
                {
                    var data = image.LockBits(
                        new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
                        System.Drawing.Imaging.ImageLockMode.ReadOnly,
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb
                    );

                    GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i,
                        0, PixelInternalFormat.Rgba, image.Width, image.Height, 0,
                        PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

                    image.UnlockBits(data);
                }
            }

            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);

            GL.BindTexture(TextureTarget.TextureCubeMap, 0);

            return texture;
        }

        public void Render()
        {
            GL.DepthFunc(DepthFunction.Lequal);

            _shader.Use();

            // Set the view and projection matrix
            Matrix4 view = 
                Matrix4.CreateTranslation(new Vector3(-0.5f, -0.5f, -0.5f)) * 
                Matrix4.CreateRotationY(0) *
                Matrix4.CreateScale(2) *
                Matrix4.CreateTranslation(_camera.Position);

            _shader.SetMatrix4("model", view, false);
            _shader.SetMatrix4("view", _camera.GetViewMatrix(), false);
            _shader.SetMatrix4("projection", _camera.GetProjectionMatrix(), false);

            // Bind the skybox texture
            GL.BindVertexArray(_vao);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.TextureCubeMap, _texture);

            // Render the skybox cube
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

            GL.BindVertexArray(0);
            GL.DepthFunc(DepthFunction.Less);
        }
    }
}
