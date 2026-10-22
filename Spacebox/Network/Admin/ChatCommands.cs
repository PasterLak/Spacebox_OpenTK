using Engine;

namespace Spacebox.Network.Admin;

public readonly record struct CommandOutputLine(string Text, Debug.MessageType Type);

public static class ChatCommands
{
    public const char Prefix = '/';
    public const int MaxOutputLines = 40;

    private static readonly List<CommandOutputLine> _output = new List<CommandOutputLine>();
    private static int _captureThread;

    public static string CurrentSender { get; private set; }

    public static bool IsCommand(string text) => !string.IsNullOrEmpty(text) && text.Length > 1 && text[0] == Prefix && !char.IsWhiteSpace(text[1]);

    public static bool IsCommandStart(string text) => !string.IsNullOrEmpty(text) && text[0] == Prefix && (text.Length == 1 || !char.IsWhiteSpace(text[1]));

    public static string LastWord(string line)
    {
        if (string.IsNullOrEmpty(line) || char.IsWhiteSpace(line[^1])) return "";
        int space = line.LastIndexOf(' ');
        return line.Substring(space + 1);
    }

    public static string ToCommandLine(string text) => IsCommand(text) ? text.Substring(1).Trim() : "";

    public static string CommandName(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine)) return "";
        string trimmed = commandLine.Trim();
        int space = trimmed.IndexOf(' ');
        return (space < 0 ? trimmed : trimmed.Substring(0, space)).ToLowerInvariant();
    }

    public static bool IsKnownLocally(string commandLine) => Engine.Commands.CommandManager.GetCommand(CommandName(commandLine)) != null;

    public static IReadOnlyList<CommandOutputLine> Run(string commandLine, string sender)
    {
        _output.Clear();
        if (string.IsNullOrWhiteSpace(commandLine)) return _output;

        CurrentSender = sender;
        _captureThread = Environment.CurrentManagedThreadId;
        Debug.MessageLogged += Capture;
        try
        {
            Debug.ExecuteCommand(commandLine);
        }
        finally
        {
            Debug.MessageLogged -= Capture;
            CurrentSender = null;
            _captureThread = 0;
        }
        return _output;
    }

    public static bool IsEchoOfChat(string text) => text.StartsWith("[Chat] ", StringComparison.Ordinal);

    private static void Capture(string text, Debug.MessageType type)
    {
        if (Environment.CurrentManagedThreadId != _captureThread || _output.Count >= MaxOutputLines || IsEchoOfChat(text)) return;
        _output.Add(new CommandOutputLine(text, type));
    }
}
