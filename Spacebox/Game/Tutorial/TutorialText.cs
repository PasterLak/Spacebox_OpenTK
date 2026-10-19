using System.Globalization;
using System.Text;

namespace Spacebox.Game.Tutorial;

public interface ITutorialTextResolver
{
    string KeyName(string action);
    string ItemName(string itemKey);
}

public static class TutorialText
{
    public static string Format(string text, ITutorialTextResolver resolver, float target = 0f)
    {
        if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text ?? "";

        var builder = new StringBuilder(text.Length + 16);
        int i = 0;
        while (i < text.Length)
        {
            int open = text.IndexOf('{', i);
            if (open < 0)
            {
                builder.Append(text, i, text.Length - i);
                break;
            }

            int close = text.IndexOf('}', open + 1);
            if (close < 0)
            {
                builder.Append(text, i, text.Length - i);
                break;
            }

            builder.Append(text, i, open - i);
            builder.Append(Resolve(text.Substring(open + 1, close - open - 1), resolver, target) ?? text.Substring(open, close - open + 1));
            i = close + 1;
        }

        return builder.ToString();
    }

    private static string Resolve(string token, ITutorialTextResolver resolver, float target)
    {
        if (token == "target") return FormatNumber(target);

        int colon = token.IndexOf(':');
        if (colon <= 0 || resolver == null) return null;

        string kind = token.Substring(0, colon);
        string value = token.Substring(colon + 1);
        return kind switch
        {
            "key" => resolver.KeyName(value),
            "item" or "block" => resolver.ItemName(value),
            _ => null
        };
    }

    public static string FormatNumber(float value)
    {
        return MathF.Abs(value - MathF.Round(value)) < 0.01f
            ? ((int)MathF.Round(value)).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
