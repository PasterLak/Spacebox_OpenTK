using System.Globalization;

namespace Spacebox.Game.Signs;

public readonly struct SignTextSize : IEquatable<SignTextSize>
{
    public const float MinScale = 1f;
    public const float MaxScale = 16f;
    public const string FillTag = "fill";
    public const string SizeTagPrefix = "size=";

    public static readonly SignTextSize Default = new SignTextSize(MinScale, false);

    public readonly float Scale;
    public readonly bool Fill;

    public SignTextSize(float scale, bool fill)
    {
        Scale = float.IsFinite(scale) ? MathF.Round(Math.Clamp(scale, MinScale, MaxScale) * 10f) / 10f : MinScale;
        Fill = fill;
    }

    public bool IsDefault => Equals(Default);

    public float Resolve(float fitScale)
    {
        float fit = Math.Min(fitScale, MaxScale);
        return Fill ? fit : Math.Min(Scale, fit);
    }

    public string ToTag()
    {
        if (Fill) return "<" + FillTag + ">";
        return IsDefault ? "" : "<" + SizeTagPrefix + Scale.ToString("0.#", CultureInfo.InvariantCulture) + ">";
    }

    public static bool TryParseTag(string tag, ref SignTextSize size)
    {
        if (tag == FillTag)
        {
            size = new SignTextSize(size.Scale, true);
            return true;
        }

        if (!tag.StartsWith(SizeTagPrefix, StringComparison.Ordinal)) return false;
        if (!float.TryParse(tag.Substring(SizeTagPrefix.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out float scale) || !float.IsFinite(scale)) return false;
        size = new SignTextSize(scale, false);
        return true;
    }

    public bool Equals(SignTextSize other) => Scale == other.Scale && Fill == other.Fill;
    public override bool Equals(object obj) => obj is SignTextSize other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Scale, Fill);
}
