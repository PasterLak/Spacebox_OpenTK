using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.Game.Resources;

namespace Spacebox.Game.Effects
{
    public class BlockDestructionTexture
    {

        public static Texture2D Generate(byte xCoord, byte yCoord)
        {
            return Generate(UVAtlas.GetBlockTexture(GameBlocks.BlocksTexture, xCoord, yCoord, GameBlocks.AtlasBlocks.SizeBlocks));
        }
        public static Texture2D Generate(Vector2 coords)
        {
            return Generate(UVAtlas.GetBlockTexture(GameBlocks.BlocksTexture, (byte)coords.X, (byte)coords.Y, GameBlocks.AtlasBlocks.SizeBlocks));
        }

        public static Texture2D Generate(Vector2Byte coords)
        {
            return Generate(UVAtlas.GetBlockTexture(GameBlocks.BlocksTexture, coords.X, coords.Y, GameBlocks.AtlasBlocks.SizeBlocks));
        }

        public static Texture2D Generate(Texture2D blockTexture)
        {

            if (blockTexture == null)
            {
                Debug.Error($"[BlockDestructionTexture] Error: blockTexture was null!");
                return CreateEmptyTexture();
            }

            Texture2D pattern = TextureManager.GetTexture("Resources/Textures/dust.png");

            if (pattern == null)
            {
                Debug.Error($"[BlockDestructionTexture] Error: Pattern == null");
                return CreateEmptyTexture();
            }

            if (pattern.Width != pattern.Height)
            {
                Debug.Error($"[BlockDestructionTexture] Error: Pattern Width != Height");
                return CreateEmptyTexture();
            }

            if (blockTexture.Width != blockTexture.Height)
            {
                Debug.Error($"[BlockDestructionTexture] Error: BlockTexture Width != Height");
                return CreateEmptyTexture();
            }


            Color4[,] pixels = pattern.GetPixelData();

            var patternSize = pattern.Width;
            var blockSize = blockTexture.Width;

            if (patternSize > blockSize)
            {
                Debug.Error($"[BlockDestructionTexture] Error: Pattern size is bigger than block texture");
                return CreateEmptyTexture();
            }

            var delta = blockSize / patternSize;

            Color4[,] newPixels = new Color4[patternSize, patternSize];

            for (int x = 0; x < patternSize; x++)
            {
                for (int y = 0; y < patternSize; y++)
                {
                    if (pixels[x, y].A == 0)
                    {
                        newPixels[x, y] = new Color4(0, 0, 0, 0);
                        continue;
                    }

                    newPixels[x, y] = blockTexture.GetPixel(x * delta, y * delta);

                }
            }

            Texture2D finalTexture = new Texture2D(patternSize, patternSize);

            finalTexture.SetPixelsData(newPixels);
            finalTexture.UpdateTexture(true);



            return finalTexture;
        }

        private static Texture2D CreateEmptyTexture()
        {
            return new Texture2D(8, 8, true);
        }
    }
}
