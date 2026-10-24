using Engine;
using Engine.Components;
using Engine.Graphics;
using OpenTK.Mathematics;

namespace Spacebox.Game.Rendering;

public sealed class BodyOutlineRenderer
{
    private const float Thickness = 0.03f;

    private static Node3D _target;
    private static Color4 _color;

    private readonly Shader _shader;

    public BodyOutlineRenderer()
    {
        _shader = Resources.Load<Shader>("Resources/Shaders/bodyOutline");
    }

    public static void Request(Node3D target, Color4 color)
    {
        _target = target;
        _color = color;
    }

    public static void Reset()
    {
        _target = null;
    }

    public void Render()
    {
        var target = _target;
        _target = null;
        var camera = Camera.Main;
        if (target == null || camera == null || !target.Enabled) return;

        _shader.Use();
        _shader.SetMatrix4("view", camera.GetViewMatrix());
        _shader.SetMatrix4("projection", camera.GetProjectionMatrix());
        _shader.SetFloat("uThickness", Thickness);
        _shader.SetVector4("uColor", _color);

        GLState.DepthTest(true);
        GLState.DepthMask(true);
        GLState.CullFace(true);
        GLState.CullMode(Engine.Graphics.Rhi.CullFace.Front);

        var components = target.Components;
        for (int i = 0; i < components.Count; i++)
        {
            if (components[i] is not ModelRendererComponent renderer || !renderer.Enabled || renderer.Model?.Mesh == null) continue;
            var matrix = target.GetRenderModelMatrix();
            if (renderer.Offset != Vector3.Zero) matrix *= Matrix4.CreateTranslation(renderer.Offset);
            _shader.SetMatrix4("model", matrix);
            _shader.SetMatrix3("normalMatrix", NormalMatrix.From(matrix));
            renderer.Model.Mesh.Render();
        }

        GLState.CullMode(Engine.Graphics.Rhi.CullFace.Back);
    }
}
