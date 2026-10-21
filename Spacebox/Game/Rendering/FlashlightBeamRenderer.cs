using Engine;
using Engine.Graphics;
using Engine.Networking;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Spacebox.Game.Player;

namespace Spacebox.Game.Rendering;

public sealed class FlashlightBeamRenderer
{
    private readonly Shader _shader;
    private readonly Mesh _mesh;

    public FlashlightBeamRenderer()
    {
        _shader = Resources.Load<Shader>("Resources/Shaders/flashlightBeam");
        _mesh = FlashlightBeamMesh.Create();
    }

    public void Render()
    {
        if (!Settings.Graphics.FlashlightBeams) return;

        var camera = Camera.Main;
        var network = NetworkManager.Instance;
        if (camera == null || network == null) return;

        bool begun = false;
        foreach (var networkObject in network.Spawner.Objects)
        {
            if (networkObject.Owner is not RemoteAstronaut remote) continue;

            var flashlight = remote.Flashlight;
            if (flashlight == null || !flashlight.Enabled || !remote.Enabled) continue;

            var direction = remote.Front;
            var start = remote.Position + direction * FlashlightBeamShape.StartOffset;
            float length = FlashlightBeamShape.Length(flashlight.Range);
            if (length <= 0f) continue;
            float radius = FlashlightBeamShape.Radius(length, flashlight.OuterCutOff);
            if (!FlashlightBeamShape.IsVisible(start, direction, length, radius, camera.Position, camera.Front)) continue;
            float fade = FlashlightBeamShape.ViewFade(start, camera.Position);
            if (fade <= 0f) continue;
            float inside = FlashlightBeamShape.Inside(start, direction, length, radius, camera.Position);

            if (!begun)
            {
                Begin(camera);
                begun = true;
            }

            DrawBeam(start, direction, remote.Right, remote.Up, length, radius, flashlight.Diffuse, flashlight.Intensity * fade, inside);
        }

        if (begun) End();
    }

    private void Begin(Camera camera)
    {
        _shader.Use();
        _shader.SetMatrix4("view", camera.GetViewMatrix());
        _shader.SetMatrix4("projection", camera.GetProjectionMatrix());
        _shader.SetVector3("uCameraPos", RenderSpace.ToRender(camera.Position));
        _shader.SetVector3("uNoiseOffset", FlashlightBeamShape.WrapNoiseOffset(RenderSpace.Origin));
        _shader.SetFloat("uTime", Time.Total);

        GLState.DepthTest(true);
        GLState.DepthMask(false);
        GLState.CullFace(false);
        GLState.GBufferWrite(false);
        GLState.Blend(true);
        GLState.BlendFunc(BlendingFactor.One, BlendingFactor.One);
    }

    private void DrawBeam(Vector3 start, Vector3 direction, Vector3 right, Vector3 up, float length, float radius, Vector3 color, float intensity, float inside)
    {
        _shader.SetMatrix4("model", FlashlightBeamShape.Model(RenderSpace.ToRender(start), direction, right, up, radius, length));
        _shader.SetVector3("uRight", right);
        _shader.SetVector3("uUp", up);
        _shader.SetVector3("uDir", direction);
        _shader.SetFloat("uSlope", radius / length);
        _shader.SetVector3("uColor", color);
        _shader.SetFloat("uIntensity", intensity);
        _shader.SetFloat("uInside", inside);
        _mesh.Render();
    }

    private static void End()
    {
        GLState.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GLState.Blend(false);
        GLState.DepthMask(true);
    }
}
