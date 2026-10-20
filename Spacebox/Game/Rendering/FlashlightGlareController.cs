using Engine;
using Engine.Networking;
using Engine.Physics;
using Engine.PostProcessing.Glare;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;

namespace Spacebox.Game.Rendering;

public static class FlashlightGlareController
{
    private static readonly Ray _occlusionRay = new Ray(Vector3.Zero, Vector3.UnitZ, 1f);

    private static float _settingStrength = 1f;
    private static float _current;
    private static Vector2 _lightPosition;
    private static Vector3 _color = Vector3.One;

    public static float Current => _current;

    public static void SetSettingStrength(float strength)
    {
        _settingStrength = Math.Clamp(strength, 0f, 1f);
    }

    public static void Update(LocalAstronaut player)
    {
        if (_settingStrength <= 0f && _current <= 0f) return;

        float target = CanSeeGlare(player) ? FindStrongest(player) : 0f;
        _current = FlashlightGlare.Smooth(_current, target * _settingStrength, Time.Delta);
        if (_current < 0.001f) _current = 0f;
        Push();
    }

    public static void Reset()
    {
        _current = 0f;
        Push();
    }

    private static bool CanSeeGlare(LocalAstronaut player)
    {
        return player != null && player.IsAlive && player.IsMain && _settingStrength > 0f && !WorldLoadingScreen.IsActive;
    }

    private static float FindStrongest(LocalAstronaut player)
    {
        var network = NetworkManager.Instance;
        if (network == null) return 0f;

        float best = 0f;
        foreach (var networkObject in network.Spawner.Objects)
        {
            if (networkObject.Owner is not RemoteAstronaut remote) continue;

            var flashlight = remote.Flashlight;
            if (flashlight == null || !flashlight.Enabled) continue;

            float strength = FlashlightGlare.Strength(remote.Position, remote.Front, flashlight.CutOff, flashlight.OuterCutOff,
                flashlight.Range, flashlight.Intensity, player.Position, player.Front);
            if (strength <= best || IsOccluded(remote.Position, player.Position)) continue;
            if (!FlashlightGlare.TryGetScreenPosition(remote.Position, player.Position, player.Front, player.Right, player.Up, player.FOV, player.AspectRatio, out var ndc)) continue;

            best = strength;
            _lightPosition = ndc;
            _color = flashlight.Diffuse;
        }

        return best;
    }

    private static bool IsOccluded(Vector3 from, Vector3 to)
    {
        var sector = World.CurrentSector;
        if (sector == null) return false;

        var delta = to - from;
        float distance = delta.Length;
        if (distance < 0.001f) return false;

        _occlusionRay.Origin = from;
        _occlusionRay.Direction = delta / distance;
        _occlusionRay.Length = distance;
        return sector.Raycast(_occlusionRay, out var hit) && hit.block != null;
    }

    private static void Push()
    {
        var effect = GetEffect();
        if (effect == null) return;

        effect.Intensity = _current;
        effect.LightPosition = _lightPosition;
        effect.Color = _color;
    }

    private static LensGlareEffect GetEffect()
    {
        return (EngineWindow.Instance as SpaceboxWindow)?.LensGlare;
    }
}
