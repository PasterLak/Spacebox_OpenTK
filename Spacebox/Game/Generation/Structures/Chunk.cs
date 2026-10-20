using Engine;
using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Oxygen;
using Spacebox.Game.Generation.Doors;
using Spacebox.Game.Generation.Structures;
using Spacebox.Game.Physics;
using Spacebox.Game.Resource;
using System;
using System.Collections.Generic;

namespace Spacebox.Game.Generation
{
    public class Chunk : IDisposable, ISpaceStructure
    {
        public const byte Size = 32;
        public const byte SizeHalf = Size / 2;

        public static bool ShowChunkBounds { get; set; } = true;
        public static bool MeasureGenerationTime { get; set; } = false;

        public int Mass { get; set; } = 0;

        public Vector3 SumPosMass { get; set; } = Vector3.Zero;

        public Vector3 PositionWorld { get; private set; }
        public Vector3SByte PositionIndex { get; private set; }

        public Block[,,] Blocks { get; private set; }

        public int DataVersion { get; private set; } = 0;
        private int _meshRequestVersion;
        public int MeshVersion => DataVersion + Volatile.Read(ref _meshRequestVersion);
        public void RequestMesh() => Interlocked.Increment(ref _meshRequestVersion);
        public bool IsDisposed { get; private set; } = false;
        public bool IsQueuedForMeshUpdate { get; set; } = false;

        private bool _isModified = false;
        public bool IsModified
        {
            get => _isModified;
            set
            {
                _isModified = value;
                if (_isModified) SpaceEntity.IsModified = true;
            }
        }

        public bool IsGenerated { get; private set; } = false;
        public bool HasSavedData { get; private set; } = false;
        public bool ShouldBeSaved => IsModified || HasSavedData;

        private Mesh _mesh;
        private readonly LightManager _lightManager;
        private bool _isLoadedOrGenerated = false;

        public SpaceEntity SpaceEntity { get; private set; }
        public BoundingBox BoundingBox { get; private set; }
        public BoundingBox GeometryBoundingBox { get; private set; }

        public Action<Chunk> OnChunkModified;

        private LOD Lod = LOD.None;
        public enum LOD : short
        {
            None = 0, L0 = Settings.LOD0, L1 = Settings.LOD1, L2 = Settings.LOD2, L3 = Settings.LOD3
        }

        public Chunk(Vector3SByte positionIndex, SpaceEntity spaceEntity, bool emptyChunk = false)
            : this(positionIndex, spaceEntity, null, isLoaded: false, emptyChunk)
        {
        }

        internal Chunk(Vector3SByte positionIndex, SpaceEntity spaceEntity, Func<Chunk, BlockGenerator> createGenerator)
        {
            InitializeStructure(positionIndex, spaceEntity);

            createGenerator(this).Generate();
            IsGenerated = true;

            _lightManager = new LightManager(this);
            _lightManager.PropagateLight();

            _isLoadedOrGenerated = true;
        }

        internal Chunk(Vector3SByte positionIndex, SpaceEntity spaceEntity, Block[,,] loadedBlocks, bool isLoaded, bool emptyChunk)
        {
            InitializeStructure(positionIndex, spaceEntity);

            if (!emptyChunk)
            {
                if (isLoaded && loadedBlocks != null)
                {
                    Array.Copy(loadedBlocks, Blocks, loadedBlocks.Length);
                    RegisterElectricalBlocks();
                    IsGenerated = true;
                    HasSavedData = true;
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
            }

            _lightManager = new LightManager(this);
            _lightManager.PropagateLight();

            _isLoadedOrGenerated = true;
        }

        private void InitializeStructure(Vector3SByte positionIndex, SpaceEntity spaceEntity)
        {
            PositionWorld = GetChunkWorldPosition(positionIndex, spaceEntity.PositionWorld);
            PositionIndex = positionIndex;
            Blocks = new Block[Size, Size, Size];
            SpaceEntity = spaceEntity;

            CreateBoundingBox();
            GeometryBoundingBox = new BoundingBox(BoundingBox);
        }

        private Vector3i ToLocalCell(int x, int y, int z)
        {
            return new Vector3i(PositionIndex.X * Size + x, PositionIndex.Y * Size + y, PositionIndex.Z * Size + z);
        }

        public void CalculateLighting()
        {
            _lightManager.PropagateLight();
        }

        public Block[,,] CreatePaddedSnapshot()
        {
            var padded = PaddedArrayPools.Blocks.Rent();
            for (int x = -1; x <= Size; x++)
            {
                for (int y = -1; y <= Size; y++)
                {
                    for (int z = -1; z <= Size; z++)
                    {
                        padded[x + 1, y + 1, z + 1] = GetBlockFromThisOrNeighbor(x, y, z);
                    }
                }
            }
            return padded;
        }

        private Block GetBlockFromThisOrNeighbor(int x, int y, int z)
        {
            if (x >= 0 && x < Size && y >= 0 && y < Size && z >= 0 && z < Size)
            {
                return Blocks[x, y, z];
            }

            MeshGenerator.GetNeighborChunkIndexAndLocalCoords(x, y, z, Size, out var offset, out var local);
            if (HasNeighbor(offset, out var neighbor))
            {
                return neighbor.Blocks[local.X, local.Y, local.Z];
            }

            return GameAssets.CreateBlockFromId(0);
        }

        public void ApplyMeshData(MeshData data)
        {
            if (IsDisposed) return;

            int oldMass = Mass;

            _mesh?.Dispose();
            if (data.Vertices.Length > 0 && data.Indices.Length > 0)
            {
                _mesh = new Mesh(data.Vertices, data.Indices, BuffersData.CreateBlockBuffer());
            }
            else
            {
                _mesh = null;
            }

            GeometryBoundingBox = BoundingBox.CreateFromMinMax(
                BoundingBox.Min + data.GeometryBoundingBox.Min,
                BoundingBox.Min + data.GeometryBoundingBox.Max);

            Mass = data.Mass;
            SumPosMass = data.SumPosMass;

            IsQueuedForMeshUpdate = false;
            Lod = LOD.L0;

            SpaceEntity.RecalculateMass(Mass - oldMass);
            OnChunkModified?.Invoke(this);

            if (Mass == 0)
            {
                DeleteChunk();
            }
        }

        public void QueueMeshUpdate(UpdateReason reason = UpdateReason.PlayerAction)
        {
            SpaceEntity?.QueueChunkUpdate(this, reason);
        }

        public void RegisterElectricalBlocks()
        {
            for (int x = 0; x < Chunk.Size; x++)
            {
                for (int y = 0; y < Chunk.Size; y++)
                {
                    for (int z = 0; z < Chunk.Size; z++)
                    {
                        Block block = Blocks[x, y, z];

                        if (block is OxygenGeneratorBlock oxygenGenerator)
                            SpaceEntity.Oxygen.RegisterGenerator(this, x, y, z, oxygenGenerator);

                        if (block is DoorBlock door && !door.HasMultiblockOffset)
                            SpaceEntity.Doors.RegisterLoaded(this, x, y, z, door);

                        if (block is IEntityBlockListener listener)
                            listener.OnAddedToEntity(this, ToLocalCell(x, y, z));

                        if (block.Id != 0 && block is ElectricalBlock eBlock)
                        {
                            if (eBlock.EFlags == ElectricalFlags.None)
                                continue;

                            int worldX = PositionIndex.X * Size + x;
                            int worldY = PositionIndex.Y * Size + y;
                            int worldZ = PositionIndex.Z * Size + z;
                            var globalPos = (worldX, worldY, worldZ);
                            SpaceEntity.ElectricManager.AddBlockFast(globalPos, eBlock, this);
                        }
                    }
                }
            }
            SpaceEntity.ElectricManager.Rebuild();
        }

        public static Vector3 GetChunkWorldPosition(Vector3SByte chunkIndex, Vector3 spaceEntityPosition)
        {
            return new Vector3(
                spaceEntityPosition.X + chunkIndex.X * Chunk.Size,
                spaceEntityPosition.Y + chunkIndex.Y * Chunk.Size,
                spaceEntityPosition.Z + chunkIndex.Z * Chunk.Size
            );
        }

        internal void ShiftWorld(Vector3 delta)
        {
            PositionWorld += delta;
            BoundingBox.Center += delta;
            GeometryBoundingBox.Center += delta;
        }

        private void CreateBoundingBox()
        {
            Vector3 chunkMin = PositionWorld;
            Vector3 chunkMax = PositionWorld + new Vector3(Size);
            BoundingBox = BoundingBox.CreateFromMinMax(chunkMin, chunkMax);
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

        private void DeleteChunk()
        {
            SpaceEntity.RemoveChunk(this);
        }

        public bool IsColliding(BoundingVolume volume, out CollideInfo collideInfo)
        {
            bool b = VoxelPhysics.IsColliding(volume, this, PositionWorld, out collideInfo);
            collideInfo.chunk = this;
            return b;
        }

        public void Render(BlockMaterial material)
        {
            if (!_isLoadedOrGenerated || IsDisposed) return;

            Matrix4 model;
            if (SpaceEntity.HasRotation)
            {
                model = Matrix4.CreateTranslation(PositionWorld - SpaceEntity.PositionWorld)
                        * SpaceEntity.RotationMatrix
                        * Matrix4.CreateTranslation(RenderSpace.ToRender(SpaceEntity.PositionWorld));
            }
            else
            {
                model = Matrix4.CreateTranslation(RenderSpace.ToRender(PositionWorld));
            }

            material.Apply(model);

            if (_mesh != null)
            {
                _mesh.Render();
            }

            if (ShowChunkBounds && VisualDebug.Enabled)
            {
                VisualDebug.DrawBoundingBox(BoundingBox, new Color4(0.5f, 0f, 0.5f, 0.4f));
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

            var oldBlock = Blocks[x, y, z];
            if (block.Id == oldBlock.Id) return;

            Blocks[x, y, z] = block;
            SpaceEntity.Oxygen.OnBlockChanged(this, x, y, z, oldBlock, block);
            if (block is IEntityBlockListener addedListener) addedListener.OnAddedToEntity(this, ToLocalCell(x, y, z));
            IsModified = true;
            DataVersion++;

            if (block.LightLevel > 0f)
            {
                Blocks[x, y, z].LightLevel = block.LightLevel;
                Blocks[x, y, z].LightColor = block.LightColor;
            }

            CheckNeigborBlocks(new Vector3Byte(x, y, z));
            _lightManager.PropagateLight();
            QueueMeshUpdate(UpdateReason.PlayerAction);
        }

        public bool DamageBlock(HitInfo hit, byte damage, bool spawnDrop)
        {
            if (damage == 0) return false;
            if (!IsInRange(hit.blockPositionIndex.X, hit.blockPositionIndex.Y, hit.blockPositionIndex.Z))
                return false;

            var block = Blocks[hit.blockPositionIndex.X, hit.blockPositionIndex.Y, hit.blockPositionIndex.Z];

            if ((((short)block.Durability) - damage) > 0)
            {
                block.Durability -= damage;
            }
            else
            {
                block.Durability = 0;
                block.OnDestroy?.Invoke(hit);
                RemoveBlock(hit.blockPositionIndex.X, hit.blockPositionIndex.Y, hit.blockPositionIndex.Z,
                    hit.normal.X, hit.normal.Y, hit.normal.Z, spawnDrop);
                return true;
            }
            return false;
        }

        public void RemoveBlock(Vector3Byte blockPos, Vector3SByte normal)
        {
            RemoveBlock(blockPos.X, blockPos.Y, blockPos.Z, normal.X, normal.Y, normal.Z, true);
        }

        public void RemoveBlock(byte x, byte y, byte z, sbyte xNormal, sbyte yNormal, sbyte zNormal)
        {
            RemoveBlock(x, y, z, xNormal, yNormal, zNormal, true);
        }

        public void RemoveBlock(byte x, byte y, byte z, sbyte xNormal, sbyte yNormal, sbyte zNormal, bool spawnDrop)
        {
            if (!IsInRange(x, y, z))
                return;

            var block = Blocks[x, y, z];
            if (block.Id == 0) return;

            if (GameAssets.Blocks.TryGetValue(block.Id, out var blockData) && blockData.IsMultiblock)
            {
                SpaceEntity.RemoveStructure(this, x, y, z, new Vector3SByte(xNormal, yNormal, zNormal), spawnDrop);
                return;
            }

            RemoveBlockCell(x, y, z, xNormal, yNormal, zNormal, spawnDrop);
        }

        internal void RemoveBlockCell(byte x, byte y, byte z, sbyte xNormal, sbyte yNormal, sbyte zNormal, bool spawnDrop)
        {
            if (!IsInRange(x, y, z))
                return;

            var block = Blocks[x, y, z];
            if (block.Id == 0) return;

            var worldBlockPosition = new Vector3(x, y, z) + PositionWorld;

            Color3Byte effectLightColor = block.LightColor;

            if (!block.IsTransparent)
            {
                int nx = x + xNormal;
                int ny = y + yNormal;
                int nz = z + zNormal;

                if (IsInRange(nx, ny, nz))
                {
                    effectLightColor = Blocks[nx, ny, nz].LightColor;
                }
                else
                {
                    MeshGenerator.GetNeighborChunkIndexAndLocalCoords(nx, ny, nz, Size, out var offset, out var local);
                    if (Neighbors.TryGetValue(offset, out var nChunk) && nChunk != null)
                    {
                        var nBlock = nChunk.GetBlock(local);
                        if (nBlock != null)
                        {
                            effectLightColor = nBlock.LightColor;
                        }
                    }
                }
            }

            var effectPosition = SpaceEntity.BlockCenterToWorldCorner(worldBlockPosition);
            World.DestructionManager.DestroyBlock(effectPosition, effectLightColor, block);

            if (spawnDrop)
            {
                World.DropEffectManager.DropBlock(effectPosition, effectLightColor, block, SpaceEntity);
            }

            if (block is DoorBlock door && !door.HasMultiblockOffset)
            {
                SpaceEntity.Doors.Unregister(door);
            }

            ClearBlockSilently(x, y, z);
        }

        internal bool ClearCellForBulkEdit(byte x, byte y, byte z)
        {
            if (!IsInRange(x, y, z)) return false;

            var block = Blocks[x, y, z];
            if (block == null || block.IsAir) return false;

            Blocks[x, y, z] = GameAssets.CreateBlockFromId(0);
            IsModified = true;
            DataVersion++;
            return true;
        }

        public void FinishBulkEdit()
        {
            _lightManager.PropagateLight();
            QueueMeshUpdate(UpdateReason.PlayerAction);

            foreach (var neighbor in Neighbors.Values)
            {
                if (neighbor != null && !neighbor.IsDisposed) neighbor.QueueMeshUpdate(UpdateReason.PlayerAction);
            }
        }

        internal void ClearBlockSilently(byte x, byte y, byte z)
        {
            if (!IsInRange(x, y, z))
                return;

            var block = Blocks[x, y, z];
            if (block == null || block.Id == 0) return;

            if (block is ElectricalBlock)
            {
                SpaceEntity.ElectricManager.RemoveBlock(this, new Vector3Byte(x, y, z));
            }

            if (block is IEntityBlockListener removedListener) removedListener.OnRemovedFromEntity();

            Blocks[x, y, z] = GameAssets.CreateBlockFromId(0);
            SpaceEntity.Oxygen.OnBlockChanged(this, x, y, z, block, Blocks[x, y, z]);
            IsModified = true;
            DataVersion++;

            CheckNeigborBlocks(new Vector3Byte(x, y, z));
            _lightManager.PropagateLight();
            QueueMeshUpdate(UpdateReason.PlayerAction);
        }

        private static int _pendingLodJobs;
        public static int PendingLodJobs => Volatile.Read(ref _pendingLodJobs);

        private void CreateLOD(LOD lod)
        {
            if (Application.IsHeadless) return;

            int downscale;
            Vector2[] uv;
            switch (lod)
            {
                case LOD.L1:
                    downscale = 2;
                    uv = GameAssets.AtlasBlocks.GetRegion(GameAtlasFactory.LodTexture).Uvs;
                    break;
                case LOD.L2:
                    downscale = 8;
                    uv = GameAssets.AtlasBlocks.GetRegion(GameAtlasFactory.LodTexture).Uvs;
                    break;
                case LOD.L3:
                    downscale = 16;
                    uv = GameAssets.AtlasBlocks.GetRegion(GameAtlasFactory.LodTexture).Uvs;
                    break;
                default: return;
            }
            var requested = lod;
            Interlocked.Increment(ref _pendingLodJobs);
            new ChunkLODMeshGenerator()
                .GenerateFromBlocksAsync(this.Blocks, downscale, uv, data =>
                {
                    Interlocked.Decrement(ref _pendingLodJobs);
                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        if (Lod == requested && !IsDisposed)
                        {
                            _mesh?.Dispose();
                            _mesh = new Mesh(data.Vertices, data.Indices, BuffersData.CreateBlockBuffer());
                        }
                    });
                }, () => Interlocked.Decrement(ref _pendingLodJobs));
        }

        public void SetLOD(int distanceSquared)
        {
            int lod0Threshold = ((int)LOD.L0) * ((int)LOD.L0);
            int lod1Threshold = ((int)LOD.L1) * ((int)LOD.L1);
            int lod2Threshold = ((int)LOD.L2) * ((int)LOD.L2);

            if (distanceSquared <= lod0Threshold)
            {
                if (Lod != LOD.L0)
                {
                    Lod = LOD.L0;
                    QueueMeshUpdate(UpdateReason.Generation);
                }
            }
            else if (distanceSquared <= lod1Threshold)
            {
                if (Lod != LOD.L1)
                {
                    Lod = LOD.L1;
                    CreateLOD(Lod);
                }
            }
            else if (distanceSquared <= lod2Threshold)
            {
                if (Lod != LOD.L2)
                {
                    Lod = LOD.L2;
                    CreateLOD(Lod);
                }
            }
            else
            {
                if (Lod != LOD.L3)
                {
                    Lod = LOD.L3;
                    CreateLOD(Lod);
                }
            }
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
            Span<Vector3SByte> touched = stackalloc Vector3SByte[ChunkBorder.MaxTouchedNeighbors];
            int count = ChunkBorder.GetTouchedNeighbors(pos, Size, touched);
            for (int i = 0; i < count; i++)
                TryUpdateNeighbor(touched[i]);
        }

        private void TryUpdateNeighbor(Vector3SByte offset)
        {
            if (HasNeighbor(offset, out Chunk neighbor))
            {
                neighbor.QueueMeshUpdate(UpdateReason.PlayerAction);
            }
        }

        private bool HasNeighbor(Vector3SByte pos, out Chunk neighbor)
        {
            if (Neighbors.TryGetValue(pos, out neighbor))
            {
                return neighbor != null;
            }
            return SpaceEntity != null && SpaceEntity.TryGetChunk(PositionIndex + pos, out neighbor) && !neighbor.IsDisposed;
        }

        public bool Raycast(Ray ray, out HitInfo info)
        {
            info = new HitInfo();
            if (!_isLoadedOrGenerated) return false;

            if (VoxelPhysics.RaycastChunk(ray, this, out info))
            {
                info.chunk = this;
                return true;
            }
            return false;
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
            IsDisposed = true;
            _mesh?.Dispose();
            OnChunkModified = null;
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