using Glslang.NET;

namespace Engine.Tests.Shaders;

public sealed class ShaderPortabilityCanaryTests
{
    private const string Vertex = """
        #version 330 core
        layout (location = 0) in vec3 aPos;
        out vec2 vUv;
        void main()
        {
            vUv = aPos.xy;
            gl_Position = vec4(aPos, 1.0);
        }
        """;

    private static string Fragment(string declarations, string body) => $$"""
        #version 330 core
        in vec2 vUv;
        out vec4 FragColor;
        uniform sampler2D tex;
        uniform float uValue;
        {{declarations}}
        void main()
        {
            {{body}}
        }
        """;

    private static List<string> Check(string fragment, string vertex = Vertex)
    {
        return ShaderValidator.Check("canary", new[] { new GlslSource(ShaderStage.Vertex, vertex), new GlslSource(ShaderStage.Fragment, fragment) });
    }

    [Fact]
    public void ValidShader_Passes()
    {
        var errors = Check(Fragment("precision highp float;\nfloat shade(float v) { return v * 0.5; }", "float x = 1; FragColor = texture(tex, vUv) * shade(uValue) * x;"));
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    [Theory]
    [InlineData("function named like a built-in (noise3)", "float noise3(vec3 p) { return p.x; }", "FragColor = vec4(noise3(vec3(vUv, 0.0)));")]
    [InlineData("texture2D in the core profile", "", "FragColor = texture2D(tex, vUv);")]
    [InlineData("gl_FragColor in the core profile", "", "gl_FragColor = vec4(1.0);")]
    [InlineData("swizzle on a scalar (GLSL 4.20)", "", "float a = uValue; FragColor = vec4(a.xxx, 1.0);")]
    [InlineData("HLSL type float3", "", "float3 c = float3(1.0); FragColor = vec4(c, 1.0);")]
    [InlineData("HLSL function saturate", "", "FragColor = vec4(saturate(uValue));")]
    [InlineData("HLSL function lerp", "", "FragColor = vec4(lerp(0.0, 1.0, uValue));")]
    [InlineData("scalar assigned to a vector", "", "vec3 c = 1.0; FragColor = vec4(c, 1.0);")]
    [InlineData("% on floats", "", "FragColor = vec4(uValue % 2.0);")]
    [InlineData("const initialized from a uniform", "", "const float k = uValue; FragColor = vec4(k);")]
    [InlineData("initializer list (GLSL 4.20)", "", "float w[2] = { 1.0, 2.0 }; FragColor = vec4(w[0]);")]
    [InlineData("int assigned to uint (GLSL 4.00)", "", "uint u = 1; FragColor = vec4(float(u));")]
    [InlineData("sampler binding layout (GLSL 4.20)", "layout (binding = 0) uniform sampler2D other;", "FragColor = texture(other, vUv);")]
    [InlineData("uniform location layout (GLSL 4.30)", "layout (location = 3) uniform float placed;", "FragColor = vec4(placed);")]
    [InlineData("function called before it is declared", "", "FragColor = vec4(later());")]
    [InlineData("float literal with a double suffix", "", "FragColor = vec4(1.0lf);")]
    public void TypicalDriverPortabilityError_IsRejected(string mistake, string declarations, string body)
    {
        var errors = Check(Fragment(declarations, body));
        Assert.True(errors.Count > 0, $"The reference compiler accepted: {mistake}");
    }

    [Fact]
    public void MissingVersionLine_IsRejected()
    {
        var errors = Check(Fragment("", "FragColor = vec4(1.0);").Replace("#version 330 core", ""));
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void VaryingTypeMismatchBetweenStages_IsRejected()
    {
        var errors = Check(Fragment("", "FragColor = vec4(1.0);").Replace("in vec2 vUv;", "in vec3 vUv;"));
        Assert.NotEmpty(errors);
    }
}
