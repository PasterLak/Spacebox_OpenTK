using Engine.CanvasUI;

namespace Engine.UI;

public abstract class DevPanelSection : FoldableContainer
{
    protected DevPanelSection(string title) : base(title)
    {
        SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand;
        Content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand };
        AddChild(Content);
    }

    protected VBoxContainer Content { get; }

    public abstract void Refresh();

    protected static HBoxContainer Row(string label, Control field)
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand };
        row.AddChild(new Label { Text = label, CustomMinimumSize = new OpenTK.Mathematics.Vector2(80f, 0f) });
        field.SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand;
        row.AddChild(field);
        return row;
    }
}
