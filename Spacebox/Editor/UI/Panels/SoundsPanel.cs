using System.Numerics;
using Engine.Audio;
using ImGuiNET;
using Spacebox.Editor.Data;
using Spacebox.Game;

namespace Spacebox.Editor.UI.Panels;

public sealed class SoundsPanel : IEditorPanel, IDisposable
{
    private AudioSource _source;
    private string _playing;
    private string _selected;
    private string _filter = "";
    private float _volume = 1f;
    private SoundUsageIndex _usages;
    private int _usagesVersion = -1;
    private int _assetsVersion = -1;

    public string Title => "Sounds";

    public void Draw(EditorContext context)
    {
        if (_assetsVersion != context.Assets.Version)
        {
            StopAndRelease();
            _assetsVersion = context.Assets.Version;
            _usagesVersion = -1;
        }

        var available = ImGui.GetContentRegionAvail();
        float listWidth = MathF.Max(available.X * 0.4f, 220f * EditorWidgets.Scale);

        if (ImGui.BeginChild("##soundList", new Vector2(listWidth, 0f), ImGuiChildFlags.Border))
            DrawList(context);
        ImGui.EndChild();

        ImGui.SameLine();
        if (ImGui.BeginChild("##soundDetails", Vector2.Zero, ImGuiChildFlags.Border))
            DrawDetails(context);
        ImGui.EndChild();

        if (_source != null && !_source.IsPlaying) _playing = null;
    }

    private void DrawList(EditorContext context)
    {
        ImGui.SetNextItemWidth(-1f);
        ImGui.InputTextWithHint("##soundFilter", "Search...", ref _filter, 128);
        EditorWidgets.Tooltip("Show only sounds whose name or folder contains this text.");
        ImGui.TextDisabled($"{GameAssets.Sounds.Count} sounds. Click a name to select, the button to play.");

        if (!ImGui.BeginChild("##soundRows")) { ImGui.EndChild(); return; }

        string currentGroup = null;
        foreach (var (name, group) in Rows(context))
        {
            if (!Matches(name, group)) continue;
            if (group != currentGroup)
            {
                currentGroup = group;
                ImGui.SeparatorText(string.IsNullOrEmpty(group) ? "(root)" : group);
            }

            ImGui.PushID(name);
            bool isPlaying = _playing == name;
            if (ImGui.SmallButton(isPlaying ? "Stop" : "Play")) Toggle(name);
            EditorWidgets.Tooltip(isPlaying ? "Stops the sound." : "Plays the sound.");
            ImGui.SameLine();
            if (ImGui.Selectable(name, name == _selected)) _selected = name;
            ImGui.PopID();
        }
        ImGui.EndChild();
    }

    private void DrawDetails(EditorContext context)
    {
        if (_selected == null || !GameAssets.Sounds.TryGetValue(_selected, out var clip))
        {
            ImGui.TextDisabled("Select a sound on the left.");
            return;
        }

        EditorWidgets.Header(_selected);
        ImGui.TextDisabled(RelativePath(context, clip.FileFullPath));
        ImGui.Text(clip.IsStereo() ? "Stereo (plays without 3D position)" : "Mono (can play at a position in the world)");
        EditorWidgets.Tooltip("Mono sounds can be placed in the world with distance falloff; stereo sounds always play at full width.");

        if (ImGui.Button(_playing == _selected ? "Stop" : "Play")) Toggle(_selected);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(160f * EditorWidgets.Scale);
        if (ImGui.SliderFloat("Volume", ref _volume, 0f, 1f, "%.2f") && _source != null) _source.Volume = _volume;
        EditorWidgets.Tooltip("Preview volume only; it does not change the file.");

        ImGui.Separator();
        ImGui.TextDisabled("Name to use in JSON: " + _selected);
        var usages = Usages(context).UsagesOf(_selected);
        ImGui.Text(usages.Count == 0 ? "Not used by any JSON file of this GameSet." : $"Used in {usages.Count} files:");
        foreach (var file in usages) ImGui.BulletText(file);
    }

    private IEnumerable<(string Name, string Group)> Rows(EditorContext context)
    {
        var rows = new List<(string, string)>();
        foreach (var pair in GameAssets.Sounds) rows.Add((pair.Key, GroupOf(context, pair.Value.FileFullPath)));
        rows.Sort((a, b) =>
        {
            int group = string.Compare(a.Item2, b.Item2, StringComparison.OrdinalIgnoreCase);
            return group != 0 ? group : string.Compare(a.Item1, b.Item1, StringComparison.OrdinalIgnoreCase);
        });
        return rows;
    }

    private bool Matches(string name, string group)
    {
        if (string.IsNullOrEmpty(_filter)) return true;
        return name.Contains(_filter, StringComparison.OrdinalIgnoreCase) || (group ?? "").Contains(_filter, StringComparison.OrdinalIgnoreCase);
    }

    private SoundUsageIndex Usages(EditorContext context)
    {
        if (_usages != null && _usagesVersion == context.Project.Version) return _usages;

        var files = new List<(string, System.Text.Json.Nodes.JsonNode)>();
        foreach (var path in context.Project.ListJsonFiles()) files.Add((path, context.Project.GetFile(path).Root));
        _usages = SoundUsageIndex.Build(files, GameAssets.Sounds.Keys.ToList());
        _usagesVersion = context.Project.Version;
        return _usages;
    }

    private void Toggle(string name)
    {
        if (_playing == name)
        {
            _source?.Stop();
            _playing = null;
            return;
        }

        if (!GameAssets.Sounds.TryGetValue(name, out var clip)) return;

        StopAndRelease();
        _source = new AudioSource(clip, AudioChannel.UI) { Volume = _volume };
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

    private static string GroupOf(EditorContext context, string fullPath)
    {
        string relative = RelativePath(context, fullPath);
        string folder = Path.GetDirectoryName(relative)?.Replace('\\', '/') ?? "";
        return folder;
    }

    private static string RelativePath(EditorContext context, string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath)) return "";
        string root = context.Project.RootPath;
        string full = Path.GetFullPath(fullPath);
        return full.StartsWith(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase) ? Path.GetRelativePath(root, full).Replace('\\', '/') : "built-in: " + Path.GetFileName(full);
    }

    public void Dispose()
    {
        StopAndRelease();
    }
}
