using System.Text.Json.Nodes;
using Engine.Graphics.Atlas;

namespace Spacebox.Editor.UI.Fields;

public sealed class ReferenceFieldEditor : IJsonFieldEditor
{
    private readonly Func<FieldContext, bool> _matches;
    private readonly Func<EditorContext, IReadOnlyList<string>> _options;
    private readonly Func<EditorContext, string, AtlasSprite> _icon;

    public ReferenceFieldEditor(Func<FieldContext, bool> matches, Func<EditorContext, IReadOnlyList<string>> options, Func<EditorContext, string, AtlasSprite> icon = null)
    {
        _matches = matches;
        _options = options;
        _icon = icon;
    }

    public bool CanEdit(FieldContext context)
    {
        return context.Value is JsonValue && context.StringValue != null && _matches(context);
    }

    public bool Draw(FieldContext context, out JsonNode value)
    {
        value = context.Value;
        string current = context.StringValue ?? "";
        var editor = context.Editor;
        Func<string, AtlasSprite> icon = _icon == null ? null : name => _icon(editor, name);

        if (!EditorWidgets.SearchCombo("##ref", ref current, _options(editor), icon)) return false;
        value = JsonValue.Create(current);
        return true;
    }
}
