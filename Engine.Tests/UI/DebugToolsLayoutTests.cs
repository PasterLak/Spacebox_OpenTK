using Engine.CanvasUI;
using Engine.CanvasUI.Validation;
using Engine.Graphics.Debugging;
using Engine.GUI;
using Engine.UI;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.Tests.UI;

[Collection(GlobalStateCollection.Name)]
public sealed class DebugToolsLayoutTests
{
    private static readonly LayoutRules Rules = new LayoutRules { MinHitTargetPixels = 12f };

    private static CanvasRoot CreateRoot()
    {
        var root = new CanvasRoot();
        DebugCanvas.Configure(root, new MonospaceFont());
        root.Resize(new Vector2(1280f, 720f));
        return root;
    }

    private static OverlayText SampleText()
    {
        var text = new OverlayText();
        text.Line("FPS: 144, TPS: 20", Overlay.Green);
        text.Line("Limit FPS:");
        text.Append("True", Overlay.Green);
        text.Section("[CAMERA]");
        text.Line("Pos: (");
        text.Append("12", Overlay.Red);
        text.Append("-4", Overlay.Green);
        text.Append("300", Overlay.Blue);
        text.Append(")");
        text.Section("[GAME LOOP]");
        text.Line("Render");
        text.Bar(0.42f, "42%");
        text.Append("Avg: 3.21 ms");
        return text;
    }

    [Fact]
    public void Overlay_IsCleanOnEveryProfile()
    {
        var root = CreateRoot();
        DebugCanvas.Area(root, UiLayer.Debug).AddChild(new OverlayPanel(SampleText()));

        var report = LayoutHarness.ValidateAll(root, ScreenProfiles.All, Rules);

        Assert.True(report.IsClean, report.Format());
    }

    [Fact]
    public void OverlayText_SizeGrowsWithLinesAndBars()
    {
        var root = CreateRoot();
        var text = new OverlayText();
        text.Line("Short");
        var panel = DebugCanvas.Area(root, UiLayer.Debug).AddChild(new OverlayPanel(text));
        var single = panel.TextControl.GetMinimumSize();

        text.Clear();
        text.Line("Short");
        text.Line("Second");
        text.Bar(0.5f);
        var multi = panel.TextControl.GetMinimumSize();

        Assert.True(multi.Y > single.Y);
        Assert.True(multi.X > single.X);
    }

    [Fact]
    public void OverlayText_SameLineSpansShareOneTextRun()
    {
        var root = CreateRoot();
        var text = new OverlayText();
        text.Section("[THREADS]");
        text.Line("Queued: 3");
        text.Append("| Completed: 7");
        var panel = DebugCanvas.Area(root, UiLayer.Debug).AddChild(new OverlayPanel(text));
        LayoutHarness.Apply(root, new ScreenProfile("HD", 1280, 720));

        var runs = new List<CanvasTextRun>();
        panel.TextControl.CollectText(runs);

        Assert.Equal(2, runs.Count);
        Assert.Equal("[THREADS]", runs[0].Text);
        Assert.Equal("Queued: 3 | Completed: 7", runs[1].Text);
        Assert.True(runs[1].Rect.Y > runs[0].Rect.Y);
    }

    [Fact]
    public void Overlay_WritesEveryGlobalElement()
    {
        var text = new OverlayText();

        Overlay.WriteTo(text, 1f);

        var sections = new List<string>();
        foreach (var span in text.Spans)
            if (span.Kind == OverlaySpanKind.Section) sections.Add(span.Text);
        Assert.Contains("[GAME LOOP]", sections);
        Assert.Contains("[LIGHTING]", sections);
        Assert.Contains("[THREADS]", sections);
        Assert.StartsWith("FPS:", text.Spans[0].Text);
    }

    [Fact]
    public void RenderModeLabel_ShowsOnlyOutsideLitMode()
    {
        var root = CreateRoot();
        var label = DebugCanvas.Area(root, UiLayer.Debug).AddChild(new RenderModeLabel());

        label.Show(RenderDebugMode.Lit);
        Assert.False(label.Visible);

        label.Show(RenderDebugMode.Wireframe);
        Assert.True(label.Visible);
        var report = LayoutHarness.ValidateAll(root, ScreenProfiles.All, Rules);
        Assert.True(report.IsClean, report.Format());
    }

    [Fact]
    public void WorldDebugOverlay_IsCleanOnEveryProfile()
    {
        var root = CreateRoot();
        root.Add(new WorldDebugControl(), UiLayer.Background);
        DebugCanvas.Area(root, UiLayer.Debug).AddChild(new OverlayPanel(SampleText()));

        var report = LayoutHarness.ValidateAll(root, ScreenProfiles.All, Rules);

        Assert.True(report.IsClean, report.Format());
    }

    [Fact]
    public void ParticleSystemPanel_WithoutSystems_IsCleanOnEveryProfile()
    {
        var root = CreateRoot();
        var panel = DebugCanvas.Area(root, UiLayer.Windows).AddChild(new ParticleSystemPanel(Array.Empty<(string, Texture2D)>()));
        panel.SetSystems(Array.Empty<ParticleSystem>());

        var report = LayoutHarness.ValidateAll(root, ScreenProfiles.All, Rules);

        Assert.True(report.IsClean, report.Format());
    }

    [Fact]
    public void ParticleEmitterRows_AreCleanOnEveryProfile()
    {
        var root = CreateRoot();
        var panel = DebugCanvas.Area(root, UiLayer.Windows).AddChild(new PanelContainer { Name = "Particles" });
        ParticleSystemPanel.PlaceAtRight(panel);
        var scroll = panel.AddChild(new ScrollContainer());
        var column = scroll.AddChild(new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand });
        var emitter = new ConeEmitter();
        foreach (var row in ParticleEmitterRows.Common(emitter)) column.AddChild(row);
        foreach (var row in ParticleEmitterRows.For(emitter)) column.AddChild(row);

        var report = LayoutHarness.ValidateAll(root, ScreenProfiles.All, Rules);

        Assert.True(report.IsClean, report.Format());
    }

    [Fact]
    public void ParticleRows_ShowAndApplyEmitterValues()
    {
        var emitter = new SphereEmitter { Radius = 4f, SpeedMin = 1f, SpeedMax = 3f };
        var root = CreateRoot();
        var column = root.Add(new VBoxContainer(), UiLayer.Windows);
        var rows = ParticleEmitterRows.For(emitter);
        foreach (var row in rows)
        {
            column.AddChild(row);
            row.Refresh();
        }
        var radius = Assert.IsType<ParticleNumberRow>(rows[1]);
        var edit = FindChild<NumberEdit>(radius);

        Assert.Equal(4.0, edit.Value, 3);

        root.Layout();
        edit.GrabFocus();
        edit.SelectAll();
        root.Update(0.016f, default, new KeyboardFrame(Array.Empty<KeyPress>(), "6"));
        root.Update(0.016f, default, new KeyboardFrame(new[] { new KeyPress(Keys.Enter, false, false, false, false) }, ""));
        Assert.Equal(6f, emitter.Radius, 3);
    }

    [Fact]
    public void EveryBuiltInEmitter_HasSpecificRows()
    {
        EmitterBase[] emitters = { new BoxEmitter(), new ConeEmitter(), new DiskEmitter(), new PlaneEmitter(), new SphereEmitter(), new PointEmitter() };
        foreach (var emitter in emitters) Assert.NotEmpty(ParticleEmitterRows.For(emitter));
    }

    private static T FindChild<T>(CanvasItem item) where T : CanvasItem
    {
        foreach (var child in item.Children)
        {
            if (child is T match) return match;
            var nested = FindChild<T>(child);
            if (nested != null) return nested;
        }
        return null;
    }
}
