using System.Text.Json;
using System.Text.Json.Nodes;
using ImGuiNET;
using Spacebox.Editor.Data;

namespace Spacebox.Editor.UI.Fields;

public sealed class NumberRangeFieldEditor : IJsonFieldEditor
{
    private readonly Func<FieldContext, bool> _matches;
    private readonly int _min;
    private readonly int _max;

    public NumberRangeFieldEditor(Func<FieldContext, bool> matches, int min, int max)
    {
        _matches = matches;
        _min = min;
        _max = max;
    }

    public bool CanEdit(FieldContext context)
    {
        return context.Value is JsonValue json && json.GetValueKind() == JsonValueKind.Number && _matches(context);
    }

    public bool Draw(FieldContext context, out JsonNode value)
    {
        value = context.Value;
        EditorJson.TryGetNumber((JsonValue)context.Value, out var number);
        int current = (int)Math.Clamp(number, _min, _max);

        ImGui.SetNextItemWidth(-1f);
        if (!ImGui.DragInt("##range", ref current, 0.25f, _min, _max, "%d", ImGuiSliderFlags.AlwaysClamp)) return false;

        value = JsonValue.Create(current);
        return true;
    }
}
