using OpenTK.Mathematics;
using Engine;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Effects
{
    public class BlockDestructionTexture
    {
        private static Texture2D _cachedEmptyTexture;

        public static Texture2D Generate(byte xCoord, byte yCoord)
        {
            Texture2D tempBlockTexture = UVAtlas.GetBlockTexture(GameAssets.BlocksTexture, xCoord, yCoord, GameAssets.AtlasBlocks.SizeBlocks);
            Texture2D result = Generate(tempBlockTexture);
            tempBlockTexture?.Dispose();
            return result;
        }

        public static Texture2D Generate(Vector2 coords)
        {
            Texture2D tempBlockTexture = UVAtlas.GetBlockTexture(GameAssets.BlocksTexture, (byte)coords.X, (byte)coords.Y, GameAssets.AtlasBlocks.SizeBlocks);
            Texture2D result = Generate(tempBlockTexture);
            tempBlockTexture?.Dispose();
            return result;
        }

        public static Texture2D Generate(Vector2Byte coords)
        {
            Texture2D tempBlockTexture = UVAtlas.GetBlockTexture(GameAssets.BlocksTexture, coords.X, coords.Y, GameAssets.AtlasBlocks.SizeBlocks);
            Texture2D result = Generate(tempBlockTexture);
            tempBlockTexture?.Dispose();
            return result;
        }

        public static Texture2D Generate(Texture2D blockTexture)
        {
            if (blockTexture == null)
            {
                Debug.Error($"[BlockDestructionTexture] Error: blockTexture was null!");
                return GetEmptyTexture();
            }

            Texture2D pattern = Engine.Resources.Get<Texture2D>("Resources/Textures/Effects/dust.png");

            if (pattern == null)
            {
                Debug.Error($"[BlockDestructionTexture] Error: Pattern == null");
                return GetEmptyTexture();
            }

            if (pattern.Width != pattern.Height || blockTexture.Width != blockTexture.Height)
            {
                Debug.Error($"[BlockDestructionTexture] Error: Texture sizes are invalid (must be square)");
                return GetEmptyTexture();
            }

            int patternSize = pattern.Width;
            int blockSize = blockTexture.Width;

            if (patternSize > blockSize)
            {
                Debug.Error($"[BlockDestructionTexture] Error: Pattern size is bigger than block texture");
                return GetEmptyTexture();
            }

            Color4[,] patternPixels = pattern.GetPixels();
            Color4[,] blockPixels = blockTexture.GetPixels();

            int delta = blockSize / patternSize;

            Color4[,] newPixels = new Color4[patternSize, patternSize];

            for (int x = 0; x < patternSize; x++)
            {
                for (int y = 0; y < patternSize; y++)
                {
                    if (patternPixels[x, y].A == 0)
                    {
                        newPixels[x, y] = new Color4(0, 0, 0, 0);
                        continue;
                    }

                    int bx = x * delta;
                    int by = y * delta;

                    if (bx < blockSize && by < blockSize)
                    {
                        newPixels[x, y] = blockPixels[bx, by];
                    }
                    else
                    {
                        newPixels[x, y] = new Color4(0, 0, 0, 0);
                    }
                }
            }

            Texture2D finalTexture = new Texture2D(patternSize, patternSize);
            finalTexture.SetPixelsData(newPixels);
            finalTexture.FilterMode = FilterMode.Nearest;

            return finalTexture;
        }

        private static Texture2D GetEmptyTexture()
        {
            if (_cachedEmptyTexture == null)
            {
                _cachedEmptyTexture = new Texture2D(8, 8, true);
                Resources.AddResource<Texture2D>("_cachedEmptyTexture",_cachedEmptyTexture, true);
            }
            return _cachedEmptyTexture;
        }
    }
}