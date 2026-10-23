using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.Menu;
using Spacebox.Network.Discovery;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class SignalBarsControl : Control
{
    private const float BarWidth = 3f;
    private const float Gap = 1f;
    private const float Height = 12f;

    public SignalBarsControl()
    {
        MouseFilter = MouseFilter.Ignore;
        CustomMinimumSize = new Vector2(ServerSignalBars.BarCount * (BarWidth + Gap) - Gap, Height);
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
    }

    public ServerListItem Item { get; set; }

    protected override void Draw(ICanvasRenderer renderer)
    {
        if (Item == null) return;
        int level = ServerSignalBars.GetLevel(Item);
        var lit = ServerTextColors.State(Item);
        var dim = new Color4(0f, 0f, 0f, 0.2f);
        for (int i = 0; i < ServerSignalBars.BarCount; i++)
        {
            float barHeight = Height * (i + 1) / ServerSignalBars.BarCount;
            renderer.DrawRect(new Rect2(i * (BarWidth + Gap), Height - barHeight, BarWidth, barHeight), i < level ? lit : dim);
        }
    }
}
