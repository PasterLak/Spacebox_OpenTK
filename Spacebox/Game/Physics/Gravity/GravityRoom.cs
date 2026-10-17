using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Oxygen;

namespace Spacebox.Game.Physics.Gravity;

public sealed class GravityRoom
{
    private const long RecalculateIntervalMs = 250;

    private static readonly SealedRoomFill _fill = new SealedRoomFill();

    private readonly AirMasks _cells = new AirMasks();
    private int _layoutVersion = int.MinValue;
    private long _nextRecalculation;
    private bool _isDirty = true;

    public bool IsSealed { get; private set; }
    public int CellCount { get; private set; }

    public void Invalidate()
    {
        _isDirty = true;
    }

    public void Update(SpaceEntity entity, Vector3i origin, int radius, int maxCells)
    {
        int version = entity.Oxygen.LayoutVersion;
        if (!_isDirty && version == _layoutVersion) return;

        long now = Environment.TickCount64;
        if (!_isDirty && now < _nextRecalculation) return;

        _isDirty = false;
        _layoutVersion = version;
        _nextRecalculation = now + RecalculateIntervalMs;

        IsSealed = _fill.Fill(entity, origin, radius, maxCells, _cells);
        CellCount = _fill.CellCount;
    }

    public bool Contains(SpaceEntity entity, Vector3 worldPosition)
    {
        if (!IsSealed) return false;

        var local = entity.WorldToEntityLocal(worldPosition);
        return _cells.Contains((int)MathF.Floor(local.X), (int)MathF.Floor(local.Y), (int)MathF.Floor(local.Z));
    }

    public void Clear()
    {
        _cells.Clear();
        IsSealed = false;
        CellCount = 0;
        _isDirty = true;
    }
}
