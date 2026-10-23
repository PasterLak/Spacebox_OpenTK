using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Engine.Tests.CanvasUI;

public class CanvasCacheTests
{
    private static readonly Color4 Red = new Color4(1f, 0f, 0f, 1f);
    private static readonly Color4 Green = new Color4(0f, 1f, 0f, 1f);
    private static readonly Color4 Blue = new Color4(0f, 0f, 1f, 1f);

    private static CanvasRoot CreateRoot(Theme theme = null)
    {
        var root = new CanvasRoot { StretchMode = StretchMode.Disabled, Theme = theme ?? new Theme { DefaultFont = new MonospaceFont(), DefaultFontSize = 12f } };
        root.Resize(new Vector2(800f, 600f));
        return root;
    }

    private sealed class CountingControl : Control
    {
        public int MinimumSizeCalls;
        public Vector2 Minimum = new Vector2(10f, 10f);

        public override Vector2 GetMinimumSize()
        {
            MinimumSizeCalls++;
            return Minimum;
        }
    }

    [Fact]
    public void ThemeColor_FollowsThemeChangesAfterLookup()
    {
        var theme = new Theme { DefaultFont = new MonospaceFont() };
        theme.SetColor("Label", "font_color", Red);
        var root = CreateRoot(theme);
        var label = root.Add(new Label("x"));

        Assert.Equal(Red, label.GetThemeColor("font_color", Blue));

        theme.SetColor("Label", "font_color", Green);
        Assert.Equal(Green, label.GetThemeColor("font_color", Blue));
    }

    [Fact]
    public void ThemeColor_FallbackIsNotCachedAsValue()
    {
        var root = CreateRoot();
        var label = root.Add(new Label("x"));

        Assert.Equal(Blue, label.GetThemeColor("missing_color", Blue));
        Assert.Equal(Red, label.GetThemeColor("missing_color", Red));
    }

    [Fact]
    public void ThemeLookups_FollowReparentingAndOverrides()
    {
        var root = CreateRoot();
        var label = root.Add(new Label("x"));
        Assert.Equal(12f, label.GetThemeFontSize());

        var panelTheme = new Theme { DefaultFont = new MonospaceFont(), DefaultFontSize = 20f };
        panelTheme.SetConstant("Label", "spacing", 7f);
        var panel = root.Add(new Panel { Theme = panelTheme });
        panel.AddChild(label);

        Assert.Equal(20f, label.GetThemeFontSize());
        Assert.Same(panelTheme.DefaultFont, label.GetThemeFont());
        Assert.Equal(7f, label.GetThemeConstant("spacing", 0f));

        label.AddThemeConstantOverride("spacing", 3f);
        label.AddThemeColorOverride("font_color", Green);
        Assert.Equal(3f, label.GetThemeConstant("spacing", 0f));
        Assert.Equal(Green, label.GetThemeColor("font_color", Blue));

        label.RemoveThemeColorOverride("font_color");
        Assert.NotEqual(Green, label.GetThemeColor("font_color", Blue));

        root.Add(label);
        Assert.Equal(12f, label.GetThemeFontSize());
        Assert.Equal(3f, label.GetThemeConstant("spacing", 0f));
    }

    [Fact]
    public void ThemeStyleBox_FollowsTypeVariationAndRootTheme()
    {
        var first = new StyleBoxFlat(Red);
        var second = new StyleBoxFlat(Green);
        var theme = new Theme { DefaultFont = new MonospaceFont() };
        theme.SetStyleBox("Panel", "panel", first);
        theme.SetStyleBox("Special", "panel", second);
        var root = CreateRoot(theme);
        var panel = root.Add(new Panel());

        Assert.Same(first, panel.GetThemeStyleBox("panel"));

        panel.ThemeTypeVariation = "Special";
        Assert.Same(second, panel.GetThemeStyleBox("panel"));

        var other = new Theme { DefaultFont = new MonospaceFont() };
        root.Theme = other;
        Assert.NotSame(second, panel.GetThemeStyleBox("panel"));
    }

    [Fact]
    public void Layout_ComputesEachMinimumSizeOncePerPass()
    {
        var root = CreateRoot();
        var outer = root.Add(new VBoxContainer());
        Container parent = outer;
        for (int i = 0; i < 6; i++) parent = parent.AddChild(new VBoxContainer());
        var leaf = parent.AddChild(new CountingControl());
        root.Layout();

        leaf.MinimumSizeCalls = 0;
        root.MarkLayoutDirty();
        root.Layout();

        Assert.Equal(1, leaf.MinimumSizeCalls);
        Assert.Equal(new Vector2(10f, 10f), leaf.Size);
    }

    [Fact]
    public void Layout_PicksUpMinimumSizeChangesBetweenPasses()
    {
        var root = CreateRoot();
        var box = root.Add(new VBoxContainer());
        var leaf = box.AddChild(new CountingControl());
        root.Layout();
        Assert.Equal(new Vector2(10f, 10f), box.Size);

        leaf.Minimum = new Vector2(30f, 40f);
        root.MarkLayoutDirty();
        root.Layout();

        Assert.Equal(new Vector2(30f, 40f), box.Size);
        Assert.Equal(new Vector2(30f, 40f), leaf.GetCombinedMinimumSize());
    }

    [Fact]
    public void Layout_ChangesMadeDuringLayoutAreSeen()
    {
        var root = CreateRoot();
        var box = root.Add(new VBoxContainer());
        var label = box.AddChild(new Label("short"));
        var wrapped = box.AddChild(new Label("one two three four five six seven eight nine ten") { Autowrap = true });
        box.CustomMinimumSize = new Vector2(60f, 0f);
        box.Resized += _ => label.Text = "a much longer text";
        root.Layout();

        Assert.Equal("a much longer text", label.Text);
        Assert.True(box.Size.X >= label.GetCombinedMinimumSize().X);
        Assert.Equal(box.Size.X, label.Size.X);
        Assert.True(wrapped.Size.Y > new MonospaceFont().GetLineHeight(12f));
    }

    [Fact]
    public void LayoutAndDraw_DoNotAllocateInSteadyState()
    {
        var root = CreateRoot();
        var panel = root.Add(new PanelContainer(), UiLayer.Windows);
        var column = panel.AddChild(new MarginContainer()).AddChild(new VBoxContainer());
        for (int i = 0; i < 20; i++)
        {
            var row = column.AddChild(new HBoxContainer());
            row.AddChild(new Label("Item " + i));
            row.AddChild(new ProgressBar { Value = i * 5 });
            row.AddChild(new Button("Use"));
            row.AddChild(new Label("wrapping description text") { Autowrap = true, CustomMinimumSize = new Vector2(80f, 0f) });
        }
        var commands = new CanvasCommandList();
        for (int i = 0; i < 3; i++)
        {
            root.MarkLayoutDirty();
            root.Layout();
            commands.Commands.Clear();
            root.Draw(commands);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10; i++)
        {
            root.MarkLayoutDirty();
            root.Layout();
            commands.Commands.Clear();
            root.Draw(commands);
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
    }
}
