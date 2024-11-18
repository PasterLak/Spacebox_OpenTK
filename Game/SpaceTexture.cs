using OpenTK.Mathematics;
using Spacebox.Common;

namespace Spacebox.Game
{
    public class SpaceTexture : Texture2D
    {
        public SpaceTexture(int width, int height)
            : base(width, height)
        {
            SetPixelated(true);
            GenerateSpaceTexture();
            UpdateTexture(true);
        }

        private void GenerateSpaceTexture()
        {
            Random random = new Random();

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    // Генерируем случайное число от 0 до 9999
                    int rand = random.Next(10000);

                    // С вероятностью 0.05% ставим белую точку (звезду)
                    if (rand < 10)
                    {
                        byte brightness = (byte)random.Next(1, 256); // Яркость звезды
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
