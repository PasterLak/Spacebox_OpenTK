using Engine;

namespace Spacebox.Network.Admin;

public readonly record struct CommandOutputLine(string Text, Debug.MessageType Type);

public static class ChatCommands
{
    public const char Prefix = '/';
    public const int MaxOutputLines = 40;

    private static readonly List<CommandOutputLine> _output = new List<CommandOutputLine>();

    public static string CurrentSender { get; private set; }

    public static bool IsCommand(string text) => !string.IsNullOrEmpty(text) && text.Length > 1 && text[0] == Prefix && !char.IsWhiteSpace(text[1]);

    public static string ToCommandLine(string text) => IsCommand(text) ? text.Substring(1).Trim() : "";

    public static IReadOnlyList<CommandOutputLine> Run(string commandLine, string sender)
    {
        _output.Clear();
        if (string.IsNullOrWhiteSpace(commandLine)) return _output;

        CurrentSender = sender;
        Debug.MessageLogged += Capture;
        try
        {
            Debug.ExecuteCommand(commandLine);
        }
        finally
        {
            Debug.MessageLogged -= Capture;
            CurrentSender = null;
        }
        return _output;
    }

    public static bool IsEchoOfChat(string text) => text.StartsWith("[Chat] ", StringComparison.Ordinal);

    private static void Capture(string text, Debug.MessageType type)
    {
        if (_output.Count >= MaxOutputLines || IsEchoOfChat(text)) return;
        _output.Add(new CommandOutputLine(text, type));
    }
}
