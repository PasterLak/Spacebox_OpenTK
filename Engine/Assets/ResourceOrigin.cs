using System.Diagnostics;

namespace Engine;

public static class ResourceOrigin
{
    private const int MaxFrames = 3;

    public static string Describe(StackTrace trace)
    {
        var frames = trace.GetFrames();
        var parts = new List<string>(MaxFrames);
        for (int i = 0; i < frames.Length && parts.Count < MaxFrames; i++)
        {
            var method = frames[i].GetMethod();
            var type = method?.DeclaringType;
            if (type == null || IsEngineInfrastructure(type)) continue;
            parts.Add(type.Name + "." + method.Name);
        }
        return parts.Count == 0 ? "unknown" : string.Join(" < ", parts);
    }

    public static bool IsEngineInfrastructure(Type type)
    {
        string name = type.FullName ?? type.Name;
        return name.StartsWith("Engine.Resources", StringComparison.Ordinal)
            || name.StartsWith("Engine.MeshBuffer", StringComparison.Ordinal)
            || name.StartsWith("Engine.Mesh", StringComparison.Ordinal)
            || name.StartsWith("Engine.Texture2D", StringComparison.Ordinal)
            || name.StartsWith("Engine.Materials", StringComparison.Ordinal)
            || name.StartsWith("System.", StringComparison.Ordinal);
    }
}
