using OpenTK.Mathematics;
using Spacebox.Common; // Добавьте этот using для использования Texture2D

namespace Spacebox.Game
{
    public static class UVAtlas
    {
        private static int _textureAtlasSize = 16;

        public static Vector2[] GetUVs(int x, int y)
        {
            float unit = 1.0f / _textureAtlasSize;

            if (x < 0 || x >= _textureAtlasSize || y < 0 || y >= _textureAtlasSize)
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

        public static int GetAtlasSize()
        {
            return _textureAtlasSize;
        }

        /// <summary>
        /// Получает текстуру выбранного блока из атласа текстур.
        /// </summary>
        /// <param name="atlasTexture">Текстура атласа блоков.</param>
        /// <param name="x">Координата X блока в атласе.</param>
        /// <param name="y">Координата Y блока в атласе.</param>
        /// <returns>Новая текстура выбранного блока.</returns>
        public static Texture2D GetBlockTexture(Texture2D atlasTexture, int x, int y)
        {
            int atlasSize = _textureAtlasSize; // Количество блоков по одной стороне атласа

            // Проверяем, что координаты блока находятся в пределах атласа
            if (x < 0 || x >= atlasSize || y < 0 || y >= atlasSize)
            {
                throw new ArgumentOutOfRangeException("Coords outside the atlas!");
            }

            // Вычисляем размер каждого блока в пикселях
            int blockWidth = atlasTexture.Width / atlasSize;
            int blockHeight = atlasTexture.Height / atlasSize;

            // Начальные пиксельные координаты блока в атласе
            int startX = x * blockWidth;
            int startY = y * blockHeight;

            // Создаем новую текстуру для блока
            Texture2D blockTexture = new Texture2D(blockWidth, blockHeight, true);

            // Копируем пиксели из атласа в новую текстуру
            for (int i = 0; i < blockHeight; i++)
            {
                for (int j = 0; j < blockWidth; j++)
                {
                    // Получаем пиксель из атласа
                    Color4 color = atlasTexture.GetPixel(startX + j, startY + i);
                    // Устанавливаем пиксель в текстуру блока
                    blockTexture.SetPixel(j, i, color);
                }
            }

            // Обновляем текстуру, чтобы загрузить ее в OpenGL
            blockTexture.UpdateTexture();

            return blockTexture;
        }
    }
}

