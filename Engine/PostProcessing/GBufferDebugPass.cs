using Engine.Graphics;
using Engine.Graphics.Debugging;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine.PostProcessing;

public sealed class GBufferDebugPass : IDisposable
{
    internal static readonly string[] Source =
    {
        "--Vert",
        "#version 330 core",
        "layout (location = 0) in vec2 aPos;",
        "layout (location = 1) in vec2 aTexCoords;",
        "out vec2 TexCoords;",
        "void main()",
        "{",
        "    TexCoords = aTexCoords;",
        "    gl_Position = vec4(aPos, 0.0, 1.0);",
        "}",
        "--Frag",
        "#version 330 core",
        "in vec2 TexCoords;",
        "out vec4 FragColor;",
        "uniform sampler2D uNormal;",
        "uniform sampler2D uDepth;",
        "uniform int uMode;",
        "uniform float uNear;",
        "uniform float uFar;",
        "void main()",
        "{",
        "    vec4 g = texture(uNormal, TexCoords);",
        "    float depth = texture(uDepth, TexCoords).r;",
        "    if (uMode == 0)",
        "    {",
        "        FragColor = g.a > 0.0 ? vec4(g.rgb, 1.0) : vec4(0.08, 0.08, 0.1, 1.0);",
        "        return;",
        "    }",
        "    if (uMode == 1)",
        "    {",
        "        if (depth >= 1.0) { FragColor = vec4(0.0, 0.0, 0.0, 1.0); return; }",
        "        float z = depth * 2.0 - 1.0;",
        "        float linear = 2.0 * uNear * uFar / (uFar + uNear - z * (uFar - uNear));",
        "        float t = clamp(log(linear / uNear) / log(uFar / uNear), 0.0, 1.0);",
        "        FragColor = vec4(vec3(1.0 - t), 1.0);",
        "        return;",
        "    }",
        "    vec3 mask = g.a > 0.75 ? vec3(0.2, 0.85, 0.3) : (g.a > 0.25 ? vec3(0.25, 0.45, 1.0) : vec3(0.0));",
        "    FragColor = vec4(mask * (0.55 + 0.45 * g.b), 1.0);",
        "}"
    };

    private readonly Shader _shader;

    public GBufferDebugPass()
    {
        _shader = new Shader(Source);
        Resources.RemoveResourceFromControll(_shader);
    }

    public void Draw(SceneRenderer scene, Vector2i clientSize, RenderDebugMode mode)
    {
        int shaderMode = mode switch
        {
            RenderDebugMode.Normals => 0,
            RenderDebugMode.Depth => 1,
            _ => 2
        };

        var camera = Camera.Main;
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, clientSize.X, clientSize.Y);
        GLState.DepthTest(false);
        GLState.Blend(false);

        _shader.Use();
        _shader.SetInt("uNormal", 0);
        _shader.SetInt("uDepth", 1);
        _shader.SetInt("uMode", shaderMode);
        _shader.SetFloat("uNear", camera?.DepthNear ?? 0.1f);
        _shader.SetFloat("uFar", camera?.DepthFar ?? 800f);

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, scene.NormalTexture);
        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2D, scene.DepthTexture);
        PostProcessManager.RenderQuad();
        GL.ActiveTexture(TextureUnit.Texture0);
    }

    public void Dispose()
    {
        _shader.Dispose();
    }
}
