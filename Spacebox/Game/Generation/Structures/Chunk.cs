using Engine;
using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Structures;
using Spacebox.Game.Physics;
using Spacebox.Game.Resource;
using System.Collections.Concurrent;


namespace Spacebox.Game.Generation
{
    public class Chunk : IDisposable, ISpaceStructure
    {

        public const byte Size = 32; // 32700+ blocks
        public const byte SizeHalf = Size / 2;
        public int Mass { get; set; } = 0; // 255x32700 = 8,338,500 (max 4,294,967,295 in uint)

        public Vector3 SumPosMass { get; set; } = Vector3.Zero;

        public Vector3 PositionWorld { get; private set; }
        public Vector3SByte PositionIndex { get; private set; }
        
        public Block[,,] Blocks { get; private set; }
        public bool NeedsToRegenerateMesh { get; set; }
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

        internal Chunk(Vector3SByte positionIndex, SpaceEntity spaceEntity, Block[,,] loadedBlocks, bool isLoaded, bool emptyChunk)
        {
            PositionWorld = GetChunkWorldPosition(positionIndex, spaceEntity.PositionWorld);
            PositionIndex = positionIndex;
            Blocks = new Block[Size, Size, Size];

            SpaceEntity = spaceEntity;

            CreateBoundingBox();
            GeometryBoundingBox = new BoundingBox(BoundingBox);

            if (!emptyChunk)
            {
                if (isLoaded && loadedBlocks != null)
                {
                    Array.Copy(loadedBlocks, Blocks, loadedBlocks.Length);
                    RegisterElectricalBlocks();
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

        public void RegisterElectricalBlocks()
        {
            for (int x = 0; x < Chunk.Size; x++)
            {
                for (int y = 0; y < Chunk.Size; y++)
                {
                    for (int z = 0; z < Chunk.Size; z++)
                    {
                        Block block = Blocks[x, y, z];

                        if (block.Id != 0 && block is ElectricalBlock eBlock)
                        {

                            if (eBlock.EFlags == ElectricalFlags.None)
                                continue;

                            int worldX = PositionIndex.X * Size + x;
                            int worldY = PositionIndex.Y * Size + y;
                            int worldZ = PositionIndex.Z * Size + z;
                            var globalPos = (worldX, worldY, worldZ);
                            //eBlock.IsActive = false;
                            SpaceEntity.ElectricManager.AddBlockFast(globalPos, eBlock, this);
                        }
                    }
                }
            }
            SpaceEntity.ElectricManager.Rebuild();
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

        public void GenerateMesh()
        {
            GenerateMesh(true);
        }

        public void GenerateMesh(bool doLight)
        {
            
            if (!_isLoadedOrGenerated) return;
            NeedsToRegenerateMesh = false;
            // Debug.Log("Regen " + PositionIndex);
            if (doLight)
            {
                _lightManager.PropagateLight();
            }

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
                BoundingBox.Min + _meshGenerator.GeometryBoundingBox.Min,
                BoundingBox.Min + _meshGenerator.GeometryBoundingBox.Max);

            SpaceEntity.RecalculateMass(Mass - oldMass);
            OnChunkModified?.Invoke(this);

        }

        private void DeleteChunk()
        {
            SpaceEntity.RemoveChunk(this);
            // SpaceEntity.ChunksToDelete.Add(this);
        }

        public bool IsColliding(BoundingVolume volume, out CollideInfo collideInfo)
        {
            bool b = VoxelPhysics.IsColliding(volume, Blocks, PositionWorld, out collideInfo);
            collideInfo.chunk = this;
            return b;
        }

        public void Render(BlockMaterial material)
        {
           
            if (!_isLoadedOrGenerated) return;

            if (NeedsToRegenerateMesh)
            {
                GenerateMesh();
                NeedsToRegenerateMesh = false;
            }

            Vector3 position = RenderSpace.ToRender(PositionWorld);


            Matrix4 model = Matrix4.CreateTranslation(position);

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
            else
            {
                // place in another chunk or create new one
            }
            if (block.Id == Blocks[x, y, z].Id) return;

            Blocks[x, y, z] = block;

            IsModified = true;

            if (block.LightLevel > 0f)
            {
                Blocks[x, y, z].LightLevel = block.LightLevel;
                Blocks[x, y, z].LightColor = block.LightColor;
            }

            CheckNeigborBlocks(new Vector3Byte(x, y, z));
            NeedsToRegenerateMesh = true;
        }

        public bool DamageBlock(Vector3Byte blockPos, Vector3SByte normal, byte damage, bool spawnDrop)
        {
            if(damage == 0) return false;
            if (!IsInRange(blockPos.X, blockPos.Y, blockPos.Z))
                return false;

            var block = Blocks[blockPos.X, blockPos.Y, blockPos.Z];

            if ((((short)block.Durability) - damage) > 0)
            {
                block.Durability -= damage;
            }
            else
            {
                block.Durability = 0;
                RemoveBlock(blockPos.X, blockPos.Y, blockPos.Z, normal.X, normal.Y, normal.Z, spawnDrop);
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

            Vector3 worldBlockPosition = new Vector3(x, y, z) + PositionWorld;

            if (block.IsTransparent)
            {
                World.DestructionManager.DestroyBlock(worldBlockPosition, block.LightColor, block);
                if (spawnDrop)
                    World.DropEffectManager.DropBlock(worldBlockPosition, block.LightColor, block, SpaceEntity);
            }
            else
            {
                if (IsInRange(x + xNormal, y + yNormal, z + zNormal))
                {
                    World.DestructionManager.DestroyBlock(worldBlockPosition,
                        Blocks[x + xNormal, y + yNormal, z + zNormal].LightColor, block);
                    if (spawnDrop)
                        World.DropEffectManager.DropBlock(worldBlockPosition,
                            Blocks[x + xNormal, y + yNormal, z + zNormal].LightColor, block, SpaceEntity);
                }
                else
                {
                    World.DestructionManager.DestroyBlock(worldBlockPosition, block.LightColor,
                        block);
                    if (spawnDrop)
                        World.DropEffectManager.DropBlock(worldBlockPosition, block.LightColor,
                       block, SpaceEntity);
                }
            }

            if (block is ElectricalBlock)
            {
                SpaceEntity.ElectricManager.RemoveBlock(this, new Vector3Byte(x, y, z));
            }


            //Mass -= Blocks[x, y, z].Mass;
            Blocks[x, y, z] = GameAssets.CreateBlockFromId(0);


            IsModified = true;
            CheckNeigborBlocks(new Vector3Byte(x, y, z));
            NeedsToRegenerateMesh = true;
        }

        private void CreateLOD(LOD lod)
        {
            
            int downscale;
            Vector2[] uv;
            switch (lod)
            {
                case LOD.L1:
                    downscale = 2;
                    uv = GameAssets.AtlasBlocks.GetUVByName("LOD");
                    break;
                case LOD.L2:
                    downscale = 8;
                    uv = GameAssets.AtlasBlocks.GetUVByName("LOD");
                    break;
                case LOD.L3:
                    downscale = 16;
                    uv = GameAssets.AtlasBlocks.GetUVByName("LOD");
                    break;
                default: return;
            }
            var requested = lod;
            new ChunkLODMeshGenerator()
                .GenerateFromBlocksAsync(this.Blocks, downscale, uv, data =>
                {
                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        if (Lod == requested)
                        {
                            _mesh?.Dispose();
                            _mesh = new Mesh(data.Vertices, data.Indices, BuffersData.CreateBlockBuffer());
                        }
                    });
                });
        }



        public void SetLOD(int distanceSquared)
        {
            int lod0Threshold = ((int)LOD.L0) * ((int)LOD.L0);
            int lod1Threshold = ((int)LOD.L1) * ((int)LOD.L1);
            int lod2Threshold = ((int)LOD.L2) * ((int)LOD.L2);


            if (Lod != LOD.L3)
            {
             //   Lod = LOD.L3;
              //  CreateLOD(Lod);
            }
            //return;
            if (distanceSquared <= lod0Threshold)
            {
                if (Lod != LOD.L0)
                {
                    Lod = LOD.L0;
                    GenerateMesh(false);
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



    public class MainThreadDispatcher
    {
        private static MainThreadDispatcher _instance;
        public static MainThreadDispatcher Instance => _instance ?? (_instance = new MainThreadDispatcher());
        private ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();
        public void Enqueue(Action action) => _queue.Enqueue(action);
        public void ExecutePending()
        {
            while (_queue.TryDequeue(out var action))
                action();
        }
    }

}