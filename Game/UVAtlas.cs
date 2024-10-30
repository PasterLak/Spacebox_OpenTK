using OpenTK.Mathematics;

namespace Spacebox.Game
{
    public static class UVAtlas
    {
        private static int _textureAtlasSize = 16; // Количество блоков по ширине и высоте в атласе

        /// <summary>
        /// Получает UV-координаты текстуры по координатам блока на атласе.
        /// </summary>
        /// <param name="x">Координата X блока на атласе (по горизонтали).</param>
        /// <param name="y">Координата Y блока на атласе (по вертикали).</param>
        /// <returns>Массив UV-координат для 4 вершин квадрата.</returns>
        public static Vector2[] GetUVs(int x, int y)
        {
            float unit = 1.0f / _textureAtlasSize;

            // Проверка на выход за пределы атласа
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
    }
}
