using System.Text.Json.Nodes;
using ImGuiNET;
using Spacebox.Editor.Data;

namespace Spacebox.Editor.UI.Fields;

public sealed class VectorFieldEditor : IJsonFieldEditor
{
    public bool CanEdit(FieldContext context)
    {
        return EditorJson.HasKeys(context.Value, "x", "y", "z");
    }

    public bool Draw(FieldContext context, out JsonNode value)
    {
        value = context.Value;
        var obj = (JsonObject)context.Value;
        bool integral = IsIntegral(obj, "x") && IsIntegral(obj, "y") && IsIntegral(obj, "z");
        ImGui.SetNextItemWidth(-1f);

        if (integral)
        {
            var v = new int[] { (int)EditorJson.GetNumber(obj, "x"), (int)EditorJson.GetNumber(obj, "y"), (int)EditorJson.GetNumber(obj, "z") };
            if (!ImGui.DragInt3("##vec", ref v[0], 0.1f)) return false;
            EditorJson.Set(obj, "x", JsonValue.Create(v[0]));
            EditorJson.Set(obj, "y", JsonValue.Create(v[1]));
            EditorJson.Set(obj, "z", JsonValue.Create(v[2]));
            return true;
        }

        var f = new System.Numerics.Vector3((float)EditorJson.GetNumber(obj, "x"), (float)EditorJson.GetNumber(obj, "y"), (float)EditorJson.GetNumber(obj, "z"));
        if (!ImGui.DragFloat3("##vec", ref f, 0.01f)) return false;
        EditorJson.Set(obj, "x", JsonValue.Create(f.X));
        EditorJson.Set(obj, "y", JsonValue.Create(f.Y));
        EditorJson.Set(obj, "z", JsonValue.Create(f.Z));
        return true;
    }

    private static bool IsIntegral(JsonObject obj, string key)
    {
        return EditorJson.Get(obj, key) is JsonValue value && EditorJson.IsIntegral(value);
    }
}
