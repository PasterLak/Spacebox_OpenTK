using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class RemapOverlay : PanelContainer
{
    private readonly ProgressBar _timeout = new ProgressBar { MinValue = 0f, MaxValue = 1f, ShowPercentage = false, CustomMinimumSize = new Vector2(0f, 8f) };
    private readonly Label _seconds = new Label { HorizontalAlignment = HorizontalAlignment.Center };

    public RemapOverlay()
    {
        CustomMinimumSize = new Vector2(320f, 0f);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 8f);
        column.AddChild(new Label { Text = "Press any key or mouse button", ThemeTypeVariation = "SectionLabel", HorizontalAlignment = HorizontalAlignment.Center });
        column.AddChild(new Label { Text = "ESC to cancel", ThemeTypeVariation = "MutedLabel", HorizontalAlignment = HorizontalAlignment.Center });
        column.AddChild(_seconds);
        column.AddChild(_timeout);
        AddChild(column);
    }

    public void Update(float progress, float secondsLeft)
    {
        _timeout.Value = Math.Clamp(progress, 0f, 1f);
        _seconds.Text = $"{(int)secondsLeft}s";
    }
}
