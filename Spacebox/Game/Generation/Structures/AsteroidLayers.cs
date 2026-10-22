namespace Spacebox.Game.Generation.Structures;

public static class AsteroidLayers
{
    public static float[] Thresholds(AsteroidLayer[] layers)
    {
        if (layers == null || layers.Length == 0) return null;

        int totalRatio = 0;
        foreach (var layer in layers)
            totalRatio += layer.Ratio;

        if (totalRatio == 0) return null;

        var thresholds = new float[layers.Length];
        float current = 0f;

        for (int i = 0; i < layers.Length; i++)
        {
            float portion = (float)layers[i].Ratio / totalRatio;
            current += portion;
            thresholds[i] = current;
        }

        return thresholds;
    }

    public static int IndexAt(float[] thresholds, float depth)
    {
        if (thresholds == null || thresholds.Length == 0) return -1;

        for (int i = 0; i < thresholds.Length; i++)
        {
            if (depth <= thresholds[i]) return i;
        }

        return thresholds.Length - 1;
    }

    public static float Depth(float distanceFromCenter, float radius)
    {
        if (radius <= 0f) return 0f;
        float depth = 1.0f - (distanceFromCenter / radius);
        return MathF.Max(0f, MathF.Min(1f, depth));
    }
}
