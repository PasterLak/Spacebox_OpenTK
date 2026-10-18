using OpenTK.Mathematics;
using Spacebox.Game.Mobs.Navigation;

namespace Spacebox.Game.Mobs.Feeding;

public sealed class FoodFinder
{
    private const int SamplesPerStep = 48;

    private static readonly Vector3i[] Faces =
    {
        new Vector3i(1, 0, 0), new Vector3i(-1, 0, 0),
        new Vector3i(0, 1, 0), new Vector3i(0, -1, 0),
        new Vector3i(0, 0, 1), new Vector3i(0, 0, -1)
    };

    private readonly Random _random;
    private MobNavigationGrid? _grid;
    private MobDefinition? _definition;
    private Vector3i _origin;
    private float _radius;
    private FoodTarget _best;
    private int _bestDistance;

    public FoodFinder(Random random)
    {
        _random = random;
    }

    public FoodTarget Result => _best;

    public void Begin(MobNavigationGrid grid, MobDefinition definition, Vector3 worldPosition)
    {
        _grid = grid;
        _definition = definition;
        _origin = grid.WorldToCell(worldPosition);
        _radius = definition.FoodSearchRadius;
        _best = default;
        _bestDistance = int.MaxValue;
    }

    public void Step()
    {
        if (_grid?.Anchor == null || _definition == null) return;

        for (int i = 0; i < SamplesPerStep; i++)
        {
            float reach = _radius * (float)_random.NextDouble();
            var cell = _origin + new Vector3i(RandomOffset(reach), RandomOffset(reach), RandomOffset(reach));

            int distance = (cell - _origin).ManhattanLength;
            if (distance >= _bestDistance) continue;
            if (!_grid.TryGetBlock(cell, out _, out _, out var block) || block.IsAir || !_definition.CanEat(block.Id)) continue;
            if (!TryFindFeedCell(cell, out var feedCell)) continue;

            _best = new FoodTarget(_grid.Anchor, cell, feedCell);
            _bestDistance = distance;
        }
    }

    private bool TryFindFeedCell(Vector3i cell, out Vector3i feedCell)
    {
        int start = _random.Next(Faces.Length);
        for (int i = 0; i < Faces.Length; i++)
        {
            var candidate = cell + Faces[(start + i) % Faces.Length];
            if (!_grid!.IsPassable(candidate)) continue;

            feedCell = candidate;
            return true;
        }

        feedCell = default;
        return false;
    }

    private int RandomOffset(float reach)
    {
        return (int)MathF.Round(((float)_random.NextDouble() * 2f - 1f) * reach);
    }
}
