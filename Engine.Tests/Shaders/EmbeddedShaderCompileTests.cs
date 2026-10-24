using Engine.CanvasUI;
using Engine.Graphics.Occlusion;

namespace Engine.Tests.Shaders;

public sealed class EmbeddedShaderCompileTests
{
    public static IEnumerable<object[]> EmbeddedShaders()
    {
        yield return new object[] { "error fallback" };
        yield return new object[] { "passthrough" };
        yield return new object[] { "canvas ui" };
        yield return new object[] { "occlusion boxes" };
        yield return new object[] { "g-buffer debug" };
    }

    [Theory]
    [MemberData(nameof(EmbeddedShaders))]
    public void EmbeddedShader_CompilesWithTheReferenceCompiler(string name)
    {
        var sources = name switch
        {
            "error fallback" => ShaderSources.FromText(ResourceFallbacks.ShaderCode),
            "passthrough" => ShaderSources.FromText(EngineWindow.PassthroughShaderCode),
            "canvas ui" => ShaderSources.FromLines(CanvasRenderer.ShaderCode),
            "occlusion boxes" => ShaderSources.FromLines(OcclusionBoxRenderer.Source),
            _ => ShaderSources.FromLines(Engine.PostProcessing.GBufferDebugPass.Source)
        };

        var errors = ShaderValidator.Check(name, sources);
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }
}
