using Engine;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Mobs;

namespace Spacebox.Editor.Preview;

public sealed class MobPreviewContent : IPreviewContent
{
    private const int CircleSegments = 48;

    private readonly Model _model;
    private readonly MobAnimator _animator;
    private readonly LineRenderer _hitRadius = new LineRenderer();
    private readonly LineRenderer _attackRange = new LineRenderer();
    private readonly LineRenderer _blockOutline = new LineRenderer();

    public Vector3 Center { get; }
    public float Radius { get; }
    public bool IsReady => _model != null;
    public Vector2 ViewAngles => new Vector2(35f, 20f);

    public MobAction Action { get; set; } = MobAction.Idle;
    public bool ShowRanges { get; set; } = true;
    public bool ShowBlock { get; set; } = true;

    public MobPreviewContent(MobDefinition definition, float scale, float hitRadius, float attackRange)
    {
        var texture = GameAssets.LoadResource<Texture2D>(definition.TexturePath);
        if (!texture.YWasFlipped) texture.FlipY();
        texture.FilterMode = FilterMode.Nearest;

        var material = new TextureMaterial(texture) { ExtraLight = Vector3.One };
        _model = new Model(GameAssets.LoadResource<Mesh>(definition.ModelPath), material);
        _model.Position = Vector3.Zero;
        _animator = new MobAnimator(_model, scale, 0f);

        var bounds = _model.Mesh.GetBounds();
        var min = bounds.Size == Vector3.Zero ? new Vector3(-0.5f) : bounds.Min;
        var max = bounds.Size == Vector3.Zero ? new Vector3(0.5f) : bounds.Max;
        Center = (min + max) * 0.5f * scale;
        Radius = MathF.Max((max - min).Length * 0.5f * scale, attackRange) + 0.5f;

        float floor = min.Y * scale;
        Setup(_hitRadius, MobPreviewShapes.Circle(hitRadius, floor, CircleSegments), new Color4(0.3f, 0.9f, 0.4f, 1f), 0.02f);
        Setup(_attackRange, MobPreviewShapes.Circle(attackRange, floor, CircleSegments), new Color4(0.95f, 0.35f, 0.3f, 1f), 0.02f);
        Setup(_blockOutline, MobPreviewShapes.CubeEdges(new Vector3(max.X * scale + 0.5f, floor, -0.5f), 1f), new Color4(0.7f, 0.7f, 0.75f, 1f), 0.015f);
    }

    public void PlayBite() => _animator.PlayBite();

    public void PlayHurt() => _animator.PlayHurt();

    public void Update(float deltaTime)
    {
        _animator.Update(deltaTime, Action);
    }

    public void Render()
    {
        _model?.Render();
        if (ShowRanges)
        {
            _hitRadius.Render();
            _attackRange.Render();
        }
        if (ShowBlock) _blockOutline.Render();
    }

    public void Dispose()
    {
        _hitRadius.Dispose();
        _attackRange.Dispose();
        _blockOutline.Dispose();
    }

    private static void Setup(LineRenderer line, List<Vector3> points, Color4 color, float thickness)
    {
        line.Color = color;
        line.Thickness = thickness;
        line.SetPoints(points);
    }
}
