using OpenTK.Mathematics;
using Spacebox.Common;
using System;

namespace Spacebox.GUI
{
    public static class IsometricIcon
    {
        private const float ShadowIntensity = 0.1f;
        private const float LightIntensity = 0.03f;

        public static Texture2D CreateIsometricIcon(Texture2D block)
        {
            if (block.Width != block.Height)
            {
                Console.WriteLine("Invalid Texture Width and Height! Should be the same.");
                return new Texture2D(32, 32, pixelated: true);
            }

            int originalSize = block.Width;
            int size = originalSize * 2;
            Texture2D isometricTexture = new Texture2D(size, size, pixelated: true);

            Color4[,] originalPixels = block.GetPixelData();
            Color4[,] isometricPixels = InitializePixels(size);

            ApplyRightSide(size, originalSize, originalPixels, isometricPixels, ShadowIntensity);
            ApplyLeftSide(size, originalSize, originalPixels, isometricPixels, ShadowIntensity);
            ApplyTop(size, originalSize, originalPixels, isometricPixels, LightIntensity);

            isometricTexture.SetPixelsData(isometricPixels);
            isometricTexture.UpdateTexture();

            return isometricTexture;
        }

        private static Color4[,] InitializePixels(int size)
        {
            Color4[,] pixels = new Color4[size, size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[x, y] = new Color4(0, 0, 0, 0);
            return pixels;
        }

        private static void ApplyRightSide(int size, int originalSize, Color4[,] original, Color4[,] isometric, float intensity)
        {
            int halfSize = size / 2;
            int startY = 0;

            for (int x = 0; x < halfSize; x++)
            {
                for (int y = 0; y < halfSize; y++)
                {
                    int newX = halfSize + x;
                    int newY = startY + y;

                    if (IsWithinBounds(newX, newY, size))
                    {
                        Color4 color = original[x, y];
                        color = ModifyColor(color, intensity);
                        isometric[newX, newY] = color;
                    }
                }

                if (x % 2 == 0) startY++;
            }
        }

        private static void ApplyLeftSide(int size, int originalSize, Color4[,] original, Color4[,] isometric, float shadowIntensity)
        {
            int halfSize = size / 2;
            int startY = size / 4;

            for (int x = 0; x < halfSize; x++)
            {
                for (int y = 0; y < halfSize; y++)
                {
                    int newX = x;
                    int newY = startY + y;

                    if (IsWithinBounds(newX, newY, size))
                    {
                        Color4 color = original[x, y];
                        color = DarkenColor(color, 0.05f);
                        isometric[newX, newY] = color;
                    }
                }

                if (x % 2 == 0) startY--;
            }
        }

        private static void ApplyTop(int size, int originalSize, Color4[,] original, Color4[,] isometric, float lightIntensity)
        {
            int halfSize = size / 2;
            int startX = 0;
            int startY = size - size / 4;

            for (int y = 0; y < halfSize; y++)
            {
                int currentStartY = startY;

                for (int x = 0; x < halfSize; x++)
                {
                    int newX = startX + x;
                    int newY = currentStartY;

                    if (IsWithinBounds(newX, newY, size))
                    {
                        Color4 color = original[x, y];
                        color = LightenColor(color, lightIntensity);
                        isometric[newX, newY] = color;
                    }

                    if (x % 2 == 0) currentStartY++;
                }

                for (int x = 0; x < halfSize - 1; x++)
                {
                    int newX = startX + x + 1;
                    int newY = currentStartY;

                    if (IsWithinBounds(newX, newY, size))
                    {
                        Color4 color = original[x, y];
                        color = LightenColor(color, 0.1f);
                        isometric[newX, newY] = color;
                    }

                    if (x % 2 == 0) currentStartY++;
                }

                startY = currentStartY;

                if (y != 0 && y % 2 == 0)
                    startY++;
                startX++;
            }
        }

        private static bool IsWithinBounds(int x, int y, int size)
        {
            return x >= 0 && y >= 0 && x < size && y < size;
        }

        private static Color4 ModifyColor(Color4 color, float intensity)
        {
            return new Color4(
                MathHelper.Clamp(color.R + intensity, 0f, 1f),
                MathHelper.Clamp(color.G + intensity, 0f, 1f),
                MathHelper.Clamp(color.B + intensity, 0f, 1f),
                color.A
            );
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
