using System.Text.RegularExpressions;

namespace Engine.Tests.Shaders;

public static class GlslPortabilityLint
{
    private static readonly (string Name, string Replacement)[] RemovedInCoreProfile =
    {
        ("texture1D", "texture"),
        ("texture2D", "texture"),
        ("texture3D", "texture"),
        ("textureCube", "texture"),
        ("texture2DLod", "textureLod"),
        ("textureCubeLod", "textureLod"),
        ("texture2DProj", "textureProj"),
        ("shadow2D", "texture"),
        ("gl_FragColor", "an out variable"),
        ("gl_FragData", "out variables"),
        ("attribute", "in"),
        ("varying", "in / out"),
        ("gl_ModelViewMatrix", "a uniform"),
        ("gl_ProjectionMatrix", "a uniform"),
        ("gl_ModelViewProjectionMatrix", "a uniform"),
        ("gl_NormalMatrix", "a uniform"),
        ("gl_Vertex", "an in attribute"),
        ("gl_Normal", "an in attribute"),
        ("gl_MultiTexCoord0", "an in attribute"),
        ("gl_TexCoord", "an out variable")
    };

    public static List<string> Check(string name, IEnumerable<GlslSource> sources)
    {
        var problems = new List<string>();
        foreach (var source in sources)
        {
            foreach (var (removed, replacement) in RemovedInCoreProfile)
            {
                if (Regex.IsMatch(source.Code, $@"\b{removed}\b"))
                    problems.Add($"{name} ({source.Stage}): '{removed}' does not exist in the forward-compatible core profile (AMD, Intel, macOS reject it), use {replacement}");
            }
        }
        return problems;
    }
}
