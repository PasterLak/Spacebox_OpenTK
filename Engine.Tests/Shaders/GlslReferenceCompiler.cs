using Glslang.NET;
using GlslShader = Glslang.NET.Shader;

namespace Engine.Tests.Shaders;

public sealed record GlslSource(ShaderStage Stage, string Code);

public static class GlslReferenceCompiler
{
    public static List<string> Check(string name, IEnumerable<GlslSource> sources)
    {
        var errors = new List<string>();
        var shaders = new List<GlslShader>();
        try
        {
            foreach (var source in sources)
            {
                var shader = new GlslShader(Input(source));
                shaders.Add(shader);
                if (!shader.Preprocess() || !shader.Parse()) errors.Add($"{name} ({source.Stage}): {Log(shader.GetInfoLog(), shader.GetDebugLog())}");
            }

            if (errors.Count > 0) return errors;

            using var program = new Glslang.NET.Program();
            foreach (var shader in shaders) program.AddShader(shader);
            if (!program.Link(MessageType.Default)) errors.Add($"{name} (link): {Log(program.GetInfoLog(), program.GetDebugLog())}");
            return errors;
        }
        finally
        {
            foreach (var shader in shaders) shader.Dispose();
        }
    }

    private static string Log(string info, string debug)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(info)) parts.Add(info.Trim());
        if (!string.IsNullOrWhiteSpace(debug)) parts.Add(debug.Trim());
        return string.Join("\n", parts);
    }

    private static CompilationInput Input(GlslSource source)
    {
        return new CompilationInput
        {
            language = SourceType.GLSL,
            stage = source.Stage,
            client = ClientType.None,
            targetLanguage = TargetLanguage.None,
            code = source.Code,
            entrypoint = "main",
            sourceEntrypoint = "main",
            defaultVersion = 110,
            defaultProfile = ShaderProfile.None,
            forwardCompatible = true,
            messages = MessageType.Default,
            resourceLimits = ResourceLimits.DefaultResource
        };
    }
}
