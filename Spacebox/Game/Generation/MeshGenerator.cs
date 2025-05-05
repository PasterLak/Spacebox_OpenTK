using OpenTK.Mathematics;
using Engine;
using System.Diagnostics;
using Engine.Physics;
using Debug = Engine.Debug;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Tools;

namespace Spacebox.Game.Generation
{
    public class MeshGenerator
    {
        private bool _EnableAO = true;
        public bool EnableAO { get => _EnableAO; set { _EnableAO = value; } }

        private const byte Size = Chunk.Size;
        private readonly Chunk _chunk;
        private readonly Block[,,] _blocks;
        private readonly bool _measureGenerationTime;
        private static readonly Face[] faces = (Face[])Enum.GetValues(typeof(Face));
        private static Vector3SByte[] faceNormals;
        private float[] vertices;
        private uint[] indices;
        private int vertexCount;
        private int indexCount;
        private Stopwatch stopwatch;
        private Dictionary<Vector3SByte, Chunk> _neighbors;
        public BoundingBox GeometryBoundingBox { get; private set; }

        public MeshGenerator(Chunk chunk, Dictionary<Vector3SByte, Chunk> Neighbors, bool measureGenerationTime = true)
        {
            _neighbors = Neighbors;
            _blocks = chunk.Blocks;
            _chunk = chunk;
            _measureGenerationTime = measureGenerationTime;
            AOVoxels.Init();
            PrecomputeData();
            GeometryBoundingBox = BoundingBox.CreateFromMinMax(Vector3.Zero, Vector3.One * Chunk.Size);
        }

        private void PrecomputeData()
        {
            faceNormals = new Vector3SByte[faces.Length];
            for (int i = 0; i < faces.Length; i++)
                faceNormals[i] = faces[i].GetNormal();
        }

        const int floatsPerVertex = BuffersData.FloatsPerVertexBlock;
        const int vertsPerBlock = 24;
        const int indicesPerBlock = 36;
        const int estimatedVertices = Size * Size * Size * vertsPerBlock * floatsPerVertex;
        const int estimatedIndices = Size * Size * Size * indicesPerBlock;

        public Mesh GenerateMesh()
        {
            if (_measureGenerationTime)
            {
                stopwatch = Stopwatch.StartNew();
            }

            vertices = new float[estimatedVertices];
            indices = new uint[estimatedIndices];
            vertexCount = 0;
            indexCount = 0;
            int mass = 0;
            sbyte xMin = sbyte.MaxValue;
            sbyte xMax = sbyte.MinValue;
            sbyte yMin = sbyte.MaxValue;
            sbyte yMax = sbyte.MinValue;
            sbyte zMin = sbyte.MaxValue;
            sbyte zMax = sbyte.MinValue;
            Vector3 sumPosMass = Vector3.Zero;

            for (sbyte x = 0; x < Size; x++)
            {
                for (sbyte y = 0; y < Size; y++)
                {
                    for (sbyte z = 0; z < Size; z++)
                    {
                        var block = _blocks[x, y, z];
                        if (block == null) continue;
                        if (block.IsAir) continue;
                        byte m = block.Mass;
                        mass += m;
                        if (x < xMin) xMin = x;
                        if (x > xMax) xMax = x;
                        if (y < yMin) yMin = y;
                        if (y > yMax) yMax = y;
                        if (z < zMin) zMin = z;
                        if (z > zMax) zMax = z;
                        sumPosMass += new Vector3(x, y, z) * m;

                        for (byte fIndex = 0; fIndex < faces.Length; fIndex++)
                        {
                            Face face = faces[fIndex];
                            Vector3SByte normal = faceNormals[fIndex];
                            sbyte nx = (sbyte)(x + normal.X);
                            sbyte ny = (sbyte)(y + normal.Y);
                            sbyte nz = (sbyte)(z + normal.Z);

                            if (block.IsTransparent && IsInRange(nx, ny, nz))
                            {
                                var nb = _blocks[nx, ny, nz];
                                if (nb != null && nb.IsTransparent) continue;
                            }

                            if (IsTransparentBlock(nx, ny, nz, normal,block.IsTransparent))
                            {
                                var faceVertices = CubeMeshData.GetFaceVertices(face);
                                var faceUVs = GameAssets.GetBlockUVsByIdAndDirection(block.BlockId, face, block.Direction);
                                var currentLightLevel = block.LightLevel / 15f;
                                var currentLightColor = block.LightColor;
                                float neighborLightLevel = 0f;
                                Vector3 neighborLightColor = Vector3.Zero;
                                var neighborBlock = GetBlockFromThisOrNeighborChunk(nx, ny, nz);
                                if (neighborBlock != null)
                                {
                                    neighborLightLevel = neighborBlock.LightLevel / 15f;
                                    neighborLightColor = neighborBlock.LightColor;
                                }
                               // float averageLightLevel = (currentLightLevel + neighborLightLevel) * 0.5f;
                                var averageLightColor = (currentLightColor * currentLightLevel + neighborLightColor * neighborLightLevel)
                                                        / (currentLightLevel + neighborLightLevel + 0.001f);
                                Vector3 ambient = new Vector3(0.2f, 0.2f, 0.2f);
                                var vertexColor = Vector3.Clamp(block.Color * (averageLightColor + ambient), Vector3.Zero, Vector3.One);

                                bool flip = false;
                                int vStart = vertexCount / BuffersData.FloatsPerVertexBlock;
                                float[] AO = new float[4];
                                byte newMask = CreateMask(face, new Vector3SByte(x, y, z), normal);
                                bool isLightOrTransparent = block.IsTransparent || block.IsLight;
                                var shading = isLightOrTransparent ? AOVoxels.GetLightedPoints : AOShading.GetAO(newMask);
                                bool same3 = false;
                                bool diagonal = false;
                                byte another = 0;
                                byte s = 0;

                                if (!isLightOrTransparent)
                                {
                                    for (byte i = 0; i < shading.Length; i++)
                                    {
                                        if (shading[i] == 0.5f)
                                        {
                                            s++;
                                            continue;
                                        }
                                        else another = i;
                                    }
                                    if (s == 3) same3 = true;
                                    if (same3)
                                    {
                                        if (another == 0 || another == 2)
                                        {
                                            diagonal = true;
                                        }
                                    }
                                }

                                for (byte i = 0; i < 4; i++)
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
                                        AO[i] = shading[i];
                                        vertices[vertexCount++] = AO[i];
                                        if (i == 0 || i == 2)
                                        {
                                            if (AO[i] < 0.5f) AO[i] -= 0.5f;
                                            if (same3)
                                            {
                                                if (!diagonal) AO[i] += 0.5f;
                                                else AO[i] -= 0.5f;
                                            }
                                        }
                                        else
                                        {
                                            if (AO[i] < 0.5f) AO[i] -= 0.5f;
                                        }
                                    }
                                    else
                                    {
                                        vertices[vertexCount++] = 1f;
                                    }

                                    vertices[vertexCount++] = block.EnableEmission ? 1f : 0f;
                                }

                                if (_EnableAO)
                                {
                                    if (AO[0] + AO[2] < AO[1] + AO[3]) flip = true;
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
            Array.Copy(vertices, finalVertices, vertexCount);
            uint[] finalIndices = new uint[indexCount];
            Array.Copy(indices, finalIndices, indexCount);
            Mesh mesh = new Mesh(finalVertices, finalIndices, BuffersData.CreateBlockBuffer());

            if (_measureGenerationTime && stopwatch != null)
            {
                stopwatch.Stop();
                Engine.Debug.Success($"Chunk mesh generation time: {stopwatch.ElapsedMilliseconds} ms");
            }

            GeometryBoundingBox = BoundingBox.CreateFromMinMax(new Vector3(xMin, yMin, zMin),
                new Vector3(xMax + 1, yMax + 1, zMax + 1));
            _chunk.Mass = mass;
            _chunk.SumPosMass = sumPosMass;
            return mesh;
        }

        private byte CreateMask(Face face, Vector3SByte blockPos, Vector3SByte normal)
        {
            byte mask = 0;
            byte faceIndex = (byte)face;
            Vector3SByte[] nbs = AOVoxels.FaceNeighborOffsets[faceIndex];
            blockPos = blockPos + normal;

            for (byte bit = 0; bit < 8; bit++)
            {
                Vector3SByte offset = nbs[bit];
                int nx = blockPos.X + offset.X;
                int ny = blockPos.Y + offset.Y;
                int nz = blockPos.Z + offset.Z;
                if (NeedsAO((sbyte)nx, (sbyte)ny, (sbyte)nz, CubeMeshData.GetNormal(face)))
                    mask |= (byte)(1 << bit);
            }
            return mask;
        }

        private bool NeedsAO(sbyte x, sbyte y, sbyte z, Vector3SByte norm)
        {
            if (IsInRange(x, y, z))
            {
                var b = _blocks[x, y, z];
                if (b.IsAir) return false;
                if (b.IsTransparent) return false;
                if (IsLightBlock(x, y, z)) return false;
                return true;
            }
            else
            {
                GetNeighborChunkIndexAndLocalCoords(x, y, z, Size, out var offset, out var local);
                if (_neighbors.TryGetValue(offset, out var chunk) && chunk != null)
                {
                    var b = chunk.GetBlock(local);
                    if (b != null)
                    {
                        if (b.IsAir) return false;
                        if (b.IsTransparent) return false;
                        if (b.LightLevel > 0) return false;
                        return true;
                    }
                }
                return false;
            }
        }

        private Block GetBlockFromThisOrNeighborChunk(sbyte x, sbyte y, sbyte z)
        {
            if (IsInRange(x, y, z))
            {
                return _blocks[x, y, z];
            }
            GetNeighborChunkIndexAndLocalCoords(x, y, z, Size, out var off, out var loc);
            if (_neighbors.TryGetValue(off, out var c) && c != null)
            {
                var b = c.GetBlock(loc);
                return b;
            }
            return null;
        }

        public static void GetNeighborChunkIndexAndLocalCoords(int x, int y, int z, byte size, out Vector3SByte offset, out Vector3Byte local)
        {
            sbyte ox = 0, oy = 0, oz = 0;
            if (x < 0) { ox = -1; x += size; }
            else if (x >= size) { ox = 1; x -= size; }
            if (y < 0) { oy = -1; y += size; }
            else if (y >= size) { oy = 1; y -= size; }
            if (z < 0) { oz = -1; z += size; }
            else if (z >= size) { oz = 1; z -= size; }
            offset = new Vector3SByte(ox, oy, oz);
            local = new Vector3Byte((byte)x, (byte)y, (byte)z);
        }

        private bool IsLightBlock(sbyte x, sbyte y, sbyte z)
        {
            return _blocks[x, y, z].LightLevel > 0;
        }

        private bool IsTransparentBlock(sbyte x, sbyte y, sbyte z, Vector3SByte normal, bool currentTransparent)
        {
            if (!IsInRange(x, y, z))
            {
                if (_neighbors.TryGetValue(normal, out var nChunk) && nChunk != null)
                {
                    var wrap = WrapBlockCoordinate(x, y, z, Size);
                    var b = nChunk.GetBlock(wrap);
                    if (b != null)
                    {
                        return currentTransparent ? b.IsAir : (b.IsAir || b.IsTransparent);
                    }
                    return true;
                }
                return true;
            }
            var bl = _blocks[x, y, z];
            return currentTransparent ? bl.IsAir : (bl.IsAir || bl.IsTransparent);
        }


        public static Vector3SByte WrapBlockCoordinate(int x, int y, int z, byte Size)
        {
            int Wrap(int v, int s) { int w = v % s; return w < 0 ? w + s : w; }
            return new Vector3SByte((sbyte)Wrap(x, Size), (sbyte)Wrap(y, Size), (sbyte)Wrap(z, Size));
        }

        private bool IsInRange(sbyte x, sbyte y, sbyte z)
        {
            return x >= 0 && x < Size && y >= 0 && y < Size && z >= 0 && z < Size;
        }
    }
}
