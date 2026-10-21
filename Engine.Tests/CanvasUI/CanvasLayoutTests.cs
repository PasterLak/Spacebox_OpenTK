using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Engine.Tests.CanvasUI;

public class CanvasLayoutTests
{
    internal sealed class FixedFont : CanvasFont
    {
        public override float BaseSize => 10f;
        public override float LineHeight => 10f;
        public override Texture2D Atlas => null;

        public override bool TryGetGlyph(char c, out CanvasGlyph glyph)
        {
            glyph = new CanvasGlyph(6f, Vector2.Zero, new Vector2(6f, 10f), new Rect2(0, 0, 1, 1));
            return true;
        }
    }

    private static CanvasRoot CreateRoot(float width = 800f, float height = 600f)
    {
        var root = new CanvasRoot { StretchMode = StretchMode.Disabled, Theme = new Theme { DefaultFont = new FixedFont(), DefaultFontSize = 10f } };
        root.Resize(new Vector2(width, height));
        return root;
    }

    [Fact]
    public void Anchors_FullRectWithMargins()
    {
        var root = CreateRoot();
        var panel = root.Add(new Panel());
        panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, 10f);
        root.Layout();

        Assert.Equal(new Rect2(10, 10, 780, 580), panel.Rect);
    }

    [Fact]
    public void Anchors_BottomRightUsesSizeAndGrowsInward()
    {
        var root = CreateRoot();
        var box = root.Add(new Control { CustomMinimumSize = new Vector2(100, 50) });
        box.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight, 5f);
        root.Layout();

        Assert.Equal(new Rect2(695, 545, 100, 50), box.Rect);
    }

    [Fact]
    public void Anchors_CenterKeepsCentered()
    {
        var root = CreateRoot();
        var box = root.Add(new Control { CustomMinimumSize = new Vector2(200, 100) });
        box.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        root.Layout();

        Assert.Equal(new Rect2(300, 250, 200, 100), box.Rect);
    }

    [Fact]
    public void MinimumSize_GrowsControlPastOffsets()
    {
        var root = CreateRoot();
        var box = root.Add(new Control { CustomMinimumSize = new Vector2(50, 50) });
        box.SetOffsets(10, 10, 20, 20);
        root.Layout();

        Assert.Equal(new Rect2(10, 10, 50, 50), box.Rect);
    }

    [Fact]
    public void HBox_ExpandSharesFreeSpaceByRatio()
    {
        var root = CreateRoot();
        var box = root.Add(new HBoxContainer());
        box.AddThemeConstantOverride("separation", 10f);
        box.SetAnchorsPreset(LayoutPreset.FullRect);
        var fixedChild = box.AddChild(new Control { CustomMinimumSize = new Vector2(100, 10) });
        var a = box.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        var b = box.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsStretchRatio = 2f });
        root.Layout();

        Assert.Equal(100f, fixedChild.Rect.Width);
        Assert.Equal(226f, a.Rect.Width);
        Assert.Equal(453f, b.Rect.Width);
        Assert.Equal(110f, a.Rect.X);
        Assert.Equal(346f, b.Rect.X);
        Assert.Equal(600f, a.Rect.Height);
    }

    [Fact]
    public void HBox_ExpanderSmallerThanMinimumKeepsMinimum()
    {
        var root = CreateRoot(300, 100);
        var box = root.Add(new HBoxContainer());
        box.AddThemeConstantOverride("separation", 0f);
        box.SetAnchorsPreset(LayoutPreset.FullRect);
        var big = box.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(250, 0) });
        var small = box.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        root.Layout();

        Assert.Equal(250f, big.Rect.Width);
        Assert.Equal(50f, small.Rect.Width);
    }

    [Fact]
    public void VBox_MinimumSizeAndAlignment()
    {
        var root = CreateRoot();
        var box = root.Add(new VBoxContainer { Alignment = BoxAlignment.End });
        box.AddThemeConstantOverride("separation", 5f);
        box.SetAnchorsPreset(LayoutPreset.FullRect);
        box.AddChild(new Control { CustomMinimumSize = new Vector2(30, 20) });
        var last = box.AddChild(new Control { CustomMinimumSize = new Vector2(60, 40), SizeFlagsHorizontal = SizeFlags.ShrinkCenter });
        root.Layout();

        Assert.Equal(new Vector2(60, 65), box.GetCombinedMinimumSize());
        Assert.Equal(new Rect2(370, 560, 60, 40), last.Rect);
    }

    [Fact]
    public void Grid_PlacesChildrenInColumns()
    {
        var root = CreateRoot();
        var grid = root.Add(new GridContainer { Columns = 3 });
        grid.AddThemeConstantOverride("h_separation", 2f);
        grid.AddThemeConstantOverride("v_separation", 3f);
        var cells = new List<Control>();
        for (int i = 0; i < 7; i++) cells.Add(grid.AddChild(new Control { CustomMinimumSize = new Vector2(10, 20) }));
        root.Layout();

        Assert.Equal(new Vector2(34, 66), grid.GetCombinedMinimumSize());
        Assert.Equal(new Rect2(24, 23, 10, 20), cells[5].Rect);
        Assert.Equal(new Rect2(0, 46, 10, 20), cells[6].Rect);
    }

    [Fact]
    public void Margin_And_Center_Containers()
    {
        var root = CreateRoot();
        var margin = root.Add(new MarginContainer().SetMargins(10, 20, 30, 40));
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        var center = margin.AddChild(new CenterContainer());
        var child = center.AddChild(new Control { CustomMinimumSize = new Vector2(100, 40) });
        root.Layout();

        Assert.Equal(new Rect2(10, 20, 760, 540), center.Rect);
        Assert.Equal(new Rect2(340, 270, 100, 40), child.Rect);
    }

    [Fact]
    public void PanelContainer_UsesStyleMargins()
    {
        var root = CreateRoot();
        var panel = root.Add(new PanelContainer());
        panel.AddThemeStyleBoxOverride("panel", new StyleBoxFlat(Color4.Black, 6f));
        var child = panel.AddChild(new Control { CustomMinimumSize = new Vector2(20, 10) });
        root.Layout();

        Assert.Equal(new Vector2(32, 22), panel.GetCombinedMinimumSize());
        Assert.Equal(new Rect2(6, 6, 20, 10), child.Rect);
    }

    [Fact]
    public void Label_MeasuresAndWraps()
    {
        var root = CreateRoot();
        var label = root.Add(new Label("hello world"));
        root.Layout();
        Assert.Equal(new Vector2(66, 10), label.GetCombinedMinimumSize());

        var lines = new List<string>();
        TextWrap.Wrap(new FixedFont(), "one two three", 10f, 40f, lines);
        Assert.Equal(new[] { "one", "two", "three" }, lines);

        TextWrap.Wrap(new FixedFont(), "abcdefghij", 10f, 30f, lines);
        Assert.Equal(new[] { "abcde", "fghij" }, lines);
    }

    [Fact]
    public void ThemeVariation_FallsBackToBaseType()
    {
        var theme = new Theme();
        var special = new StyleBoxFlat(Color4.Red);
        var plain = new StyleBoxFlat(Color4.Blue);
        theme.SetStyleBox("Slot", "panel", special);
        theme.SetStyleBox("Panel", "panel", plain);
        var root = CreateRoot();
        root.Theme = theme;

        var a = root.Add(new Panel { ThemeTypeVariation = "Slot" });
        var b = root.Add(new Panel());
        Assert.Same(special, a.GetThemeStyleBox("panel"));
        Assert.Same(plain, b.GetThemeStyleBox("panel"));

        var local = new StyleBoxFlat(Color4.Green);
        var container = root.Add(new Control { Theme = new Theme().SetStyleBox("Panel", "panel", local) });
        var nested = container.AddChild(new Panel());
        Assert.Same(local, nested.GetThemeStyleBox("panel"));
    }

    [Fact]
    public void Stretch_ExpandKeepsVirtualHeight()
    {
        var root = new CanvasRoot { VirtualSize = new Vector2(1280, 720), StretchMode = StretchMode.Expand };
        root.Resize(new Vector2(2560, 1080));

        Assert.Equal(1.5f, root.Scale, 3);
        Assert.Equal(720f, root.ViewportSize.Y, 3);
        Assert.Equal(2560f / 1.5f, root.ViewportSize.X, 3);
        Assert.Equal(new Vector2(150, 75), root.ScreenToCanvas(new Vector2(225, 112.5f)));
    }

    [Fact]
    public void Stretch_KeepAspectLetterboxes()
    {
        var root = new CanvasRoot { VirtualSize = new Vector2(1280, 720), StretchMode = StretchMode.KeepAspect };
        root.Resize(new Vector2(1280, 1000));

        Assert.Equal(new Vector2(1280, 720), root.ViewportSize);
        Assert.Equal(new Vector2(0, 140), root.ScreenTransform.Origin);
    }

    [Fact]
    public void Button_ClickFiresPressedOnlyWhenReleasedInside()
    {
        var root = CreateRoot();
        var button = root.Add(new Button("Go"));
        button.SetOffsets(100, 100, 200, 140);
        int pressed = 0;
        button.Pressed += _ => pressed++;
        root.Layout();

        root.Update(0.016f, new PointerState { Position = new Vector2(150, 120) });
        Assert.True(button.IsHovered);
        root.Update(0.016f, new PointerState { Position = new Vector2(150, 120), LeftDown = true });
        Assert.Equal("pressed", button.DrawState);
        root.Update(0.016f, new PointerState { Position = new Vector2(150, 120) });
        Assert.Equal(1, pressed);

        root.Update(0.016f, new PointerState { Position = new Vector2(150, 120), LeftDown = true });
        root.Update(0.016f, new PointerState { Position = new Vector2(500, 500), LeftDown = true });
        root.Update(0.016f, new PointerState { Position = new Vector2(500, 500) });
        Assert.Equal(1, pressed);
        Assert.False(button.IsHovered);
    }

    [Fact]
    public void HitTest_RespectsMouseFilterAndOrder()
    {
        var root = CreateRoot();
        var bottom = root.Add(new Panel());
        bottom.SetOffsets(0, 0, 100, 100);
        var top = root.Add(new Panel());
        top.SetOffsets(50, 50, 150, 150);
        var ignored = top.AddChild(new Label("x"));
        ignored.SetOffsets(0, 0, 100, 100);
        root.Layout();

        Assert.Same(top, root.HitTest(new Vector2(60, 60)));
        Assert.Same(bottom, root.HitTest(new Vector2(10, 10)));
        Assert.Null(root.HitTest(new Vector2(300, 300)));

        top.Visible = false;
        root.Layout();
        Assert.Same(bottom, root.HitTest(new Vector2(60, 60)));
    }

    [Fact]
    public void Tooltip_AppearsAfterHoverDelay()
    {
        var root = CreateRoot();
        var panel = root.Add(new Panel { TooltipText = "Iron ingot" });
        panel.SetOffsets(0, 0, 100, 100);
        root.Layout();

        root.Update(0.1f, new PointerState { Position = new Vector2(10, 10) });
        Assert.Null(root.TooltipText);
        for (int i = 0; i < 10; i++) root.Update(0.1f, new PointerState { Position = new Vector2(10, 10) });
        Assert.Equal("Iron ingot", root.TooltipText);
    }

    [Fact]
    public void Draw_RecordsCommandsInTreeOrderWithModulate()
    {
        var root = CreateRoot();
        var parent = root.Add(new ColorRect(Color4.Red) { Modulate = new Color4(1f, 1f, 1f, 0.5f) });
        parent.SetOffsets(10, 10, 60, 60);
        var child = parent.AddChild(new ColorRect(Color4.White));
        child.SetOffsets(5, 5, 15, 15);
        root.Layout();

        var list = new CanvasCommandList();
        root.Draw(list);

        Assert.Equal(2, list.Commands.Count);
        Assert.Equal(new Rect2(10, 10, 50, 50), list.Commands[0].Rect);
        Assert.Equal(new Rect2(15, 15, 10, 10), list.Commands[1].Rect);
        Assert.Equal(0.5f, list.Commands[1].Color.A, 3);
    }

    [Fact]
    public void Node2D_TransformsChildren()
    {
        var root = CreateRoot();
        var node = root.Add(new Node2D { Position = new Vector2(100, 50), Scale = new Vector2(2, 2) });
        var child = node.AddChild(new Node2D { Position = new Vector2(10, 0), Rotation = MathF.PI / 2f });

        var p = child.GlobalTransform.Apply(new Vector2(5, 0));
        Assert.Equal(120f, p.X, 3);
        Assert.Equal(60f, p.Y, 3);
        var inverse = child.GlobalTransform.Inverse().Apply(p);
        Assert.Equal(5f, inverse.X, 3);
        Assert.Equal(0f, inverse.Y, 3);
    }

    [Fact]
    public void ProgressBar_ClampsAndComputesRatio()
    {
        var bar = new ProgressBar { MinValue = 0, MaxValue = 200 };
        bar.Value = 50;
        Assert.Equal(0.25f, bar.Ratio, 3);
        bar.Value = 500;
        Assert.Equal(1f, bar.Ratio, 3);
    }
}
