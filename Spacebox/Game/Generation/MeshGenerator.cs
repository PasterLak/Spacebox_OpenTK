using OpenTK.Mathematics;
using Engine;
using System.Diagnostics;
using Engine.Physics;
using Debug = Engine.Debug;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Shapes;
using Spacebox.Game.Generation.Tools;
using System;
using System.Collections.Generic;

namespace Spacebox.Game.Generation
{
    public class MeshData
    {
        public float[] Vertices;
        public uint[] Indices;
        public int VertexCount;
        public int IndexCount;
        public BoundingBox GeometryBoundingBox;
        public int Mass;
        public Vector3 SumPosMass;

        public bool HasGeometry => VertexCount > 0 && IndexCount > 0;

        public void Release()
        {
            if (Vertices != null) System.Buffers.ArrayPool<float>.Shared.Return(Vertices);
            if (Indices != null) System.Buffers.ArrayPool<uint>.Shared.Return(Indices);
            Vertices = null;
            Indices = null;
            VertexCount = 0;
            IndexCount = 0;
        }
    }

    public class MeshGenerator
    {
        private static bool _EnableAO = true;
        public static bool EnableAO { get => _EnableAO; set { _EnableAO = value; } }

        private const byte Size = Chunk.Size;
        private readonly Block[,,] _paddedBlocks;
        private readonly bool _measureGenerationTime;
        private readonly bool _buildGeometry = !Application.IsHeadless;
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

        private const int FloatsPerFace = 4 * BuffersData.FloatsPerVertexBlock;
        private const int IndicesPerFace = 6;

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
            var buffers = MeshBuildBuffers.Pool.Rent();
            try
            {
                vertices = buffers.Vertices;
                indices = buffers.Indices;
                return BuildMeshData();
            }
            finally
            {
                buffers.Vertices = vertices;
                buffers.Indices = indices;
                vertices = null;
                indices = null;
                MeshBuildBuffers.Pool.Return(buffers);
            }
        }

        private MeshData BuildMeshData()
        {
            if (_measureGenerationTime)
            {
                stopwatch = Stopwatch.StartNew();
            }

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

                        if (!_buildGeometry || block.IsMeshHidden) continue;

                        if (BlockShapes.TryGet(block, out var shape))
                        {
                            AddShape(block, shape, x, y, z);
                            continue;
                        }

                        var transformedVectors = BlockRotationHelper.GetCachedFaceVectors(
                            GameAssets.GetBaseFrontDirection(block.Id),
                            block.Direction,
                            block.Rotation
                        );

                        var multiblockData = GameAssets.Blocks.TryGetValue(block.Id, out var blockData) && blockData.IsMultiblock ? blockData : null;

                        for (byte fIndex = 0; fIndex < faces.Length; fIndex++)
                        {
                            AddRotatedFace(block, faces[fIndex], fIndex, x, y, z, transformedVectors, multiblockData);
                        }
                    }
                }
            }

            float[] finalVertices = System.Buffers.ArrayPool<float>.Shared.Rent(Math.Max(1, vertexCount));
            Array.Copy(vertices, finalVertices, vertexCount);
            uint[] finalIndices = System.Buffers.ArrayPool<uint>.Shared.Rent(Math.Max(1, indexCount));
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
                VertexCount = vertexCount,
                IndexCount = indexCount,
                GeometryBoundingBox = BoundingBox.CreateFromMinMax(new Vector3(xMin, yMin, zMin), new Vector3(xMax + 1, yMax + 1, zMax + 1)),
                Mass = mass,
                SumPosMass = sumPosMass
            };
        }

        private void AddRotatedFace(Block block, Face face, byte fIndex, sbyte x, sbyte y, sbyte z, IReadOnlyDictionary<Direction, Vector3> transformedVectors, Resource.BlockData multiblockData)
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

            EnsureCapacity(FloatsPerFace, IndicesPerFace);

            Vector3[] faceVertices = _faceVertices;
            faceVertices[0] = faceCenter - upVector - rightVector;
            faceVertices[1] = faceCenter - upVector + rightVector;
            faceVertices[2] = faceCenter + upVector + rightVector;
            faceVertices[3] = faceCenter + upVector - rightVector;

            var faceUVs = GameAssets.GetBlockUVs(block, face);
            if (multiblockData != null)
                faceUVs = MultiblockLayout.GetFaceUVs(faceUVs, multiblockData.GetFaceTiles(face), multiblockData.Size, block.MultiblockOffset, faceDir);

            CalculateVertexColors(block, nx, ny, nz, RoundToSByte(up), RoundToSByte(right));

            int vStart = vertexCount / BuffersData.FloatsPerVertexBlock;
            float[] AO = _faceAO;

            bool isLightOrTransparent = block.IsTransparent || block.IsLight;

            if (!isLightOrTransparent && _EnableAO)
            {
                byte aoMask = CalculateFaceAOMask(new Vector3SByte(x, y, z), normal, up, right);
                for (int i = 0; i < 4; i++)
                    AO[i] = AOShading.Get(aoMask, i);
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
                vertices[vertexCount++] = _vertexColors[i].X;
                vertices[vertexCount++] = _vertexColors[i].Y;
                vertices[vertexCount++] = _vertexColors[i].Z;
                vertices[vertexCount++] = normal.X;
                vertices[vertexCount++] = normal.Y;
                vertices[vertexCount++] = normal.Z;
                vertices[vertexCount++] = AO[i];
                vertices[vertexCount++] = block.EnableEmission ? 1f : 0f;
            }

            if (_EnableAO && !isLightOrTransparent)
            {
                bool flip = AOShading.ShouldFlip(AO[0], AO[1], AO[2], AO[3]);
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

        private void AddShape(Block block, BlockShape shape, sbyte x, sbyte y, sbyte z)
        {
            var multiblockData = GameAssets.Blocks.TryGetValue(block.Id, out var shapeData) && shapeData.IsMultiblock ? shapeData : null;
            if (multiblockData != null) shape = shape.ForCell(multiblockData.Size, block.MultiblockOffset);

            var support = shape.GetSupportOffset(block.Direction, block.Rotation);
            bool supported = BlockShapes.CanSupport(GetBlockSafe((sbyte)(x + support.X), (sbyte)(y + support.Y), (sbyte)(z + support.Z)));
            var pose = shape.GetPose(block.Direction, block.Rotation, supported);
            var geometry = shape.Geometry;

            int vertexTotal = geometry.VertexCount;
            EnsureCapacity(vertexTotal * BuffersData.FloatsPerVertexBlock, geometry.Indices.Length);

            var color = Vector3.Clamp(block.Color * (block.LightColor.ToVector3() + Ambient), Vector3.Zero, Vector3.One);
            float emission = block.EnableEmission ? 1f : 0f;
            int vStart = vertexCount / BuffersData.FloatsPerVertexBlock;

            for (int f = 0; f < faces.Length; f++)
            {
                var faceUVs = GameAssets.GetBlockUVs(block, faces[f]) ?? BlockShapeTexturing.DefaultFaceUVs;
                if (multiblockData != null)
                    faceUVs = MultiblockLayout.GetFaceUVs(faceUVs, multiblockData.GetFaceTiles(faces[f]), multiblockData.Size, block.MultiblockOffset, (Direction)faces[f]);
                _shapeFaceUVs[(int)faces[f]] = faceUVs;
            }

            for (int i = 0; i < vertexTotal; i++)
            {
                var position = pose.Positions[i];
                var normal = pose.Normals[i];
                var uv = BlockShapeTexturing.MapToFace(_shapeFaceUVs[(int)shape.VertexFaces[i]], geometry.TexCoords[i]);

                vertices[vertexCount++] = position.X + x;
                vertices[vertexCount++] = position.Y + y;
                vertices[vertexCount++] = position.Z + z;
                vertices[vertexCount++] = uv.X;
                vertices[vertexCount++] = uv.Y;
                vertices[vertexCount++] = color.X;
                vertices[vertexCount++] = color.Y;
                vertices[vertexCount++] = color.Z;
                vertices[vertexCount++] = normal.X;
                vertices[vertexCount++] = normal.Y;
                vertices[vertexCount++] = normal.Z;
                vertices[vertexCount++] = 1f;
                vertices[vertexCount++] = emission;
            }

            var shapeIndices = geometry.Indices;
            for (int i = 0; i < shapeIndices.Length; i++)
                indices[indexCount++] = (uint)(vStart + shapeIndices[i]);
        }

        private readonly Vector2[][] _shapeFaceUVs = new Vector2[6][];
        private readonly float[] _faceAO = new float[4];
        private readonly Vector3[] _faceVertices = new Vector3[4];
        private readonly Vector3SByte[] _aoOffsets = new Vector3SByte[8];

        private void EnsureCapacity(int extraFloats, int extraIndices)
        {
            if (vertexCount + extraFloats > vertices.Length)
                Array.Resize(ref vertices, Math.Max(vertices.Length * 2, vertexCount + extraFloats));
            if (indexCount + extraIndices > indices.Length)
                Array.Resize(ref indices, Math.Max(indices.Length * 2, indexCount + extraIndices));
        }

        private static readonly Vector3 Ambient = new Vector3(0.2f, 0.2f, 0.2f);
        private static readonly sbyte[] CornerUp = { -1, -1, 1, 1 };
        private static readonly sbyte[] CornerRight = { -1, 1, 1, -1 };
        private readonly Vector3[] _vertexColors = new Vector3[4];

        private void CalculateVertexColors(Block block, sbyte fx, sbyte fy, sbyte fz, Vector3SByte up, Vector3SByte right)
        {
            TrySampleLight(fx, fy, fz, out var center);
            var ownLight = block.IsLight ? block.LightColor.ToVector3() : Vector3.Zero;

            for (int i = 0; i < 4; i++)
            {
                int su = CornerUp[i];
                int sr = CornerRight[i];

                var sum = center;
                int count = 1;

                bool hasUp = TrySampleLight((sbyte)(fx + up.X * su), (sbyte)(fy + up.Y * su), (sbyte)(fz + up.Z * su), out var upLight);
                if (hasUp) { sum += upLight; count++; }

                bool hasRight = TrySampleLight((sbyte)(fx + right.X * sr), (sbyte)(fy + right.Y * sr), (sbyte)(fz + right.Z * sr), out var rightLight);
                if (hasRight) { sum += rightLight; count++; }

                if ((hasUp || hasRight) &&
                    TrySampleLight((sbyte)(fx + up.X * su + right.X * sr), (sbyte)(fy + up.Y * su + right.Y * sr), (sbyte)(fz + up.Z * su + right.Z * sr), out var cornerLight))
                {
                    sum += cornerLight;
                    count++;
                }

                var light = Vector3.ComponentMax(sum / count, ownLight);
                _vertexColors[i] = Vector3.Clamp(block.Color * (light + Ambient), Vector3.Zero, Vector3.One);
            }
        }

        private bool TrySampleLight(sbyte x, sbyte y, sbyte z, out Vector3 light)
        {
            var b = GetBlockSafe(x, y, z);
            if (b == null || !(b.IsAir || b.IsTransparent || b.IsMeshHidden || b.HasCustomShape))
            {
                light = Vector3.Zero;
                return false;
            }

            light = b.LightColor.ToVector3();
            return true;
        }

        private byte CalculateFaceAOMask(Vector3SByte blockPos, Vector3SByte normal, Vector3 up, Vector3 right)
        {
            byte mask = 0;
            Vector3SByte facePos = blockPos + normal;

            Vector3SByte[] offsets = _aoOffsets;
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
            if (b == null || b.IsAir || b.IsTransparent || b.IsMeshHidden || b.HasCustomShape) return false;
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
                return currentTransparent ? (b.IsAir || b.IsMeshHidden || b.HasCustomShape) : (b.IsAir || b.IsTransparent || b.IsMeshHidden || b.HasCustomShape);
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