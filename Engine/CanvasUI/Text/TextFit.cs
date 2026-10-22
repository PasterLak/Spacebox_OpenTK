namespace Engine.CanvasUI;

public enum TextOverrun
{
    None,
    Clip,
    Ellipsis
}

public static class TextFit
{
    public const string Ellipsis = "…";

    public static int FitLength(CanvasFont font, string text, float size, float maxWidth)
    {
        if (string.IsNullOrEmpty(text) || font == null) return 0;
        int low = 0, high = text.Length;
        while (low < high)
        {
            int mid = (low + high + 1) / 2;
            if (font.MeasureWidth(text, 0, mid, size) <= maxWidth) low = mid;
            else high = mid - 1;
        }
        return low;
    }

    public static string Ellipsize(CanvasFont font, string text, float size, float maxWidth)
    {
        if (string.IsNullOrEmpty(text) || font == null) return text ?? "";
        if (font.MeasureWidth(text, size) <= maxWidth) return text;

        float ellipsisWidth = font.MeasureWidth(Ellipsis, size);
        if (ellipsisWidth > maxWidth) return "";
        int length = FitLength(font, text, size, maxWidth - ellipsisWidth);
        while (length > 0 && text[length - 1] == ' ') length--;
        return text.Substring(0, length) + Ellipsis;
    }
}
