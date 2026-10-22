using Engine.Tests.Shaders;

namespace Spacebox.Tests.Shaders;

public sealed class ShaderCompileTests
{
    public static IEnumerable<object[]> GameShaders()
    {
        string root = ShaderFiles.ShadersFolder();
        foreach (var file in Directory.EnumerateFiles(root, "*.glsl", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (ShaderFiles.IsInclude(relative)) continue;
            yield return new object[] { relative };
        }
    }

    [Theory]
    [MemberData(nameof(GameShaders))]
    public void GameShader_IsPortableAcrossGpuVendors(string relative)
    {
        var sources = ShaderSources.FromFile(ShaderFiles.PathWithoutExtension(relative));
        var problems = ShaderValidator.Check(relative, sources);
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void EveryShaderFileIsFound()
    {
        Assert.True(GameShaders().Count() > 30);
    }
}
