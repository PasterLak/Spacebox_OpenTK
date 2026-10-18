using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Physics.Gravity;

public static class GravityField
{
    public const float FadeDistance = 6f;

    private static readonly List<IGravitySource> _sources = new List<IGravitySource>();
    private static readonly object _lock = new object();

    public static bool HasSources => _sources.Count > 0;

    public static void Register(IGravitySource source)
    {
        lock (_lock)
        {
            if (!_sources.Contains(source)) _sources.Add(source);
        }
    }

    public static void Unregister(IGravitySource source)
    {
        lock (_lock) _sources.Remove(source);
    }

    public static void UnregisterEntity(SpaceEntity entity)
    {
        lock (_lock) _sources.RemoveAll(source => source.Entity == entity);
    }

    public static void Clear()
    {
        lock (_lock) _sources.Clear();
    }

    public static Vector3 Sample(Vector3 worldPosition)
    {
        if (_sources.Count == 0) return Vector3.Zero;

        var direction = Vector3.Zero;
        float strength = 0f;

        lock (_lock)
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                var source = _sources[i];
                if (!TryGetInfluence(source, worldPosition, out float distance, out float fade)) continue;

                float closeness = fade * (1f - distance / source.Radius) + 0.001f;
                direction += source.WorldDirection * closeness;
                strength = MathF.Max(strength, source.Strength * fade);
            }
        }

        float length = direction.Length;
        return length > 1e-4f ? direction * (strength / length) : Vector3.Zero;
    }

    public static IGravitySource? GetDominantSource(Vector3 worldPosition)
    {
        if (_sources.Count == 0) return null;

        IGravitySource? dominant = null;
        float strongest = 0f;

        lock (_lock)
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                var source = _sources[i];
                if (!TryGetInfluence(source, worldPosition, out _, out float fade)) continue;

                float strength = source.Strength * fade;
                if (strength <= strongest) continue;

                strongest = strength;
                dominant = source;
            }
        }

        return dominant;
    }

    private static bool TryGetInfluence(IGravitySource source, Vector3 worldPosition, out float distance, out float fade)
    {
        distance = 0f;
        fade = 0f;
        if (!source.IsEmitting) return false;

        float radius = source.Radius;
        float distanceSquared = Vector3.DistanceSquared(worldPosition, source.WorldCenter);
        if (distanceSquared >= radius * radius) return false;

        distance = MathF.Sqrt(distanceSquared);
        fade = 1f - SmoothStep(radius - MathF.Min(FadeDistance, radius * 0.25f), radius, distance);
        return fade > 0f && source.Affects(worldPosition);
    }

    private static float SmoothStep(float edge0, float edge1, float value)
    {
        if (edge1 <= edge0) return value >= edge1 ? 1f : 0f;
        float t = Math.Clamp((value - edge0) / (edge1 - edge0), 0f, 1f);
        return t * t * (3f - 2f * t);
    }
}
