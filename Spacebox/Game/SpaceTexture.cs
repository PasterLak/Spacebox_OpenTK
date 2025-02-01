using OpenTK.Mathematics;
using Engine;
using Engine.Utils;

namespace Spacebox.Game
{
    public class SpaceTexture : Texture2D
    {
        public SpaceTexture(int width, int height, Random random)
            : base(width, height)
        {
            SetPixelated(true);
            GenerateSpaceTexture(random);
            UpdateTexture(true);
        }

        private void GenerateSpaceTexture(Random random)
        {
            //FastRandom r = new FastRandom();
            
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int rand = random.Next(10000);

                    if (rand < 10)
                    {
                        byte brightness = (byte)random.Next(1, 256);
                        SetPixel(x, y, new Color4(brightness / 255f, brightness / 255f, brightness / 255f, 1f));
                    }
                    else
                    {
                        SetPixel(x, y, Color4.Black);
                    }
                }
            }
        }
    }
}