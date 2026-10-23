using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Engine.Tests.CanvasUI;

public sealed class CanvasStyleExtrasTests
{
    private static CanvasRoot CreateRoot()
    {
        var root = new CanvasRoot { StretchMode = StretchMode.Disabled, Theme = new Theme { DefaultFont = new CanvasLayoutTests.FixedFont(), DefaultFontSize = 10f } };
        root.Resize(new Vector2(800f, 600f));
        return root;
    }

    private static bool Covers(CanvasCommandList list, Vector2 point)
    {
        foreach (var command in list.Commands)
        {
            var r = command.Rect;
            if (point.X >= r.X && point.X < r.X + r.Width && point.Y >= r.Y && point.Y < r.Y + r.Height) return true;
        }
        return false;
    }

    [Fact]
    public void RoundedBox_LeavesTheCornersEmptyAndFillsTheMiddle()
    {
        var list = new CanvasCommandList();
        var box = new StyleBoxFlat(Color4.White).SetCornerRadius(6f);

        box.Draw(list, new Rect2(0f, 0f, 100f, 50f));

        Assert.False(Covers(list, new Vector2(0.5f, 0.5f)));
        Assert.False(Covers(list, new Vector2(99.5f, 49.5f)));
        Assert.True(Covers(list, new Vector2(50f, 0.5f)));
        Assert.True(Covers(list, new Vector2(0.5f, 25f)));
    }

    [Fact]
    public void CornerInset_ShrinksTowardsTheMiddleRow()
    {
        Assert.True(StyleBoxFlat.CornerInset(6f, 0) > StyleBoxFlat.CornerInset(6f, 3));
        Assert.Equal(0f, StyleBoxFlat.CornerInset(6f, 5));
    }

    [Fact]
    public void BoldLabel_IsOnePixelWider()
    {
        var root = CreateRoot();
        var plain = root.Add(new Label { Text = "Iron" });
        var bold = root.Add(new Label { Text = "Iron", Bold = true });
        root.Layout();

        Assert.Equal(plain.GetMinimumSize().X + 1f + 4 * Label.BoldLetterSpacing, bold.GetMinimumSize().X);
    }

    [Fact]
    public void Tooltip_ShowsForATitleOrAnImageWithoutText()
    {
        var root = CreateRoot();
        var panel = root.Add(new Panel { TooltipTitle = "Iron ingot", TooltipImageSize = new Vector2(40f, 20f) });
        panel.SetOffsets(0, 0, 100, 100);
        root.Layout();

        for (int i = 0; i < 12; i++) root.Update(0.1f, new PointerState { Position = new Vector2(10, 10) });

        Assert.Same(panel, root.TooltipSource);
        Assert.Null(root.TooltipText);
        Assert.True(root.TooltipPanel.Visible);
    }

    [Fact]
    public void Tooltip_ShowsCustomContentAndReleasesItWhenHidden()
    {
        var root = CreateRoot();
        var content = new Label { Text = "Needs 2 iron" };
        var panel = root.Add(new Panel { TooltipContent = content });
        panel.SetOffsets(0, 0, 100, 100);
        root.Layout();

        for (int i = 0; i < 12; i++) root.Update(0.1f, new PointerState { Position = new Vector2(10, 10) });
        Assert.True(root.TooltipPanel.Visible);
        Assert.NotNull(content.Parent);

        for (int i = 0; i < 2; i++) root.Update(0.1f, new PointerState { Position = new Vector2(500, 500) });
        Assert.False(root.TooltipPanel.Visible);
        Assert.Null(content.Parent);
    }
}
