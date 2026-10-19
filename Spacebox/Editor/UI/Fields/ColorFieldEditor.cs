using System.Numerics;
using System.Text.Json.Nodes;
using ImGuiNET;
using Spacebox.Editor.Data;

namespace Spacebox.Editor.UI.Fields;

public sealed class ColorFieldEditor : IJsonFieldEditor
{
    public bool CanEdit(FieldContext context)
    {
        return EditorJson.HasKeys(context.Value, "r", "g", "b") || EditorJson.HasKeys(context.Value, "r", "g", "b", "a");
    }

    public bool Draw(FieldContext context, out JsonNode value)
    {
        value = context.Value;
        var obj = (JsonObject)context.Value;
        bool hasAlpha = EditorJson.FindKey(obj, "a") != null;

        var color = new Vector4(
            (float)EditorJson.GetNumber(obj, "r") / 255f,
            (float)EditorJson.GetNumber(obj, "g") / 255f,
            (float)EditorJson.GetNumber(obj, "b") / 255f,
            hasAlpha ? (float)EditorJson.GetNumber(obj, "a", 255) / 255f : 1f);

        ImGui.SetNextItemWidth(-1f);
        bool changed;
        if (hasAlpha)
        {
            changed = ImGui.ColorEdit4("##color", ref color);
        }
        else
        {
            var rgb = new Vector3(color.X, color.Y, color.Z);
            changed = ImGui.ColorEdit3("##color", ref rgb);
            color = new Vector4(rgb, 1f);
        }
        if (!changed) return false;

        EditorJson.Set(obj, "r", JsonValue.Create(ToByte(color.X)));
        EditorJson.Set(obj, "g", JsonValue.Create(ToByte(color.Y)));
        EditorJson.Set(obj, "b", JsonValue.Create(ToByte(color.Z)));
        if (hasAlpha) EditorJson.Set(obj, "a", JsonValue.Create(ToByte(color.W)));
        return true;
    }

    private static int ToByte(float value)
    {
        return (int)MathF.Round(Math.Clamp(value, 0f, 1f) * 255f);
    }
}
