using OpenTK.Mathematics;
using Engine;

namespace Spacebox.Game
{
    public static class ItemModelGeneratorHelper
    {
        // position(3) + normal(3) + uv(2)
        public static void AddFace(List<float> vertices, List<uint> indices, uint indexOffset,
                                   Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
                                   Vector3 normal,
                                   Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
        {
            vertices.AddRange(new float[]
            {
                v1.X, v1.Y, v1.Z, normal.X, normal.Y, normal.Z, uv1.X, uv1.Y,
                v2.X, v2.Y, v2.Z, normal.X, normal.Y, normal.Z, uv2.X, uv2.Y,
                v3.X, v3.Y, v3.Z, normal.X, normal.Y, normal.Z, uv3.X, uv3.Y,
                v4.X, v4.Y, v4.Z, normal.X, normal.Y, normal.Z, uv4.X, uv4.Y
            });

            indices.AddRange(new uint[]
            {
                indexOffset, indexOffset + 1, indexOffset + 2,
                indexOffset, indexOffset + 2, indexOffset + 3
            });
        }

        public static void AddFaceBack(List<float> vertices, List<uint> indices, uint indexOffset,
                                       Quad quad, float depth, float modelSize,
                                       Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
        {
            Vector3 v1 = new Vector3(quad.X, quad.Y, 0) * modelSize;
            Vector3 v2 = new Vector3(quad.X, quad.Y, 0) * modelSize;
            Vector3 v3 = new Vector3(quad.X, quad.Y + quad.Height, 0) * modelSize;
            Vector3 v4 = new Vector3(quad.X, quad.Y + quad.Height, 0) * modelSize;

            v2 += new Vector3(0, 0, depth);
            v3 += new Vector3(0, 0, depth);

            Vector3 normal = new Vector3(-1, 0, 0);

            vertices.AddRange(new float[]
            {
                v1.X, v1.Y, v1.Z, normal.X, normal.Y, normal.Z, uv1.X, uv1.Y,
                v2.X, v2.Y, v2.Z, normal.X, normal.Y, normal.Z, uv2.X, uv2.Y,
                v3.X, v3.Y, v3.Z, normal.X, normal.Y, normal.Z, uv3.X, uv3.Y,
                v4.X, v4.Y, v4.Z, normal.X, normal.Y, normal.Z, uv4.X, uv4.Y
            });

            indices.AddRange(new uint[]
            {
                indexOffset, indexOffset + 1, indexOffset + 2,
                indexOffset, indexOffset + 2, indexOffset + 3
            });
        }

        public static void AddFaceForward(List<float> vertices, List<uint> indices, uint indexOffset,
                                          Quad quad, float depth, float modelSize,
                                          Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
        {
            Vector3 v1 = new Vector3(quad.X + quad.Width, quad.Y, 0) * modelSize;
            Vector3 v4 = new Vector3(quad.X + quad.Width, quad.Y, 0) * modelSize;
            Vector3 v3 = new Vector3(quad.X + quad.Width, quad.Y + quad.Height, 0) * modelSize;
            Vector3 v2 = new Vector3(quad.X + quad.Width, quad.Y + quad.Height, 0) * modelSize;

            v3 += new Vector3(0, 0, depth);
            v4 += new Vector3(0, 0, depth);

            Vector3 normal = new Vector3(1, 0, 0);

            vertices.AddRange(new float[]
            {
                v1.X, v1.Y, v1.Z, normal.X, normal.Y, normal.Z, uv1.X, uv1.Y,
                v2.X, v2.Y, v2.Z, normal.X, normal.Y, normal.Z, uv2.X, uv2.Y,
                v3.X, v3.Y, v3.Z, normal.X, normal.Y, normal.Z, uv3.X, uv3.Y,
                v4.X, v4.Y, v4.Z, normal.X, normal.Y, normal.Z, uv4.X, uv4.Y
            });

            indices.AddRange(new uint[]
            {
                indexOffset, indexOffset + 1, indexOffset + 2,
                indexOffset, indexOffset + 2, indexOffset + 3
            });
        }

        public static void AddFaceTop(List<float> vertices, List<uint> indices, uint indexOffset,
                                      Quad quad, float depth, float modelSize,
                                      Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
        {
            Vector3 v1 = new Vector3(quad.X, quad.Y + quad.Height, 0) * modelSize;
            Vector3 v2 = new Vector3(quad.X, quad.Y + quad.Height, 0) * modelSize;
            Vector3 v3 = new Vector3(quad.X + quad.Width, quad.Y + quad.Height, 0) * modelSize;
            Vector3 v4 = new Vector3(quad.X + quad.Width, quad.Y + quad.Height, 0) * modelSize;

            v2 += new Vector3(0, 0, depth);
            v3 += new Vector3(0, 0, depth);

            Vector3 normal = new Vector3(0, 1, 0);

            vertices.AddRange(new float[]
            {
                v1.X, v1.Y, v1.Z, normal.X, normal.Y, normal.Z, uv1.X, uv1.Y,
                v2.X, v2.Y, v2.Z, normal.X, normal.Y, normal.Z, uv2.X, uv2.Y,
                v3.X, v3.Y, v3.Z, normal.X, normal.Y, normal.Z, uv3.X, uv3.Y,
                v4.X, v4.Y, v4.Z, normal.X, normal.Y, normal.Z, uv4.X, uv4.Y
            });

            indices.AddRange(new uint[]
            {
                indexOffset, indexOffset + 1, indexOffset + 2,
                indexOffset, indexOffset + 2, indexOffset + 3
            });
        }

        public static void AddFaceButton(List<float> vertices, List<uint> indices, uint indexOffset,
                                         Quad quad, float depth, float modelSize,
                                         Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
        {
            Vector3 v1 = new Vector3(quad.X, quad.Y, 0) * modelSize;
            Vector3 v4 = new Vector3(quad.X, quad.Y, 0) * modelSize;
            Vector3 v3 = new Vector3(quad.X + quad.Width, quad.Y, 0) * modelSize;
            Vector3 v2 = new Vector3(quad.X + quad.Width, quad.Y, 0) * modelSize;

            v4 += new Vector3(0, 0, depth);
            v3 += new Vector3(0, 0, depth);

            Vector3 normal = new Vector3(0, -1, 0);

            vertices.AddRange(new float[]
            {
                v1.X, v1.Y, v1.Z, normal.X, normal.Y, normal.Z, uv1.X, uv1.Y,
                v2.X, v2.Y, v2.Z, normal.X, normal.Y, normal.Z, uv2.X, uv2.Y,
                v3.X, v3.Y, v3.Z, normal.X, normal.Y, normal.Z, uv3.X, uv3.Y,
                v4.X, v4.Y, v4.Z, normal.X, normal.Y, normal.Z, uv4.X, uv4.Y
            });

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
            public int id = 0;

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

                    int quadWidth = 1;
                    while (x + quadWidth < width && !processed[x + quadWidth, y] && pixels[x + quadWidth, y].Equals(currentColor))
                        quadWidth++;

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
                        if (done) break;
                        quadHeight++;
                    }

                    for (int dy = 0; dy < quadHeight; dy++)
                    {
                        for (int dx = 0; dx < quadWidth; dx++)
                            processed[x + dx, y + dy] = true;
                    }

                    float u = (float)x / width;
                    float v = (float)y / height;
                    float uWidth = (float)quadWidth / width;
                    float uHeight = (float)quadHeight / height;

                    bool needsLeft = false, needsRight = false, needsTop = false, needsBottom = false;

                    if (x == 0)
                        needsLeft = true;
                    else
                    {
                        for (int dy = 0; dy < quadHeight; dy++)
                        {
                            if (pixels[x - 1, y + dy].A < 1) { needsLeft = true; break; }
                        }
                    }

                    if (x + quadWidth >= width)
                        needsRight = true;
                    else
                    {
                        for (int dy = 0; dy < quadHeight; dy++)
                        {
                            if (pixels[x + quadWidth, y + dy].A < 1) { needsRight = true; break; }
                        }
                    }

                    if (y == 0)
                        needsTop = true;
                    else
                    {
                        for (int dx = 0; dx < quadWidth; dx++)
                        {
                            if (pixels[x + dx, y - 1].A < 1) { needsTop = true; break; }
                        }
                    }

                    if (y + quadHeight >= height)
                        needsBottom = true;
                    else
                    {
                        for (int dx = 0; dx < quadWidth; dx++)
                        {
                            if (pixels[x + dx, y + quadHeight].A < 1) { needsBottom = true; break; }
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
