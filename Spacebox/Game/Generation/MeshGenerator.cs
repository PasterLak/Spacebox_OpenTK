using OpenTK.Mathematics;
using Spacebox.Common;
using System.Diagnostics;
using Spacebox.Common.Physics;
using Debug = Spacebox.Common.Debug;

namespace Spacebox.Game.Generation
{
    public class MeshGenerator
    {
        private bool _EnableAO = true;

        public bool EnableAO
        {
            get => _EnableAO;
            set { _EnableAO = value; }
        }

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
        Stopwatch stopwatch = null;
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

            sbyte SizeHalf = (sbyte)(Size * 0.5f);

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
                        if (block == null)
                        {

                            continue;
                        }
                        if (block.IsAir()) continue;

                        byte m = block.Mass;
                        mass += m;
                        
                        if(x < xMin) xMin = x;
                        if(x > xMax) xMax = x;
                        
                        if(y < yMin) yMin = y;
                        if(y > yMax) yMax = y;
                        
                        if(z < zMin) zMin = z;
                        if(z > zMax) zMax = z;

                        // mass

               
                        sumPosMass += new Vector3(x,y,z) * m;



                        for (int fIndex = 0; fIndex < faces.Length; fIndex++)
                        {
                            Face face = faces[fIndex];
                            Vector3SByte normal = faceNormals[fIndex];

                            sbyte nx = (sbyte)(x + normal.X);
                            sbyte ny = (sbyte)(y + normal.Y);
                            sbyte nz = (sbyte)(z + normal.Z);
                            
                            if (block.IsTransparent && IsInRange(nx, ny, nz))
                            {
                                var neighborBlock = _blocks[nx, ny, nz];
                                if (neighborBlock != null && neighborBlock.IsTransparent) continue;
                            }


                            if (IsTransparentBlock(nx, ny, nz, normal))
                            {
                                var faceVertices = CubeMeshData.GetFaceVertices(face);
                                var faceUVs =
                                    GameBlocks.GetBlockUVsByIdAndDirection(block.BlockId, face, block.Direction);

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
                                Vector3 averageLightColor =
                                    (currentLightColor * currentLightLevel + neighborLightColor * neighborLightLevel) /
                                    (currentLightLevel + neighborLightLevel + 0.001f);

                                Vector3 ambient = new Vector3(0.2f, 0.2f, 0.2f);
                                Vector3 vertexColor = Vector3.Clamp(block.Color * (averageLightColor + ambient),
                                    Vector3.Zero, Vector3.One);

                                bool flip = false;
                                int vStart = vertexCount / BuffersData.FloatsPerVertexBlock;
                                float[] AO = new float[4];


                                byte newMask = CreateMask(face, new Vector3SByte(x, y, z), normal);

                                bool isLightOrTransparent = block.IsTransparent || block.IsLight();

                                var shading = isLightOrTransparent
                                    ? AOVoxels.GetLightedPoints
                                    : AOShading.GetAO(newMask);

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
                                        AO[i] = shading[i];
                                        vertices[vertexCount++] = AO[i];

                                        if (i == 0 || i == 2)
                                        {
                                            if (AO[i] < 0.5f) AO[i] -= 0.5f;

                                            if (same3)
                                            {
                                                if (!diagonal)
                                                {
                                                    AO[i] += 0.5f;
                                                }
                                                else
                                                    AO[i] -= 0.5f;
                                            }
                                        }
                                        else
                                        {
                                            if (AO[i] < 0.5f) AO[i] -= 0.5f;
                                        }
                                    }
                                    else
                                        vertices[vertexCount++] = 1f;

                                    vertices[vertexCount++] = block.enableEmission ?  1f : 0f;

                                }

                                if (_EnableAO)
                                {
                                    if (AO[0] + AO[2] < AO[1] + AO[3])
                                    {
                                        flip = true;
                                    }
                                    else
                                    {
                                        flip = false;
                                    }


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

            Mesh mesh = new Mesh(finalVertices, finalIndices, BuffersData.CreateBlockBuffer());

            if (_measureGenerationTime && stopwatch != null)
            {
                stopwatch.Stop();
                Common.Debug.Success($"Chunk mesh generation time: {stopwatch.ElapsedMilliseconds} ms");
            }

            GeometryBoundingBox = BoundingBox.CreateFromMinMax(new Vector3(xMin, yMin, zMin),
                new Vector3(xMax +1 , yMax+1 , zMax+1 ));

            _chunk.Mass = mass;
            _chunk.SumPosMass = sumPosMass;

            return mesh;
        }

        private byte CreateMask(Face face, Vector3SByte blockPos, Vector3SByte normal)
        {
            byte mask = 0;
            byte faceIndex = (byte)face;

            Vector3SByte[] neighbors = AOVoxels.FaceNeighborOffsets[faceIndex];

            blockPos = blockPos + normal;

            for (byte bit = 0; bit < 8; bit++)
            {
                Vector3SByte offset = neighbors[bit];
                int neighborX = blockPos.X + offset.X;
                int neighborY = blockPos.Y + offset.Y;
                int neighborZ = blockPos.Z + offset.Z;

                var norm = new Vector3SByte((sbyte)neighborX, (sbyte)neighborY, (sbyte)neighborZ) - offset;

                if (CheckForAO((sbyte)neighborX, (sbyte)neighborY, (sbyte)neighborZ, norm))
                {
                    mask |= (byte)(1 << bit);
                }
            }

            return mask;
        }

        private bool CheckForAO(sbyte x, sbyte y, sbyte z, Vector3SByte norm)
        {
            if (IsSolid(x, y, z))
            {
                if (IsTransparentBlock(x, y, z, norm)) return false;

                if (IsLightBlock(x, y, z)) return false;

                return true;
            }

            return false;
        }

        private bool IsLightBlock(sbyte x, sbyte y, sbyte z)
        {
            return _blocks[x, y, z].LightLevel > 0;
        }

        private bool IsSolid(sbyte x, sbyte y, sbyte z)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size || z < 0 || z >= Size)
                return false;
            return !_blocks[x, y, z].IsAir();
        }

        private bool IsTransparentBlock(sbyte x, sbyte y, sbyte z, Vector3SByte normal)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size || z < 0 || z >= Size)
            {
                Chunk neighborChunk = _neighbors[normal];
                
                if (neighborChunk != null)
                {
                    Block block = neighborChunk.GetBlock(WrapBlockCoordinate(x,y,z, Size));

                    if (block != null)
                    {
                        return block.IsAir() || block.IsTransparent;
                    }
                }
                else
                {
                    return true;
                }
            }

            var b = _blocks[x, y, z];
            return b.IsAir() || b.IsTransparent;
        }
        
        public static Vector3SByte WrapBlockCoordinate(int x, int y, int z,  byte Size)
        {
            int Wrap(int coord, int size)
            {
                int wrapped = coord % size;
                if (wrapped < 0)
                    wrapped += size;
                return wrapped;
            }

            return new Vector3SByte(
                (sbyte)Wrap(x, Size),
                (sbyte)Wrap(y, Size),
                (sbyte)Wrap(z, Size)
            );
        }

        private bool IsInRange(sbyte x, sbyte y, sbyte z)
        {
            return x >= 0 && x < Size && y >= 0 && y < Size && z >= 0 && z < Size;
        }
    }
}