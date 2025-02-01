using OpenTK.Mathematics;
using Engine;

namespace Spacebox.Game.Resources
{
    public static class UVAtlas
    {

        public static Vector2[] GetUVs(Vector2 v, int sideInBlocks)
        {
            return GetUVs((int)v.X, (int)v.Y, sideInBlocks);
        }

        public static Vector2[] GetUVs(Vector2Byte v, int sideInBlocks)
        {
            return GetUVs(v.X, v.Y, sideInBlocks);
        }
        public static Vector2[] GetUVs(int x, int y, int sideInBlocks)
        {
            float unit = 1.0f / sideInBlocks;

            if (x < 0 || x >= sideInBlocks || y < 0 || y >= sideInBlocks)
            {
                x = 0;
                y = 0;
            }

            float u = x * unit;
            float v = y * unit;

            return new Vector2[]
            {
                new Vector2(u, v),
                new Vector2(u + unit, v),
                new Vector2(u + unit, v + unit),
                new Vector2(u, v + unit)
            };
        }



        public static Texture2D GetBlockTexture(Texture2D atlasTexture, Vector2Byte v, int sideInBlocks)
        {
            return GetBlockTexture(atlasTexture, v.X, v.Y, sideInBlocks);
        }
        public static Texture2D GetBlockTexture(Texture2D atlasTexture, int x, int y, int sideInBlocks)
        {
            int atlasSize = sideInBlocks;


            if (x < 0 || x >= atlasSize || y < 0 || y >= atlasSize)
            {
                throw new ArgumentOutOfRangeException("Coords outside the atlas!");
            }


            int blockWidth = atlasTexture.Width / atlasSize;
            int blockHeight = atlasTexture.Height / atlasSize;


            int startX = x * blockWidth;
            int startY = y * blockHeight;


            Texture2D blockTexture = new Texture2D(blockWidth, blockHeight, true);


            for (int i = 0; i < blockHeight; i++)
            {
                for (int j = 0; j < blockWidth; j++)
                {

                    Color4 color = atlasTexture.GetPixel(startX + j, startY + i);

                    blockTexture.SetPixel(j, i, color);
                }
            }


            blockTexture.UpdateTexture();

            return blockTexture;
        }
    }
}

