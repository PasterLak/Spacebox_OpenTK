using Glslang.NET;

namespace Engine.Tests.Shaders;

public static class ShaderSources
{
    public static List<GlslSource> FromLines(string[] lines)
    {
        return Split(ShaderParser.ParseShadersFromLines(lines));
    }

    public static List<GlslSource> FromText(string text)
    {
        return FromLines(text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None));
    }

    public static List<GlslSource> FromFile(string pathWithoutExtension)
    {
        return Split(ShaderParser.ParseShadersFromPath(pathWithoutExtension));
    }

    private static List<GlslSource> Split((string vertex, string fragment, string geometry) sources)
    {
        var result = new List<GlslSource>
        {
            new GlslSource(ShaderStage.Vertex, sources.vertex),
            new GlslSource(ShaderStage.Fragment, sources.fragment)
        };
        if (!string.IsNullOrWhiteSpace(sources.geometry)) result.Add(new GlslSource(ShaderStage.Geometry, sources.geometry));
        return result;
    }
}
