using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.Canvas.Hud;

public sealed class HitImageControl : HudOverlayControl
{
    private const float Scale = 0.1f;

    public HitImage Image { get; set; }

    protected override bool IsShown() => base.IsShown() && Image != null && Image.IsVisible && Image.Texture != null;

    protected override void DrawOverlay(ICanvasRenderer renderer)
    {
        var texture = Image.Texture;
        var rect = LocalRect;
        float maxWidth = rect.Width * Scale;
        float maxHeight = rect.Height * Scale;
        float aspect = (float)texture.Width / texture.Height;
        var size = maxWidth / aspect <= maxHeight ? new Vector2(maxWidth, maxWidth / aspect) : new Vector2(maxHeight * aspect, maxHeight);
        var target = new Rect2(MathF.Round((rect.Width - size.X) * 0.5f), MathF.Round((rect.Height - size.Y) * 0.5f), size.X, size.Y);
        renderer.DrawTexture(texture, target, CanvasDraw.UvOf(texture, null, false, true), new Color4(1f, 1f, 1f, Image.Opacity));
    }
}
