using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.Canvas.Hud;

public sealed class GravityIndicatorControl : HudOverlayControl
{
    private const string IconName = "gravity";
    private const float SizeRatio = 5f;
    private const float MarginRatio = 2f;

    private readonly GravityIndicatorUI _gravityIndicator;

    public GravityIndicatorControl(GravityIndicatorUI gravityIndicator)
    {
        _gravityIndicator = gravityIndicator;
    }

    protected override bool IsShown() => base.IsShown() && _gravityIndicator.IsInGravity;

    protected override void DrawOverlay(ICanvasRenderer renderer)
    {
        var icon = UIAtlas.Get(IconName);
        if (!icon.IsValid) return;

        float size = MathF.Round(Unit * SizeRatio);
        float margin = MathF.Round(Unit * MarginRatio);
        var rect = LocalRect;
        AtlasSpriteDraw.Draw(renderer, icon, new Rect2(rect.Width - size - margin, rect.Height - size - margin, size, size), Color4.White);
    }
}
