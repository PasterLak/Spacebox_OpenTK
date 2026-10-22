using Engine.CanvasUI;
using Engine.CanvasUI.Validation;
using OpenTK.Mathematics;

namespace Engine.Tests.CanvasUI;

public class LayoutValidatorTests
{
    private static CanvasRoot CreateRoot()
    {
        var root = new CanvasRoot { Theme = new Theme { DefaultFont = new MonospaceFont(12f, 6f, 14f), DefaultFontSize = 12f } };
        root.Stretch.MinimumViewportSize = new Vector2(1024f, 576f);
        root.Stretch.MinScale = 0.75f;
        root.Stretch.MaxSafeAspect = 21f / 9f;
        root.Resize(new Vector2(1280f, 720f));
        return root;
    }

    private static LayoutReport Validate(CanvasRoot root) => LayoutHarness.Validate(root, new ScreenProfile("HD", 1280, 720));

    private static Button AddButton(Control parent, string text, LayoutPreset preset, float margin = 10f)
    {
        var button = parent.AddChild(new Button(text) { CustomMinimumSize = new Vector2(0f, 32f) });
        button.SetAnchorsAndOffsetsPreset(preset, margin);
        return button;
    }

    private static CanvasRoot BuildResponsiveScreen()
    {
        var root = CreateRoot();
        var hud = root.Add(new SafeArea { Name = "Hud" }, UiLayer.Hud);
        var bar = hud.AddChild(new HBoxContainer { Name = "Hotbar" });
        bar.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterBottom, 12f);
        for (int i = 0; i < 9; i++) bar.AddChild(new Button((i + 1).ToString()) { CustomMinimumSize = new Vector2(40f, 40f), TooltipText = "Slot " + (i + 1) });
        AddButton(hud, "Menu", LayoutPreset.TopRight);

        var windows = root.Add(new ExclusionArea { Name = "Windows", Gap = 8f }, UiLayer.Windows);
        windows.ExcludeChildrenOf(hud);
        var window = windows.AddChild(new PanelContainer { Name = "Window", CustomMaximumSize = new Vector2(700f, 0f) });
        window.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        var column = window.AddChild(new VBoxContainer());
        column.AddChild(new Label("Inventory"));
        column.AddChild(new Label("A long description that is cut with an ellipsis when the window is narrow") { Overrun = TextOverrun.Ellipsis });
        var grid = column.AddChild(new GridContainer { Columns = 8 });
        for (int i = 0; i < 32; i++) grid.AddChild(new Button { CustomMinimumSize = new Vector2(40f, 40f), TooltipText = "Item " + i });
        return root;
    }

    [Fact]
    public void ResponsiveScreen_IsCleanOnEveryProfile()
    {
        var root = BuildResponsiveScreen();

        var report = LayoutHarness.ValidateAll(root, ScreenProfiles.All);

        Assert.True(report.IsClean, report.Format());
        Assert.Equal(ScreenProfiles.All.Count, report.CheckedProfiles);
    }

    [Fact]
    public void ResponsiveScreen_TooltipsStayInsideTheSafeArea()
    {
        var root = BuildResponsiveScreen();

        var report = LayoutReport.Merge(ScreenProfiles.Quick.Select(p => LayoutHarness.ValidateTooltips(root, p)));

        Assert.True(report.IsClean, report.Format());
    }

    [Fact]
    public void ProfileMatrix_CoversCommonAspectRatiosAndScales()
    {
        var aspects = ScreenProfiles.All.Select(p => MathF.Round(p.Aspect, 2)).Distinct().ToList();

        Assert.Contains(1.33f, aspects);
        Assert.Contains(1.25f, aspects);
        Assert.Contains(1.78f, aspects);
        Assert.Contains(1.6f, aspects);
        Assert.Contains(3.56f, aspects);
        Assert.Contains(ScreenProfiles.All, p => p.Aspect < 1f);
        Assert.Contains(ScreenProfiles.All, p => p.UiScale > 1f);
        Assert.Contains(ScreenProfiles.All, p => !p.SafeMargins.IsZero);
        Assert.Contains(ScreenProfiles.All, p => p.Width >= 5120);
    }

    [Fact]
    public void Detects_ControlsOutsideTheViewportAndSafeArea()
    {
        var root = CreateRoot();
        var outside = root.Add(new Panel { Name = "Outside" });
        outside.SetOffsets(1200, 10, 1400, 60);
        var unsafePanel = root.Add(new Panel { Name = "Unsafe" });
        unsafePanel.SetOffsets(10, 10, 100, 60);

        var report = LayoutHarness.Validate(root, new ScreenProfile("TV", 1280, 720).WithSafeMargins(new Thickness(40f)));

        Assert.Contains(report.Issues, i => i.Kind == LayoutIssueKind.OutsideViewport && i.Control == outside);
        Assert.Contains(report.Issues, i => i.Kind == LayoutIssueKind.OutsideSafeArea && i.Control == unsafePanel);
    }

    [Fact]
    public void Detects_ContentOverflowingItsContainer()
    {
        var root = CreateRoot();
        var squeeze = root.Add(new SqueezeContainer());
        squeeze.SetOffsets(10, 10, 300, 100);
        var box = squeeze.AddChild(new HBoxContainer { Name = "Row" });
        for (int i = 0; i < 3; i++) box.AddChild(new Control { CustomMinimumSize = new Vector2(40f, 10f) });
        var grid = root.Add(new GridContainer { Name = "Grid", Columns = 2 });
        grid.SetOffsets(400, 10, 500, 100);
        var wide = grid.AddChild(new Control { CustomMinimumSize = new Vector2(30f, 30f) });

        var report = Validate(root);

        Assert.Contains(report.Issues, i => i.Kind == LayoutIssueKind.Squeezed && i.Control == box);
        Assert.DoesNotContain(report.Issues, i => i.Control == wide);
    }

    [Fact]
    public void Detects_OverlappingRegionsAndWindowsCoveringTheHud()
    {
        var root = CreateRoot();
        var a = root.Add(new Panel { Name = "A" }, UiLayer.Hud);
        a.SetOffsets(10, 10, 200, 200);
        var b = root.Add(new Panel { Name = "B" }, UiLayer.Hud);
        b.SetOffsets(100, 100, 300, 300);
        var window = root.Add(new Panel { Name = "Window" }, UiLayer.Windows);
        window.SetOffsets(250, 250, 500, 500);

        var report = Validate(root);

        Assert.Contains(report.Issues, i => i.Kind == LayoutIssueKind.Overlap && i.Control == b && i.Other == a);
        Assert.Contains(report.Issues, i => i.Kind == LayoutIssueKind.CoveredByLayer && i.Control == window && i.Other == b);

        var rules = new LayoutRules().AllowOverlap(a, b).AllowCover(UiLayer.Windows, UiLayer.Hud);
        Assert.True(LayoutValidator.Validate(root, rules).IsClean);
    }

    private sealed class SqueezeContainer : Container
    {
        protected override void LayoutChildren()
        {
            foreach (var child in Children)
                if (child is Control control) FitChildInRect(control, new Rect2(Rect.Position, new Vector2(50f, 20f)));
        }
    }

    [Fact]
    public void Detects_TextThatDoesNotFitAndAllowsEllipsis()
    {
        var root = CreateRoot();
        var squeeze = root.Add(new SqueezeContainer());
        squeeze.SetOffsets(10, 10, 300, 100);
        var label = squeeze.AddChild(new Label("This text is too wide"));
        var cut = root.Add(new Label("This text is cut") { Overrun = TextOverrun.Ellipsis });
        cut.SetOffsets(10, 200, 60, 220);

        var report = Validate(root);

        Assert.Contains(report.Issues, i => i.Kind == LayoutIssueKind.TextOverflow && i.Control == label);
        Assert.Contains(report.Issues, i => i.Kind == LayoutIssueKind.Squeezed && i.Control == label);
        Assert.DoesNotContain(report.Issues, i => i.Control == cut);
        Assert.True(cut.IsTextTruncated);
    }

    [Fact]
    public void AutowrapLabel_GrowsToItsWrappedHeightInTheSameLayout()
    {
        var root = CreateRoot();
        var panel = root.Add(new PanelContainer { CustomMinimumSize = new Vector2(120f, 0f) });
        panel.SetOffsets(10, 10, 130, 20);
        var label = panel.AddChild(new Label("Several words that need more than one line to fit") { Autowrap = true });

        var report = Validate(root);

        Assert.True(report.IsClean, report.Format());
        Assert.True(label.Size.Y >= label.GetTextSize().Y);
        Assert.True(label.GetTextSize().Y > 14f);
    }

    [Fact]
    public void Detects_SmallAndCoveredHitTargets()
    {
        var root = CreateRoot();
        var small = root.Add(new Button { Name = "Small", CustomMinimumSize = new Vector2(10f, 10f) });
        small.SetOffsets(10, 10, 20, 20);
        var covered = root.Add(new Button { Name = "Covered", CustomMinimumSize = new Vector2(60f, 60f) });
        covered.SetOffsets(100, 100, 160, 160);
        var cover = root.Add(new Panel { Name = "Cover" });
        cover.SetOffsets(90, 90, 170, 170);

        var report = Validate(root);

        Assert.Contains(report.Issues, i => i.Kind == LayoutIssueKind.SmallHitTarget && i.Control == small);
        Assert.Contains(report.Issues, i => i.Kind == LayoutIssueKind.UnreachableHitTarget && i.Control == covered && i.Other == cover);
    }

    [Fact]
    public void Detects_TextTooSmallOnScreen()
    {
        var root = CreateRoot();
        var label = root.Add(new Label("tiny"));
        label.AddThemeFontSizeOverride(6f);
        label.SetOffsets(10, 10, 100, 30);

        var report = Validate(root);

        Assert.Contains(report.Issues, i => i.Kind == LayoutIssueKind.SmallText && i.Control == label);
    }

    [Fact]
    public void Detects_PopupCoveringItsAnchor()
    {
        var root = CreateRoot();
        var popup = root.Add(new PopupPanel { Name = "Popup", StayInSafeArea = false }, UiLayer.Popup);
        popup.AddChild(new Control { CustomMinimumSize = new Vector2(1300f, 100f) });
        popup.Popup(new Rect2(600f, 300f, 40f, 40f), PopupSide.Right);

        var report = Validate(root);

        Assert.Contains(report.Issues, i => i.Kind == LayoutIssueKind.PopupCoversAnchor && i.Control == popup);
    }

    [Fact]
    public void ModalBlockedControlsAreNotReportedAsUnreachable()
    {
        var root = CreateRoot();
        var button = root.Add(new Button("Behind") { CustomMinimumSize = new Vector2(80f, 32f) }, UiLayer.Hud);
        button.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        var backdrop = root.Add(new ColorRect(new Color4(0f, 0f, 0f, 0.5f)) { MouseFilter = MouseFilter.Stop }, UiLayer.Modal);
        backdrop.SetAnchorsPreset(LayoutPreset.FullRect);

        var report = Validate(root);

        Assert.True(report.IsClean, report.Format());
    }

    [Fact]
    public void Report_FormatsIssuesWithPathsAndThrows()
    {
        var root = CreateRoot();
        var outside = root.Add(new Panel { Name = "Lost" });
        outside.SetOffsets(-50, 10, 10, 60);

        var report = Validate(root);

        Assert.False(report.IsClean);
        Assert.Contains("Hud/Lost", report.Format());
        Assert.Contains("[HD 1280x720]", report.Format());
        Assert.Throws<LayoutValidationException>(report.ThrowIfErrors);
    }
}
