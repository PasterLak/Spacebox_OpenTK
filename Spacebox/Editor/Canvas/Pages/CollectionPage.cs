using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Editor.Canvas.Inspector;
using Spacebox.Editor.Data;

namespace Spacebox.Editor.Canvas.Pages;

public sealed class CollectionPage : EditorPage
{
    private readonly string _scope;
    private readonly IEntrySource _source;
    private readonly FieldWidgetRegistry _widgets;
    private readonly IEntryView _preview;
    private readonly IEntryIconProvider _icons;
    private readonly IEntryView _summary;
    private readonly LineEdit _filter = new LineEdit { PlaceholderText = "Search...", TooltipText = "Show only entries whose id or name contains this text." };
    private readonly ComboBox _group = new ComboBox { Editable = false, PlaceholderText = "All groups", TooltipText = "Show only one group (folder) of entries." };
    private readonly Label _count = new Label { FontColor = EditorCanvasTheme.TextDim, VerticalAlignment = VerticalAlignment.Center };
    private readonly ItemList _list = new ItemList { Name = "Entries", SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand, IconSize = 18f };
    private readonly Label _header = new Label { FontColor = EditorCanvasTheme.Accent };
    private readonly Label _path = new Label { FontColor = EditorCanvasTheme.TextDim, Overrun = TextOverrun.Ellipsis };
    private readonly Label _empty = new Label("Select an entry on the left or create a new one.") { FontColor = EditorCanvasTheme.TextDim };
    private readonly VBoxContainer _details = new VBoxContainer { Name = "Details" };
    private readonly HBoxContainer _actions = new HBoxContainer();
    private readonly Control _summaryHolder = new VBoxContainer();
    private readonly PopupMenu _newMenu = new PopupMenu();
    private readonly ConfirmDialog _confirm = new ConfirmDialog();
    private JsonDocumentView _document;
    private SplitContainer _editorSplit;
    private Button _moveUp;
    private Button _moveDown;
    private IReadOnlyList<EditorEntry> _shownEntries;
    private int _shownCount = -1;
    private string _shownFilter = "";
    private string _shownGroup = "";
    private EditorEntry _selected;
    private bool _previewShown;

    public CollectionPage(string title, string scope, IEntrySource source, FieldWidgetRegistry widgets, IEntryView preview = null, IEntryIconProvider icons = null, IEntryView summary = null, string description = "")
    {
        Title = title;
        _scope = scope;
        _source = source;
        _widgets = widgets;
        _preview = preview;
        _icons = icons;
        _summary = summary;
        Description = description;
    }

    public override string Title { get; }
    public override string Description { get; }

    protected override void Build()
    {
        var split = AddChild(new SplitContainer { SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand, MinFirst = 180f, MinSecond = 360f });
        split.Ratio = 0.26f;

        var left = split.AddChild(new VBoxContainer());
        left.AddChild(_filter);
        left.AddChild(_group);
        var row = left.AddChild(new HBoxContainer());
        var create = row.AddChild(new Button("+ New") { TooltipText = "Creates a new entry with default values." });
        create.Pressed += _ => OpenNewMenu(create);
        row.AddChild(_count);
        left.AddChild(_list);
        _filter.TextChanged += _ => RefreshList(true);
        _group.ValueChanged += _ => RefreshList(true);
        _list.ItemSelected += index => Select(_list.Items[index].Metadata as EditorEntry);

        var right = split.AddChild(new VBoxContainer());
        right.AddChild(_empty);
        right.AddChild(_details);
        _details.SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand;
        _details.AddChild(_header);
        _details.AddChild(_path);
        _details.AddChild(_actions);
        Action(_actions, "Duplicate", "Copies this entry under a new id.", () => Select(_source.Duplicate(_selected)));
        Action(_actions, "Delete", "Deletes this entry (asks first). Removed ids turn placed blocks and items in worlds into nothing.", AskDelete);
        if (_source is IReorderableEntrySource reorderable)
        {
            _moveUp = Action(_actions, "Move up", "Moves this entry one place up. The order is the order in the game.", () => reorderable.Move(_selected, -1));
            _moveDown = Action(_actions, "Move down", "Moves this entry one place down. The order is the order in the game.", () => reorderable.Move(_selected, 1));
        }
        _details.AddChild(_summaryHolder);
        if (_summary != null) _summaryHolder.AddChild(_summary.Control);

        _document = new JsonDocumentView(_widgets) { SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand };
        if (_preview is IEntryEditor)
        {
            var editorControl = _details.AddChild(_preview.Control);
            editorControl.SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand;
        }
        else if (_preview != null)
        {
            _editorSplit = _details.AddChild(new SplitContainer { SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand, MinFirst = 260f, MinSecond = 200f });
            _editorSplit.Ratio = 0.55f;
            _editorSplit.AddChild(_document);
            var previewControl = _editorSplit.AddChild(_preview.Control);
            previewControl.SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand;
        }
        else
        {
            _details.AddChild(_document);
        }

        RefreshList(true);
    }

    protected override void OnShown()
    {
        RefreshList(true);
        if (_selected == null && _shownEntries?.Count > 0) Select(_shownEntries[0]);
    }

    public override void Tick(float deltaTime)
    {
        var entries = _source.GetEntries();
        if (!ReferenceEquals(entries, _shownEntries) || entries.Count != _shownCount) RefreshList(false);
        ResolveSelection(entries);
        UpdateDetails();
        if (_selected != null)
        {
            _summary?.Tick(Editor, deltaTime);
            if (_previewShown) _preview?.Tick(Editor, deltaTime);
        }
    }

    public override void Release()
    {
        _preview?.Release();
        _summary?.Release();
    }

    private void RefreshList(bool force)
    {
        var entries = _source.GetEntries();
        string filter = _filter.Text;
        string group = _group.Value;
        if (!force && ReferenceEquals(entries, _shownEntries) && entries.Count == _shownCount && filter == _shownFilter && group == _shownGroup) return;

        _shownEntries = entries;
        _shownCount = entries.Count;
        _shownFilter = filter;
        _shownGroup = group;
        RefreshGroups();

        _list.Clear();
        int shown = 0;
        foreach (var entry in entries)
        {
            if (!string.IsNullOrEmpty(group) && entry.Group != group) continue;
            string label = _source.Definition.GetLabel(entry.Node, FileLabel(entry));
            if (!string.IsNullOrEmpty(filter) && label.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;

            string display = entry.Group != null && string.IsNullOrEmpty(group) ? $"[{entry.Group}] {label}" : label;
            var item = _list.AddItem(display, null, entry);
            item.Tooltip = entry.File.RelativePath;
            if (_icons != null) item.WithIcon(_icons.GetIcon(Editor, entry));
            if (_selected != null && ReferenceEquals(entry.Node, _selected.Node)) _list.Select(_list.Items.Count - 1);
            shown++;
        }
        _count.Text = shown == entries.Count ? $"{entries.Count} entries" : $"{shown} of {entries.Count}";
    }

    private void RefreshGroups()
    {
        var groups = _source.Groups;
        _group.Visible = groups.Count > 0;
        var options = new List<ListItem> { new ListItem { Text = "" , Secondary = "All groups" } };
        foreach (var group in groups) options.Add(new ListItem { Text = group });
        _group.SetOptions(options);
    }

    private void ResolveSelection(IReadOnlyList<EditorEntry> entries)
    {
        if (_selected == null) return;
        foreach (var entry in entries)
        {
            if (!ReferenceEquals(entry.Node, _selected.Node)) continue;
            if (!ReferenceEquals(entry, _selected)) _selected = entry;
            return;
        }

        Select(entries.Count > 0 ? entries[0] : null);
    }

    private void Select(EditorEntry entry)
    {
        _selected = entry;
        _empty.Visible = entry == null;
        _details.Visible = entry != null;
        if (entry == null)
        {
            _document.Clear();
            return;
        }

        int index = _list.IndexOf(item => item.Metadata is EditorEntry e && ReferenceEquals(e.Node, entry.Node));
        if (index >= 0 && index != _list.SelectedIndex) _list.Select(index);
        if (index >= 0) _list.EnsureVisible(index);
        if (_preview is not IEntryEditor) _document.BindNode(Editor, entry.File, entry.Node, _source.Definition.Schema(entry.Node), _scope);
        _summary?.Show(Editor, entry);
        _summaryHolder.Visible = _summary != null && _summary.Applies(Editor, entry);
        _previewShown = _preview != null && _preview.Applies(Editor, entry);
        if (_preview != null)
        {
            _preview.Control.Visible = _previewShown;
            if (_previewShown) _preview.Show(Editor, entry);
        }
    }

    private void UpdateDetails()
    {
        if (_selected == null) return;
        _header.Text = _source.Definition.GetLabel(_selected.Node, FileLabel(_selected)) + (_selected.File.IsDirty ? "  *" : "");
        _path.Text = _selected.File.RelativePath;
        if (_source is IReorderableEntrySource reorderable && _moveUp != null)
        {
            bool up = reorderable.CanMove(_selected, -1);
            bool down = reorderable.CanMove(_selected, 1);
            _moveUp.Visible = _moveDown.Visible = up || down;
            _moveUp.Disabled = !up;
            _moveDown.Disabled = !down;
        }
    }

    private void OpenNewMenu(Button anchor)
    {
        var groups = _source.Groups;
        if (groups.Count == 0)
        {
            Select(_source.Add(null));
            return;
        }

        _newMenu.ClearItems();
        foreach (var group in groups)
        {
            string name = group;
            _newMenu.AddItem("New in " + name, () => Select(_source.Add(name)));
        }
        _newMenu.AddSeparator();
        _newMenu.AddItem("New without group", () => Select(_source.Add(null)));
        _newMenu.Open(Root, new Vector2(anchor.Rect.X, anchor.Rect.End.Y));
    }

    private void AskDelete()
    {
        var entry = _selected;
        if (entry == null) return;
        string label = _source.Definition.GetLabel(entry.Node, FileLabel(entry));
        _confirm.Show(Root, "Delete entry", $"Delete '{label}'? Files are only changed when you save; placed blocks and items with a removed id turn into nothing in worlds.", new[] { "Delete", "Cancel" }, result =>
        {
            if (result != 0) return;
            _source.Remove(entry);
            Select(null);
            RefreshList(true);
        });
    }

    private static Button Action(Container parent, string text, string tooltip, Action action)
    {
        var button = parent.AddChild(new Button(text) { TooltipText = tooltip, ThemeTypeVariation = "SmallButton" });
        button.Pressed += _ => action();
        return button;
    }

    private static string FileLabel(EditorEntry entry)
    {
        string file = Path.GetFileNameWithoutExtension(entry.File.RelativePath);
        return string.Equals(file, "block", StringComparison.OrdinalIgnoreCase) || string.Equals(file, "item", StringComparison.OrdinalIgnoreCase)
            ? Path.GetFileName(Path.GetDirectoryName(entry.File.RelativePath)) ?? file
            : file;
    }
}
