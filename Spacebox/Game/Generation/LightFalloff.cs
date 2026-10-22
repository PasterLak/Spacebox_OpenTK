using Engine;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Generation;

public static class LightFalloff
{
    public const int MinChannel = 3;
    public const float SourceLevel = 15f;
    public const float MaxPropagatedLevel = 14.9f;

    public static bool TryDecay(Color3Byte source, out int r, out int g, out int b)
    {
        r = source.R * 8 / 10;
        g = source.G * 8 / 10;
        b = source.B * 8 / 10;
        return Math.Max(r, Math.Max(g, b)) >= MinChannel;
    }

    public static bool TrySpreadInto(Block target, int r, int g, int b)
    {
        if (!(target.IsAir || target.IsTransparent || target.HasCustomShape)) return false;
        if (target.LightLevel >= SourceLevel) return false;

        var current = target.LightColor;
        int nr = Math.Max(current.R, r);
        int ng = Math.Max(current.G, g);
        int nb = Math.Max(current.B, b);
        if (nr == current.R && ng == current.G && nb == current.B) return false;

        target.LightColor = new Color3Byte((byte)nr, (byte)ng, (byte)nb);
        target.LightLevel = MathF.Min(MaxPropagatedLevel, Math.Max(nr, Math.Max(ng, nb)) / 255f * 15f);
        return true;
    }
}
