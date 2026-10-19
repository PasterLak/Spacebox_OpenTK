using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Effects;

public sealed class EntityBoxOutline : IDisposable
{
    private static readonly int[] EdgePath = { 0, 1, 2, 3, 0, 4, 5, 6, 7, 4, 5, 1, 2, 6, 7, 3 };

    private readonly LineRenderer _lines;
    private readonly List<Vector3> _points = new List<Vector3>(EdgePath.Length);
    private readonly Vector3[] _corners = new Vector3[8];
    private readonly float _inflate;

    private SpaceEntity _entity;
    private Vector3i _min;
    private Vector3i _max;
    private Vector3 _entityPosition;
    private Quaternion _entityOrientation;

    public bool Enabled { get; private set; }

    public EntityBoxOutline(Color4 color, float thickness, float inflate)
    {
        _inflate = inflate;
        _lines = new LineRenderer { Color = color, Thickness = thickness };
    }

    public void Show(SpaceEntity entity, Vector3i minCell, Vector3i maxCell)
    {
        if (entity == null)
        {
            Hide();
            return;
        }

        var min = Vector3i.ComponentMin(minCell, maxCell);
        var max = Vector3i.ComponentMax(minCell, maxCell);
        Enabled = true;

        if (_entity == entity && _min == min && _max == max &&
            _entityPosition == entity.PositionWorld && _entityOrientation == entity.Orientation)
            return;

        _entity = entity;
        _min = min;
        _max = max;
        _entityPosition = entity.PositionWorld;
        _entityOrientation = entity.Orientation;
        Rebuild();
    }

    public void Hide()
    {
        Enabled = false;
    }

    public void Render()
    {
        if (Enabled) _lines.Render();
    }

    public void Dispose()
    {
        _lines.Dispose();
    }

    private void Rebuild()
    {
        var low = new Vector3(_min.X, _min.Y, _min.Z) - new Vector3(_inflate);
        var high = new Vector3(_max.X + 1, _max.Y + 1, _max.Z + 1) + new Vector3(_inflate);

        _corners[0] = new Vector3(low.X, low.Y, low.Z);
        _corners[1] = new Vector3(high.X, low.Y, low.Z);
        _corners[2] = new Vector3(high.X, high.Y, low.Z);
        _corners[3] = new Vector3(low.X, high.Y, low.Z);
        _corners[4] = new Vector3(low.X, low.Y, high.Z);
        _corners[5] = new Vector3(high.X, low.Y, high.Z);
        _corners[6] = new Vector3(high.X, high.Y, high.Z);
        _corners[7] = new Vector3(low.X, high.Y, high.Z);

        _points.Clear();
        for (int i = 0; i < EdgePath.Length; i++)
            _points.Add(_entity.EntityLocalToWorld(_corners[EdgePath[i]]));

        _lines.SetPoints(_points);
    }
}
