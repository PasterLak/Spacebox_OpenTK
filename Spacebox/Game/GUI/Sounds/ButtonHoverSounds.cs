using Engine.CanvasUI;

namespace Spacebox.Game.GUI.Sounds;

public sealed class ButtonHoverSounds
{
    private const int MaxParentDepth = 3;

    private BaseButton _last;

    public void Update(CanvasRoot root, bool enabled)
    {
        var button = enabled && root != null ? FindButton(root.Hovered) : null;
        if (ReferenceEquals(button, _last)) return;

        _last = button;
        if (button != null && !button.Disabled && !button.ToggleMode && button.IsVisibleInTree) UiSounds.Play(UiSound.ButtonHover);
    }

    private static BaseButton FindButton(Control control)
    {
        var cursor = control;
        for (int depth = 0; cursor != null && depth <= MaxParentDepth; depth++)
        {
            if (cursor is BaseButton button) return button;
            cursor = cursor.Parent as Control;
        }
        return null;
    }
}
