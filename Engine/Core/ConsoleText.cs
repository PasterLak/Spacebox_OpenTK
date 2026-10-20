using System.Text;

namespace Engine;

public static class ConsoleText
{
    public static string Build(IReadOnlyList<(string Text, Debug.MessageType Type)> messages, Debug.MessageType filter)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < messages.Count; i++)
        {
            var (text, type) = messages[i];
            if (filter != Debug.MessageType.Info && type != filter) continue;
            if (builder.Length > 0) builder.Append('\n');
            builder.Append(text);
        }
        return builder.ToString();
    }
}
