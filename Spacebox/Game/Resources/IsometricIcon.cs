using OpenTK.Mathematics;
using Spacebox.Engine;

namespace Spacebox.GUI
{
    public static class IsometricIcon
    {
        private const float ShadowIntensityLeftSide = 0.1f; // 0.1
        private const float ShadowIntensityRightSide = 0; // 0.1
        private const float LightIntensity = 0.1f; // 0.03, left side

       
        public static Texture2D CreateIsometricIcon(Texture2D walls,  Texture2D topSide)
        {
            if (!ValidateTextures(walls,topSide))
            {
                return new Texture2D(32, 32, pixelated: true);
            }

            int originalSize = walls.Width;
            int size = originalSize * 2;
            Texture2D isometricTexture = new Texture2D(size, size, pixelated: true);

            Color4[,] origLeftPixels = walls.GetPixelData();
          
            Color4[,] origTopPixels = topSide.GetPixelData();

            Color4[,] isometricPixels = InitializePixels(size);

            ApplyRightSide(size, origLeftPixels, isometricPixels, ShadowIntensityRightSide);
            ApplyLeftSide(size, origLeftPixels, isometricPixels, ShadowIntensityLeftSide);
            ApplyTop(size, origTopPixels, isometricPixels, LightIntensity);

         
            isometricPixels = ImageProcessing.MirrorX(isometricPixels);

            //isometricPixels = ImageProcessing.MirrorX(isometricPixels);

            isometricTexture.SetPixelsData(isometricPixels);
            isometricTexture.UpdateTexture();


           
            walls.Dispose();

            return isometricTexture;
        }

        private static bool ValidateTextures(Texture2D leftSide, Texture2D topSide)
        {
            if (leftSide.Width != leftSide.Height)
            {
                Debug.Log("Invalid Texture Width and Height! Should be the same.");
                return false;
            }
           
            if (topSide.Width != topSide.Height)
            {
                Debug.Log("Invalid Texture Width and Height! Should be the same.");
                return false;
            }

            if (leftSide.Width != topSide.Width)
            {
                Debug.Log("[IsometricIcon] All sides must have the same size!");
                return false;
            }

            return true;
        }

        private static Color4[,] InitializePixels(int size)
        {
            Color4[,] pixels = new Color4[size, size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[x, y] = new Color4(0, 0, 0, 0);
            return pixels;
        }

        private static void ApplyRightSide(int size,Color4[,] original, Color4[,] isometric, float intensity)
        {
            int halfSize = size / 2;
            int startY = 0;

            for (int x = 0; x < halfSize; x++)
            {
                for (int y = 0; y < halfSize; y++)
                {
                    int newX = halfSize + x;
                    int newY = startY + y;

                    
                        Color4 color = original[x, y];
                        color = LightenColor(color, intensity + 0.001f * x);
                        isometric[newX, newY] = color;
                    
                }

                if (x % 2 == 0) startY++;
            }
        }

        private static void ApplyLeftSide(int size,  Color4[,] original, Color4[,] isometric, float shadowIntensity)
        {
            int halfSize = size / 2;
            int startX = 0;
            int startY = size / 4;

            for (int x = 0; x < halfSize; x++)
            {
                for (int y = 0; y < halfSize; y++)
                {
                    int newX = startX + x;
                    int newY = startY + y;

                    
                        Color4 color = original[x, y];
                        color = DarkenColor(color, shadowIntensity + 0.001f * x);

                        if(x == halfSize-1) color = DarkenColor(color, shadowIntensity);
                        isometric[newX, newY] = color;
                    
                }

                if (x % 2 == 0) startY--;
            }
        }

        private static void ApplyTop(int size, Color4[,] original, Color4[,] isometric, float lightIntensity)
        {
            int halfSize = size / 2;

            int startX = 0;
            int startY = size - size / 4;

            for (int y = 0; y < halfSize; y++)
            {
                int s = startY;

                for (int x = 0; x < halfSize; x++)
                {
                    int newX = startX + x;
                    int newY = startY;

                        Color4 color = original[x, y];
                        color = LightenColor(color, lightIntensity);
                        isometric[newX, newY] = color;
                    

                    if (x % 2 == 0) startY--;
                }

                startY = s;

                for (int x = 0; x < halfSize - 1; x++)
                {
                    int newX = startX + x + 1;
                    int newY = startY;

                   
                        Color4 color = original[x, y];
                        color = LightenColor(color, 0.1f);
                        isometric[newX, newY] = color;
                    

                    if (x % 2 == 0) startY--;
                }

                startY = s;

                if (y != 0 && y % 2 == 0)
                    startY++;
                startX++;
            }
        }

        private static Color4 LightenColor(Color4 color, float intensity)
        {
            return new Color4(
                MathHelper.Clamp(color.R + intensity, 0f, 1f),
                MathHelper.Clamp(color.G + intensity, 0f, 1f),
                MathHelper.Clamp(color.B + intensity, 0f, 1f),
                color.A
            );
        }

        private static Color4 DarkenColor(Color4 color, float amount)
        {
            return new Color4(
                MathHelper.Clamp(color.R - amount, 0f, 1f),
                MathHelper.Clamp(color.G - amount, 0f, 1f),
                MathHelper.Clamp(color.B - amount, 0f, 1f),
                color.A
            );
        }
    }
}
