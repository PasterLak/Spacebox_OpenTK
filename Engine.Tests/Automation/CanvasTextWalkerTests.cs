using Engine.Automation;
using Engine.CanvasUI;
using Engine.GUI;
using Engine.UI;
using OpenTK.Mathematics;

namespace Engine.Tests.Automation;

public sealed class CanvasTextWalkerTests
{
    [Fact]
    public void Collect_ReadsVisibleControlsAndTextSources()
    {
        var root = new CanvasRoot();
        DebugCanvas.Configure(root, new MonospaceFont());
        root.Resize(new Vector2(1280f, 720f));
        var column = root.Add(new VBoxContainer(), UiLayer.Windows);
        column.AddChild(new Label("Title"));
        column.AddChild(new Button("Craft"));
        column.AddChild(new Label("Hidden") { Visible = false });
        var text = new OverlayText();
        text.Line("FPS: 60");
        column.AddChild(new OverlayTextControl { Text = text });
        root.Layout();

        var runs = new List<CanvasTextRun>();
        CanvasTextWalker.Collect(root.Content, runs);

        var texts = runs.ConvertAll(run => run.Text);
        Assert.Equal(new[] { "Title", "Craft", "FPS: 60" }, texts);
        Assert.All(runs, run => Assert.True(run.Rect.Width > 0f));
    }
}
