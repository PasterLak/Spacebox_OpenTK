using System.Text.Json;
using Engine;
using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Editor.Data;
using Spacebox.Game.Resource;

namespace Spacebox.Editor.Canvas.Pages;

public sealed class LootPreviewView : RefreshingEntryView
{
    private const int MaxItems = 255;

    private readonly VBoxContainer _root = new VBoxContainer();
    private readonly HFlowContainer _items = new HFlowContainer();
    private readonly Label _seedLabel = Dim("");
    private readonly Random _random = new Random();
    private int _seed;

    protected override Control Create()
    {
        _seed = _random.Next();
        var toolbar = new HBoxContainer();
        var generate = toolbar.AddChild(new Button("Generate") { TooltipText = "Rolls the table again with a new random seed, like a new storage in the world." });
        generate.Pressed += _ => _seed = _random.Next();
        toolbar.AddChild(_seedLabel);
        _root.AddChild(toolbar);
        _root.AddChild(_items);
        return _root;
    }

    protected override string Signature(EditorEntry entry) => _seed + "|" + entry.Node.ToJsonString();

    protected override void Rebuild(EditorContext editor, EditorEntry entry)
    {
        _items.ClearChildren();
        _seedLabel.Text = $"  Seed {_seed}";

        List<LootEntry> entries;
        try
        {
            entries = JsonSerializer.Deserialize<List<LootEntry>>(entry.Node.ToJsonString());
        }
        catch (Exception ex)
        {
            _items.AddChild(new Label($"The table cannot be read: {ex.Message}") { FontColor = EditorCanvasTheme.Error });
            return;
        }

        var rolls = LootRoller.Roll(entries, _seed, MaxItems, _ => true);
        if (rolls.Count == 0)
        {
            _items.AddChild(Dim("Nothing this time. Press Generate to roll again."));
            return;
        }

        foreach (var roll in rolls)
        {
            AddStack(_items, editor, roll.ItemId, $"x{roll.Count}");
            _items.AddChild(new Control { CustomMinimumSize = new Vector2(12f, 0f) });
        }
    }
}
