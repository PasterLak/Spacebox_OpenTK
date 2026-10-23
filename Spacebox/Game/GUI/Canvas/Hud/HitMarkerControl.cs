using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.Canvas.Hud;

public sealed class HitMarkerControl : HudOverlayControl
{
    private const float SizeRatio = 1.2f;
    private const float GapRatio = 0.5f;
    private static readonly Color4 HitColor = Color4.White;
    private static readonly Color4 HeadshotColor = new Color4(1f, 0.2f, 0.2f, 1f);
    private static readonly Vector2[] Directions = { new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f) };

    private readonly HitMarkerUI _hitMarker;

    public HitMarkerControl(HitMarkerUI hitMarker)
    {
        _hitMarker = hitMarker;
    }

    protected override bool IsShown() => _hitMarker.IsVisible;

    protected override void DrawOverlay(ICanvasRenderer renderer)
    {
        float length = Unit * SizeRatio;
        float gap = Unit * GapRatio;
        float thickness = _hitMarker.IsHeadshot ? 3f : 2f;
        var color = _hitMarker.IsHeadshot ? HeadshotColor : HitColor;
        foreach (var direction in Directions)
        {
            var normal = direction.Normalized();
            DrawLine(renderer, Center + normal * gap, Center + normal * (gap + length), thickness, color);
        }
    }
}
