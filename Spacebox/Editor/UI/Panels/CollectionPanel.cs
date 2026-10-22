using System.Numerics;
using System.Text.Json.Nodes;
using ImGuiNET;
using Spacebox.Editor.Data;
using Spacebox.Editor.Preview;

namespace Spacebox.Editor.UI.Panels;

public sealed class CollectionPanel : IEditorPanel
{
    private readonly string _scope;
    private readonly IEntrySource _source;
    private readonly JsonInspector _inspector;
    private readonly IEntryPreview _preview;
    private readonly IEntryIconProvider _icons;
    private readonly IEntrySummary _summary;
    private PreviewViewport _viewport;
    private EditorEntry _selected;
    private string _selectedKey;
    private string _filter = "";
    private string _groupFilter = "";
    private string _newGroup = "";
    private bool _rawMode;
    private string _rawText = "";
    private EditorEntry _pendingDelete;
    private bool _initialized;

    public string Title { get; }

    public CollectionPanel(string title, string scope, IEntrySource source, JsonInspector inspector, IEntryPreview preview = null, IEntryIconProvider icons = null, IEntrySummary summary = null)
    {
        Title = title;
        _scope = scope;
        _source = source;
        _inspector = inspector;
        _preview = preview;
        _icons = icons;
        _summary = summary;
    }

    public void Draw(EditorContext context)
    {
        var entries = _source.GetEntries();
        ResolveSelection(entries);
        if (_selected == null && !_initialized && entries.Count > 0) Select(entries[0], context);
        _initialized = true;

        var available = ImGui.GetContentRegionAvail();
        float listWidth = MathF.Max(available.X * 0.27f, 160f * EditorWidgets.Scale);

        if (ImGui.BeginChild("##list", new Vector2(listWidth, available.Y), ImGuiChildFlags.Border))
            DrawList(context, entries);
        ImGui.EndChild();

        ImGui.SameLine();
        if (ImGui.BeginChild("##details", new Vector2(0f, available.Y), ImGuiChildFlags.Border))
            DrawDetails(context);
        ImGui.EndChild();

        DrawDeletePopup();
    }

    private void DrawList(EditorContext context, IReadOnlyList<EditorEntry> entries)
    {
        ImGui.SetNextItemWidth(-1f);
        ImGui.InputTextWithHint("##filter", "Search...", ref _filter, 128);
        EditorWidgets.Tooltip("Show only entries whose id or name contains this text.");

        var groups = _source.Groups;
        if (groups.Count > 0)
        {
            ImGui.SetNextItemWidth(-1f);
            if (ImGui.BeginCombo("##group", string.IsNullOrEmpty(_groupFilter) ? "All groups" : _groupFilter))
            {
                if (ImGui.Selectable("All groups", string.IsNullOrEmpty(_groupFilter))) _groupFilter = "";
                for (int i = 0; i < groups.Count; i++)
                {
                    if (ImGui.Selectable(groups[i], groups[i] == _groupFilter)) _groupFilter = groups[i];
                }
                ImGui.EndCombo();
            }
        }

        bool create = ImGui.Button("+ New");
        EditorWidgets.Tooltip("Creates a new entry with default values.");
        if (create)
        {
            if (groups.Count > 0) ImGui.OpenPopup("##newInGroup");
            else Select(_source.Add(null), context);
        }

        if (ImGui.BeginPopup("##newInGroup"))
        {
            for (int i = 0; i < groups.Count; i++)
            {
                if (ImGui.Selectable(groups[i])) Select(_source.Add(groups[i]), context);
            }
            ImGui.Separator();
            ImGui.SetNextItemWidth(160f * EditorWidgets.Scale);
            ImGui.InputTextWithHint("##newGroup", "new group", ref _newGroup, 64);
            ImGui.SameLine();
            if (ImGui.Button("Create") && !string.IsNullOrWhiteSpace(_newGroup))
            {
                Select(_source.Add(_newGroup.Trim()), context);
                _newGroup = "";
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }

        ImGui.SameLine();
        ImGui.TextDisabled($"{entries.Count} entries");
        ImGui.Separator();

        if (!ImGui.BeginChild("##entries")) { ImGui.EndChild(); return; }

        float iconSize = ImGui.GetTextLineHeight() * 1.3f;
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (!string.IsNullOrEmpty(_groupFilter) && entry.Group != _groupFilter) continue;

            var label = _source.Definition.GetLabel(entry.Node, FileLabel(entry));
            if (!string.IsNullOrEmpty(_filter) && label.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) < 0) continue;

            ImGui.PushID(entry.Key);
            if (_icons != null)
            {
                EditorWidgets.Icon(_icons.GetIcon(context, entry), iconSize);
                ImGui.SameLine();
            }

            string display = entry.Group != null && string.IsNullOrEmpty(_groupFilter) ? $"[{entry.Group}] {label}" : label;
            if (ImGui.Selectable(display, entry.Key == _selectedKey, ImGuiSelectableFlags.None, new Vector2(0f, iconSize)))
                Select(entry, context);
            ImGui.PopID();
        }

        ImGui.EndChild();
    }

    private void DrawDetails(EditorContext context)
    {
        if (_selected == null)
        {
            ImGui.TextDisabled("Select an entry on the left or create a new one.");
            return;
        }

        var entry = _selected;
        string label = _source.Definition.GetLabel(entry.Node, FileLabel(entry));
        EditorWidgets.Header(label + (entry.File.IsDirty ? "  *" : ""));
        ImGui.TextDisabled(entry.File.RelativePath);

        if (ImGui.Button("Duplicate")) Select(_source.Duplicate(entry), context);
        EditorWidgets.Tooltip("Copies this entry under a new id.");
        ImGui.SameLine();
        if (ImGui.Button("Delete")) _pendingDelete = entry;
        EditorWidgets.Tooltip("Deletes this entry (asks first). Removed ids turn placed blocks and items in worlds into nothing.");
        ImGui.SameLine();
        if (ImGui.Checkbox("Raw JSON", ref _rawMode) && _rawMode) _rawText = EditorJsonFile.Serialize(entry.Node);
        EditorWidgets.Tooltip("Edit the entry as JSON text instead of the property list.");

        if (entry.File.Error != null)
        {
            ImGui.TextColored(new Vector4(1f, 0.4f, 0.3f, 1f), "Parse error: " + entry.File.Error);
        }

        _summary?.Draw(context, entry);

        var available = ImGui.GetContentRegionAvail();
        bool hasPreview = _preview != null && _preview.CanPreview(context, entry);

        if (hasPreview && available.X > available.Y * 1.3f)
        {
            float previewWidth = available.X * 0.45f;
            if (ImGui.BeginChild("##inspector", new Vector2(available.X - previewWidth - ImGui.GetStyle().ItemSpacing.X, available.Y)))
                DrawEditor(context, entry);
            ImGui.EndChild();
            ImGui.SameLine();
            if (ImGui.BeginChild("##preview", new Vector2(0f, available.Y)))
                DrawPreview(context, entry);
            ImGui.EndChild();
        }
        else
        {
            if (hasPreview)
            {
                float previewHeight = MathF.Min(available.Y * 0.45f, available.X * 0.75f);
                if (ImGui.BeginChild("##preview", new Vector2(0f, previewHeight)))
                    DrawPreview(context, entry);
                ImGui.EndChild();
            }

            if (ImGui.BeginChild("##inspector"))
                DrawEditor(context, entry);
            ImGui.EndChild();
        }
    }

    private void DrawEditor(EditorContext context, EditorEntry entry)
    {
        if (_rawMode)
        {
            var size = ImGui.GetContentRegionAvail() - new Vector2(0f, ImGui.GetFrameHeightWithSpacing());
            ImGui.InputTextMultiline("##raw", ref _rawText, 1 << 20, size);
            if (ImGui.Button("Apply JSON")) ApplyRaw(context, entry);
            EditorWidgets.Tooltip("Checks the JSON text and replaces the entry with it.");
            ImGui.SameLine();
            if (ImGui.Button("Revert")) _rawText = EditorJsonFile.Serialize(entry.Node);
            EditorWidgets.Tooltip("Throws away the text changes.");
            return;
        }

        if (_inspector.Draw(context, "##props", entry.Node, _source.Definition.Schema(entry.Node), _scope))
            entry.File.MarkDirty();
    }

    private void DrawPreview(EditorContext context, EditorEntry entry)
    {
        _viewport ??= context.CreateViewport();
        _preview.DrawControls(context, entry);
        _preview.Apply(context, entry, _viewport);
        _viewport.Draw(ImGui.GetContentRegionAvail());
    }

    private void ApplyRaw(EditorContext context, EditorEntry entry)
    {
        try
        {
            var parsed = EditorJsonFile.Parse(_rawText);
            if (!ReplaceContents(entry.Node, parsed))
            {
                context.SetStatus("Raw JSON must keep the same kind (object or array)", true);
                return;
            }
            entry.File.MarkDirty();
            context.SetStatus("JSON applied");
        }
        catch (Exception ex)
        {
            context.SetStatus("Invalid JSON: " + ex.Message, true);
        }
    }

    private static bool ReplaceContents(JsonNode target, JsonNode source)
    {
        if (target is JsonObject targetObject && source is JsonObject sourceObject)
        {
            targetObject.Clear();
            foreach (var key in sourceObject.Select(p => p.Key).ToArray())
            {
                var value = sourceObject[key];
                sourceObject.Remove(key);
                targetObject[key] = value;
            }
            return true;
        }

        if (target is JsonArray targetArray && source is JsonArray sourceArray)
        {
            targetArray.Clear();
            while (sourceArray.Count > 0)
            {
                var value = sourceArray[0];
                sourceArray.RemoveAt(0);
                targetArray.Add(value);
            }
            return true;
        }

        return false;
    }

    private void DrawDeletePopup()
    {
        if (_pendingDelete != null) ImGui.OpenPopup("Delete entry?");

        bool open = true;
        if (!ImGui.BeginPopupModal("Delete entry?", ref open, ImGuiWindowFlags.AlwaysAutoResize))
        {
            if (!open) _pendingDelete = null;
            return;
        }

        var label = _pendingDelete == null ? "" : _source.Definition.GetLabel(_pendingDelete.Node, FileLabel(_pendingDelete));
        ImGui.Text($"Delete '{label}'? Files are only changed when you save.");
        if (ImGui.Button("Delete"))
        {
            if (_pendingDelete != null) _source.Remove(_pendingDelete);
            if (_pendingDelete == _selected) Select(null, null);
            _pendingDelete = null;
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            _pendingDelete = null;
            ImGui.CloseCurrentPopup();
        }
        ImGui.EndPopup();
    }

    private void ResolveSelection(IReadOnlyList<EditorEntry> entries)
    {
        if (_selected == null) return;

        for (int i = 0; i < entries.Count; i++)
        {
            if (ReferenceEquals(entries[i].Node, _selected.Node))
            {
                _selected = entries[i];
                _selectedKey = entries[i].Key;
                return;
            }
        }

        _selected = null;
        _selectedKey = null;
    }

    private void Select(EditorEntry entry, EditorContext context)
    {
        _selected = entry;
        _selectedKey = entry?.Key;
        if (_rawMode && entry != null) _rawText = EditorJsonFile.Serialize(entry.Node);
    }

    private static string FileLabel(EditorEntry entry)
    {
        return Path.GetFileNameWithoutExtension(entry.File.RelativePath);
    }
}
