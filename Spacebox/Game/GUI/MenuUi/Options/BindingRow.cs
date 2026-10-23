using Engine.CanvasUI;
using Engine.InputPro;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class BindingRow : HBoxContainer
{
    private readonly Label _binding = new Label { CustomMinimumSize = new Vector2(140f, 0f), HorizontalAlignment = HorizontalAlignment.Center, SizeFlagsVertical = SizeFlags.ShrinkCenter, MouseFilter = MouseFilter.Stop };

    public BindingRow(InputAction action, Action change)
    {
        Action = action;
        CustomMinimumSize = new Vector2(0f, 28f);
        AddThemeConstantOverride("separation", 10f);
        string tooltip = string.IsNullOrEmpty(action.Description) ? action.Name : action.Description;
        AddChild(new Label { Text = action.Name, TooltipText = tooltip, CustomMinimumSize = new Vector2(150f, 0f), SizeFlagsVertical = SizeFlags.ShrinkCenter, MouseFilter = MouseFilter.Stop });
        AddChild(new Label { Text = action.Description, ThemeTypeVariation = "MutedLabel", Overrun = TextOverrun.Ellipsis, TooltipText = tooltip, SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand, SizeFlagsVertical = SizeFlags.ShrinkCenter, MouseFilter = MouseFilter.Stop });
        AddChild(_binding);
        var button = new Button { Text = "Change", ThemeTypeVariation = "SmallButton", CustomMinimumSize = new Vector2(80f, 0f), SizeFlagsVertical = SizeFlags.ShrinkCenter, TooltipText = "Click, then press the new key or mouse button. Save keeps the change, Back restores the old keys." };
        button.Pressed += _ => change();
        AddChild(button);
    }

    public InputAction Action { get; }

    public void SetBinding(string text, bool duplicate)
    {
        _binding.Text = text;
        _binding.ThemeTypeVariation = duplicate ? "WarningLabel" : "Label";
        _binding.TooltipText = duplicate ? "This key is also used by another action." : "Current key for this action.";
    }
}
