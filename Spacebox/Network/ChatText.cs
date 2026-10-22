namespace Spacebox.Network;

public static class ChatText
{
    public const int MaxLength = 256;

    public static string Clean(string text, int maxLength = MaxLength)
    {
        if (string.IsNullOrWhiteSpace(text) || maxLength <= 0) return "";

        var chars = new char[Math.Min(text.Length, maxLength)];
        int length = 0;
        for (int i = 0; i < text.Length && length < chars.Length; i++)
        {
            char c = text[i];
            chars[length++] = char.IsControl(c) ? ' ' : c;
        }

        return new string(chars, 0, length).Trim();
    }
}
