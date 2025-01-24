﻿using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.Common.Physics;
using Spacebox.Game.Physics;


namespace Spacebox.Game.Generation
{
    public class Chunk : IDisposable
    {

        public const byte Size = 32; // 32700+ blocks

        public int Mass { get; set; } = 0; // 255x32700 = 8,338,500 (max 4,294,967,295 in uint)

        public Vector3 SumPosMass { get; set; } = Vector3.Zero;

        public Vector3 PositionWorld { get; private set; }
        public Vector3SByte PositionIndex { get; private set; }
        public bool NeedsToRegenerateMesh = false;
        public Block[,,] Blocks { get; private set; }
        public bool ShowChunkBounds { get; set; } = true;
        public bool MeasureGenerationTime { get; set; } = true;
        private bool _isModified = false;
        public bool IsModified
        {
            get => _isModified;
            set
            {
                _isModified = value;

                if (_isModified) SpaceEntity.SetModified();
            }
        }

        public bool IsGenerated { get; private set; } = false;

        private Mesh _mesh;
        private readonly MeshGenerator _meshGenerator;
        private readonly LightManager _lightManager;
        private bool _isLoadedOrGenerated = false;

        private bool needsToRegenerateMesh = false;

        public SpaceEntity SpaceEntity { get; private set; }
        private BoundingBox boundingBox;
        public BoundingBox GeometryBoundingBox { get; private set; }

        public Action<Chunk> OnChunkModified;

        public Chunk(Vector3SByte positionIndex, SpaceEntity spaceEntity, bool emptyChunk = false)
            : this(positionIndex, spaceEntity, null, isLoaded: false, emptyChunk)
        {
        }

        internal Chunk(Vector3SByte positionIndex, SpaceEntity spaceEntity, Block[,,] loadedBlocks, bool isLoaded, bool emptyChunk)
        {
            PositionWorld = GetChunkWorldPosition(positionIndex, spaceEntity.PositionWorld);
            PositionIndex = positionIndex;
            Blocks = new Block[Size, Size, Size];

            SpaceEntity = spaceEntity;

            CreateBoundingBox();
            GeometryBoundingBox = new BoundingBox(boundingBox);

            if (!emptyChunk)
            {
                if (isLoaded && loadedBlocks != null)
                {
                    Array.Copy(loadedBlocks, Blocks, loadedBlocks.Length);
                    IsGenerated = true;
                }
                else
                {
                    BlockGenerator blockGenerator = new BlockGeneratorSphere(this, PositionWorld);
                    blockGenerator.Generate();
                    IsGenerated = true;
                }
            }
            else
            {
                BlockGenerator blockGenerator = new BlockGeneratorEmptyChunk(this, PositionWorld);
                blockGenerator.Generate();
                IsGenerated = true;
                IsModified = true;
            }

            _lightManager = new LightManager(this);
            _lightManager.PropagateLight();

            _meshGenerator = new MeshGenerator(this, Neighbors, MeasureGenerationTime);


            _isLoadedOrGenerated = true;
        }

        public static Vector3SByte GetChunkIndex(Vector3 worldPosition, Vector3 entityWorldPosition)
        {
            Vector3 relativePosition = worldPosition - entityWorldPosition;

            int indexX = (int)MathF.Floor(relativePosition.X / Chunk.Size);
            int indexY = (int)MathF.Floor(relativePosition.Y / Chunk.Size);
            int indexZ = (int)MathF.Floor(relativePosition.Z / Chunk.Size);

            return new Vector3SByte((sbyte)indexX, (sbyte)indexY, (sbyte)indexZ);
        }

        public static Vector3 GetChunkWorldPosition(Vector3SByte chunkIndex, Vector3 spaceEntityPosition)
        {
            return new Vector3(
                spaceEntityPosition.X + chunkIndex.X * Chunk.Size,
                spaceEntityPosition.Y + chunkIndex.Y * Chunk.Size,
                spaceEntityPosition.Z + chunkIndex.Z * Chunk.Size
            );
        }

        private void CreateBoundingBox()
        {
            Vector3 chunkMin = PositionWorld;
            Vector3 chunkMax = PositionWorld + new Vector3(Size);
            boundingBox = BoundingBox.CreateFromMinMax(chunkMin, chunkMax);
        }

        public int GetNeigborCount()
        {
            var count = 0;
            foreach (var n in Neighbors)
            {
                if (n.Value != null) count++;
            }

            return count;
        }

        public void GenerateMesh()
        {
            GenerateMesh(true);
        }
        public void GenerateMesh(bool doLight)
        {
            if (!_isLoadedOrGenerated) return;
            needsToRegenerateMesh = false;
            if (doLight)
            _lightManager.PropagateLight();
            int oldMass = Mass;

            Mesh newMesh = _meshGenerator.GenerateMesh();

            if (Mass == 0)
            {
                SpaceEntity.RecalculateMass(Mass - oldMass);
                _mesh?.Dispose();
                newMesh?.Dispose();
                DeleteChunk();

                return;
            }

            _mesh?.Dispose();
            _mesh = newMesh;

            GeometryBoundingBox = BoundingBox.CreateFromMinMax(
                boundingBox.Min + _meshGenerator.GeometryBoundingBox.Min,
                boundingBox.Min + _meshGenerator.GeometryBoundingBox.Max);

            SpaceEntity.RecalculateMass(Mass - oldMass);
            OnChunkModified?.Invoke(this);
           
        }

        private void DeleteChunk()
        {
            SpaceEntity.RemoveChunk(this);
        }

        public bool IsColliding(BoundingVolume volume)
        {
            return VoxelPhysics.IsColliding(volume, Blocks, PositionWorld);
        }

        public void Render(Shader shader)
        {
            if (!_isLoadedOrGenerated) return;

            if (needsToRegenerateMesh)
            {
                GenerateMesh();
                needsToRegenerateMesh = false;
            }

            Vector3 relativePosition = PositionWorld - Camera.Main.Position;

            Vector3 position = Camera.Main.CameraRelativeRender ? relativePosition : PositionWorld;


            Matrix4 model = Matrix4.CreateTranslation(position);
            shader.SetMatrix4("model", model);

            if (_mesh != null)
                _mesh.Draw(shader);

            if (ShowChunkBounds && VisualDebug.ShowDebug)
            {
                VisualDebug.DrawBoundingBox(boundingBox, new Color4(0.5f, 0f, 0.5f, 0.4f));
            }
        }

        public void AddNeighbor(Chunk neighbor)
        {
            Vector3SByte direction = neighbor.PositionIndex - this.PositionIndex;

            Neighbors[direction] = neighbor;
        }

        public void RemoveNeighbor(Chunk neighbor)
        {
            Vector3SByte direction = neighbor.PositionIndex - this.PositionIndex;

            Neighbors[direction] = null;
        }

        public void PlaceBlock(Vector3Byte pos, Block block)
        {
            PlaceBlock(pos.X, pos.Y, pos.Z, block);
        }

        public void PlaceBlock(int x, int y, int z, Block block)
        {
            if (!IsInRange(x, y, z))
            {
                return;
            }
            else
            {
                // place in another chunk or create new one
            }


            Blocks[x, y, z] = block;

            IsModified = true;

            if (block.LightLevel > 0f)
            {
                Blocks[x, y, z].LightLevel = block.LightLevel;
                Blocks[x, y, z].LightColor = block.LightColor;
            }

            CheckNeigborBlocks(new Vector3Byte(x, y, z));
            needsToRegenerateMesh = true;
        }

        public void RemoveBlock(Vector3Byte blockPos, Vector3SByte normal)
        {
            RemoveBlock(blockPos.X, blockPos.Y, blockPos.Z, normal.X, normal.Y, normal.Z);
        }

        public void RemoveBlock(byte x, byte y, byte z, sbyte xNormal, sbyte yNormal, sbyte zNormal)
        {
            if (!IsInRange(x, y, z))
                return;

            Vector3 worldBlockPosition = new Vector3(x, y, z) + PositionWorld;

            if (Blocks[x, y, z].IsTransparent)
            {
                World.DestructionManager.DestroyBlock(worldBlockPosition, Blocks[x, y, z].LightColor, Blocks[x, y, z]);
                World.DropEffectManager.DestroyBlock(worldBlockPosition, Blocks[x, y, z].LightColor, Blocks[x, y, z]);
            }
            else
            {
                if (IsInRange(x + xNormal, y + yNormal, z + zNormal))
                {
                    World.DestructionManager.DestroyBlock(worldBlockPosition,
                        Blocks[x + xNormal, y + yNormal, z + zNormal].LightColor, Blocks[x, y, z]);
                    World.DropEffectManager.DestroyBlock(worldBlockPosition,
                        Blocks[x + xNormal, y + yNormal, z + zNormal].LightColor, Blocks[x, y, z]);
                }
                else
                {
                    World.DestructionManager.DestroyBlock(worldBlockPosition, Blocks[x, y, z].LightColor,
                        Blocks[x, y, z]);
                    World.DropEffectManager.DestroyBlock(worldBlockPosition, Blocks[x, y, z].LightColor,
                        Blocks[x, y, z]);
                }
            }


            //Mass -= Blocks[x, y, z].Mass;
            Blocks[x, y, z] = GameBlocks.CreateBlockFromId(0);


            IsModified = true;
            CheckNeigborBlocks(new Vector3Byte(x, y, z));
            needsToRegenerateMesh = true;
        }

        public Block? GetBlock(Vector3SByte pos)
        {
            return GetBlock((int)pos.X, (int)pos.Y, (int)pos.Z);
        }

        public Block? GetBlock(Vector3Byte pos)
        {
            return GetBlock((int)pos.X, (int)pos.Y, (int)pos.Z);
        }

        public Block? GetBlock(Vector3 pos)
        {
            return GetBlock((int)pos.X, (int)pos.Y, (int)pos.Z);
        }

        public Block? GetBlock(int x, int y, int z)
        {
            if (IsInRange(x, y, z))
                return Blocks[x, y, z];
            return null;
        }

        private bool IsInRange(int x, int y, int z)
        {
            return x >= 0 && x < Size && y >= 0 && y < Size && z >= 0 && z < Size;
        }

        private void CheckNeigborBlocks(Vector3Byte pos)
        {
            if (pos.X == 0)
            {
                if (HasNeighbor(new Vector3SByte(-1, 0, 0), out Chunk neighbor))
                {
                    neighbor.GenerateMesh();
                }
            }

            if (pos.X == Size - 1)
            {
                if (HasNeighbor(new Vector3SByte(1, 0, 0), out Chunk neighbor))
                {
                    neighbor.GenerateMesh();
                }
            }

            if (pos.Y == 0)
            {
                if (HasNeighbor(new Vector3SByte(0, -1, 0), out Chunk neighbor))
                {
                    neighbor.GenerateMesh();
                }
            }

            if (pos.Y == Size - 1)
            {
                if (HasNeighbor(new Vector3SByte(0, 1, 0), out Chunk neighbor))
                {
                    neighbor.GenerateMesh();
                }
            }

            if (pos.Z == 0)
            {
                if (HasNeighbor(new Vector3SByte(0, 0, -1), out Chunk neighbor))
                {
                    neighbor.GenerateMesh();
                }
            }

            if (pos.Z == Size - 1)
            {
                if (HasNeighbor(new Vector3SByte(0, 0, 1), out Chunk neighbor))
                {
                    neighbor.GenerateMesh();
                }
            }
        }

        private bool HasNeighbor(Vector3SByte pos, out Chunk neighbor)
        {
            if (Neighbors.TryGetValue(pos, out neighbor))
            {
                return neighbor != null;
            }
            else
            {
                return false;
            }
        }

        public bool Raycast(Ray ray, out VoxelPhysics.HitInfo info)
        {
            info = new VoxelPhysics.HitInfo();

            if (!_isLoadedOrGenerated) return false;

            if (VoxelPhysics.Raycast(ray, PositionWorld, Blocks, out info))
            {
                info.chunk = this;
                return true;
            }

            return false;
        }

        private void ChangeBlockColor(Block block, Vector3 color, bool regenerateMesh)
        {
            block.Color = color;

            if (regenerateMesh)
            {
                GenerateMesh();
            }

            IsModified = true;
        }

        public void ClearChunk()
        {
            for (byte x = 0; x < Size; x++)
            {
                for (byte y = 0; y < Size; y++)
                {
                    for (byte z = 0; z < Size; z++)
                    {
                        Blocks[x, y, z] = new Block(new Vector2(0, 0));
                    }
                }
            }
            IsModified = true;
        }

        public Vector3 GetCenterOfMass()
        {
            if (Mass == 0f)
                return PositionWorld;


            var local = (SumPosMass / Mass) + new Vector3(0.5f, 0.5f, 0.5f);

            return PositionWorld + local;
        }

        public void Dispose()
        {
            _mesh?.Dispose();
        }


        public readonly Dictionary<Vector3SByte, Chunk> Neighbors = new Dictionary<Vector3SByte, Chunk>()
        {
            { new Vector3SByte(1, 0, 0), null },
            { new Vector3SByte(-1, 0, 0), null },

            { new Vector3SByte(0, 1, 0), null },
            { new Vector3SByte(0, -1, 0), null },

            { new Vector3SByte(0, 0, 1), null },
            { new Vector3SByte(0, 0, -1), null },
        };

       

    }
}