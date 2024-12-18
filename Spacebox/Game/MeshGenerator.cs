using OpenTK.Mathematics;
using Spacebox.Common;
using System.Diagnostics;

namespace Spacebox.Game.Rendering
{
    public class MeshGenerator
    {
        private bool _EnableAO = true;
        public bool EnableAO { get => _EnableAO; set { _EnableAO = value; } }

        private const byte Size = Chunk.Size;
        private readonly Block[,,] _blocks;
        private readonly bool _measureGenerationTime;
        
        private static readonly Face[] faces = (Face[])Enum.GetValues(typeof(Face));
        private static Vector3SByte[] faceNormals;

        private float[] vertices;
        private uint[] indices;
        private int vertexCount;
        private int indexCount;
        Stopwatch stopwatch = null;

        public MeshGenerator(Block[,,] blocks, bool measureGenerationTime = true)
        {
            _blocks = blocks;
            _measureGenerationTime = measureGenerationTime;

            AOVoxels.Init();
            PrecomputeData();
        }

        private void PrecomputeData()
        {
            faceNormals = new Vector3SByte[faces.Length];
            for (int i = 0; i < faces.Length; i++)
                faceNormals[i] = faces[i].GetNormal();
        }

        public Mesh GenerateMesh()
        {
            
            if (_measureGenerationTime)
            {
                stopwatch = Stopwatch.StartNew();
            }

            int estimatedVertices = Size * Size * Size * 24;
            int estimatedIndices = Size * Size * Size * 36;
            vertices = new float[estimatedVertices];
            indices = new uint[estimatedIndices];
            vertexCount = 0;
            indexCount = 0;

            for (sbyte x = 0; x < Size; x++)
            {
                for (sbyte y = 0; y < Size; y++)
                {
                    for (sbyte z = 0; z < Size; z++)
                    {
                        var block = _blocks[x, y, z];
                        if (block.IsAir()) continue;

                        for (int fIndex = 0; fIndex < faces.Length; fIndex++)
                        {
                            Face face = faces[fIndex];
                            Vector3SByte normal = faceNormals[fIndex];

                            sbyte nx = (sbyte)(x + normal.X);
                            sbyte ny = (sbyte)(y + normal.Y);
                            sbyte nz = (sbyte)(z + normal.Z);

                            if (IsTransparentBlock(nx, ny, nz))
                            {
                                var faceVertices = CubeMeshData.GetFaceVertices(face);
                                var faceUVs = GameBlocks.GetBlockUVsByIdAndDirection(block.BlockId, face, block.Direction);

                                float currentLightLevel = block.LightLevel / 15f;
                                Vector3 currentLightColor = block.LightColor;
                                float neighborLightLevel = 0f;
                                Vector3 neighborLightColor = Vector3.Zero;

                                if (IsInRange(nx, ny, nz))
                                {
                                    var neighborBlock = _blocks[nx, ny, nz];
                                    neighborLightLevel = neighborBlock.LightLevel / 15f;
                                    neighborLightColor = neighborBlock.LightColor;
                                }

                                float averageLightLevel = (currentLightLevel + neighborLightLevel) * 0.5f;
                                Vector3 averageLightColor = (currentLightColor * currentLightLevel + neighborLightColor * neighborLightLevel) / 
                                                            (currentLightLevel + neighborLightLevel + 0.001f);

                                Vector3 ambient = new Vector3(0.2f, 0.2f, 0.2f);
                                Vector3 vertexColor = Vector3.Clamp(block.Color * (averageLightColor + ambient), Vector3.Zero, Vector3.One);

                                bool flip = false;
                                int vStart = vertexCount / 12;
                                for (int i = 0; i < 4; i++)
                                {
                                    var vertex = faceVertices[i];
                                    vertices[vertexCount++] = vertex.X + x;
                                    vertices[vertexCount++] = vertex.Y + y;
                                    vertices[vertexCount++] = vertex.Z + z;
                                    vertices[vertexCount++] = faceUVs[i].X;
                                    vertices[vertexCount++] = faceUVs[i].Y;
                                    vertices[vertexCount++] = vertexColor.X;
                                    vertices[vertexCount++] = vertexColor.Y;
                                    vertices[vertexCount++] = vertexColor.Z;
                                    vertices[vertexCount++] = normal.X;
                                    vertices[vertexCount++] = normal.Y;
                                    vertices[vertexCount++] = normal.Z;
                                    if (_EnableAO)
                                    {
                                        bool hasDiagonalBlock;
                                        float n = ComputeAO2(face, Vector3SByte.CreateFrom(vertex),
                                            new Vector3SByte(x, y, z), normal, out hasDiagonalBlock);
                                        if (hasDiagonalBlock && (i == 0 || i == 2))
                                            flip = true;
                                        vertices[vertexCount++] = n;
                                    }
                                    else
                                        vertices[vertexCount++] = 1f;
                                }

                                if (_EnableAO)
                                {
                                    if (flip)
                                    {
                                        indices[indexCount++] = (uint)(vStart + 1);
                                        indices[indexCount++] = (uint)(vStart + 2);
                                        indices[indexCount++] = (uint)(vStart + 3);
                                        indices[indexCount++] = (uint)(vStart + 3);
                                        indices[indexCount++] = (uint)(vStart + 0);
                                        indices[indexCount++] = (uint)(vStart + 1);
                                    }
                                    else
                                    {
                                        indices[indexCount++] = (uint)(vStart + 0);
                                        indices[indexCount++] = (uint)(vStart + 1);
                                        indices[indexCount++] = (uint)(vStart + 2);
                                        indices[indexCount++] = (uint)(vStart + 2);
                                        indices[indexCount++] = (uint)(vStart + 3);
                                        indices[indexCount++] = (uint)(vStart + 0);
                                    }
                                }
                                else
                                {
                                    indices[indexCount++] = (uint)(vStart + 0);
                                    indices[indexCount++] = (uint)(vStart + 1);
                                    indices[indexCount++] = (uint)(vStart + 2);
                                    indices[indexCount++] = (uint)(vStart + 2);
                                    indices[indexCount++] = (uint)(vStart + 3);
                                    indices[indexCount++] = (uint)(vStart + 0);
                                }
                            }
                        }
                    }
                }
            }

            float[] finalVertices = new float[vertexCount];
            Array.Copy(vertices, 0, finalVertices, 0, vertexCount);

            uint[] finalIndices = new uint[indexCount];
            Array.Copy(indices, 0, finalIndices, 0, indexCount);

            Mesh mesh = new Mesh(finalVertices, finalIndices);

            if (_measureGenerationTime && stopwatch != null)
            {
                stopwatch.Stop();
                Common.Debug.Success($"Chunk mesh generation time: {stopwatch.ElapsedMilliseconds} ms");
            }

            return mesh;
        }

        private float ComputeAO2(Face face, Vector3SByte vertex, Vector3SByte blockPos, Vector3SByte normal, out bool hasDiagonalBlock)
        {
            byte neighbours = 0;
            var sidePos = new Vector3SByte((sbyte)(blockPos.X + normal.X), (sbyte)(blockPos.Y + normal.Y), (sbyte)(blockPos.Z + normal.Z));
            hasDiagonalBlock = false;
            var neigbordPositions = AOVoxels.GetNeigbordPositions(face, vertex);

            sbyte dx = 0, dy = 0, dz = 0;
            for (int i = 0; i < neigbordPositions.Length; i++)
            {
                var newPos = new Vector3SByte((sbyte)(sidePos.X + neigbordPositions[i].X),
                                              (sbyte)(sidePos.Y + neigbordPositions[i].Y),
                                              (sbyte)(sidePos.Z + neigbordPositions[i].Z));
                if (IsSolid(newPos.X, newPos.Y, newPos.Z)) neighbours++;
                dx += neigbordPositions[i].X;
                dy += neigbordPositions[i].Y;
                dz += neigbordPositions[i].Z;
            }

            if (dx != 0 || dy != 0 || dz != 0)
            {
                var diagonal = new Vector3SByte((sbyte)(sidePos.X + dx), (sbyte)(sidePos.Y + dy), (sbyte)(sidePos.Z + dz));
                if (IsSolid(diagonal.X, diagonal.Y, diagonal.Z))
                {
                    neighbours++;
                    hasDiagonalBlock = true;
                }
            }

            return 1f - (neighbours * 0.25f);
        }

        private bool IsSolid(sbyte x, sbyte y, sbyte z)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size || z < 0 || z >= Size)
                return false;
            return !_blocks[x, y, z].IsAir();
        }

        private bool IsTransparentBlock(sbyte x, sbyte y, sbyte z)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size || z < 0 || z >= Size)
                return true;

            var b = _blocks[x, y, z];
            return b.IsAir() || b.IsTransparent;
        }

        private bool IsInRange(sbyte x, sbyte y, sbyte z)
        {
            return x >= 0 && x < Size && y >= 0 && y < Size && z >= 0 && z < Size;
        }
    }
}