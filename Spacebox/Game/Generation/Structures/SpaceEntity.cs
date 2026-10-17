using Engine;
using Engine.Multithreading;
using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game.Effects;
using Spacebox.Game.GameMath;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Oxygen;
using Spacebox.Game.Generation.Structures;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Resource;

using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System;

namespace Spacebox.Game.Generation
{
    public enum UpdateReason { PlayerAction = 0, Explosion = 1000, Lighting = 2000, Generation = 3000 }

    public class SpaceEntity : SpatialCell, IDisposable, ISpaceStructure
    {
        public const byte SizeChunks = 16;
        public const byte SizeChunksHalf = SizeChunks / 2;
        public const short SizeBlocks = SizeChunks * Chunk.Size;
        public const short SizeBlocksHalf = SizeChunks * Chunk.Size / 2;

        public readonly long EntityID;
        public ulong Mass { get; set; } = 0;

        public Octree<Chunk> Octree { get; private set; }

        private Vector3 sumPosCenterOfMass;
        public Vector3 CenterOfMass { get; private set; }
        public Vector3 CenterOfMassWorld => ToWorldSpace(CenterOfMass);

        public Quaternion Orientation { get; private set; } = Quaternion.Identity;
        public bool HasRotation { get; private set; }
        private Quaternion _inverseOrientation = Quaternion.Identity;
        private Matrix4 _rotationMatrix = Matrix4.Identity;
        private readonly Ray _entitySpaceRay = new Ray(Vector3.Zero, Vector3.UnitZ, 1f);
        private readonly BoundingSphere _entitySpaceSphere = new BoundingSphere(Vector3.Zero, 0f);
        private readonly BoundingBox _worldBoundsScratch = new BoundingBox(Vector3.Zero, Vector3.Zero);

        private bool _isModified = false;
        public bool IsModified
        {
            get => _isModified;
            set
            {
                _isModified = value;
                Sector.IsModified = true;
            }
        }
        public bool IsGenerated { get; set; } = false;

        public Sector Sector { get; private set; }

        public List<Chunk> Chunks { get; private set; } = new List<Chunk>();
        private readonly Dictionary<Vector3SByte, Chunk> _chunksByIndex = new Dictionary<Vector3SByte, Chunk>();
        private List<Chunk> MeshesTogenerate = new List<Chunk>();

        public PriorityQueue<Chunk, int> MeshUpdateQueue { get; private set; } = new PriorityQueue<Chunk, int>();
        public ConcurrentQueue<(Chunk chunk, MeshData data, int version)> ReadyMeshesQueue { get; private set; } = new ConcurrentQueue<(Chunk, MeshData, int)>();

        private Tag tag;

        private string _entityMassString = "0 tn";
        public BoundingBox GeometryBoundingBox { get; private set; }
        public ElectricNetworkManager ElectricManager { get; private set; }
        public OxygenSystem Oxygen { get; private set; }
        public Particle StarParticle;

        private StringBuilder StringBuilder = new StringBuilder();

        public SpaceEntity(long id, Vector3 positionWorld, Sector sector)
        {
            EntityID = id;
            Name = id.ToString();
            Position = positionWorld;
            PositionWorld = positionWorld;
            Sector = sector;
            BoundingBox = new BoundingBox(positionWorld, new Vector3(SizeBlocks, SizeBlocks, SizeBlocks));
            sumPosCenterOfMass = positionWorld;
            Octree = new Octree<Chunk>(SizeBlocks, Vector3.Zero, Chunk.Size, 1.0f);

            GeometryBoundingBox = new BoundingBox(positionWorld, Vector3.Zero);

            ElectricManager = new ElectricNetworkManager();
            Oxygen = new OxygenSystem(this);

            CalculateCenterOfMass();

            MainThreadDispatcher.Instance.Enqueue(() =>
            {
                if (TagManager.Instance != null)
                {
                    tag = TagManager.Instance.CreateTag("", CenterOfMassWorld, Color4.DarkGreen, false, GUI.Tag.Alignment.Right);
                }
            });

            CreateStar();
        }

        public bool IsDynamic()
        {
            return EntityID < 0;
        }

        public bool IsProcedural()
        {
            return EntityID >= 0;
        }

        private void CreateStar()
        {
        }

        public void CreateFirstBlock(Block block)
        {
            if (Chunks.Count != 0)
            {
                Debug.Error("[spaceEntity] CreateFirstBlock was not possible: A chunk already exists");
                return;
            }

            if (GameAssets.Blocks.TryGetValue(block.Id, out var blockData) && blockData.IsMultiblock)
            {
                PlaceStructureCells(Vector3.Zero, block, blockData);
                return;
            }

            Chunk chunk = new Chunk(new Vector3SByte(0, 0, 0), this, true);

            chunk.PlaceBlock(new Vector3Byte(0, 0, 0), block);
            AddChunk(chunk, false);
            chunk.QueueMeshUpdate(UpdateReason.Generation);
        }

        public void AddChunks(Chunk[] chunks, bool generateMesh)
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                var chunk = chunks[i];

                var local = SpaceMath.Entity.ChunkIndexToLocal(chunk.PositionIndex);

                Octree.Add(chunk, BoundingBox.CreateFromMinMax(local, local + Vector3.One * Chunk.Size));
                Chunks.Add(chunk);
                _chunksByIndex[chunk.PositionIndex] = chunk;

                chunk.OnChunkModified += UpdateEntityGeometryMinMax;

                UpdateNeighbors(chunk);
                OnChunkAdded(chunk);
            }

            for (int i = 0; i < chunks.Length; i++)
            {
                chunks[i].CalculateLighting();
            }

            for (int i = 0; i < chunks.Length; i++)
            {
                if (generateMesh)
                {
                    chunks[i].QueueMeshUpdate(UpdateReason.Generation);
                }
                else
                {
                    MeshesTogenerate.Add(chunks[i]);
                }
            }

            RecalculateGeometryBoundingBox();
            RecalculateMass();
        }

        public void GenerateMesh()
        {
            foreach (var chunk in MeshesTogenerate)
            {
                chunk.QueueMeshUpdate(UpdateReason.Generation);
            }
            MeshesTogenerate.Clear();
            IsGenerated = true;
        }

        public void AddChunk(Chunk chunk)
        {
            AddChunk(chunk, true);
        }

        public void AddChunk(Chunk chunk, bool generateMesh = true)
        {
            var local = SpaceMath.Entity.ChunkIndexToLocal(chunk.PositionIndex);

            Octree.Add(chunk, BoundingBox.CreateFromMinMax(local, local + Vector3.One * Chunk.Size));
            Chunks.Add(chunk);
            _chunksByIndex[chunk.PositionIndex] = chunk;

            chunk.OnChunkModified += UpdateEntityGeometryMinMax;
            UpdateNeighbors(chunk);
            OnChunkAdded(chunk);
            Oxygen.OnChunksChanged();
            RecalculateGeometryBoundingBox();
            RecalculateMass();

            chunk.CalculateLighting();

            if (generateMesh)
            {
                chunk.QueueMeshUpdate(UpdateReason.Generation);
            }
        }

        public void RemoveChunk(Chunk chunk)
        {
            Octree.Remove(chunk);
            Chunks.Remove(chunk);
            if (_chunksByIndex.TryGetValue(chunk.PositionIndex, out var indexed) && indexed == chunk)
                _chunksByIndex.Remove(chunk.PositionIndex);
            OnChunkRemoved(chunk);

            chunk.OnChunkModified -= UpdateEntityGeometryMinMax;
            chunk.Dispose();

            UpdateNeighbors(chunk, true);
            Oxygen.OnChunksChanged();
            RecalculateGeometryBoundingBox();
            RecalculateMass();
            if (Chunks.Count == 0 && CanBeDeletedWhenEmpty)
            {
                DeleteSpaceEntity();
            }
        }

        public bool TryGetChunk(Vector3SByte index, out Chunk chunk)
        {
            return _chunksByIndex.TryGetValue(index, out chunk);
        }

        public virtual bool SavesOnlyChangedChunks => false;
        public virtual IReadOnlyCollection<Vector3SByte> RemovedChunks => Array.Empty<Vector3SByte>();
        public virtual bool IsChunkGenerated(Vector3SByte index) => true;
        protected virtual bool CanBeDeletedWhenEmpty => true;

        protected virtual void OnChunkAdded(Chunk chunk)
        {
        }

        protected virtual void OnChunkRemoved(Chunk chunk)
        {
        }

        private void DeleteSpaceEntity()
        {
            Sector.DestroyEntity(this, true);
        }

        public void RecalculateMass(int chunkMassDifference)
        {
            var x = (long)Mass + chunkMassDifference;
            if (x >= 0)
            {
                Mass = (ulong)(x);
            }
            else
            {
                Mass = 0;
            }

            sumPosCenterOfMass = Vector3.Zero;

            for (int i = 0; i < Chunks.Count; i++)
            {
                sumPosCenterOfMass += Chunks[i].GetCenterOfMass() * Chunks[i].Mass;
            }

            CalculateCenterOfMass();
            CalculateGravityRadius();
            _entityMassString = Mass.ToString("N0").Replace(",", ".");
        }

        public void RecalculateMass()
        {
            Mass = 0;
            sumPosCenterOfMass = Vector3.Zero;

            for (int i = 0; i < Chunks.Count; i++)
            {
                var chunkMass = Chunks[i].Mass;
                Mass = (ulong)((long)Mass + chunkMass);

                sumPosCenterOfMass += Chunks[i].GetCenterOfMass() * chunkMass;
            }
            CalculateCenterOfMass();
            CalculateGravityRadius();
            _entityMassString = Mass.ToString("N0").Replace(",", ".");
        }

        private void UpdateEntityGeometryMinMax(Chunk chunk)
        {
            RecalculateGeometryBoundingBox();
            CalculateGravityRadius();
        }

        private void RecalculateGeometryBoundingBox()
        {
            GeometryBoundingBox = new BoundingBox(Vector3.Zero, Vector3.Zero);
            if (Chunks.Count == 0)
            {
                return;
            }

            Vector3 min = Chunks[0].GeometryBoundingBox.Min;
            Vector3 max = Chunks[0].GeometryBoundingBox.Max;

            foreach (var chunk in Chunks)
            {
                if (chunk.Mass > 0)
                {
                    min = Vector3.ComponentMin(min, chunk.GeometryBoundingBox.Min);
                    max = Vector3.ComponentMax(max, chunk.GeometryBoundingBox.Max);
                }
            }

            GeometryBoundingBox = BoundingBox.CreateFromMinMax(min, max);
        }

        private bool PlaceBlockInternal(Vector3 localPos, Block block)
        {
            int chunkX = (int)MathF.Floor(localPos.X / Chunk.Size);
            int chunkY = (int)MathF.Floor(localPos.Y / Chunk.Size);
            int chunkZ = (int)MathF.Floor(localPos.Z / Chunk.Size);
            Vector3SByte chunkIndex = new Vector3SByte((sbyte)chunkX, (sbyte)chunkY, (sbyte)chunkZ);

            int blockX = (int)MathF.Floor(localPos.X) - chunkX * Chunk.Size;
            int blockY = (int)MathF.Floor(localPos.Y) - chunkY * Chunk.Size;
            int blockZ = (int)MathF.Floor(localPos.Z) - chunkZ * Chunk.Size;
            Vector3Byte blockPos = new Vector3Byte((byte)blockX, (byte)blockY, (byte)blockZ);

            if (Octree.TryFindDataAtPosition(localPos + new Vector3(0.5f, 0.5f, 0.5f), out var chunk))
            {
                chunk.PlaceBlock(blockPos, block);

                if (block.Is<ElectricalBlock>(out var el))
                {
                    ElectricManager.AddBlock(((int)localPos.X, (int)localPos.Y, (int)localPos.Z), el, chunk);
                }
            }
            else
            {
                Chunk newChunk = new Chunk(chunkIndex, this, true);
                AddChunk(newChunk, false);
                newChunk.PlaceBlock(blockPos, block);

                if (block.Is<ElectricalBlock>(out var el))
                {
                    ElectricManager.AddBlock(((int)localPos.X, (int)localPos.Y, (int)localPos.Z), el, newChunk);
                }
            }
            return true;
        }

        public bool TryPlaceBlockLocal(Vector3 localBlockPosition, Block block)
        {
            Vector3 worldBlockPos = PositionWorld + localBlockPosition;
            return TryPlaceBlock(worldBlockPos, block);
        }

        public bool TryPlaceBlock(Vector3 worldPosition, Block block)
        {
            if (block.Id == 0)
                return true;

            if (GameAssets.Blocks.TryGetValue(block.Id, out var blockData) && blockData.IsMultiblock)
                return TryPlaceStructure(worldPosition, block, blockData);

            if (!IsPositionWithinEntitySize(worldPosition) || !IsPositionWithinGravityRadius(worldPosition))
                return false;

            Vector3 localPos = WorldPositionToLocal(worldPosition);
            return PlaceBlockInternal(localPos, block);
        }

        private static readonly List<MultiblockCell> _structureCells = new List<MultiblockCell>();

        public bool CanPlaceStructure(Vector3 originPosition, BlockData blockData, Direction direction, Rotation rotation)
        {
            MultiblockLayout.GetCells(blockData, direction, rotation, _structureCells);

            for (int i = 0; i < _structureCells.Count; i++)
            {
                var offset = _structureCells[i].Offset;
                var position = originPosition + new Vector3(offset.X, offset.Y, offset.Z);

                if (!IsPositionWithinEntitySize(position) || !IsPositionWithinGravityRadius(position) || !IsCellEmpty(position))
                    return false;
            }

            return true;
        }

        public bool IsCellEmpty(Vector3 position)
        {
            var local = WorldPositionToLocal(position);
            var cell = new Vector3i((int)MathF.Floor(local.X), (int)MathF.Floor(local.Y), (int)MathF.Floor(local.Z));
            if (!TryGetBlockLocal(cell, out var chunk, out var index)) return true;

            var block = chunk.Blocks[index.X, index.Y, index.Z];
            return block == null || block.IsAir;
        }

        public bool TryGetBlockLocal(Vector3i localCell, out Chunk chunk, out Vector3Byte indexInChunk)
        {
            int cx = (int)MathF.Floor(localCell.X / (float)Chunk.Size);
            int cy = (int)MathF.Floor(localCell.Y / (float)Chunk.Size);
            int cz = (int)MathF.Floor(localCell.Z / (float)Chunk.Size);
            indexInChunk = new Vector3Byte((byte)(localCell.X - cx * Chunk.Size), (byte)(localCell.Y - cy * Chunk.Size), (byte)(localCell.Z - cz * Chunk.Size));

            if (cx < sbyte.MinValue || cx > sbyte.MaxValue || cy < sbyte.MinValue || cy > sbyte.MaxValue || cz < sbyte.MinValue || cz > sbyte.MaxValue)
            {
                chunk = null;
                return false;
            }

            return TryGetChunk(new Vector3SByte((sbyte)cx, (sbyte)cy, (sbyte)cz), out chunk);
        }

        public bool TryGetStructureBounds(Chunk chunk, Vector3Byte index, out Vector3 minPosition, out Vector3 size)
        {
            minPosition = Vector3.Zero;
            size = Vector3.One;

            var block = chunk.Blocks[index.X, index.Y, index.Z];
            if (block == null || !GameAssets.Blocks.TryGetValue(block.Id, out var blockData) || !blockData.IsMultiblock)
                return false;

            var originLocal = GetCellLocal(chunk, index) - MultiblockLayout.GetOriginOffset(blockData, block);
            MultiblockLayout.GetCells(blockData, block.Direction, block.Rotation, _structureCells);

            var min = new Vector3i(int.MaxValue);
            var max = new Vector3i(int.MinValue);
            for (int i = 0; i < _structureCells.Count; i++)
            {
                min = Vector3i.ComponentMin(min, _structureCells[i].Offset);
                max = Vector3i.ComponentMax(max, _structureCells[i].Offset);
            }

            var minLocal = originLocal + min;
            minPosition = LocalPositionToWorld(new Vector3(minLocal.X, minLocal.Y, minLocal.Z));
            size = new Vector3(max.X - min.X + 1, max.Y - min.Y + 1, max.Z - min.Z + 1);
            return true;
        }

        public void RemoveStructure(Chunk chunk, byte x, byte y, byte z, Vector3SByte normal, bool spawnDrop)
        {
            var block = chunk.Blocks[x, y, z];
            if (!GameAssets.Blocks.TryGetValue(block.Id, out var blockData))
                return;

            var clickedLocal = GetCellLocal(chunk, new Vector3Byte(x, y, z));
            var originLocal = clickedLocal - MultiblockLayout.GetOriginOffset(blockData, block);
            short id = block.Id;
            var direction = block.Direction;
            var rotation = block.Rotation;

            MultiblockLayout.GetCells(blockData, direction, rotation, _structureCells);
            var cells = _structureCells.ToArray();
            bool clickedRemoved = false;

            for (int i = 0; i < cells.Length; i++)
            {
                var cellLocal = originLocal + cells[i].Offset;
                if (!TryGetBlockLocal(cellLocal, out var cellChunk, out var cellIndex)) continue;

                var cellBlock = cellChunk.Blocks[cellIndex.X, cellIndex.Y, cellIndex.Z];
                var model = cellBlock.MultiblockOffset;
                if (cellBlock.Id != id || cellBlock.Direction != direction || cellBlock.Rotation != rotation ||
                    model.X != cells[i].Model.X || model.Y != cells[i].Model.Y || model.Z != cells[i].Model.Z)
                    continue;

                bool isClicked = cellLocal == clickedLocal;
                clickedRemoved |= isClicked;
                cellChunk.RemoveBlockCell(cellIndex.X, cellIndex.Y, cellIndex.Z, normal.X, normal.Y, normal.Z, spawnDrop && isClicked);
            }

            if (!clickedRemoved && !chunk.Blocks[x, y, z].IsAir)
                chunk.RemoveBlockCell(x, y, z, normal.X, normal.Y, normal.Z, spawnDrop);
        }

        private static Vector3i GetCellLocal(Chunk chunk, Vector3Byte index)
        {
            return new Vector3i(
                chunk.PositionIndex.X * Chunk.Size + index.X,
                chunk.PositionIndex.Y * Chunk.Size + index.Y,
                chunk.PositionIndex.Z * Chunk.Size + index.Z);
        }

        private bool TryPlaceStructure(Vector3 originPosition, Block originBlock, BlockData blockData)
        {
            if (!CanPlaceStructure(originPosition, blockData, originBlock.Direction, originBlock.Rotation))
                return false;

            PlaceStructureCells(WorldPositionToLocal(originPosition), originBlock, blockData);
            return true;
        }

        private void PlaceStructureCells(Vector3 originLocal, Block originBlock, BlockData blockData)
        {
            MultiblockLayout.GetCells(blockData, originBlock.Direction, originBlock.Rotation, _structureCells);
            var cells = _structureCells.ToArray();

            for (int i = 0; i < cells.Length; i++)
            {
                var cell = cells[i];
                var cellBlock = cell.IsOrigin ? originBlock : GameAssets.CreateBlockFromId(originBlock.Id);
                cellBlock.Direction = originBlock.Direction;
                cellBlock.Rotation = originBlock.Rotation;
                cellBlock.MultiblockOffset = cell.Model;

                PlaceBlockInternal(originLocal + new Vector3(cell.Offset.X, cell.Offset.Y, cell.Offset.Z), cellBlock);
            }
        }

        public Vector3SByte GetChunkIndex(Vector3 worldPosition)
        {
            Vector3 relativePosition = worldPosition - PositionWorld;

            int indexX = (int)MathF.Floor(relativePosition.X / Chunk.Size);
            int indexY = (int)MathF.Floor(relativePosition.Y / Chunk.Size);
            int indexZ = (int)MathF.Floor(relativePosition.Z / Chunk.Size);

            return new Vector3SByte((sbyte)indexX, (sbyte)indexY, (sbyte)indexZ);
        }

        private static readonly Vector3SByte[] Directions = new Vector3SByte[]
        {
            new Vector3SByte(1, 0, 0),
            new Vector3SByte(-1, 0, 0),
            new Vector3SByte(0, 1, 0),
            new Vector3SByte(0, -1, 0),
            new Vector3SByte(0, 0, 1),
            new Vector3SByte(0, 0, -1)
        };

        public void UpdateNeighbors(Chunk chunk, bool removing = false)
        {
            foreach (var dir in Directions)
            {
                var index = (chunk.PositionIndex + dir);
                Vector3 neighborCoord = SpaceMath.Entity.ChunkIndexToLocal(index) + new Vector3(Chunk.SizeHalf, Chunk.SizeHalf, Chunk.SizeHalf);

                if (Octree.TryFindDataAtPosition(neighborCoord, out Chunk neighbor))
                {
                    if (removing)
                    {
                        neighbor.RemoveNeighbor(chunk);
                        chunk.RemoveNeighbor(neighbor);
                    }
                    else
                    {
                        neighbor.AddNeighbor(chunk);
                        chunk.AddNeighbor(neighbor);
                    }
                }
            }
        }

        public void QueueChunkUpdate(Chunk chunk, UpdateReason reason)
        {
            if (chunk.IsQueuedForMeshUpdate || chunk.IsDisposed) return;
            chunk.IsQueuedForMeshUpdate = true;

            int distance = 0;
            if (Camera.Main != null)
            {
                distance = (int)Vector3.DistanceSquared(ToEntitySpace(Camera.Main.Position), chunk.GeometryBoundingBox.Center);
            }

            MeshUpdateQueue.Enqueue(chunk, (int)reason + distance);
        }

        public bool IsPositionInChunk(Vector3 world, out Chunk chunk)
        {
            var local = WorldPositionToLocal(world);

            if (Octree.TryFindDataAtPosition(local, out chunk))
            {
                return true;
            }
            else
            {
                chunk = null;
                return false;
            }
        }

        public Vector3 WorldPositionToLocal(Vector3 world)
        {
            return world - PositionWorld;
        }

        public Vector3 LocalPositionToWorld(Vector3 local)
        {
            return local + PositionWorld;
        }

        public override void Update()
        {
            base.Update();
            HandleTag();
            Oxygen.Update();
            ProcessMeshUpdates();
        }

        private void ProcessMeshUpdates()
        {
            int dispatchedThisFrame = 0;
            while (dispatchedThisFrame < 4 && MeshUpdateQueue.TryDequeue(out Chunk chunk, out int priority))
            {
                if (chunk.IsDisposed) continue;

                int currentVersion = chunk.DataVersion;
                var snapshot = chunk.CreatePaddedSnapshot();
                var index = chunk.PositionIndex;

                WorkerPoolManager.Enqueue(token =>
                {
                    if (token.IsCancellationRequested || chunk.IsDisposed)
                    {
                        chunk.IsQueuedForMeshUpdate = false;
                        return;
                    }

                    try
                    {
                        var generator = new MeshGenerator(index, snapshot, Chunk.MeasureGenerationTime);
                        var meshData = generator.GenerateMeshData();

                        MainThreadDispatcher.Instance.Enqueue(() =>
                        {
                            if (chunk.IsDisposed) return;

                            if (chunk.DataVersion != currentVersion)
                            {
                                chunk.IsQueuedForMeshUpdate = false;
                                QueueChunkUpdate(chunk, UpdateReason.Generation);
                                return;
                            }

                            ReadyMeshesQueue.Enqueue((chunk, meshData, currentVersion));
                        });
                    }
                    catch (Exception ex)
                    {
                        Engine.Debug.Error($"[SpaceEntity] Mesh generation failed: {ex}");
                        chunk.IsQueuedForMeshUpdate = false;
                    }
                }, WorkerPoolManager.Priority.Medium);

                dispatchedThisFrame++;
            }

            int uploadedThisFrame = 0;
            while (uploadedThisFrame < 2 && ReadyMeshesQueue.TryDequeue(out var result))
            {
                if (result.chunk.IsDisposed) continue;

                if (result.chunk.DataVersion == result.version)
                {
                    result.chunk.ApplyMeshData(result.data);
                }
                else
                {
                    result.chunk.IsQueuedForMeshUpdate = false;
                    QueueChunkUpdate(result.chunk, UpdateReason.Generation);
                }

                uploadedThisFrame++;
            }
        }

        private void HandleTag()
        {
            Camera camera = Camera.Main;

            if (camera == null) return;

            var dis = (int)(Vector3.Distance(CenterOfMassWorld, camera.Position));

            if (VisualDebug.Enabled)
            {
                StringBuilder.Append("Name: ")
                    .Append(Name)
                     .Append("\n")
                     .Append("ID: ")
                  .Append(EntityID)
                   .Append("\n")
                  .Append(IsProcedural() ? "Asteroid" : "Spaceship")
                  .Append("\nWpos: ")
                  .Append(Block.RoundVector3(CenterOfMassWorld))
                  .Append("\n")
                  .Append(dis)
                  .Append(" m\n")
                  .Append(_entityMassString)
                  .Append(" tn, gR: ")
                  .Append((int)GravityRadius)
                  .Append(" m");
            }
            else
            {
                StringBuilder.Append(dis).Append(" m");
                if (Mass > 0)
                {
                    StringBuilder.Append("\n")
                      .Append(_entityMassString)
                      .Append(" tn");
                }
            }

            if (tag != null)
            {
                tag.Text = StringBuilder.ToString();
            }
            else if (TagManager.Instance != null)
            {
                tag = TagManager.Instance.CreateTag(StringBuilder.ToString(), CenterOfMassWorld, Color4.DarkGreen, false, GUI.Tag.Alignment.Right);
            }

            StringBuilder.Clear();
        }

        public bool Raycast(Ray ray, out HitInfo hitInfo)
        {
            if (!HasRotation)
            {
                bool directHit = RaycastEntitySpace(ray, out hitInfo);
                if (directHit) hitInfo.normalWorld = hitInfo.normal.ToVector3();
                return directHit;
            }

            _entitySpaceRay.Origin = ToEntitySpace(ray.Origin);
            _entitySpaceRay.Direction = DirectionToEntitySpace(ray.Direction).Normalized();
            _entitySpaceRay.Length = ray.Length;

            if (!RaycastEntitySpace(_entitySpaceRay, out hitInfo)) return false;

            hitInfo.hitPosition = ToWorldSpace(hitInfo.hitPosition);
            hitInfo.blockPositionWorld = ToWorldSpace(hitInfo.blockPositionWorld);
            hitInfo.normalWorld = DirectionToWorldSpace(hitInfo.normal.ToVector3());
            return true;
        }

        private bool RaycastEntitySpace(Ray ray, out HitInfo hitInfo)
        {
            hitInfo = new HitInfo();

            if (Chunks.Count == 0) return false;

            if (VoxelPhysics.RaycastChunks(this, ray, out var crossedChunks))
            {
                foreach (var chunkHit in crossedChunks)
                {
                    var length = ray.Length - chunkHit.Distance;

                    if (length <= 0) continue;

                    Ray chunkRay = new Ray(chunkHit.HitPosition, ray.Direction, length);

                    if (chunkHit.Chunk.Raycast(chunkRay, out hitInfo))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool RemoveBlockAtLocal(Vector3 localBlockPosition, Vector3SByte removalNormal)
        {
            Vector3 worldBlockPos = PositionWorld + localBlockPosition;
            if (!IsPositionWithinEntitySize(worldBlockPos))
            {
                return false;
            }
            int chunkX = (int)MathF.Floor(localBlockPosition.X / Chunk.Size);
            int chunkY = (int)MathF.Floor(localBlockPosition.Y / Chunk.Size);
            int chunkZ = (int)MathF.Floor(localBlockPosition.Z / Chunk.Size);
            Vector3SByte chunkIndex = new Vector3SByte((sbyte)chunkX, (sbyte)chunkY, (sbyte)chunkZ);

            int blockX = (int)MathF.Floor(localBlockPosition.X) - chunkX * Chunk.Size;
            int blockY = (int)MathF.Floor(localBlockPosition.Y) - chunkY * Chunk.Size;
            int blockZ = (int)MathF.Floor(localBlockPosition.Z) - chunkZ * Chunk.Size;

            if (Octree.TryFindDataAtPosition(localBlockPosition + new Vector3(0.5f, 0.5f, 0.5f), out Chunk chunk))
            {
                var block = chunk.GetBlock((byte)blockX, (byte)blockY, (byte)blockZ);

                chunk.RemoveBlock((byte)blockX, (byte)blockY, (byte)blockZ,
                                 removalNormal.X, removalNormal.Y, removalNormal.Z, true);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void RenderEffect(float disSqr)
        {
            return;
        }

        HashSet<Chunk> chunks = new HashSet<Chunk>();
        public void Render(Camera camera, BlockMaterial material)
        {
            chunks.Clear();
            var cameraEntitySpace = ToEntitySpace(camera.Position);
            Octree.FindDataInRadius(WorldPositionToLocal(cameraEntitySpace), Settings.CHUNK_VISIBLE_RADIUS, chunks);

            foreach (var chunk in chunks)
            {
                var dis = (int)(cameraEntitySpace - chunk.GeometryBoundingBox.Center).LengthSquared;

                if (dis < Settings.CHUNK_VISIBLE_RADIUS * Settings.CHUNK_VISIBLE_RADIUS)
                {
                    bool visible = chunk.GeometryBoundingBox.Contains(cameraEntitySpace);

                    if (!visible)
                    {
                        visible = camera.Frustum.IsInFrustum(ToWorldBounds(chunk.GeometryBoundingBox));
                    }

                    if (!visible) continue;

                    chunk.SetLOD(dis);
                    chunk.Render(material);
                }
            }

            if (VisualDebug.Enabled)
            {
                VisualDebug.DrawBoundingBox(BoundingBox, Color4.Cyan);
                VisualDebug.DrawPosition(GeometryBoundingBox.Center, 6, Color4.Orange);

                VisualDebug.DrawBoundingBox(GeometryBoundingBox, Color4.Orange);
                VisualDebug.DrawPosition(CenterOfMassWorld, 8, Color4.Lime);
            }
        }

        public bool IsColliding(BoundingVolume volume, out CollideInfo collideInfo)
        {
            if (HasRotation && volume is BoundingSphere sphere)
            {
                _entitySpaceSphere.Center = ToEntitySpace(sphere.Center);
                _entitySpaceSphere.Radius = sphere.Radius;
                volume = _entitySpaceSphere;
            }

            bool c = false;

            var collideInfo2 = new CollideInfo();

            for (int i = 0; i < Chunks.Count; i++)
            {
                c = Chunks[i].IsColliding(volume, out collideInfo);

                if (c) return true;
            }
            collideInfo = collideInfo2;
            return false;
        }

        public bool IsPositionWithinEntitySize(Vector3 worldPosition)
        {
            Vector3 local = worldPosition - PositionWorld;
            return local.X >= -SizeBlocksHalf && local.X < SizeBlocksHalf
                && local.Y >= -SizeBlocksHalf && local.Y < SizeBlocksHalf
                && local.Z >= -SizeBlocksHalf && local.Z < SizeBlocksHalf;
        }

        public float GravityRadius { get; private set; }

        private const float GravityScale = 1.0f;

        private void CalculateGravityRadius()
        {
            if (Mass == 0)
            {
                GravityRadius = 0f;
                return;
            }

            GravityRadius = GravityScale * MathF.Pow((float)Mass, 0.32f);

            GravityRadius += GeometryBoundingBox.Diagonal;

            int dis = (int)Vector3.Distance(GeometryBoundingBox.Center, CenterOfMass);
            GravityRadius += dis;

            if (GravityRadius < 10)
            {
                GravityRadius = 10;
            }
        }
        public bool IsPositionWithinGravityRadius(Vector3 worldPosition)
        {
            float distanceSquared = Vector3.DistanceSquared(CenterOfMass, worldPosition);
            return distanceSquared <= GravityRadius * GravityRadius;
        }

        private void CalculateCenterOfMass()
        {
            if (Mass == 0)
            {
                CenterOfMass = BoundingBox.Center;
                if (tag != null) tag.WorldPosition = CenterOfMassWorld;

                return;
            }

            CenterOfMass = sumPosCenterOfMass / Mass;
            if (tag != null) tag.WorldPosition = CenterOfMassWorld;
        }

        public void SetOrientation(Quaternion orientation)
        {
            Orientation = orientation.LengthSquared > 0f ? Quaternion.Normalize(orientation) : Quaternion.Identity;
            _inverseOrientation = Quaternion.Invert(Orientation);
            _rotationMatrix = Matrix4.CreateFromQuaternion(Orientation);
            HasRotation = MathF.Abs(MathF.Abs(Orientation.W) - 1f) > 0.000001f;

            if (tag != null) tag.WorldPosition = CenterOfMassWorld;
        }

        public Matrix4 RotationMatrix => _rotationMatrix;

        public Vector3 ToEntitySpace(Vector3 worldPosition)
        {
            if (!HasRotation) return worldPosition;
            return PositionWorld + Vector3.Transform(worldPosition - PositionWorld, _inverseOrientation);
        }

        public Vector3 ToWorldSpace(Vector3 entitySpacePosition)
        {
            if (!HasRotation) return entitySpacePosition;
            return PositionWorld + Vector3.Transform(entitySpacePosition - PositionWorld, Orientation);
        }

        public Vector3 DirectionToEntitySpace(Vector3 worldDirection)
        {
            return HasRotation ? Vector3.Transform(worldDirection, _inverseOrientation) : worldDirection;
        }

        public Vector3 DirectionToWorldSpace(Vector3 entitySpaceDirection)
        {
            return HasRotation ? Vector3.Transform(entitySpaceDirection, Orientation) : entitySpaceDirection;
        }

        public Vector3 BlockCenterToWorldCorner(Vector3 entitySpaceCorner)
        {
            var half = new Vector3(0.5f);
            return ToWorldSpace(entitySpaceCorner + half) - half;
        }

        public BoundingBox ToWorldBounds(BoundingBox entitySpaceBox)
        {
            if (!HasRotation) return entitySpaceBox;

            var half = entitySpaceBox.Extents;
            var axisX = Vector3.Transform(new Vector3(half.X, 0, 0), Orientation);
            var axisY = Vector3.Transform(new Vector3(0, half.Y, 0), Orientation);
            var axisZ = Vector3.Transform(new Vector3(0, 0, half.Z), Orientation);
            var extents = Abs(axisX) + Abs(axisY) + Abs(axisZ);

            _worldBoundsScratch.Center = ToWorldSpace(entitySpaceBox.Center);
            _worldBoundsScratch.Size = extents * 2f;
            return _worldBoundsScratch;
        }

        private static Vector3 Abs(Vector3 v)
        {
            return new Vector3(MathF.Abs(v.X), MathF.Abs(v.Y), MathF.Abs(v.Z));
        }

        public static Chunk GetOrCreateChunk(SpaceEntity entity, Vector3SByte idx)
        {
            var localPos = SpaceMath.Entity.ChunkIndexToLocal(idx) + new Vector3(Chunk.SizeHalf, Chunk.SizeHalf, Chunk.SizeHalf);
            if (!entity.Octree.TryFindDataAtPosition(localPos, out Chunk chunk) || chunk == null)
            {
                chunk = new Chunk(idx, entity, true);

                var local = SpaceMath.Entity.ChunkIndexToLocal(chunk.PositionIndex);

                entity.Octree.Add(chunk, BoundingBox.CreateFromMinMax(local, local + Vector3.One * Chunk.Size));
                entity.Chunks.Add(chunk);

                chunk.OnChunkModified += entity.UpdateEntityGeometryMinMax;

                entity.UpdateNeighbors(chunk);
                entity.RecalculateGeometryBoundingBox();
                entity.RecalculateMass();

                chunk.CalculateLighting();
            }
            return chunk;
        }

        public void Dispose()
        {
            Sector = null;
            MeshUpdateQueue.Clear();
            foreach (var mesh in MeshesTogenerate)
            {
                mesh.Dispose();
            }
            foreach (var ch in Chunks)
            {
                ch.Dispose();
            }

            var t = tag;
            if (t != null)
            {
                MainThreadDispatcher.Instance.Enqueue(() => TagManager.Instance?.ReleaseTag(t));
                tag = null;
            }
        }
    }
}