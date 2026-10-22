namespace Engine.Tests.Shaders;

public static class ShaderValidator
{
    public static List<string> Check(string name, IReadOnlyCollection<GlslSource> sources)
    {
        var problems = GlslPortabilityLint.Check(name, sources);
        problems.AddRange(GlslReferenceCompiler.Check(name, sources));
        return problems;
    }
}
