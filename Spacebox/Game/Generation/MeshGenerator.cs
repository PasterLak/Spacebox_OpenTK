using OpenTK.Mathematics;
using Engine;
using System.Diagnostics;
using Engine.Physics;
using Debug = Engine.Debug;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Tools;
using System;
using System.Collections.Generic;

namespace Spacebox.Game.Generation
{
    public class MeshData
    {
        public float[] Vertices;
        public uint[] Indices;
        public BoundingBox GeometryBoundingBox;
        public int Mass;
        public Vector3 SumPosMass;
    }

    public class MeshGenerator
    {
        private static bool _EnableAO = true;
        public static bool EnableAO { get => _EnableAO; set { _EnableAO = value; } }

        private const byte Size = Chunk.Size;
        private readonly Block[,,] _paddedBlocks;
        private readonly bool _measureGenerationTime;
        private static readonly Face[] faces = (Face[])Enum.GetValues(typeof(Face));
        private static Vector3SByte[] faceNormals;
        private float[] vertices;
        private uint[] indices;
        private int vertexCount;
        private int indexCount;
        private Stopwatch stopwatch;
        private Vector3SByte _chunkIndex;

        public MeshGenerator(Vector3SByte chunkIndex, Block[,,] paddedBlocks, bool measureGenerationTime = true)
        {
            _chunkIndex = chunkIndex;
            _paddedBlocks = paddedBlocks;
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

        const int floatsPerVertex = BuffersData.FloatsPerVertexBlock;
        const int vertsPerBlock = 24;
        const int indicesPerBlock = 36;
        const int estimatedVertices = Size * Size * Size * vertsPerBlock * floatsPerVertex;
        const int estimatedIndices = Size * Size * Size * indicesPerBlock;

        private Block GetBlockSafe(sbyte x, sbyte y, sbyte z)
        {
            int px = x + 1;
            int py = y + 1;
            int pz = z + 1;
            if (px >= 0 && px < 34 && py >= 0 && py < 34 && pz >= 0 && pz < 34)
            {
                return _paddedBlocks[px, py, pz];
            }
            return null;
        }

        public MeshData GenerateMeshData()
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
                        var block = _paddedBlocks[x + 1, y + 1, z + 1];
                        if (block == null || block.IsAir) continue;

                        byte m = block.Mass;
                        mass += m;
                        if (x < xMin) xMin = x;
                        if (x > xMax) xMax = x;
                        if (y < yMin) yMin = y;
                        if (y > yMax) yMax = y;
                        if (z < zMin) zMin = z;
                        if (z > zMax) zMax = z;
                        sumPosMass += new Vector3(x, y, z) * m;

                        var transformedVectors = BlockRotationHelper.GetTransformedFaceVectors(
                            GameAssets.GetBaseFrontDirection(block.Id),
                            block.Direction,
                            block.Rotation
                        );

                        for (byte fIndex = 0; fIndex < faces.Length; fIndex++)
                        {
                            AddRotatedFace(block, faces[fIndex], fIndex, x, y, z, transformedVectors);
                        }
                    }
                }
            }

            float[] finalVertices = new float[vertexCount];
            Array.Copy(vertices, finalVertices, vertexCount);
            uint[] finalIndices = new uint[indexCount];
            Array.Copy(indices, finalIndices, indexCount);

            if (_measureGenerationTime && stopwatch != null)
            {
                stopwatch.Stop();
                Engine.Debug.Success($"Chunk mesh generation time: {stopwatch.ElapsedMilliseconds} ms");
            }

            return new MeshData
            {
                Vertices = finalVertices,
                Indices = finalIndices,
                GeometryBoundingBox = BoundingBox.CreateFromMinMax(new Vector3(xMin, yMin, zMin), new Vector3(xMax + 1, yMax + 1, zMax + 1)),
                Mass = mass,
                SumPosMass = sumPosMass
            };
        }

        private void AddRotatedFace(Block block, Face face, byte fIndex, sbyte x, sbyte y, sbyte z, Dictionary<Direction, Vector3> transformedVectors)
        {
            Direction faceDir = (Direction)face;
            Vector3 transformedNormal = transformedVectors[faceDir];
            Vector3SByte normal = new Vector3SByte(
                (sbyte)Math.Round(transformedNormal.X),
                (sbyte)Math.Round(transformedNormal.Y),
                (sbyte)Math.Round(transformedNormal.Z)
            );

            sbyte nx = (sbyte)(x + normal.X);
            sbyte ny = (sbyte)(y + normal.Y);
            sbyte nz = (sbyte)(z + normal.Z);

            if (block.IsTransparent && IsInRange(nx, ny, nz))
            {
                var nb = _paddedBlocks[nx + 1, ny + 1, nz + 1];
                if (nb != null && nb.IsTransparent) return;
            }

            if (!IsTransparentBlock(nx, ny, nz, normal, block.IsTransparent)) return;

            Vector3 up = BlockRotationHelper.GetFaceUp(transformedVectors, faceDir);
            Vector3 right = BlockRotationHelper.GetFaceRight(transformedVectors, faceDir);

            Vector3 blockCenter = new Vector3(0.5f, 0.5f, 0.5f);
            Vector3 faceCenter = blockCenter + transformedNormal * 0.5f;
            Vector3 upVector = up * 0.5f;
            Vector3 rightVector = right * 0.5f;

            Vector3[] faceVertices = new Vector3[4];
            faceVertices[0] = faceCenter - upVector - rightVector;
            faceVertices[1] = faceCenter - upVector + rightVector;
            faceVertices[2] = faceCenter + upVector + rightVector;
            faceVertices[3] = faceCenter + upVector - rightVector;

            var faceUVs = GameAssets.GetBlockUVs(block, face);

            var currentLightLevel = block.LightLevel / 15f;
            var currentLightColor = block.LightColor;
            float neighborLightLevel = 0f;
            Color3Byte neighborLightColor = Color3Byte.Zero;

            var neighborBlock = GetBlockSafe(nx, ny, nz);
            if (neighborBlock != null)
            {
                neighborLightLevel = neighborBlock.LightLevel / 15f;
                neighborLightColor = neighborBlock.LightColor;
            }

            var averageLightColor = (currentLightColor.ToVector3() * currentLightLevel + neighborLightColor.ToVector3() * neighborLightLevel)
                                    / (currentLightLevel + neighborLightLevel + 0.001f);
            Vector3 ambient = new Vector3(0.2f, 0.2f, 0.2f);
            var vertexColor = Vector3.Clamp(block.Color * (averageLightColor + ambient), Vector3.Zero, Vector3.One);

            int vStart = vertexCount / BuffersData.FloatsPerVertexBlock;
            float[] AO = new float[4];

            bool isLightOrTransparent = block.IsTransparent || block.IsLight;

            if (!isLightOrTransparent && _EnableAO)
            {
                byte aoMask = CalculateFaceAOMask(new Vector3SByte(x, y, z), normal, up, right);
                var shading = AOShading.GetAO(aoMask);
                AO[0] = shading[0];
                AO[1] = shading[1];
                AO[2] = shading[2];
                AO[3] = shading[3];
            }
            else
            {
                AO[0] = AO[1] = AO[2] = AO[3] = 1f;
            }

            for (byte i = 0; i < 4; i++)
            {
                vertices[vertexCount++] = faceVertices[i].X + x;
                vertices[vertexCount++] = faceVertices[i].Y + y;
                vertices[vertexCount++] = faceVertices[i].Z + z;
                vertices[vertexCount++] = faceUVs[i].X;
                vertices[vertexCount++] = faceUVs[i].Y;
                vertices[vertexCount++] = vertexColor.X;
                vertices[vertexCount++] = vertexColor.Y;
                vertices[vertexCount++] = vertexColor.Z;
                vertices[vertexCount++] = normal.X;
                vertices[vertexCount++] = normal.Y;
                vertices[vertexCount++] = normal.Z;
                vertices[vertexCount++] = AO[i];
                vertices[vertexCount++] = block.EnableEmission ? 1f : 0f;
            }

            if (_EnableAO && !isLightOrTransparent)
            {
                bool flip = (AO[0] + AO[2]) < (AO[1] + AO[3]);
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

        private byte CalculateFaceAOMask(Vector3SByte blockPos, Vector3SByte normal, Vector3 up, Vector3 right)
        {
            byte mask = 0;
            Vector3SByte facePos = blockPos + normal;

            Vector3SByte[] offsets = new Vector3SByte[8];
            offsets[0] = RoundToSByte(-up);
            offsets[1] = RoundToSByte(-up - right);
            offsets[2] = RoundToSByte(-right);
            offsets[3] = RoundToSByte(up - right);
            offsets[4] = RoundToSByte(up);
            offsets[5] = RoundToSByte(up + right);
            offsets[6] = RoundToSByte(right);
            offsets[7] = RoundToSByte(-up + right);

            for (byte i = 0; i < 8; i++)
            {
                sbyte checkX = (sbyte)(facePos.X + offsets[i].X);
                sbyte checkY = (sbyte)(facePos.Y + offsets[i].Y);
                sbyte checkZ = (sbyte)(facePos.Z + offsets[i].Z);

                if (IsOccluder(checkX, checkY, checkZ))
                {
                    mask |= (byte)(1 << i);
                }
            }

            return mask;
        }

        private Vector3SByte RoundToSByte(Vector3 v)
        {
            return new Vector3SByte(
                (sbyte)Math.Round(v.X),
                (sbyte)Math.Round(v.Y),
                (sbyte)Math.Round(v.Z)
            );
        }

        private bool IsOccluder(sbyte x, sbyte y, sbyte z)
        {
            var b = GetBlockSafe(x, y, z);
            if (b == null || b.IsAir || b.IsTransparent) return false;
            return true;
        }

        private byte CreateMask(Face face, Vector3SByte blockPos, Vector3SByte normal)
        {
            byte mask = 0;
            Vector3SByte[] nbs = AOVoxels.FaceNeighborOffsets[face];
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
            var b = GetBlockSafe(x, y, z);
            if (b != null)
            {
                if (b.IsAir) return false;
                if (b.IsTransparent) return false;
                if (b.LightLevel > 0) return false;
                return true;
            }
            return false;
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
            var b = GetBlockSafe(x, y, z);
            return b != null && b.LightLevel > 0;
        }

        private bool IsTransparentBlock(sbyte x, sbyte y, sbyte z, Vector3SByte normal, bool currentTransparent)
        {
            var b = GetBlockSafe(x, y, z);
            if (b != null)
            {
                return currentTransparent ? b.IsAir : (b.IsAir || b.IsTransparent);
            }
            return true;
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