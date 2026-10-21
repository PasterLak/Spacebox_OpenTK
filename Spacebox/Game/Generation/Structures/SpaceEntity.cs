using Engine;
using Engine.Multithreading;
using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game.Effects;
using Spacebox.Game.GameMath;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Oxygen;
using Spacebox.Game.Generation.Doors;
using Spacebox.Game.Physics.Gravity;
using Spacebox.Game.Generation.Structures;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Resource;

using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System;

using Spacebox.Game.Ships;
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
        public int MassVersion { get; private set; }
        public ShipBody Ship { get; private set; }
        private ShipControlSystem _shipControl;
        public ShipControlSystem ShipControl => _shipControl ??= new ShipControlSystem(this);
        public ShipBody EnsureShipBody() => Ship ??= new ShipBody(this);
        public Vector3i? SavedSectorIndex { get; set; }
        public bool HasShipControl => _shipControl != null && _shipControl.HasParts;

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
        private readonly BoundingBoxOBB _debugBox = new BoundingBoxOBB(Vector3.Zero, Vector3.One, Quaternion.Identity);

        private bool _isModified = false;
        public bool IsModified
        {
            get => _isModified;
            set
            {
                _isModified = value;
                if (Sector != null) Sector.IsModified = true;
            }
        }
        public bool IsGenerated { get; set; } = false;

        public Sector Sector { get; private set; }

        internal void AttachToSector(Sector sector)
        {
            Sector = sector;
        }

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
        public DoorSystem Doors { get; private set; }
        private Spacebox.Game.Signs.SignRenderer _signs;
        public Spacebox.Game.Signs.SignRenderer Signs => _signs ??= new Spacebox.Game.Signs.SignRenderer(this);
        private Spacebox.Game.Pictures.PictureFrameRenderer _pictures;
        public Spacebox.Game.Pictures.PictureFrameRenderer Pictures => _pictures ??= new Spacebox.Game.Pictures.PictureFrameRenderer(this);
        private Spacebox.Game.Rendering.InvisibleBlockRenderer _invisibleBlocks;
        public Spacebox.Game.Rendering.InvisibleBlockRenderer InvisibleBlocks => _invisibleBlocks ??= new Spacebox.Game.Rendering.InvisibleBlockRenderer(this);
        public CellReservations Reservations { get; } = new CellReservations();
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
            Doors = new DoorSystem(this);
            if (IsDynamic()) Ship = new ShipBody(this);

            CalculateCenterOfMass();

            MainThreadDispatcher.Instance.Enqueue(() => EnsureTag(""));

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
            RecalculateMass();
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
            MassVersion++;
            _entityMassString = Mass.ToString("N0").Replace(",", ".");
        }

        private void UpdateEntityGeometryMinMax(Chunk chunk)
        {
            RecalculateGeometryBoundingBox();
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
            var cell = new Vector3i((int)MathF.Floor(localPos.X), (int)MathF.Floor(localPos.Y), (int)MathF.Floor(localPos.Z));
            int chunkX = (int)MathF.Floor(cell.X / (float)Chunk.Size);
            int chunkY = (int)MathF.Floor(cell.Y / (float)Chunk.Size);
            int chunkZ = (int)MathF.Floor(cell.Z / (float)Chunk.Size);
            Vector3SByte chunkIndex = new Vector3SByte((sbyte)chunkX, (sbyte)chunkY, (sbyte)chunkZ);

            int blockX = cell.X - chunkX * Chunk.Size;
            int blockY = cell.Y - chunkY * Chunk.Size;
            int blockZ = cell.Z - chunkZ * Chunk.Size;
            Vector3Byte blockPos = new Vector3Byte((byte)blockX, (byte)blockY, (byte)blockZ);

            if (Octree.TryFindDataAtPosition(new Vector3(cell.X + 0.5f, cell.Y + 0.5f, cell.Z + 0.5f), out var chunk))
            {
                chunk.PlaceBlock(blockPos, block);

                if (block.Is<ElectricalBlock>(out var el))
                {
                    ElectricManager.AddBlock((cell.X, cell.Y, cell.Z), el, chunk);
                }
            }
            else
            {
                Chunk newChunk = new Chunk(chunkIndex, this, true);
                AddChunk(newChunk, false);
                newChunk.PlaceBlock(blockPos, block);

                if (block.Is<ElectricalBlock>(out var el))
                {
                    ElectricManager.AddBlock((cell.X, cell.Y, cell.Z), el, newChunk);
                }
            }
            return true;
        }

        public bool TryPlaceBlockLocal(Vector3 localBlockPosition, Block block)
        {
            var cell = new Vector3i((int)MathF.Floor(localBlockPosition.X), (int)MathF.Floor(localBlockPosition.Y), (int)MathF.Floor(localBlockPosition.Z));
            return TryPlaceBlockAtCell(cell, block);
        }

        public bool TryPlaceBlockAtCell(Vector3i localCell, Block block)
        {
            return TryPlaceBlock(CellCenter(localCell), block);
        }

        public bool CanPlaceBlockAtCell(Vector3i localCell, BlockData blockData, Direction direction, Rotation rotation)
        {
            return CanPlaceBlock(CellCenter(localCell), blockData, direction, rotation);
        }

        public Vector3 CellCorner(Vector3i localCell)
        {
            return PositionWorld + new Vector3(localCell.X, localCell.Y, localCell.Z);
        }

        private Vector3 CellCenter(Vector3i localCell)
        {
            return PositionWorld + new Vector3(localCell.X + 0.5f, localCell.Y + 0.5f, localCell.Z + 0.5f);
        }

        public bool TryPlaceBlock(Vector3 worldPosition, Block block)
        {
            if (block.Id == 0)
                return true;

            if (GameAssets.Blocks.TryGetValue(block.Id, out var blockData) && blockData.IsMultiblock)
                return TryPlaceStructure(worldPosition, block, blockData);

            if (!IsPositionWithinEntitySize(worldPosition) || !IsWithinBuildRange(worldPosition))
                return false;

            Vector3 localPos = WorldPositionToLocal(worldPosition);
            if (!IsCellEmpty(worldPosition) || IsCellReserved(worldPosition)) return false;
            if (blockData != null && !IsReservedAreaFree(worldPosition, blockData, block.Direction, block.Rotation)) return false;
            if (blockData != null && !HasRequiredSupport(worldPosition, blockData, block.Direction, block.Rotation)) return false;

            return PlaceBlockInternal(localPos, block);
        }

        public bool IsCellReserved(Vector3 position)
        {
            var local = WorldPositionToLocal(position);
            var cell = new Vector3i((int)MathF.Floor(local.X), (int)MathF.Floor(local.Y), (int)MathF.Floor(local.Z));
            return Doors.IsReserved(cell) || Reservations.IsReserved(cell);
        }

        public bool CanPlaceBlock(Vector3 position, BlockData blockData, Direction direction, Rotation rotation)
        {
            if (blockData.IsMultiblock) return CanPlaceStructure(position, blockData, direction, rotation);
            return IsCellEmpty(position) && !IsCellReserved(position) && IsReservedAreaFree(position, blockData, direction, rotation) &&
                   HasRequiredSupport(position, blockData, direction, rotation);
        }

        public bool HasRequiredSupport(Vector3 position, BlockData blockData, Direction direction, Rotation rotation)
        {
            if (blockData.Shape == null || !blockData.Shape.RequiresSupport) return true;

            var local = WorldPositionToLocal(position);
            var cell = new Vector3i((int)MathF.Floor(local.X), (int)MathF.Floor(local.Y), (int)MathF.Floor(local.Z));
            return Spacebox.Game.Generation.Shapes.BlockShapes.IsSupported(this, cell, blockData.Shape, direction, rotation);
        }

        private static readonly List<Vector3i> _reservedArea = new List<Vector3i>();

        public bool IsReservedAreaFree(Vector3 originPosition, BlockData blockData, Direction direction, Rotation rotation)
        {
            if (blockData is not IReservedAreaBlockData area) return true;

            area.GetReservedArea(direction, rotation, _reservedArea);
            for (int i = 0; i < _reservedArea.Count; i++)
            {
                var position = originPosition + new Vector3(_reservedArea[i].X, _reservedArea[i].Y, _reservedArea[i].Z);
                if (!IsPositionWithinEntitySize(position))
                {
                    if (area.AreaMustBeInsideEntity) return false;
                    continue;
                }

                if (!IsCellEmpty(position) || IsCellReserved(position)) return false;
            }

            return true;
        }

        internal void ClearCellSilently(Vector3i localCell)
        {
            if (TryGetBlockLocal(localCell, out var chunk, out var index))
                chunk.ClearBlockSilently(index.X, index.Y, index.Z);
        }

        internal void PlaceCellRaw(Vector3i localCell, Block block)
        {
            PlaceBlockInternal(new Vector3(localCell.X, localCell.Y, localCell.Z), block);
        }

        public bool TryGetReservedArea(Vector3 originPosition, BlockData blockData, Direction direction, Rotation rotation, out Vector3 minPosition, out Vector3 size)
        {
            minPosition = Vector3.Zero;
            size = Vector3.Zero;
            if (blockData is not IReservedAreaBlockData area) return false;

            area.GetReservedArea(direction, rotation, _reservedArea);
            if (_reservedArea.Count == 0) return false;

            var min = new Vector3i(int.MaxValue);
            var max = new Vector3i(int.MinValue);
            for (int i = 0; i < _reservedArea.Count; i++)
            {
                min = Vector3i.ComponentMin(min, _reservedArea[i]);
                max = Vector3i.ComponentMax(max, _reservedArea[i]);
            }

            minPosition = originPosition + new Vector3(min.X, min.Y, min.Z);
            size = new Vector3(max.X - min.X + 1, max.Y - min.Y + 1, max.Z - min.Z + 1);
            return true;
        }

        private static readonly List<MultiblockCell> _structureCells = new List<MultiblockCell>();

        public bool CanPlaceStructure(Vector3 originPosition, BlockData blockData, Direction direction, Rotation rotation)
        {
            MultiblockLayout.GetCells(blockData, direction, rotation, _structureCells);
            if (!IsWithinBuildRange(originPosition)) return false;

            for (int i = 0; i < _structureCells.Count; i++)
            {
                var offset = _structureCells[i].Offset;
                var position = originPosition + new Vector3(offset.X, offset.Y, offset.Z);

                if (!IsPositionWithinEntitySize(position) || !IsCellEmpty(position) || IsCellReserved(position))
                    return false;
                if (!HasRequiredSupport(position, blockData, direction, rotation))
                    return false;
            }

            return IsReservedAreaFree(originPosition, blockData, direction, rotation);
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

        public void GetStructureCells(Chunk chunk, Vector3Byte index, List<Vector3i> result)
        {
            result.Clear();
            var local = GetCellLocal(chunk, index);
            var block = chunk.Blocks[index.X, index.Y, index.Z];
            if (block == null || !GameAssets.Blocks.TryGetValue(block.Id, out var blockData) || !blockData.IsMultiblock)
            {
                result.Add(local);
                return;
            }

            var originLocal = local - MultiblockLayout.GetOriginOffset(blockData, block);
            MultiblockLayout.GetCells(blockData, block.Direction, block.Rotation, _structureCells);
            for (int i = 0; i < _structureCells.Count; i++)
                result.Add(originLocal + _structureCells[i].Offset);
        }

        public Block GetStructureOrigin(Chunk chunk, Vector3Byte index)
        {
            return TryGetStructureOrigin(chunk, index, out var originChunk, out var originIndex)
                ? originChunk.Blocks[originIndex.X, originIndex.Y, originIndex.Z]
                : chunk.Blocks[index.X, index.Y, index.Z];
        }

        public bool TryGetStructureOrigin(Chunk chunk, Vector3Byte index, out Chunk originChunk, out Vector3Byte originIndex)
        {
            originChunk = null;
            originIndex = default;
            var block = chunk.Blocks[index.X, index.Y, index.Z];
            if (block == null || !block.HasMultiblockOffset || !GameAssets.Blocks.TryGetValue(block.Id, out var blockData))
                return false;

            var originLocal = GetCellLocal(chunk, index) - MultiblockLayout.GetOriginOffset(blockData, block);
            if (!TryGetBlockLocal(originLocal, out originChunk, out originIndex)) return false;

            var origin = originChunk.Blocks[originIndex.X, originIndex.Y, originIndex.Z];
            return origin != null && origin.Id == block.Id && !origin.HasMultiblockOffset;
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
                if (!cell.IsOrigin) cellBlock.ConfigureAsMultiblockPart();

                PlaceBlockInternal(originLocal + new Vector3(cell.Offset.X, cell.Offset.Y, cell.Offset.Z), cellBlock);
            }

            if (originBlock is DoorBlock door)
            {
                var origin = new Vector3i((int)MathF.Floor(originLocal.X), (int)MathF.Floor(originLocal.Y), (int)MathF.Floor(originLocal.Z));
                Doors.Register(origin, door);
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
            if (chunk.IsDisposed) return;
            chunk.RequestMesh();
            if (chunk.IsQueuedForMeshUpdate) return;
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
            _shipControl?.Update(Time.Delta);
            Ship?.Update(Time.Delta);
            HandleTag();
            Doors.Update(Time.Delta);
            Oxygen.Update();
            UpdateLods();
            ProcessMeshUpdates();
        }

        private int _meshJobsInFlight;
        private const float LodUpdateInterval = 0.25f;
        private float _lodTimer;
        private readonly HashSet<Chunk> _lodChunks = new HashSet<Chunk>();

        private void UpdateLods()
        {
            var camera = Camera.Main;
            if (camera == null) return;

            _lodTimer -= Time.Delta;
            if (_lodTimer > 0f && !StreamingBudget.IsLoading) return;
            _lodTimer = LodUpdateInterval;

            var cameraEntitySpace = ToEntitySpace(camera.Position);
            int radiusSquared = Settings.CHUNK_VISIBLE_RADIUS * Settings.CHUNK_VISIBLE_RADIUS;

            _lodChunks.Clear();
            Octree.FindDataInRadius(WorldPositionToLocal(cameraEntitySpace), Settings.CHUNK_VISIBLE_RADIUS, _lodChunks);
            foreach (var chunk in _lodChunks)
            {
                int distanceSquared = (int)(cameraEntitySpace - chunk.GeometryBoundingBox.Center).LengthSquared;
                if (distanceSquared < radiusSquared) chunk.SetLOD(distanceSquared);
            }
        }

        public virtual void CollectStreamingWork(Vector3 cameraPosition, ref StreamingWork work)
        {
            work.Meshes += MeshUpdateQueue.Count + ReadyMeshesQueue.Count + Volatile.Read(ref _meshJobsInFlight);
        }

        private void ProcessMeshUpdates()
        {
            int dispatchedThisFrame = 0;
            int maxDispatch = StreamingBudget.MeshDispatchPerFrame;
            while (dispatchedThisFrame < maxDispatch && MeshUpdateQueue.TryDequeue(out Chunk chunk, out int priority))
            {
                if (chunk.IsDisposed) continue;

                int currentVersion = chunk.MeshVersion;
                var snapshot = chunk.CreatePaddedSnapshot();
                var index = chunk.PositionIndex;

                Interlocked.Increment(ref _meshJobsInFlight);
                WorkerPoolManager.Enqueue(token =>
                {
                    try
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
                            if (chunk.IsDisposed)
                            {
                                meshData.Release();
                                return;
                            }

                            if (chunk.MeshVersion != currentVersion)
                            {
                                meshData.Release();
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
                    }
                    finally
                    {
                        PaddedArrayPools.Blocks.Return(snapshot);
                        Interlocked.Decrement(ref _meshJobsInFlight);
                    }
                }, WorkerPoolManager.Priority.Medium);

                dispatchedThisFrame++;
            }

            int uploadedThisFrame = 0;
            int maxUpload = StreamingBudget.MeshUploadPerFrame;
            while (uploadedThisFrame < maxUpload && ReadyMeshesQueue.TryDequeue(out var result))
            {
                if (result.chunk.IsDisposed)
                {
                    result.data.Release();
                    continue;
                }

                if (result.chunk.MeshVersion == result.version)
                {
                    result.chunk.ApplyMeshData(result.data);
                }
                else
                {
                    result.data.Release();
                    result.chunk.IsQueuedForMeshUpdate = false;
                    QueueChunkUpdate(result.chunk, UpdateReason.Generation);
                }

                uploadedThisFrame++;
            }
        }

        private void HandleTag()
        {
            Camera camera = Camera.Main;

            if (camera == null || IsDisposed) return;

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
                  .Append(" tn");
            }
            else
            {
                StringBuilder.Append(dis).Append(" m");
                if (Mass > 0 && TagManager.Instance?.Radar?.ShowMass == true)
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
            else
            {
                EnsureTag(StringBuilder.ToString());
            }

            StringBuilder.Clear();
        }

        private void EnsureTag(string text)
        {
            if (tag != null || IsDisposed || TagManager.Instance == null) return;

            tag = TagManager.Instance.CreateTag(text, CenterOfMassWorld, Color4.DarkGreen, false, GUI.Tag.Alignment.Right);
            tag.Category = GUI.Tag.TagCategory.Entity;
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
            bool hitVoxel = RaycastVoxels(ray, out hitInfo);

            if (Doors.RaycastMovingDoors(ray, out var doorHit, out float doorDistance))
            {
                float voxelDistance = hitVoxel ? (hitInfo.hitPosition - ray.Origin).Length : float.MaxValue;
                if (doorDistance < voxelDistance)
                {
                    hitInfo = doorHit;
                    return true;
                }
            }

            return hitVoxel;
        }

        private bool RaycastVoxels(Ray ray, out HitInfo hitInfo)
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
                    chunk.SetLOD(dis);

                    bool visible = chunk.GeometryBoundingBox.Contains(cameraEntitySpace);

                    if (!visible)
                    {
                        visible = camera.Frustum.IsInFrustum(ToWorldBounds(chunk.GeometryBoundingBox));
                    }

                    if (!visible) continue;
                    if (!PassesOcclusion(chunk, cameraEntitySpace)) continue;

                    chunk.Render(material);
                }
            }

            Doors.Render();
            _signs?.Render();
            _pictures?.Render();
            if (Spacebox.Game.Rendering.InvisibleBlockVisibility.IsShown) InvisibleBlocks.Render();
            _shipControl?.Render();

            if (VisualDebug.Enabled)
            {
                DrawDebugBox(BoundingBox, Color4.Cyan);
                Engine.GUI.GizmoIcons.Add(ToWorldSpace(GeometryBoundingBox.Center), Engine.GUI.GizmoIcon.GeometryCenter, Color4.Orange);

                DrawDebugBox(GeometryBoundingBox, Color4.Orange);
                VisualDebug.DrawAxes(CenterOfMassWorld, 3f);
                Engine.GUI.GizmoIcons.Add(CenterOfMassWorld, Engine.GUI.GizmoIcon.CenterOfMass, Color4.Lime);
            }
        }

        private const float OcclusionBoxMargin = 1f;
        private const float OcclusionNearDistance = 2f;

        private bool PassesOcclusion(Chunk chunk, Vector3 cameraEntitySpace)
        {
            var culler = Engine.EngineWindow.Instance?.Occlusion;
            if (culler == null) return true;

            var bounds = chunk.GeometryBoundingBox;
            var margin = new Vector3(OcclusionBoxMargin);
            var near = new Vector3(OcclusionBoxMargin + OcclusionNearDistance);
            bool cameraNear = Vector3.ComponentMin(Vector3.ComponentMax(cameraEntitySpace, bounds.Min - near), bounds.Max + near) == cameraEntitySpace;

            bool visible = culler.Prepare(chunk.Occlusion, cameraNear, chunk.RenderMatrix(), bounds.Min - chunk.PositionWorld - margin, bounds.Max - chunk.PositionWorld + margin);
            if (!visible && VisualDebug.Enabled) DrawDebugBox(bounds, new Color4(1f, 0.15f, 0.15f, 0.7f));
            return visible;
        }

        public void DrawDebugBox(BoundingBox entitySpaceBox, Color4 color, float thickness = 0f)
        {
            if (!HasRotation)
            {
                if (thickness > 0f) VisualDebug.DrawBoundingBox(entitySpaceBox, color, thickness);
                else VisualDebug.DrawBoundingBox(entitySpaceBox, color);
                return;
            }

            _debugBox.Center = ToWorldSpace(entitySpaceBox.Center);
            _debugBox.Size = entitySpaceBox.Size;
            _debugBox.Rotation = Orientation;
            if (thickness > 0f) VisualDebug.DrawOBB(_debugBox, color, thickness);
            else VisualDebug.DrawOBB(_debugBox, color);
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

            if (volume is BoundingSphere movingDoorProbe &&
                Doors.IntersectsMovingDoor(WorldPositionToLocal(movingDoorProbe.Center), movingDoorProbe.Radius))
                return true;

            return false;
        }

        public bool IsPositionWithinEntitySize(Vector3 worldPosition)
        {
            Vector3 local = worldPosition - PositionWorld;
            return local.X >= -SizeBlocksHalf && local.X < SizeBlocksHalf
                && local.Y >= -SizeBlocksHalf && local.Y < SizeBlocksHalf
                && local.Z >= -SizeBlocksHalf && local.Z < SizeBlocksHalf;
        }

        public const int BuildAttachDistance = 16;

        private Func<Vector3SByte, Block[,,]?> _chunkBlocksLookup;

        public bool HasBlockNear(Vector3 worldPosition, int range)
        {
            var local = WorldToEntityLocal(worldPosition);
            var cell = new Vector3i((int)MathF.Floor(local.X), (int)MathF.Floor(local.Y), (int)MathF.Floor(local.Z));
            _chunkBlocksLookup ??= GetChunkBlocksForProximity;
            return BlockProximity.HasBlockWithin(cell, range, _chunkBlocksLookup);
        }

        public bool IsWithinBuildRange(Vector3 worldPosition)
        {
            var local = WorldPositionToLocal(worldPosition);
            var cell = new Vector3i((int)MathF.Floor(local.X), (int)MathF.Floor(local.Y), (int)MathF.Floor(local.Z));
            _chunkBlocksLookup ??= GetChunkBlocksForProximity;
            return BlockProximity.HasBlockWithin(cell, BuildAttachDistance, _chunkBlocksLookup);
        }

        private Block[,,]? GetChunkBlocksForProximity(Vector3SByte index)
        {
            return TryGetChunk(index, out var chunk) && !chunk.IsDisposed && chunk.Mass > 0 ? chunk.Blocks : null;
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

        public void SetPose(Vector3 positionWorld, Quaternion orientation)
        {
            var delta = positionWorld - PositionWorld;
            bool rotated = orientation != Orientation;
            if (delta != Vector3.Zero) ShiftWorld(delta);
            if (rotated) SetOrientation(orientation);
            if (tag != null) tag.WorldPosition = CenterOfMassWorld;
            if (delta != Vector3.Zero || rotated) World.Instance?.OnEntityMoved(this);
        }

        private void ShiftWorld(Vector3 delta)
        {
            var oldPosition = PositionWorld;
            PositionWorld += delta;
            Position = PositionWorld;
            BoundingBox.Center += delta;
            GeometryBoundingBox.Center += delta;
            sumPosCenterOfMass += delta * Mass;
            CenterOfMass += delta;

            for (int i = 0; i < Chunks.Count; i++)
                Chunks[i].ShiftWorld(delta);

            Sector?.OnEntityMoved(this, oldPosition);
        }

        public Vector3 ToWorldSpace(Vector3 entitySpacePosition)
        {
            if (!HasRotation) return entitySpacePosition;
            return PositionWorld + Vector3.Transform(entitySpacePosition - PositionWorld, Orientation);
        }

        public Vector3 WorldToEntityLocal(Vector3 worldPosition)
        {
            var offset = worldPosition - PositionWorld;
            return HasRotation ? Vector3.Transform(offset, _inverseOrientation) : offset;
        }

        public Vector3 EntityLocalToWorld(Vector3 localPosition)
        {
            return PositionWorld + (HasRotation ? Vector3.Transform(localPosition, Orientation) : localPosition);
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
            if (entity.TryGetChunk(idx, out var existing) && existing != null) return existing;
            if (entity.Octree.TryFindDataAtPosition(localPos, out Chunk chunk) && chunk != null) return chunk;

            chunk = new Chunk(idx, entity, true);
            entity.AddChunk(chunk, false);
            return chunk;
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
            Doors.Dispose();
            _signs?.Dispose();
            _pictures?.Dispose();
            _invisibleBlocks?.Dispose();
            _shipControl?.Dispose();
            GravityField.UnregisterEntity(this);
            Spacebox.Game.Player.Respawn.DnaReconstructorRegistry.UnregisterEntity(this);
            Spacebox.Game.Protection.ClaimRegistry.UnregisterEntity(this);
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