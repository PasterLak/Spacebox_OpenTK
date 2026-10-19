using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using ImGuiNET;
using Spacebox.Editor.Data;

namespace Spacebox.Editor.UI.Fields;

public sealed class PrimitiveFieldEditor : IJsonFieldEditor
{
    private static readonly string[] MultilineNames = { "Description", "Info" };

    public bool CanEdit(FieldContext context)
    {
        return context.Value is JsonValue || context.Value == null;
    }

    public bool Draw(FieldContext context, out JsonNode value)
    {
        value = context.Value;
        ImGui.SetNextItemWidth(-1f);

        if (context.Value == null)
        {
            ImGui.TextDisabled("null");
            ImGui.SameLine();
            if (!ImGui.SmallButton("Set")) return false;
            value = JsonSchema.CreateEmpty(context.SchemaType ?? typeof(string));
            return true;
        }

        var json = (JsonValue)context.Value;
        switch (json.GetValueKind())
        {
            case JsonValueKind.True:
            case JsonValueKind.False:
                bool flag = json.GetValue<bool>();
                if (!ImGui.Checkbox("##bool", ref flag)) return false;
                value = JsonValue.Create(flag);
                return true;

            case JsonValueKind.Number:
                return DrawNumber(context, json, out value);

            default:
                string text = json.TryGetValue(out string s) ? s : json.ToJsonString();
                bool changed = context.NameIs(MultilineNames)
                    ? ImGui.InputTextMultiline("##text", ref text, 4096, new System.Numerics.Vector2(-1f, ImGui.GetTextLineHeight() * 4f))
                    : ImGui.InputText("##text", ref text, 1024);
                if (!changed) return false;
                value = JsonValue.Create(text);
                return true;
        }
    }

    private static bool DrawNumber(FieldContext context, JsonValue json, out JsonNode value)
    {
        value = json;
        EditorJson.TryGetNumber(json, out var number);
        var type = Nullable.GetUnderlyingType(context.SchemaType ?? typeof(double)) ?? context.SchemaType ?? typeof(double);
        bool integral = IsIntegralType(type) || context.SchemaType == null && EditorJson.IsIntegral(json);

        if (integral)
        {
            GetRange(type, out double min, out double max);
            int current = (int)Math.Clamp(number, int.MinValue, int.MaxValue);
            if (!ImGui.DragInt("##int", ref current, 0.25f, (int)Math.Max(min, int.MinValue), (int)Math.Min(max, int.MaxValue))) return false;
            value = JsonValue.Create(current);
            return true;
        }

        float f = (float)number;
        string format = Math.Abs(f) < 0.1f && f != 0f ? "%.4f" : "%.3f";
        if (!ImGui.DragFloat("##float", ref f, Math.Max(0.001f, Math.Abs(f) * 0.01f), 0f, 0f, format)) return false;
        value = JsonValue.Create(Math.Round(f, 4));
        return true;
    }

    private static bool IsIntegralType(Type type)
    {
        return type == typeof(int) || type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) ||
               type == typeof(ushort) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong);
    }

    private static void GetRange(Type type, out double min, out double max)
    {
        if (type == typeof(byte)) { min = byte.MinValue; max = byte.MaxValue; return; }
        if (type == typeof(sbyte)) { min = sbyte.MinValue; max = sbyte.MaxValue; return; }
        if (type == typeof(short)) { min = short.MinValue; max = short.MaxValue; return; }
        if (type == typeof(ushort)) { min = ushort.MinValue; max = ushort.MaxValue; return; }
        min = 0;
        max = 0;
    }
}
