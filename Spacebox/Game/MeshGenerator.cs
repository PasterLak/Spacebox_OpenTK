using OpenTK.Mathematics;
using Spacebox.Common;
using System.Diagnostics;

namespace Spacebox.Game.Rendering
{
    public class MeshGenerator
    {
        private bool _EnableAO = true;
        public bool EnableAO
        {
            get => _EnableAO;
            set
            {
                _EnableAO = value;
            }
        }
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

            for (sbyte x = 0; x < Size; x++)
            {
                for (sbyte y = 0; y < Size; y++)
                {
                    for (sbyte z = 0; z < Size; z++)
                    {
                        Block block = _blocks[x, y, z];
                        if (block.IsAir())
                            continue;

                        foreach (Face face in Enum.GetValues(typeof(Face)))
                        {
                            Vector3SByte normal = face.GetNormal();
                            sbyte nx = (sbyte)(x + normal.X);
                            sbyte ny = (sbyte)(y + normal.Y);
                            sbyte nz = (sbyte)(z + normal.Z);

                            if (IsTransparentBlock(nx, ny, nz))
                            {
                                Vector3[] faceVertices = CubeMeshData.GetFaceVertices(face);


                                Vector2[] faceUVs = GameBlocks.GetBlockUVsByIdAndDirection(block.BlockId, face, block.Direction);

                                //Vector3i normald = FaceExtensions.GetNormal(face);

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

                                float averageLightLevel = (currentLightLevel + neighborLightLevel) * 0.5f;
                                Vector3 averageLightColor = (currentLightColor * currentLightLevel + neighborLightColor * neighborLightLevel) /
                                                            (currentLightLevel + neighborLightLevel + 0.001f);

                                Vector3 ambient = new Vector3(0.2f, 0.2f, 0.2f);
                                Vector3 vertexColor = Vector3.Clamp(block.Color * (averageLightColor + ambient), Vector3.Zero, Vector3.One);
                                //vertexColor = new Vector3(0,0,0);
                                byte mask = CreateMask(faceVertices);
                                float[] ao = new float[4];

                                if (_EnableAO)
                                {
                                    ao[0] = ComputeAO(new Vector3SByte(x, y, z), faceVertices[0], normal, mask);
                                    ao[1] = ComputeAO(new Vector3SByte(x, y, z), faceVertices[1], normal, mask);
                                    ao[2] = ComputeAO(new Vector3SByte(x, y, z), faceVertices[2], normal, mask);
                                    ao[3] = ComputeAO(new Vector3SByte(x, y, z), faceVertices[3], normal, mask);

                                    
                                }
                                
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
                                    vertices.Add(normal.X);
                                    vertices.Add(normal.Y);
                                    vertices.Add(normal.Z);
                                    if (_EnableAO)
                                        vertices.Add(ao[i]);
                                    else
                                        vertices.Add(1f);
                                }

                                uint[] faceIndices;
                                
                                bool flip = false;

                                flip = ao[1] + ao[3] > ao[2] + ao[0];
                               

                                if (_EnableAO)
                                {
                                    if (flip)
                                    {
                                       // ao[2] = ao[3];
                                       
                                        faceIndices = new uint[6]{ 1,2,3,3,0,1};
                                    }else
                                    faceIndices = new uint[6]{ 0, 1, 2, 2, 3, 0 };
                                }
                                else
                                {
                                    faceIndices = new uint[6]{ 0, 1, 2, 2, 3, 0 };
                                    
                                }
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
                Common.Debug.Success($"Chunk mesh generation time: {stopwatch.ElapsedMilliseconds} ms");
            }

            return mesh;
        }

        private byte CreateMask(Vector3[] vertex)
        {
            byte[] verticesAsByte = new byte[4];

            for (sbyte j = 0; j < vertex.Length; j++)
            {
                verticesAsByte[j] = VectorToBitNumber(Vector3SByte.CreateFrom(vertex[j]));
            }

            return CombineBits(verticesAsByte);
        }

        private float ComputeAO(Vector3SByte blockPos, Vector3 vertex, Vector3SByte normal, byte mask)
        {
            byte neighbours = 0;
            var sidePos = blockPos + normal;

            Vector3SByte[] neighboursPositions = ApplyMaskToPosition(Vector3SByte.Zero, Vector3SByte.CreateFrom(vertex), mask);

            var diagonal = Vector3SByte.Zero;
            for (sbyte i = 0; i < neighboursPositions.Length; i++)
            {
                var newPos = sidePos + neighboursPositions[i];
                if (newPos != sidePos)
                    if (IsSolid(newPos)) neighbours++;

                diagonal += neighboursPositions[i];
            }

            if (diagonal != Vector3SByte.Zero)
            {
                diagonal += sidePos;
            
                if (diagonal != sidePos)
                    if (IsSolid(diagonal)) neighbours++;

            }
            
            return 1f - (neighbours * (1f / 5f));
        }

        private static Vector3SByte[] ApplyMaskToPosition(Vector3SByte position, Vector3SByte vertex, byte mask)
        {
            Vector3SByte[] result = new Vector3SByte[3];

            for (byte i = 0; i < 3; i++)
            {
                Vector3SByte currentPosition = position;

                sbyte maskValue = (sbyte)((mask >> (2 - i)) & 1);

                if (maskValue == 1)
                {
                    if (i == 0)
                    {
                        currentPosition.X = ApplyShift(currentPosition.X, vertex.X);
                    }
                    if (i == 1)
                    {
                        currentPosition.Y = ApplyShift(currentPosition.Y, vertex.Y);
                    }
                    if (i == 2)
                    {
                        currentPosition.Z = ApplyShift(currentPosition.Z, vertex.Z);
                    }
                }
                else
                {
                    result[i] = currentPosition;
                }

                result[i] = currentPosition;
            }

            return result;
        }
        private static sbyte ApplyShift(sbyte componentValue, sbyte shiftValue)
        {
            if (shiftValue == 0)
            {
                return (sbyte)(componentValue - 1);
            }
            else
            {
                return (sbyte)(componentValue + shiftValue);
            }
        }

        private static byte VectorToBitNumber(Vector3SByte vertex)
        {
            byte bitNumber = (byte)((vertex.X << 2) | (vertex.Y << 1) | vertex.Z);
            return bitNumber;
        }
        private static byte CombineBits(byte[] numbers)
        {
            byte result = 0;

            for (byte i = 0; i < numbers.Length; i++)
            {
                result |= numbers[i];
            }

            return result;
        }


        private bool IsSolid(Vector3SByte pos)
        {
            return IsSolid(pos.X, pos.Y, pos.Z);
        }

        private bool IsSolid(sbyte x, sbyte y, sbyte z)
        {
            if (!IsInRange(x, y, z)) return false;
            return !_blocks[x, y, z].IsAir();
        }

        private bool IsTransparentBlock(sbyte x, sbyte y, sbyte z)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size || z < 0 || z >= Size)
                return true;

            Block block = _blocks[x, y, z];
            return block.IsAir() || block.IsTransparent;
        }

        private bool IsInRange(sbyte x, sbyte y, sbyte z)
        {
            return x >= 0 && x < Size && y >= 0 && y < Size && z >= 0 && z < Size;
        }
    }
}
