using BenchmarkDotNet.Attributes;
using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Benchmarks.Engine;

[MemoryDiagnoser]
public class CanvasLayoutBenchmark
{
    private CanvasRoot _root;

    [Params(4)]
    public int Windows;

    [Params(30)]
    public int Rows;

    [GlobalSetup]
    public void Setup()
    {
        _root = CanvasTreeFactory.Create(Windows, Rows);
    }

    [Benchmark]
    public void FullLayout()
    {
        _root.MarkLayoutDirty();
        _root.Layout();
    }

    [Benchmark]
    public int Draw()
    {
        _commands.Commands.Clear();
        _root.Draw(_commands);
        return _commands.Commands.Count;
    }

    private readonly CanvasCommandList _commands = new CanvasCommandList();
}

public static class CanvasTreeFactory
{
    public static CanvasRoot Create(int windows, int rows)
    {
        var root = new CanvasRoot
        {
            StretchMode = StretchMode.Disabled,
            Theme = new Theme { DefaultFont = new MonospaceFont(), DefaultFontSize = 12f }
        };
        root.Resize(new Vector2(1920f, 1080f));

        for (int w = 0; w < windows; w++)
        {
            var panel = root.Add(new PanelContainer(), UiLayer.Windows);
            panel.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft, 10f + w * 40f, new Vector2(420f, 600f));
            var margin = panel.AddChild(new MarginContainer());
            var column = margin.AddChild(new VBoxContainer());
            column.AddChild(new Label("Window " + w) { Bold = true });
            for (int r = 0; r < rows; r++) column.AddChild(CreateRow(r));
        }

        root.Layout();
        return root;
    }

    private static Control CreateRow(int index)
    {
        var row = new HBoxContainer();
        row.AddChild(new Label("Item " + index) { SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand });
        var values = row.AddChild(new VBoxContainer());
        values.AddChild(new Label("Value " + index * 7));
        values.AddChild(new ProgressBar());
        row.AddChild(new Button("Use"));
        row.AddChild(new Label("A longer description of the item that wraps over several lines") { Autowrap = true, CustomMinimumSize = new Vector2(140f, 0f) });
        return row;
    }
}
