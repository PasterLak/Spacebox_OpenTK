using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.MenuUi;
using Spacebox.Game.Ships.Sharing;

namespace Spacebox.Game.GUI.Canvas.Windows;

public sealed class ShipLibraryWindow : GameWindow
{
    public const string EmptyText = "No ships yet. Export one from the Ship viewer, or copy .ship files into the Ships folder.";
    private const float ListWidth = 220f;
    private const float InfoWidth = 280f;
    private const float ColumnGap = 8f;
    private const float ContentHeight = 260f;

    private readonly VBoxContainer _rows = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private readonly List<SelectableRow> _rowItems = new List<SelectableRow>();
    private readonly List<ShipLibraryEntry> _shownEntries = new List<ShipLibraryEntry>();
    private readonly Label _empty = StatusText.Create(EmptyText, StatusKind.Muted);
    private readonly VBoxContainer _info = new VBoxContainer();
    private readonly Label _name = new Label { ThemeTypeVariation = "MenuTitle", Autowrap = true };
    private readonly Label _author = new Label { Autowrap = true };
    private readonly Label _size = new Label { Autowrap = true };
    private readonly Label _file = StatusText.Create("", StatusKind.Muted);
    private readonly Label _description = new Label { Autowrap = true };
    private readonly Label _warning = StatusText.Create("", StatusKind.Warning);
    private readonly Button _place = new Button { Text = "Place", SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private ShipLibraryEntry _shown;
    private bool _infoBuilt;

    public ShipLibraryWindow() : base("Ships")
    {
        Name = "Ships";
        var hint = Body.AddChild(StatusText.Create("Export ships in the main menu: Mods > Ship viewer.", StatusKind.Muted));
        hint.Autowrap = true;
        hint.CustomMinimumSize = new Vector2(ListWidth + InfoWidth + ColumnGap, 0f);

        var content = Body.AddChild(new HBoxContainer());
        content.AddThemeConstantOverride("separation", ColumnGap);
        var listColumn = content.AddChild(new VBoxContainer { CustomMinimumSize = new Vector2(ListWidth, ContentHeight) });
        listColumn.AddChild(MenuLayout.ScrollList(_rows, ContentHeight));
        _rows.AddThemeConstantOverride("separation", 2f);
        _empty.Autowrap = true;
        _empty.CustomMinimumSize = new Vector2(ListWidth - 30f, 0f);

        var infoPanel = content.AddChild(new PanelContainer { ThemeTypeVariation = "InsetPanel", CustomMinimumSize = new Vector2(InfoWidth, ContentHeight) });
        infoPanel.AddChild(_info);
        _info.AddThemeConstantOverride("separation", 4f);
        foreach (var label in new[] { _name, _author, _size, _file, _description, _warning })
        {
            label.Autowrap = true;
            label.CustomMinimumSize = new Vector2(InfoWidth - 20f, 0f);
            _info.AddChild(label);
        }

        var footer = Body.AddChild(new HBoxContainer());
        footer.AddThemeConstantOverride("separation", ColumnGap);
        footer.AddChild(_place);
        _place.Pressed += _ => ShipLibraryUI.PlaceSelected();
        var folder = footer.AddChild(new Button { Text = "Open folder", SizeFlagsHorizontal = SizeFlags.ExpandFill });
        folder.Pressed += _ => ShipLibraryUI.OpenFolder();
        var close = footer.AddChild(new Button { Text = "Close", SizeFlagsHorizontal = SizeFlags.ExpandFill });
        close.Pressed += _ => ShipLibraryUI.Close();
    }

    public override bool IsOpen => ShipLibraryUI.IsOpen;

    protected override void Refresh()
    {
        var selected = ShipLibraryUI.Selected;
        bool changed = !_infoBuilt || !ReferenceEquals(selected, _shown);
        Show(ShipLibraryUI.Entries, selected, changed ? ShipLibraryUI.MissingGameSetsOf(selected?.Ship) : null);
    }

    public void Show(IReadOnlyList<ShipLibraryEntry> entries, ShipLibraryEntry selected, IReadOnlyList<string> missingGameSets)
    {
        if (!SameEntries(entries)) BuildRows(entries);
        for (int i = 0; i < _rowItems.Count; i++) _rowItems[i].IsSelected = ReferenceEquals(_rowItems[i].Metadata, selected);
        if (!_infoBuilt || !ReferenceEquals(selected, _shown)) ShowInfo(selected, missingGameSets);
        _place.Disabled = selected?.Ship == null;
    }

    private bool SameEntries(IReadOnlyList<ShipLibraryEntry> entries)
    {
        if (entries.Count != _shownEntries.Count || (entries.Count == 0 && _rows.Children.Count == 0)) return false;
        for (int i = 0; i < entries.Count; i++)
            if (!ReferenceEquals(entries[i], _shownEntries[i])) return false;
        return true;
    }

    private void BuildRows(IReadOnlyList<ShipLibraryEntry> entries)
    {
        _rows.ClearChildren();
        _rowItems.Clear();
        _shownEntries.Clear();
        _shownEntries.AddRange(entries);
        if (entries.Count == 0)
        {
            _rows.AddChild(_empty);
            return;
        }

        foreach (var entry in entries)
        {
            var row = _rows.AddChild(new SelectableRow(0f) { Metadata = entry });
            row.AddLine(entry.Title, entry.Ship != null ? "Label" : "WarningLabel");
            row.Selected += r => ShipLibraryUI.Select((ShipLibraryEntry)r.Metadata);
            row.Activated += _ => ShipLibraryUI.PlaceSelected();
            _rowItems.Add(row);
        }
    }

    private void ShowInfo(ShipLibraryEntry entry, IReadOnlyList<string> missingGameSets)
    {
        _infoBuilt = true;
        _shown = entry;
        if (entry == null)
        {
            Set(_name, "");
            Set(_author, "");
            Set(_size, "");
            Set(_file, "");
            Set(_description, "");
            Set(_warning, "");
            return;
        }

        var ship = entry.Ship;
        if (ship == null)
        {
            Set(_name, "This file cannot be read:");
            Set(_author, "");
            Set(_size, "");
            Set(_file, System.IO.Path.GetFileName(entry.Path));
            Set(_description, "");
            Set(_warning, entry.Error ?? "");
            return;
        }

        Set(_name, ship.Name);
        Set(_author, string.IsNullOrWhiteSpace(ship.Author) ? "" : $"by {ship.Author}");
        Set(_size, $"{ship.Blocks} blocks, {ship.Size.X} x {ship.Size.Y} x {ship.Size.Z}");
        Set(_file, $"Game {ship.GameVersion}, {System.IO.Path.GetFileName(entry.Path)}");
        Set(_description, ship.Description ?? "");
        Set(_warning, missingGameSets == null || missingGameSets.Count == 0 ? "" : $"Made with GameSets this world does not have: {string.Join(", ", missingGameSets)}. Their blocks are left out.");
    }

    private static void Set(Label label, string text)
    {
        text = string.IsNullOrWhiteSpace(text) ? "" : text;
        if (label.Text != text) label.Text = text;
        bool visible = text.Length > 0;
        if (label.Visible != visible) label.Visible = visible;
    }
}
