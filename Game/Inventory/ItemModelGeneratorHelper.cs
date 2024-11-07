using OpenTK.Mathematics;
using Spacebox.Common;

namespace Spacebox.Game
{
    public static class ItemModelGeneratorHelper
    {
        private const int CellSize = 32; // Размер одной ячейки в UV атласе (32x32 пикселя)

        public static ItemModel GenerateModel(Texture2D atlasTexture, int cellX, int cellY, float modelSize = 1.0f, float modelDepth = 0.2f)
        {
            // Извлечение конкретной ячейки текстуры из атласа
            Texture2D cellTexture = UVAtlas.GetBlockTexture(atlasTexture, cellX, cellY);

            // Получение данных пикселей
            Color4[,] pixels = cellTexture.GetPixelData();
            int width = cellTexture.Width;
            int height = cellTexture.Height;

            // Выполнение greedy meshing на текстуре ячейки
            var quads = GreedyMesh(pixels, width, height);

            // Создание карты покрытия пикселей для быстрого поиска соседей
            bool[,] coverage = new bool[width, height];
            foreach (var quad in quads)
            {
                for (int dy = 0; dy < quad.Height; dy++)
                {
                    for (int dx = 0; dx < quad.Width; dx++)
                    {
                        coverage[quad.X + dx, quad.Y + dy] = true;
                    }
                }
            }

            // Генерация данных вершин и индексов
            List<float> vertices = new List<float>();
            List<uint> indices = new List<uint>();
            uint indexOffset = 0;

            foreach (var quad in quads)
            {
                // Определение позиций вершин квадрата
                Vector3 frontBottomLeft = new Vector3(quad.X * modelSize, quad.Y * modelSize, 0);
                Vector3 frontBottomRight = new Vector3((quad.X + quad.Width) * modelSize, quad.Y * modelSize, 0);
                Vector3 frontTopRight = new Vector3((quad.X + quad.Width) * modelSize, (quad.Y + quad.Height) * modelSize, 0);
                Vector3 frontTopLeft = new Vector3(quad.X * modelSize, (quad.Y + quad.Height) * modelSize, 0);

                Vector3 backBottomLeft = frontBottomLeft + new Vector3(0, 0, modelDepth);
                Vector3 backBottomRight = frontBottomRight + new Vector3(0, 0, modelDepth);
                Vector3 backTopRight = frontTopRight + new Vector3(0, 0, modelDepth);
                Vector3 backTopLeft = frontTopLeft + new Vector3(0, 0, modelDepth);

                // Нормали
                Vector3 normalFront = Vector3.UnitZ;
                Vector3 normalBack = -Vector3.UnitZ;
                Vector3 normalLeft = -Vector3.UnitX;
                Vector3 normalRight = Vector3.UnitX;
                Vector3 normalTop = Vector3.UnitY;
                Vector3 normalBottom = -Vector3.UnitY;

                // UV координаты для фронтальной и задней граней
                Vector2 uv1 = new Vector2(quad.U, quad.V);
                Vector2 uv2 = new Vector2(quad.U + quad.UWidth, quad.V);
                Vector2 uv3 = new Vector2(quad.U + quad.UWidth, quad.V + quad.UHeight);
                Vector2 uv4 = new Vector2(quad.U, quad.V + quad.UHeight);

                // Добавление фронтальной грани
                AddFace(vertices, indices, indexOffset, frontTopLeft, 
                    frontTopRight, frontBottomRight, frontBottomLeft, normalFront, uv1, uv2, uv3, uv4);
                indexOffset += 4;

                // Добавление задней грани
                AddFace(vertices, indices, indexOffset, backTopLeft, backBottomLeft, backBottomRight, backTopRight, normalBack, uv1, uv2, uv3, uv4);
                indexOffset += 4;

                // Проверка и добавление боковых граней
                // 1. Левая грань
                if (quad.NeedsLeftSide)
                {
                    AddSideFace(vertices, indices, indexOffset, backTopLeft, backBottomLeft, frontBottomLeft, frontTopLeft, normalLeft, uv1, uv2, uv3, uv4);
                    indexOffset += 4;
                }

                // 2. Правая грань
                if (quad.NeedsRightSide)
                {
                    AddSideFace(vertices, indices, indexOffset, backBottomRight, backTopRight, frontTopRight, frontBottomRight, normalRight, uv1, uv2, uv3, uv4);
                    indexOffset += 4;
                }

                // 3. Верхняя грань (можно добавить аналогично боковым граням, если требуется)
                if (quad.NeedsTopSide)
                {
                    AddTopOrBottomFace(vertices, indices, indexOffset, backTopLeft, backTopRight, frontTopRight, frontTopLeft, normalTop, uv1, uv2, uv3, uv4);
                    indexOffset += 4;
                }

                // 4. Нижняя грань (можно добавить аналогично боковым граням, если требуется)
                if (quad.NeedsBottomSide)
                {
                    AddTopOrBottomFace(vertices, indices, indexOffset, backBottomLeft, backBottomRight, frontBottomRight, frontBottomLeft, normalBottom, uv1, uv2, uv3, uv4);
                    indexOffset += 4;
                }
            }

            // Создание Mesh
            float[] vertexArray = vertices.ToArray();
            uint[] indexArray = indices.ToArray();
            Mesh mesh = new Mesh(vertexArray, indexArray);

            return new ItemModel(mesh, cellTexture);
        }

        // position(3) + uv(2) + color(3)
        public static void AddFace(List<float> vertices, List<uint> indices, uint indexOffset,
                            Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
                            Vector3 color, // Используем цвет вместо нормали
                            Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
        {
            vertices.AddRange(new float[]
            {
        v1.X, v1.Y, v1.Z, uv1.X, uv1.Y, color.X, color.Y, color.Z,
        v2.X, v2.Y, v2.Z, uv2.X, uv2.Y, color.X, color.Y, color.Z,
        v3.X, v3.Y, v3.Z, uv3.X, uv3.Y, color.X, color.Y, color.Z,
        v4.X, v4.Y, v4.Z, uv4.X, uv4.Y, color.X, color.Y, color.Z
            });

           
            indices.AddRange(new uint[]
            {
        indexOffset, indexOffset + 1, indexOffset + 2,
        indexOffset, indexOffset + 2, indexOffset + 3
            });
        }


        private static void AddSideFace(List<float> vertices, List<uint> indices, uint indexOffset,
                                        Vector3 backTop, Vector3 backBottom, Vector3 frontBottom, Vector3 frontTop,
                                        Vector3 normal,
                                        Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
        {
            // Для боковых граней можно использовать отдельные UV координаты или повторять текстуру
            // Здесь используется повторение текстуры по высоте грани
            // Можно изменить UV по своему усмотрению

            // Расчет высоты грани для UV
            float height = (frontTop - frontBottom).Length;
            Vector2 uvScale = new Vector2(1.0f, height);

            vertices.AddRange(new float[]
            {
                backTop.X, backTop.Y, backTop.Z, normal.X, normal.Y, normal.Z, uv1.X, uv1.Y,
                backBottom.X, backBottom.Y, backBottom.Z, normal.X, normal.Y, normal.Z, uv2.X, uv2.Y,
                frontBottom.X, frontBottom.Y, frontBottom.Z, normal.X, normal.Y, normal.Z, uv3.X, uv3.Y,
                frontTop.X, frontTop.Y, frontTop.Z, normal.X, normal.Y, normal.Z, uv4.X, uv4.Y
            });

            // Индексы для боковой грани
            indices.AddRange(new uint[]
            {
                indexOffset, indexOffset + 1, indexOffset + 2,
                indexOffset, indexOffset + 2, indexOffset + 3
            });
        }

        private static void AddTopOrBottomFace(List<float> vertices, List<uint> indices, uint indexOffset,
                                              Vector3 back1, Vector3 back2, Vector3 front2, Vector3 front1,
                                              Vector3 normal,
                                              Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
        {
            vertices.AddRange(new float[]
            {
                back1.X, back1.Y, back1.Z, normal.X, normal.Y, normal.Z, uv1.X, uv1.Y,
                back2.X, back2.Y, back2.Z, normal.X, normal.Y, normal.Z, uv2.X, uv2.Y,
                front2.X, front2.Y, front2.Z, normal.X, normal.Y, normal.Z, uv3.X, uv3.Y,
                front1.X, front1.Y, front1.Z, normal.X, normal.Y, normal.Z, uv4.X, uv4.Y
            });

            // Индексы для верхней или нижней грани
            indices.AddRange(new uint[]
            {
                indexOffset, indexOffset + 1, indexOffset + 2,
                indexOffset, indexOffset + 2, indexOffset + 3
            });
        }

        public class Quad
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public float U { get; set; }
            public float V { get; set; }
            public float UWidth { get; set; }
            public float UHeight { get; set; }
            public Color4 Color { get; set; }

            // Свойства для определения необходимости боковых граней
            public bool NeedsLeftSide { get; set; }
            public bool NeedsRightSide { get; set; }
            public bool NeedsTopSide { get; set; }
            public bool NeedsBottomSide { get; set; }
        }

        public static List<Quad> GreedyMesh(Color4[,] pixels, int width, int height)
        {
            bool[,] processed = new bool[width, height];
            List<Quad> quads = new List<Quad>();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (processed[x, y])
                        continue;

                    Color4 currentColor = pixels[x, y];
                    if (currentColor.A < 1) 
                    {
                        processed[x, y] = true;
                        continue;
                    }

                    // Определение ширины квадрата
                    int quadWidth = 1;
                    while (x + quadWidth < width && !processed[x + quadWidth, y] && pixels[x + quadWidth, y].Equals(currentColor))
                    {
                        quadWidth++;
                    }

                    // Определение высоты квадрата
                    int quadHeight = 1;
                    bool done = false;
                    while (y + quadHeight < height)
                    {
                        for (int i = 0; i < quadWidth; i++)
                        {
                            if (processed[x + i, y + quadHeight] || !pixels[x + i, y + quadHeight].Equals(currentColor))
                            {
                                done = true;
                                break;
                            }
                        }
                        if (done)
                            break;
                        quadHeight++;
                    }

                    // Пометка области квадрата как обработанной
                    for (int dy = 0; dy < quadHeight; dy++)
                    {
                        for (int dx = 0; dx < quadWidth; dx++)
                        {
                            processed[x + dx, y + dy] = true;
                        }
                    }

                    // Расчёт UV координат
                    float u = (float)x / width;
                    float v = (float)y / height;
                    float uWidth = (float)quadWidth / width;
                    float uHeight = (float)quadHeight / height;

                    // Определение необходимости боковых граней
                    bool needsLeft = false, needsRight = false, needsTop = false, needsBottom = false;

                    // Проверка левой стороны
                    if (x == 0)
                    {
                        needsLeft = true;
                    }
                    else
                    {
                        for (int dy = 0; dy < quadHeight; dy++)
                        {
                            if (pixels[x - 1, y + dy].A < 1)
                            {
                                needsLeft = true;
                                break;
                            }
                        }
                    }

                    // Проверка правой стороны
                    if (x + quadWidth >= width)
                    {
                        needsRight = true;
                    }
                    else
                    {
                        for (int dy = 0; dy < quadHeight; dy++)
                        {
                            if (pixels[x + quadWidth, y + dy].A < 1)
                            {
                                needsRight = true;
                                break;
                            }
                        }
                    }

                    // Проверка верхней стороны
                    if (y == 0)
                    {
                        needsTop = true;
                    }
                    else
                    {
                        for (int dx = 0; dx < quadWidth; dx++)
                        {
                            if (pixels[x + dx, y - 1].A < 1)
                            {
                                needsTop = true;
                                break;
                            }
                        }
                    }

                    // Проверка нижней стороны
                    if (y + quadHeight >= height)
                    {
                        needsBottom = true;
                    }
                    else
                    {
                        for (int dx = 0; dx < quadWidth; dx++)
                        {
                            if (pixels[x + dx, y + quadHeight].A < 1)
                            {
                                needsBottom = true;
                                break;
                            }
                        }
                    }

                    quads.Add(new Quad
                    {
                        X = x,
                        Y = y,
                        Width = quadWidth,
                        Height = quadHeight,
                        U = u,
                        V = v,
                        UWidth = uWidth,
                        UHeight = uHeight,
                        Color = currentColor,
                        NeedsLeftSide = needsLeft,
                        NeedsRightSide = needsRight,
                        NeedsTopSide = needsTop,
                        NeedsBottomSide = needsBottom
                    });
                }
            }

            return quads;
        }
    }
}
