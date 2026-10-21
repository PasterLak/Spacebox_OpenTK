using System.Globalization;

namespace Spacebox.Game.Commands.Teleport;

public readonly record struct TeleportCoordinate(float Value, bool Relative)
{
    public const char RelativePrefix = '~';

    public float Resolve(float origin) => Relative ? origin + Value : Value;

    public static bool TryParse(string text, out TeleportCoordinate coordinate)
    {
        coordinate = default;
        if (string.IsNullOrEmpty(text)) return false;

        bool relative = text[0] == RelativePrefix;
        string number = relative ? text.Substring(1) : text;
        if (relative && number.Length == 0)
        {
            coordinate = new TeleportCoordinate(0f, true);
            return true;
        }

        if (!float.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) || !float.IsFinite(value)) return false;
        coordinate = new TeleportCoordinate(value, relative);
        return true;
    }
}
