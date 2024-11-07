using OpenTK.Mathematics;
using Spacebox.Extensions;
using System.Diagnostics;

namespace Spacebox.Game.Rendering
{
    public class MeshGenerator
    {
        private const byte Size = Chunk.Size;
        private readonly Block[,,] _blocks;
        private readonly bool _measureGenerationTime;

        public MeshGenerator(Block[,,] blocks, bool measureGenerationTime = true)
        {
            _blocks = blocks;
            _measureGenerationTime = measureGenerationTime;
        }

        public Mesh GenerateMesh()
        {
            Stopwatch stopwatch = null;
            if (_measureGenerationTime)
            {
                stopwatch = Stopwatch.StartNew();
            }

            List<float> vertices = new List<float>();
            List<uint> indices = new List<uint>();
            uint index = 0;

            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    for (int z = 0; z < Size; z++)
                    {
                        Block block = _blocks[x, y, z];
                        if (block.IsAir())
                            continue;

                        foreach (Face face in Enum.GetValues(typeof(Face)))
                        {
                            Vector3i normal = face.GetNormal();
                            int nx = x + normal.X;
                            int ny = y + normal.Y;
                            int nz = z + normal.Z;

                            if (IsTransparentBlock(nx, ny, nz))
                            {
                                Vector3[] faceVertices = CubeMeshData.GetFaceVertices(face);
                                Vector2[] faceUVs = UVAtlas.GetUVs((int)block.TextureCoords.X, (int)block.TextureCoords.Y);

                                float currentLightLevel = block.LightLevel / 15f;
                                Vector3 currentLightColor = block.LightColor;

                                float neighborLightLevel = 0f;
                                Vector3 neighborLightColor = Vector3.Zero;

                                if (IsInRange(nx, ny, nz))
                                {
                                    Block neighborBlock = _blocks[nx, ny, nz];
                                    neighborLightLevel = neighborBlock.LightLevel / 15f;
                                    neighborLightColor = neighborBlock.LightColor;
                                }

                                float averageLightLevel = (currentLightLevel + neighborLightLevel) / 2f;
                                Vector3 averageLightColor = (currentLightColor * currentLightLevel + neighborLightColor * neighborLightLevel) /
                                                            (currentLightLevel + neighborLightLevel + 0.001f);

                                Vector3 ambient = new Vector3(0.2f, 0.2f, 0.2f);
                                Vector3 vertexColor = Vector3.Clamp(block.Color * (averageLightColor + ambient), Vector3.Zero, Vector3.One);

                                for (int i = 0; i < faceVertices.Length; i++)
                                {
                                    var vertex = faceVertices[i];
                                    vertices.Add(vertex.X + x);
                                    vertices.Add(vertex.Y + y);
                                    vertices.Add(vertex.Z + z);
                                    vertices.Add(faceUVs[i].X);
                                    vertices.Add(faceUVs[i].Y);
                                    vertices.Add(vertexColor.X);
                                    vertices.Add(vertexColor.Y);
                                    vertices.Add(vertexColor.Z);
                                }

                                uint[] faceIndices = { 0, 1, 2, 2, 3, 0 };
                                foreach (var fi in faceIndices)
                                {
                                    indices.Add(index + fi);
                                }

                                index += 4;
                            }
                        }
                    }
                }
            }

            Mesh mesh = new Mesh(vertices.ToArray(), indices.ToArray());

            if (_measureGenerationTime && stopwatch != null)
            {
                stopwatch.Stop();
                Debug.WriteLine($"Chunk mesh generation time: {stopwatch.ElapsedMilliseconds} ms");
            }

            return mesh;
        }

        private bool IsTransparentBlock(int x, int y, int z)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size || z < 0 || z >= Size)
                return true;

            Block block = _blocks[x, y, z];
            return block.IsAir() || block.IsTransparent;
        }

        private bool IsInRange(int x, int y, int z)
        {
            return x >= 0 && x < Size && y >= 0 && y < Size && z >= 0 && z < Size;
        }
    }
}
