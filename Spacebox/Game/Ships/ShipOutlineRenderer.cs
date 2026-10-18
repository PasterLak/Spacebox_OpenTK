using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Ships;

public sealed class ShipOutlineRenderer : IDisposable
{
    private const float Padding = 0.03f;

    private static readonly Vector3[] StripCorners =
    {
        new Vector3(-1, -1, -1), new Vector3(1, -1, -1), new Vector3(1, 1, -1), new Vector3(-1, 1, -1),
        new Vector3(-1, -1, -1), new Vector3(-1, -1, 1), new Vector3(1, -1, 1), new Vector3(1, 1, 1),
        new Vector3(-1, 1, 1), new Vector3(-1, -1, 1), new Vector3(1, -1, 1), new Vector3(1, -1, -1),
        new Vector3(1, 1, -1), new Vector3(1, 1, 1), new Vector3(-1, 1, 1), new Vector3(-1, 1, -1)
    };

    private readonly List<Outline> _outlines = new List<Outline>();
    private int _used;

    public void Begin()
    {
        _used = 0;
    }

    public void Add(SpaceEntity entity, Vector3 centerLocal, Vector3 size, Color4 color, float thickness)
    {
        if (entity == null) return;
        if (_used == _outlines.Count) _outlines.Add(new Outline());

        var outline = _outlines[_used++];
        outline.Set(size + new Vector3(Padding * 2f), color, thickness);
        outline.Line.Position = entity.EntityLocalToWorld(centerLocal);
        outline.Line.Rotation = Node3D.QuaternionToEuler(entity.Orientation);
    }

    public void Render()
    {
        for (int i = 0; i < _used; i++)
            _outlines[i].Line.Render();
    }

    public void Dispose()
    {
        for (int i = 0; i < _outlines.Count; i++)
            _outlines[i].Line.Dispose();
        _outlines.Clear();
        _used = 0;
    }

    private sealed class Outline
    {
        public readonly LineRenderer Line = new LineRenderer();
        private Vector3 _size = new Vector3(-1f);

        public void Set(Vector3 size, Color4 color, float thickness)
        {
            if (Line.Color != color) Line.Color = color;
            if (Line.Thickness != thickness) Line.Thickness = thickness;
            if (_size == size) return;

            _size = size;
            Line.Points.Clear();
            var half = size * 0.5f;
            for (int i = 0; i < StripCorners.Length; i++)
                Line.Points.Add(StripCorners[i] * half);
            Line.SetNeedsRebuild();
        }
    }
}
