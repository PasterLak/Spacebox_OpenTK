using System.Collections.Concurrent;
using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Doors;

public sealed class DoorSystem : IDisposable
{
    private const string CrushDeathMessage = "Crushed by a door";
    private const float PushMargin = 0.02f;
    private const float DropRadius = 0.15f;
    private const float PushedSpeedMultiplier = 1.5f;
    private const string MovingBlockShader = "Resources/Shaders/movingBlock";

    private sealed class DoorRecord
    {
        public DoorBlock Door;
        public Vector3i Origin;
        public float Progress;
        public int AnimationDirection;
        public bool WaitingForMesh;
        public bool? PendingOpen;
        public Vector3 BoxMinOffset;
        public Vector3 BoxSize;
        public Vector3 HomeLight;
        public Vector3 OpenLight;
        public SimpleBlock Visual;
        public readonly List<Chunk> AffectedChunks = new List<Chunk>();
    }

    private static readonly List<MultiblockCell> _cells = new List<MultiblockCell>();

    private readonly SpaceEntity _entity;
    private readonly Dictionary<DoorBlock, DoorRecord> _doors = new Dictionary<DoorBlock, DoorRecord>();
    private readonly ConcurrentQueue<(Vector3i, DoorBlock)> _pending = new ConcurrentQueue<(Vector3i, DoorBlock)>();
    private readonly HashSet<Vector3i> _reserved = new HashSet<Vector3i>();
    private readonly List<Block> _movingBlocks = new List<Block>();
    private readonly List<Vector3i> _fromCells = new List<Vector3i>();
    private readonly List<Vector3i> _toCells = new List<Vector3i>();
    private readonly HashSet<Vector3i> _doorVolume = new HashSet<Vector3i>();
    private bool _reservedDirty;

    public static event Action<SpaceEntity, Vector3i, bool> StateChanged;

    public void ApplyRemoteState(Vector3i home, bool open)
    {
        FlushPending();

        foreach (var record in _doors.Values)
        {
            if (GetHome(record) != home) continue;

            if (record.AnimationDirection != 0 || record.WaitingForMesh)
            {
                record.PendingOpen = open;
                return;
            }

            if (record.Door.IsOpen == open) return;
            if (open) Open(record);
            else Close(record);
            return;
        }
    }

    public DoorSystem(SpaceEntity entity)
    {
        _entity = entity;
    }

    public static Vector3i GetSlideOffset(DoorBlockData data, Direction direction, Rotation rotation)
    {
        return MultiblockLayout.ToEntityOffset(data.ModelSlide, MultiblockLayout.GetTransform(data, direction, rotation));
    }

    public static void GetSlideArea(DoorBlockData data, Direction direction, Rotation rotation, List<Vector3i> offsets)
    {
        offsets.Clear();
        var slide = GetSlideOffset(data, direction, rotation);
        MultiblockLayout.GetCells(data, direction, rotation, _cells);
        for (int i = 0; i < _cells.Count; i++)
            offsets.Add(_cells[i].Offset + slide);
    }

    public void RegisterLoaded(Chunk chunk, int x, int y, int z, DoorBlock door)
    {
        _pending.Enqueue((ToLocal(chunk, x, y, z), door));
    }

    public void Register(Vector3i origin, DoorBlock door)
    {
        _doors[door] = new DoorRecord { Door = door, Origin = origin, Progress = door.IsOpen ? 1f : 0f };
        _reservedDirty = true;
    }

    public void Unregister(DoorBlock door)
    {
        if (!_doors.Remove(door, out var record)) return;

        DestroyVisual(record);
        _reservedDirty = true;
    }

    public bool IsReserved(Vector3i localCell)
    {
        FlushPending();
        if (_reservedDirty) RebuildReserved();
        return _reserved.Contains(localCell);
    }

    public void Toggle(Chunk chunk, Vector3Byte index)
    {
        FlushPending();

        var cellBlock = chunk.Blocks[index.X, index.Y, index.Z];
        if (cellBlock is not DoorBlock cellDoor || cellDoor.Data == null) return;

        var originLocal = ToLocal(chunk, index.X, index.Y, index.Z) - MultiblockLayout.GetOriginOffset(cellDoor.Data, cellDoor);
        if (!_entity.TryGetBlockLocal(originLocal, out var originChunk, out var originIndex)) return;
        if (originChunk.Blocks[originIndex.X, originIndex.Y, originIndex.Z] is not DoorBlock door) return;
        if (!_doors.TryGetValue(door, out var record)) return;
        if (record.AnimationDirection != 0 || record.WaitingForMesh) return;

        if (door.IsOpen) Close(record);
        else Open(record);
    }

    public void Update(float deltaTime)
    {
        FlushPending();
        if (_doors.Count == 0) return;

        foreach (var record in _doors.Values)
        {
            if (record.AnimationDirection != 0)
            {
                Animate(record, deltaTime);
            }
            else if (record.WaitingForMesh)
            {
                if (AreChunksMeshed(record)) DestroyVisual(record);
            }
            else if (record.PendingOpen.HasValue)
            {
                bool open = record.PendingOpen.Value;
                record.PendingOpen = null;
                if (record.Door.IsOpen != open)
                {
                    if (open) Open(record);
                    else Close(record);
                }
            }
            else if (!Spacebox.Network.GameSession.IsRemoteClient && record.Door.IsOpen && record.Door.Data.AutoClose)
            {
                record.Door.AutoCloseTimer -= deltaTime;
                if (record.Door.AutoCloseTimer <= 0f) Close(record);
            }
        }
    }

    public void Render()
    {
        if (_doors.Count == 0) return;

        foreach (var record in _doors.Values)
        {
            if (record.Visual == null) continue;

            var slide = GetSlideOffset(record.Door);
            var home = GetHome(record);
            var origin = new Vector3(home.X, home.Y, home.Z) + new Vector3(slide.X, slide.Y, slide.Z) * record.Progress;

            record.Visual.Position = _entity.ToWorldSpace(_entity.LocalPositionToWorld(origin) + new Vector3(0.5f));
            record.Visual.SetBlockTransform(MultiblockLayout.GetTransform(record.Door.Data, record.Door.Direction, record.Door.Rotation) * _entity.RotationMatrix);
            record.Visual.ExtraLight = Vector3.Lerp(record.HomeLight, record.OpenLight, record.Progress);
            record.Visual.Render();
        }
    }

    public void Dispose()
    {
        foreach (var record in _doors.Values)
            DestroyVisual(record);

        _doors.Clear();
        _reserved.Clear();
    }

    private void Open(DoorRecord record)
    {
        var slide = GetSlideOffset(record.Door);
        CollectCells(record, GetHome(record), _fromCells);
        CollectCells(record, GetHome(record) + slide, _toCells);

        if (!AreCellsFree(_toCells)) return;

        MoveCells(record, slide);
        record.Door.IsOpen = true;
        record.Door.AutoCloseTimer = record.Door.Data.OpenTime;
        StartAnimation(record, 1);
        StateChanged?.Invoke(_entity, GetHome(record), true);
    }

    private void Close(DoorRecord record)
    {
        var slide = GetSlideOffset(record.Door);
        var home = GetHome(record);
        CollectCells(record, home + slide, _fromCells);
        CollectCells(record, home, _toCells);

        if (!AreCellsFree(_toCells))
        {
            record.Door.AutoCloseTimer = record.Door.Data.OpenTime;
            return;
        }

        MoveCells(record, -slide);
        record.Door.IsOpen = false;
        record.Door.AutoCloseTimer = 0f;
        StartAnimation(record, -1);
        StateChanged?.Invoke(_entity, GetHome(record), false);
    }

    private void StartAnimation(DoorRecord record, int direction)
    {
        record.AnimationDirection = direction;
        record.Progress = direction > 0 ? 0f : 1f;
        record.WaitingForMesh = false;
        SampleLight(record, direction);
        SetHidden(record, true);
        CalculateBox(record);
        PlaySound(record, direction);

        if (record.Visual == null)
        {
            var (vertices, indices) = MultiblockPreviewMesh.Build(record.Door.Data, BlockState.Active);
            var shader = Resources.Load<Shader>(MovingBlockShader, true);
            record.Visual = new SimpleBlock(new TextureMaterial(GameAssets.BlocksTexture, shader) { WritesGBuffer = true }, Vector3.Zero) { DisposesTexture = false };
            record.Visual.SetCustomMesh(vertices, indices);
        }

        _reservedDirty = true;
    }

    private void PlaySound(DoorRecord record, int direction)
    {
        var data = record.Door.Data;
        GetMovingBox(record, out var min, out var max);
        DoorSounds.Play(direction > 0 ? data.OpenSound : data.CloseSound, _entity.EntityLocalToWorld((min + max) * 0.5f));
    }

    private void SampleLight(DoorRecord record, int direction)
    {
        _doorVolume.Clear();
        _doorVolume.UnionWith(_fromCells);
        _doorVolume.UnionWith(_toCells);

        var fromLight = BlockLightSampler.AverageAroundCells(_entity, _fromCells, _doorVolume);
        var toLight = BlockLightSampler.AverageAroundCells(_entity, _toCells, _doorVolume);

        record.HomeLight = direction > 0 ? fromLight : toLight;
        record.OpenLight = direction > 0 ? toLight : fromLight;
    }

    private void Animate(DoorRecord record, float deltaTime)
    {
        float step = deltaTime * record.Door.Data.Speed / Math.Max(0.001f, record.Door.Data.SlideDistance);
        record.Progress = Math.Clamp(record.Progress + step * record.AnimationDirection, 0f, 1f);

        PushPlayer(record);
        PushBodies(record);
        PushDrops(record);

        bool finished = record.AnimationDirection > 0 ? record.Progress >= 1f : record.Progress <= 0f;
        if (!finished) return;

        record.AnimationDirection = 0;
        SetHidden(record, false);
        record.WaitingForMesh = true;
    }

    private void MoveCells(DoorRecord record, Vector3i delta)
    {
        _movingBlocks.Clear();

        for (int i = 0; i < _fromCells.Count; i++)
        {
            if (_entity.TryGetBlockLocal(_fromCells[i], out var chunk, out var index))
                _movingBlocks.Add(chunk.Blocks[index.X, index.Y, index.Z]);
            else
                _movingBlocks.Add(null);
        }

        for (int i = 0; i < _fromCells.Count; i++)
        {
            if (_movingBlocks[i] != null)
                _entity.ClearCellSilently(_fromCells[i]);
        }

        for (int i = 0; i < _toCells.Count; i++)
        {
            if (_movingBlocks[i] != null)
                _entity.PlaceCellRaw(_toCells[i], _movingBlocks[i]);
        }

        record.Origin += delta;
    }

    private void SetHidden(DoorRecord record, bool hidden)
    {
        record.AffectedChunks.Clear();
        CollectCells(record, record.Origin, _toCells);

        for (int i = 0; i < _toCells.Count; i++)
        {
            if (!_entity.TryGetBlockLocal(_toCells[i], out var chunk, out var index)) continue;

            var block = chunk.Blocks[index.X, index.Y, index.Z];
            if (block is not DoorBlock) continue;

            block.IsMeshHidden = hidden;
            if (!record.AffectedChunks.Contains(chunk)) record.AffectedChunks.Add(chunk);
        }

        for (int i = 0; i < record.AffectedChunks.Count; i++)
            record.AffectedChunks[i].QueueMeshUpdate(UpdateReason.PlayerAction);
    }

    private static bool AreChunksMeshed(DoorRecord record)
    {
        for (int i = 0; i < record.AffectedChunks.Count; i++)
        {
            var chunk = record.AffectedChunks[i];
            if (!chunk.IsDisposed && chunk.IsQueuedForMeshUpdate) return false;
        }
        return true;
    }

    private bool AreCellsFree(List<Vector3i> cells)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            if (!_entity.TryGetBlockLocal(cells[i], out var chunk, out var index)) continue;

            var block = chunk.Blocks[index.X, index.Y, index.Z];
            if (block != null && !block.IsAir) return false;
        }
        return true;
    }

    public bool IntersectsMovingDoor(Vector3 localCenter, float radius)
    {
        if (_doors.Count == 0) return false;

        foreach (var record in _doors.Values)
        {
            if (record.AnimationDirection == 0) continue;

            GetMovingBox(record, out var min, out var max);
            if (SphereIntersectsBox(localCenter, radius, min, max)) return true;
        }
        return false;
    }

    public bool TryGetMovingDoorContact(Vector3 localCenter, float radius, out Vector3 normal, out float depth)
    {
        normal = Vector3.Zero;
        depth = 0f;
        if (_doors.Count == 0) return false;

        bool found = false;
        foreach (var record in _doors.Values)
        {
            if (record.AnimationDirection == 0) continue;

            GetMovingBox(record, out var min, out var max);
            if (!SphereBoxMath.TryGetContact(localCenter, radius, min, max, out var candidateNormal, out float candidateDepth)) continue;
            if (candidateDepth <= depth) continue;

            normal = candidateNormal;
            depth = candidateDepth;
            found = true;
        }
        return found;
    }

    public bool RaycastMovingDoors(Engine.Physics.Ray entitySpaceRay, out HitInfo hitInfo, out float distance)
    {
        hitInfo = default;
        distance = float.MaxValue;
        if (_doors.Count == 0) return false;

        DoorRecord closest = null;
        Vector3 closestNormal = Vector3.Zero;
        var origin = _entity.WorldPositionToLocal(entitySpaceRay.Origin);

        foreach (var record in _doors.Values)
        {
            if (record.AnimationDirection == 0) continue;

            GetMovingBox(record, out var min, out var max);
            if (RayIntersectsBox(origin, entitySpaceRay.Direction, entitySpaceRay.Length, min, max, out float hitDistance, out var normal) && hitDistance < distance)
            {
                distance = hitDistance;
                closest = record;
                closestNormal = normal;
            }
        }

        if (closest == null) return false;
        if (!_entity.TryGetBlockLocal(closest.Origin, out var chunk, out var index)) return false;

        var block = chunk.Blocks[index.X, index.Y, index.Z];
        hitInfo.hitPosition = entitySpaceRay.Origin + entitySpaceRay.Direction * distance;
        hitInfo.blockPositionWorld = chunk.PositionWorld + new Vector3(index.X, index.Y, index.Z) + new Vector3(0.5f);
        hitInfo.blockPositionIndex = index;
        hitInfo.blockPositionEntity = closest.Origin;
        hitInfo.normal = new Vector3SByte((sbyte)closestNormal.X, (sbyte)closestNormal.Y, (sbyte)closestNormal.Z);
        hitInfo.normalWorld = closestNormal;
        hitInfo.chunk = chunk;
        hitInfo.block = block;
        return true;
    }

    private static bool RayIntersectsBox(Vector3 origin, Vector3 direction, float length, Vector3 min, Vector3 max, out float distance, out Vector3 normal)
    {
        float tMin = 0f;
        float tMax = length;
        normal = Vector3.Zero;
        distance = 0f;

        for (int axis = 0; axis < 3; axis++)
        {
            float o = origin[axis];
            float d = direction[axis];

            if (MathF.Abs(d) < 1e-6f)
            {
                if (o < min[axis] || o > max[axis]) return false;
                continue;
            }

            float t1 = (min[axis] - o) / d;
            float t2 = (max[axis] - o) / d;
            float sign = -1f;
            if (t1 > t2)
            {
                (t1, t2) = (t2, t1);
                sign = 1f;
            }

            if (t1 > tMin)
            {
                tMin = t1;
                normal = Vector3.Zero;
                normal[axis] = sign;
            }

            if (t2 < tMax) tMax = t2;
            if (tMin > tMax) return false;
        }

        if (normal == Vector3.Zero) return false;

        distance = tMin;
        return true;
    }

    private void PushBodies(DoorRecord record)
    {
        var physics = World.Instance?.Physics;
        if (physics == null) return;

        GetMovingBox(record, out var min, out var max);
        var direction = GetMoveDirection(record);
        var velocityWorld = _entity.DirectionToWorldSpace(direction) * record.Door.Data.Speed * PushedSpeedMultiplier;
        var bodies = physics.Bodies;

        for (int i = 0; i < bodies.Count; i++)
        {
            var body = bodies[i];
            if (body.IsKinematic || !body.Enabled || body.Owner == null) continue;

            var local = _entity.WorldPositionToLocal(_entity.ToEntitySpace(body.Center));
            if (!SphereIntersectsBox(local, body.Radius, min, max)) continue;
            if (Vector3.Dot(local - (min + max) * 0.5f, direction) <= 0f) continue;

            var pushed = local + direction * GetPushDistance(local, body.Radius, min, max, direction);
            body.Owner.Position = _entity.ToWorldSpace(_entity.LocalPositionToWorld(pushed));

            float along = Vector3.Dot(body.Velocity, velocityWorld.Normalized());
            if (along < velocityWorld.Length)
                body.Velocity += velocityWorld.Normalized() * (velocityWorld.Length - along);
        }
    }

    private void PushDrops(DoorRecord record)
    {
        var drops = World.DropEffectManager;
        if (drops == null) return;

        GetMovingBox(record, out var min, out var max);
        var direction = GetMoveDirection(record);
        var velocityWorld = _entity.DirectionToWorldSpace(direction) * record.Door.Data.Speed * PushedSpeedMultiplier;

        drops.PushDrops(position =>
        {
            var local = _entity.WorldPositionToLocal(_entity.ToEntitySpace(position));
            if (!SphereIntersectsBox(local, DropRadius, min, max)) return null;
            if (Vector3.Dot(local - (min + max) * 0.5f, direction) <= 0f) return null;

            var pushed = local + direction * GetPushDistance(local, DropRadius, min, max, direction);
            return _entity.ToWorldSpace(_entity.LocalPositionToWorld(pushed));
        }, velocityWorld);
    }

    private Vector3 GetMoveDirection(DoorRecord record)
    {
        var slide = GetSlideOffset(record.Door);
        return new Vector3(slide.X, slide.Y, slide.Z).Normalized() * record.AnimationDirection;
    }

    private void PushPlayer(DoorRecord record)
    {
        var player = World.Instance?.Player;
        if (player == null || !player.IsAlive) return;

        float radius = player.Body?.Radius ?? 0.4f;
        var local = _entity.WorldPositionToLocal(_entity.ToEntitySpace(player.Position));

        GetMovingBox(record, out var min, out var max);
        if (!SphereIntersectsBox(local, radius, min, max)) return;

        var slide = GetSlideOffset(record.Door);
        var direction = new Vector3(slide.X, slide.Y, slide.Z).Normalized() * record.AnimationDirection;
        if (Vector3.Dot(local - (min + max) * 0.5f, direction) <= 0f) return;

        var pushed = local + direction * GetPushDistance(local, radius, min, max, direction);
        var pushedWorld = _entity.ToWorldSpace(_entity.LocalPositionToWorld(pushed));

        var physics = World.Instance.Physics;
        if (physics != null && physics.OverlapsVoxels(pushedWorld, radius, out _))
        {
            CrushPlayer();
            return;
        }

        player.SetPosition(pushedWorld);
    }

    private static float GetPushDistance(Vector3 center, float radius, Vector3 min, Vector3 max, Vector3 direction)
    {
        float distance = 0f;
        if (direction.X > 0.5f) distance = max.X + radius - center.X;
        else if (direction.X < -0.5f) distance = center.X - (min.X - radius);
        else if (direction.Y > 0.5f) distance = max.Y + radius - center.Y;
        else if (direction.Y < -0.5f) distance = center.Y - (min.Y - radius);
        else if (direction.Z > 0.5f) distance = max.Z + radius - center.Z;
        else if (direction.Z < -0.5f) distance = center.Z - (min.Z - radius);

        return Math.Max(0f, distance) + PushMargin;
    }

    private static bool SphereIntersectsBox(Vector3 center, float radius, Vector3 min, Vector3 max)
    {
        var closest = Vector3.Clamp(center, min, max);
        return (closest - center).LengthSquared < radius * radius;
    }

    private void GetMovingBox(DoorRecord record, out Vector3 min, out Vector3 max)
    {
        var home = GetHome(record);
        var slide = GetSlideOffset(record.Door);
        min = new Vector3(home.X, home.Y, home.Z) + record.BoxMinOffset + new Vector3(slide.X, slide.Y, slide.Z) * record.Progress;
        max = min + record.BoxSize;
    }

    private void CalculateBox(DoorRecord record)
    {
        MultiblockLayout.GetCells(record.Door.Data, record.Door.Direction, record.Door.Rotation, _cells);

        var min = new Vector3i(int.MaxValue);
        var max = new Vector3i(int.MinValue);
        for (int i = 0; i < _cells.Count; i++)
        {
            min = Vector3i.ComponentMin(min, _cells[i].Offset);
            max = Vector3i.ComponentMax(max, _cells[i].Offset);
        }

        record.BoxMinOffset = new Vector3(min.X, min.Y, min.Z);
        record.BoxSize = new Vector3(max.X - min.X + 1, max.Y - min.Y + 1, max.Z - min.Z + 1);
    }

    private static void CrushPlayer()
    {
        var player = World.Instance?.Player;
        if (player == null || !player.IsAlive) return;

        player.TakeDamage(player.HealthBar.StatsData.Value, new DeathCase(CrushDeathMessage));
    }

    private void CollectCells(DoorRecord record, Vector3i origin, List<Vector3i> result)
    {
        result.Clear();
        MultiblockLayout.GetCells(record.Door.Data, record.Door.Direction, record.Door.Rotation, _cells);
        for (int i = 0; i < _cells.Count; i++)
            result.Add(origin + _cells[i].Offset);
    }

    private Vector3i GetHome(DoorRecord record)
    {
        return record.Door.IsOpen ? record.Origin - GetSlideOffset(record.Door) : record.Origin;
    }

    private static Vector3i GetSlideOffset(DoorBlock door)
    {
        return GetSlideOffset(door.Data, door.Direction, door.Rotation);
    }

    private void RebuildReserved()
    {
        _reservedDirty = false;
        _reserved.Clear();

        foreach (var record in _doors.Values)
        {
            var home = GetHome(record);
            var free = record.Door.IsOpen ? home : home + GetSlideOffset(record.Door);
            CollectCells(record, free, _fromCells);

            for (int i = 0; i < _fromCells.Count; i++)
                _reserved.Add(_fromCells[i]);
        }
    }

    private void FlushPending()
    {
        while (_pending.TryDequeue(out var pending))
            Register(pending.Item1, pending.Item2);
    }

    private static Vector3i ToLocal(Chunk chunk, int x, int y, int z)
    {
        return new Vector3i(
            chunk.PositionIndex.X * Chunk.Size + x,
            chunk.PositionIndex.Y * Chunk.Size + y,
            chunk.PositionIndex.Z * Chunk.Size + z);
    }

    private static void DestroyVisual(DoorRecord record)
    {
        record.Visual?.Destroy();
        record.Visual = null;
        record.WaitingForMesh = false;
    }
}
