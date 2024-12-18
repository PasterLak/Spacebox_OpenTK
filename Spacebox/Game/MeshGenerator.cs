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
            
            AOVoxels.Init();
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

                                bool flip = false;
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
                                    {
                                        var n = ComputeAO2(face, Vector3SByte.CreateFrom(vertex),
                                            new Vector3SByte(x, y, z), normal, out var hasDiagonalBlock);

                                        if (hasDiagonalBlock)
                                        {
                                            if (i == 0 || i == 2)
                                            {
                                                flip = true;    
                                            }
                                        }
                                        
                                        vertices.Add(n);
                                  
                                    }
                                        
                                    else
                                        vertices.Add(1f);
                                }

                                uint[] faceIndices;

                                //flip = ao[1] + ao[3] > ao[2] + ao[0];

                                if (_EnableAO)
                                {
                                    if (flip)
                                        faceIndices = new uint[6]{ 1,2,3,3,0,1};
                                    else
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

        private float ComputeAO2(Face face, Vector3SByte vertex,Vector3SByte blockPos,Vector3SByte normal, out bool hasDiagonalBlock)
        {
            byte neighbours = 0;
            var sidePos = blockPos + normal;
            hasDiagonalBlock = false;
            var neigbordPositions = AOVoxels.GetNeigbordPositions(face, vertex);
            
            var diagonal = Vector3SByte.Zero;
            for (sbyte i = 0; i < neigbordPositions.Length; i++)
            {
                var newPos = sidePos + neigbordPositions[i];
                
                if (IsSolid(newPos)) neighbours++;

                diagonal += neigbordPositions[i];
            }

            if (diagonal != Vector3SByte.Zero)
            {
                diagonal += sidePos;

                if (IsSolid(diagonal))
                {
                    neighbours++;
                    hasDiagonalBlock = true;
                }
               
            }
            
            return 1f - (neighbours * (1f / 4f));
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
