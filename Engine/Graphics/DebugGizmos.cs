using Engine.GUI;
using Engine.Light;
using OpenTK.Mathematics;

namespace Engine;

public static class DebugGizmos
{
    public const float LightRangeDrawDistance = 60f;
    public const float CameraFrustumLength = 1.5f;

    private static readonly Color4 LightColor = new Color4(1f, 0.9f, 0.4f, 1f);
    private static readonly Color4 LightRangeColor = new Color4(1f, 0.9f, 0.4f, 0.35f);
    private static readonly Color4 CameraColor = new Color4(0.6f, 0.85f, 1f, 1f);
    private static readonly Color4 ParticlesColor = new Color4(0.4f, 1f, 1f, 1f);
    private static readonly List<Camera> _cameras = new List<Camera>();
    private static readonly Action<Node3D> _collect = Collect;

    public static void Draw(Node3D root)
    {
        if (!VisualDebug.Enabled) return;

        DrawLights();

        _cameras.Clear();
        root?.Traverse(_collect);

        for (int i = 0; i < _cameras.Count; i++) DrawCamera(_cameras[i]);

        var particles = ParticleSystem.All;
        for (int i = 0; i < particles.Count; i++) DrawParticles(particles[i]);
    }

    private static void Collect(Node3D node)
    {
        if (!node.Enabled) return;
        if (node is Camera camera && !camera.IsMain && camera.DrawsDebugGizmo) _cameras.Add(camera);
    }

    private static void DrawLights()
    {
        var main = Camera.Main;
        var lights = LightSystem.All;
        for (int i = 0; i < lights.Count; i++)
        {
            var light = lights[i];
            if (!light.Enabled || light is DirectionalLight) continue;

            var position = light.PositionWorld;
            bool spot = light is SpotLight;
            if (main != null && (position - main.Position).LengthSquared < 0.25f) continue;

            GizmoIcons.Add(position, spot ? GizmoIcon.SpotLight : GizmoIcon.PointLight, LightColor);
            if (main != null && (position - main.Position).LengthSquared > LightRangeDrawDistance * LightRangeDrawDistance) continue;

            if (light is SpotLight spotLight)
            {
                float angle = MathHelper.RadiansToDegrees(MathF.Acos(Math.Clamp(spotLight.OuterCutOff, -1f, 1f)));
                VisualDebug.DrawCone(position, spotLight.Direction, angle, MathF.Min(spotLight.Range, 12f), LightRangeColor, 12);
            }
            else if (light is PointLight point)
            {
                VisualDebug.DrawSphere(position, point.Range, 10, LightRangeColor);
            }
        }
    }

    private static void DrawCamera(Camera camera)
    {
        var position = camera.PositionWorld;
        GizmoIcons.Add(position, GizmoIcon.Camera, CameraColor);
        VisualDebug.DrawFrustum(position, camera.Front, camera.Up, camera.FOV, camera.AspectRatio, CameraFrustumLength, CameraColor);
    }

    private static void DrawParticles(ParticleSystem particles)
    {
        if (!particles.Enabled || !ParticleSystem.GloballyEnabled) return;

        if (!particles.EmitsByItself) return;

        var position = particles.PositionWorld;
        if (!GizmoIcons.IsInRange(position)) return;

        GizmoIcons.Add(position, GizmoIcon.Particles, ParticlesColor, particles.ParticlesCount.ToString());
        particles.Emitter.Debug();
    }
}
