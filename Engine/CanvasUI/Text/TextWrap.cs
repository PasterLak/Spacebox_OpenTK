namespace Engine.CanvasUI;

public static class TextWrap
{
    public static void Wrap(CanvasFont font, string text, float size, float maxWidth, List<string> lines)
    {
        lines.Clear();
        if (string.IsNullOrEmpty(text)) return;
        if (font == null || maxWidth <= 0f)
        {
            lines.AddRange(text.Split('\n'));
            return;
        }

        int paragraphStart = 0;
        while (paragraphStart <= text.Length)
        {
            int paragraphEnd = text.IndexOf('\n', paragraphStart);
            if (paragraphEnd < 0) paragraphEnd = text.Length;
            WrapParagraph(font, text, paragraphStart, paragraphEnd, size, maxWidth, lines);
            paragraphStart = paragraphEnd + 1;
        }
    }

    private static void WrapParagraph(CanvasFont font, string text, int start, int end, float size, float maxWidth, List<string> lines)
    {
        if (start >= end)
        {
            lines.Add("");
            return;
        }

        int lineStart = start;
        int lastBreak = -1;
        for (int i = start; i < end; i++)
        {
            if (text[i] == ' ') lastBreak = i;
            if (font.MeasureWidth(text, lineStart, i - lineStart + 1, size) <= maxWidth) continue;

            if (lastBreak > lineStart)
            {
                lines.Add(text.Substring(lineStart, lastBreak - lineStart));
                lineStart = lastBreak + 1;
            }
            else if (i > lineStart)
            {
                lines.Add(text.Substring(lineStart, i - lineStart));
                lineStart = i;
            }
            lastBreak = -1;
        }

        if (lineStart < end) lines.Add(text.Substring(lineStart, end - lineStart));
    }
}
