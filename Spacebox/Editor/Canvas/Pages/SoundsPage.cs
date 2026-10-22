using Engine.Audio;
using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Editor.Data;
using Spacebox.Game;

namespace Spacebox.Editor.Canvas.Pages;

public sealed class SoundsPage : EditorPage
{
    private readonly LineEdit _filter = new LineEdit { PlaceholderText = "Search...", TooltipText = "Show only sounds whose name or folder contains this text." };
    private readonly Label _count = new Label { FontColor = EditorCanvasTheme.TextDim };
    private readonly ItemList _list = new ItemList { Name = "Sounds", SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand };
    private readonly Label _empty = new Label("Select a sound on the left.") { FontColor = EditorCanvasTheme.TextDim };
    private readonly VBoxContainer _details = new VBoxContainer();
    private readonly Label _header = new Label { FontColor = EditorCanvasTheme.Accent };
    private readonly Label _path = new Label { FontColor = EditorCanvasTheme.TextDim };
    private readonly Label _channels = new Label { TooltipText = "Mono sounds can be placed in the world with distance falloff; stereo sounds always play at full width.", MouseFilter = MouseFilter.Stop };
    private readonly Button _play = new Button("Play") { TooltipText = "Plays or stops the sound." };
    private readonly HSlider _volume = new HSlider { MinValue = 0, MaxValue = 1, Value = 1, CustomMinimumSize = new Vector2(140f, 0f), TooltipText = "Preview volume only; it does not change the file." };
    private readonly Label _jsonName = new Label { FontColor = EditorCanvasTheme.TextDim };
    private readonly Label _usages = new Label { Autowrap = true };
    private AudioSource _source;
    private string _playing;
    private string _selected;
    private SoundUsageIndex _usageIndex;
    private int _usagesVersion = -1;
    private int _assetsVersion = -1;
    private string _shownFilter;
    private string _shownDetails;

    public override string Title => "Sounds";
    public override string Description => "Every sound of the GameSet: play them and see which files use them.";

    protected override void Build()
    {
        var split = AddChild(new SplitContainer { SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand, MinFirst = 200f, MinSecond = 260f });
        split.Ratio = 0.4f;
        var left = split.AddChild(new VBoxContainer());
        left.AddChild(_filter);
        left.AddChild(_count);
        left.AddChild(_list);
        _list.ItemSelected += index => _selected = _list.Items[index].Metadata as string;
        _list.ItemActivated += index => Toggle(_list.Items[index].Metadata as string);

        var right = split.AddChild(new VBoxContainer());
        right.AddChild(_empty);
        right.AddChild(_details);
        _details.AddChild(_header);
        _details.AddChild(_path);
        _details.AddChild(_channels);
        var row = _details.AddChild(new HBoxContainer());
        row.AddChild(_play);
        _play.Pressed += _ => Toggle(_selected);
        row.AddChild(new Label("Volume") { VerticalAlignment = VerticalAlignment.Center });
        row.AddChild(_volume);
        _volume.ValueChanged += value =>
        {
            if (_source != null) _source.Volume = (float)value;
        };
        _details.AddChild(new HSeparator());
        _details.AddChild(_jsonName);
        _details.AddChild(_usages);
    }

    public override void Tick(float deltaTime)
    {
        if (_assetsVersion != Editor.Assets.Version)
        {
            StopAndRelease();
            _assetsVersion = Editor.Assets.Version;
            _usagesVersion = -1;
            _shownFilter = null;
        }

        if (_source != null && !_source.IsPlaying) _playing = null;
        if (_shownFilter != _filter.Text) RefreshList();
        UpdateDetails();
    }

    public override void Hide()
    {
        StopAndRelease();
    }

    public override void Release()
    {
        StopAndRelease();
    }

    private void RefreshList()
    {
        _shownFilter = _filter.Text;
        _list.Clear();
        _count.Text = $"{GameAssets.Sounds.Count} sounds. Double click to play.";

        var rows = new List<(string Name, string Group)>();
        foreach (var pair in GameAssets.Sounds) rows.Add((pair.Key, GroupOf(pair.Value.FileFullPath)));
        rows.Sort((a, b) =>
        {
            int group = string.Compare(a.Group, b.Group, StringComparison.OrdinalIgnoreCase);
            return group != 0 ? group : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });

        string currentGroup = null;
        foreach (var (name, group) in rows)
        {
            if (!Matches(name, group)) continue;
            if (group != currentGroup)
            {
                currentGroup = group;
                _list.AddHeader(string.IsNullOrEmpty(group) ? "(root)" : group);
            }

            _list.AddItem(name, null, name).Indent = 1;
            if (name == _selected) _list.Select(_list.Items.Count - 1);
        }
    }

    private void UpdateDetails()
    {
        bool has = _selected != null && GameAssets.Sounds.ContainsKey(_selected);
        _empty.Visible = !has;
        _details.Visible = has;
        if (!has) return;

        _play.Text = _playing == _selected ? "Stop" : "Play";
        string key = _selected + "|" + Editor.Project.Version;
        if (key == _shownDetails) return;
        _shownDetails = key;

        var clip = GameAssets.Sounds[_selected];
        _header.Text = _selected;
        _path.Text = RelativePath(clip.FileFullPath);
        _channels.Text = clip.IsStereo() ? "Stereo (plays without 3D position)" : "Mono (can play at a position in the world)";
        _jsonName.Text = "Name to use in JSON: " + _selected;
        var usages = Usages().UsagesOf(_selected);
        _usages.Text = usages.Count == 0 ? "Not used by any JSON file of this GameSet." : $"Used in {usages.Count} files:\n- " + string.Join("\n- ", usages);
    }

    private bool Matches(string name, string group)
    {
        string filter = _filter.Text;
        if (string.IsNullOrEmpty(filter)) return true;
        return name.Contains(filter, StringComparison.OrdinalIgnoreCase) || (group ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private SoundUsageIndex Usages()
    {
        if (_usageIndex != null && _usagesVersion == Editor.Project.Version) return _usageIndex;

        var files = new List<(string, System.Text.Json.Nodes.JsonNode)>();
        foreach (var path in Editor.Project.ListJsonFiles()) files.Add((path, Editor.Project.GetFile(path).Root));
        _usageIndex = SoundUsageIndex.Build(files, new List<string>(GameAssets.Sounds.Keys));
        _usagesVersion = Editor.Project.Version;
        return _usageIndex;
    }

    private void Toggle(string name)
    {
        if (name == null) return;
        if (_playing == name)
        {
            _source?.Stop();
            _playing = null;
            return;
        }

        if (!GameAssets.Sounds.TryGetValue(name, out var clip)) return;

        StopAndRelease();
        _source = new AudioSource(clip, AudioChannel.UI) { Volume = (float)_volume.Value };
        _source.Play();
        _playing = name;
        _selected = name;
    }

    private void StopAndRelease()
    {
        _source?.Stop();
        _source?.Dispose();
        _source = null;
        _playing = null;
    }

    private string GroupOf(string fullPath)
    {
        return Path.GetDirectoryName(RelativePath(fullPath))?.Replace('\\', '/') ?? "";
    }

    private string RelativePath(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath)) return "";
        string root = Editor.Project.RootPath;
        string full = Path.GetFullPath(fullPath);
        return full.StartsWith(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase) ? Path.GetRelativePath(root, full).Replace('\\', '/') : "built-in: " + Path.GetFileName(full);
    }
}
