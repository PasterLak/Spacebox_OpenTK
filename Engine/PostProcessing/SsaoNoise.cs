using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.PostProcessing
{

    public static class SsaoNoise
    {
        public static Texture2D GenerateRotationNoise(int size = 4, int seed = 1337)
        {
            var rng = new Random(seed);
            var bytes = new byte[size * size * 4];
            int i = 0;

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    double ang = rng.NextDouble() * Math.PI * 2.0;
                    float rx = (float)Math.Cos(ang); // [-1..1]
                    float ry = (float)Math.Sin(ang); // [-1..1]

                    byte r = (byte)((rx * 0.5f + 0.5f) * 255.0f); // -> [0..255]
                    byte g = (byte)((ry * 0.5f + 0.5f) * 255.0f);
                    byte b = 128;  // z = 0.5
                    byte a = 255;

                    bytes[i++] = r;
                    bytes[i++] = g;
                    bytes[i++] = b;
                    bytes[i++] = a;
                }

            var tex = new Texture2D(new PixelData { Width = size, Height = size, Data = bytes }, filterMode: FilterMode.Nearest);

            tex.Use();
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 0); 
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);


            return tex;
        }
    }
}
