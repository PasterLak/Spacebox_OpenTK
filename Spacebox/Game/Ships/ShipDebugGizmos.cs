using Engine;
using Engine.GUI;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Ships;

public static class ShipDebugGizmos
{
    private const float ThrustArrowLength = 2f;
    private const float AimLineLength = 6f;
    private const float CameraFrustumLength = 1.5f;
    private const float CameraFieldOfView = 70f;

    private static readonly Color4 ThrusterColor = new Color4(1f, 0.55f, 0.15f, 1f);
    private static readonly Color4 CannonColor = new Color4(1f, 0.3f, 0.3f, 1f);
    private static readonly Color4 CameraColor = new Color4(0.6f, 0.85f, 1f, 1f);

    public static void Draw(ShipControlSystem control)
    {
        if (!VisualDebug.Enabled || control == null) return;

        var entity = control.Entity;

        var thrusters = control.Thrusters;
        for (int i = 0; i < thrusters.Count; i++)
        {
            var thruster = thrusters[i];
            var nozzle = entity.EntityLocalToWorld(thruster.ExhaustPointLocal);
            var exhaust = entity.DirectionToWorldSpace(thruster.Frame.Front);
            VisualDebug.DrawArrow(nozzle, nozzle + exhaust * ThrustArrowLength, ThrusterColor);
            GizmoIcons.Add(entity.EntityLocalToWorld(thruster.Frame.Center), GizmoIcon.Thruster, ThrusterColor);
        }

        var cannons = control.Cannons;
        for (int i = 0; i < cannons.Count; i++)
        {
            var cannon = cannons[i];
            var pivot = entity.EntityLocalToWorld(cannon.PivotLocal);
            var aim = entity.DirectionToWorldSpace(cannon.AimDirection);
            VisualDebug.DrawLine(pivot, pivot + aim * AimLineLength, CannonColor);
            GizmoIcons.Add(pivot, GizmoIcon.Cannon, CannonColor);
        }

        foreach (var part in control.Parts)
        {
            if (part is not ShipCameraBlock camera) continue;

            var eye = entity.EntityLocalToWorld(camera.ViewPointLocal);
            var front = entity.DirectionToWorldSpace(camera.Frame.Front);
            var up = entity.DirectionToWorldSpace(camera.Frame.Up);
            VisualDebug.DrawFrustum(eye, front, up, CameraFieldOfView, 16f / 9f, CameraFrustumLength, CameraColor);
            GizmoIcons.Add(eye, GizmoIcon.Camera, CameraColor);
        }
    }
}
