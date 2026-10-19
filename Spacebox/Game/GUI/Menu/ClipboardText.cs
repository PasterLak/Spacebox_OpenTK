namespace Spacebox.Game.GUI.Menu;

public static class ClipboardText
{
    public static string Clean(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || maxLength <= 0) return "";

        var chars = new char[Math.Min(text.Length, maxLength)];
        int length = 0;
        foreach (char c in text)
        {
            if (c == '\r' || c == '\n') break;
            if (char.IsControl(c)) continue;
            chars[length++] = c;
            if (length == chars.Length) break;
        }

        return new string(chars, 0, length).Trim();
    }
}
