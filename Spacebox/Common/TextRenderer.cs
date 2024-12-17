using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public class TextRenderer : IDisposable
    {
        private int _vao;
        private int _vbo;

        private Shader _shader;
        private BitmapFont _font;

        private Matrix4 _projection;

        public void SetProjection(Matrix4 p)
        { _projection = p; }

        public TextRenderer( BitmapFont font, int screenWidth, int screenHeight)
        {
            _shader =  ShaderManager.GetShader("Shaders/font"); 
            _font = font;

          
            _projection = Matrix4.CreateOrthographicOffCenter(
                0, screenWidth,
                screenHeight, 0,
                -1, 1);

        
            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();

            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);

         
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * 6 * 4, IntPtr.Zero, BufferUsageHint.DynamicDraw);

          
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);

         
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }

        public void RenderText(string text, float x, float y, float scale, Vector3 color)
        {
            _shader.Use();
            _shader.SetVector3("textColor", color);
            _shader.SetMatrix4("projection", _projection, false);
            _shader.SetInt("textTexture", 0);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindVertexArray(_vao);
            char lastChar;
            int lineIndex = 1;

            float xStart = x;

            foreach (char c in text)
            {
                if(c == '\n') { lineIndex++; x = xStart;  }
                if(c == ' ')
                {
                   // x += 32 * scale;
                   // continue;
                }
                if (!_font.ContainsGlyph(c))
                {
                    //Console.WriteLine("No glyph: " + c);
                    continue;
                }

                var glyph = _font.GetGlyph(c);

                float xpos = x + glyph.Bearing.X * scale;
                float ypos = y + (glyph.Bearing.Y) * scale;

                float w = glyph.Size.X * scale;
                float h = glyph.Size.Y * scale;

                int line = GetNewLine(lineIndex, (int)h);

              
                float[] vertices = {
            // x          y            u                         v
            xpos,        ypos + line,        glyph.TexOffset.X,        glyph.TexOffset.Y,
            xpos,        ypos + line + h,    glyph.TexOffset.X,        glyph.TexOffset.Y + glyph.TexSize.Y,
            xpos + w,    ypos + line + h,    glyph.TexOffset.X + glyph.TexSize.X, glyph.TexOffset.Y + glyph.TexSize.Y,

            xpos,        ypos + line,        glyph.TexOffset.X,        glyph.TexOffset.Y,
            xpos + w,    ypos+ line + h,    glyph.TexOffset.X + glyph.TexSize.X, glyph.TexOffset.Y + glyph.TexSize.Y,
            xpos + w,    ypos + line,        glyph.TexOffset.X + glyph.TexSize.X, glyph.TexOffset.Y
        };

                GL.BindTexture(TextureTarget.Texture2D, _font.Texture);

                GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
                GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, sizeof(float) * vertices.Length, vertices);

                GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

                x += (glyph.Advance) * scale;
            }

            GL.BindVertexArray(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        private int GetNewLine(int currentLineNumber, int glyphHeight)
        {
            return currentLineNumber * glyphHeight;
        }

        public void Dispose()
        {
            GL.DeleteBuffer(_vbo);
            GL.DeleteVertexArray(_vao);
        }
    }
}
