using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Engine.Tests.CanvasUI;

public class CanvasLayoutFeatureTests
{
    private static CanvasRoot CreateRoot(float width = 800f, float height = 600f)
    {
        var root = new CanvasRoot { StretchMode = StretchMode.Disabled, Theme = new Theme { DefaultFont = new MonospaceFont(10f, 6f, 10f), DefaultFontSize = 10f } };
        root.Resize(new Vector2(width, height));
        return root;
    }

    [Fact]
    public void MaximumSize_LimitsStretchedAnchorsAroundTheGrowDirection()
    {
        var root = CreateRoot();
        var panel = root.Add(new Panel { CustomMaximumSize = new Vector2(400f, 0f), GrowHorizontal = GrowDirection.Both });
        panel.SetAnchorsPreset(LayoutPreset.FullRect);
        root.Layout();

        Assert.Equal(new Rect2(200f, 0f, 400f, 600f), panel.Rect);
    }

    [Fact]
    public void MaximumSize_IsRespectedByContainers()
    {
        var root = CreateRoot();
        var box = root.Add(new VBoxContainer());
        box.SetAnchorsPreset(LayoutPreset.FullRect);
        var child = box.AddChild(new Control { CustomMaximumSize = new Vector2(300f, 0f), SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.ShrinkCenter });
        root.Layout();

        Assert.Equal(new Rect2(250f, 0f, 300f, 0f), child.Rect);
    }

    [Fact]
    public void Layers_AreOrderedRegardlessOfCreationOrder()
    {
        var root = CreateRoot();
        root.Layer(UiLayer.Tooltip);
        root.Layer(UiLayer.Hud);
        root.Layer(UiLayer.Modal);
        root.Layer(UiLayer.Windows);

        var order = root.Content.Children.Cast<UiLayerRoot>().Select(l => l.Layer).ToArray();
        Assert.Equal(new[] { UiLayer.Hud, UiLayer.Windows, UiLayer.Modal, UiLayer.Tooltip }, order);
    }

    [Fact]
    public void Modal_BlocksHitsOnLowerLayersOnlyWhileVisible()
    {
        var root = CreateRoot();
        var button = root.Add(new Button("Go"), UiLayer.Hud);
        button.SetOffsets(10, 10, 110, 50);
        var dialog = root.Add(new Panel { Visible = false }, UiLayer.Modal);
        dialog.SetOffsets(300, 300, 400, 400);
        root.Layout();

        Assert.Same(button, root.HitTest(new Vector2(20, 20)));
        dialog.Visible = true;
        root.Layout();
        Assert.Null(root.HitTest(new Vector2(20, 20)));
        Assert.Same(dialog, root.HitTest(new Vector2(350, 350)));
        Assert.True(root.IsBlockedByModal(button));
    }

    [Fact]
    public void SafeArea_AnchorsChildrenInsideTheSafeRect()
    {
        var root = CreateRoot(1000f, 500f);
        root.Stretch.SafeMargins = new Thickness(50f, 20f, 50f, 20f);
        var area = root.Add(new SafeArea());
        var corner = area.AddChild(new Control { CustomMinimumSize = new Vector2(100f, 40f) });
        corner.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight, 10f);
        root.Resize(new Vector2(1000f, 500f));

        Assert.Equal(new Rect2(840f, 430f, 100f, 40f), corner.Rect);
    }

    [Fact]
    public void ExclusionArea_KeepsChildrenAwayFromOtherControls()
    {
        var root = CreateRoot(1000f, 600f);
        var hud = root.Add(new SafeArea(), UiLayer.Hud);
        var bar = hud.AddChild(new Panel { CustomMinimumSize = new Vector2(400f, 100f) });
        bar.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterBottom);
        var area = root.Add(new ExclusionArea { Gap = 10f }, UiLayer.Windows);
        area.ExcludeChildrenOf(hud);
        var window = area.AddChild(new Panel());
        window.SetAnchorsPreset(LayoutPreset.FullRect);
        root.Layout();

        Assert.Equal(new Rect2(0f, 0f, 1000f, 490f), window.Rect);
    }

    [Fact]
    public void RectCut_PicksTheLargestFreeSide()
    {
        var area = new Rect2(0f, 0f, 1000f, 600f);

        Assert.Equal(new Rect2(0f, 0f, 1000f, 500f), RectCut.LargestOutside(area, new Rect2(300f, 500f, 400f, 100f)));
        Assert.Equal(new Rect2(100f, 0f, 900f, 600f), RectCut.LargestOutside(area, new Rect2(0f, 0f, 100f, 600f)));
        Assert.Equal(area, RectCut.LargestOutside(area, new Rect2(2000f, 0f, 10f, 10f)));
    }

    [Fact]
    public void ScrollContainer_ScrollsOverflowingContentAndClampsTheOffset()
    {
        var root = CreateRoot(400f, 300f);
        var scroll = root.Add(new ScrollContainer());
        scroll.SetOffsets(0, 0, 200, 100);
        var column = scroll.AddChild(new VBoxContainer());
        for (int i = 0; i < 10; i++) column.AddChild(new Control { CustomMinimumSize = new Vector2(150f, 30f) });
        root.Layout();

        Assert.True(scroll.IsOverflowing);
        Assert.Equal(0f, scroll.GetCombinedMinimumSize().Y);
        root.Update(0f, new PointerState { Position = new Vector2(50, 50) });
        root.Update(0f, new PointerState { Position = new Vector2(50, 50), Scroll = -1f });
        root.Layout();
        Assert.Equal(40f, scroll.ScrollOffset);
        Assert.Equal(-40f, column.Rect.Y);

        scroll.ScrollOffset = 10000f;
        root.Layout();
        Assert.Equal(scroll.MaxScroll, scroll.ScrollOffset);
        Assert.Equal(100f, column.Rect.End.Y, 0);
    }

    [Fact]
    public void Label_EllipsisTruncatesToTheAvailableWidth()
    {
        var root = CreateRoot();
        var label = root.Add(new Label("Very long label text") { Overrun = TextOverrun.Ellipsis });
        label.SetOffsets(0, 0, 60, 10);
        root.Layout();

        Assert.Equal(0f, label.GetCombinedMinimumSize().X);
        Assert.True(label.IsTextTruncated);
        var list = new CanvasCommandList();
        root.Draw(list);
        Assert.InRange(list.Commands.Count, 1, 10);
    }

    [Fact]
    public void TextFit_EllipsizeFitsAndKeepsShortText()
    {
        var font = new MonospaceFont(10f, 6f, 10f);

        Assert.Equal("Short", TextFit.Ellipsize(font, "Short", 10f, 100f));
        string cut = TextFit.Ellipsize(font, "Hello world again", 10f, 48f);
        Assert.EndsWith(TextFit.Ellipsis, cut);
        Assert.True(font.MeasureWidth(cut, 10f) <= 48f);
        Assert.Equal("", TextFit.Ellipsize(font, "Hello", 10f, 3f));
    }

    [Theory]
    [InlineData(PopupSide.Below, 100f, 140f)]
    [InlineData(PopupSide.Above, 100f, 30f)]
    public void PopupPlacement_UsesThePreferredSideWhenItFits(PopupSide side, float x, float y)
    {
        var rect = PopupPlacement.Place(new Rect2(100f, 100f, 40f, 40f), new Vector2(80f, 70f), new Rect2(0f, 0f, 800f, 600f), side);

        Assert.Equal(new Rect2(x, y, 80f, 70f), rect);
    }

    [Fact]
    public void PopupPlacement_FlipsAndClampsAtTheEdges()
    {
        var bounds = new Rect2(0f, 0f, 800f, 600f);

        var below = PopupPlacement.Place(new Rect2(760f, 560f, 40f, 40f), new Vector2(120f, 80f), bounds, PopupSide.Below, 4f);
        Assert.Equal(new Rect2(680f, 476f, 120f, 80f), below);

        var right = PopupPlacement.Place(new Rect2(760f, 0f, 40f, 40f), new Vector2(120f, 80f), bounds, PopupSide.Right, 4f);
        Assert.Equal(new Rect2(636f, 0f, 120f, 80f), right);
        Assert.False(right.Intersects(new Rect2(760f, 0f, 40f, 40f)));
    }

    [Fact]
    public void Tooltip_IsPlacedOnTheTooltipLayerInsideTheSafeArea()
    {
        var root = CreateRoot();
        root.Stretch.SafeMargins = new Thickness(0f, 0f, 20f, 20f);
        root.Resize(new Vector2(800f, 600f));
        var panel = root.Add(new Panel { TooltipText = "A rather long tooltip text" });
        panel.SetOffsets(700, 540, 780, 580);
        root.Layout();

        for (int i = 0; i < 3; i++) root.Update(0.3f, new PointerState { Position = new Vector2(770, 575) });
        root.Layout();

        var tooltip = root.TooltipPanel;
        Assert.True(tooltip.IsVisibleInTree);
        Assert.Equal(UiLayer.Tooltip, UiLayerRoot.LayerOf(tooltip));
        Assert.True(root.SafeRect.Encloses(tooltip.Rect));
        Assert.False(tooltip.Rect.Contains(new Vector2(770, 575)));
    }
}
