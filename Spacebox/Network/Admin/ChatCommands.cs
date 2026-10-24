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

    public static string RemoteSender => IsRemoteName(CurrentSender) ? CurrentSender : null;

    public static bool IsRemoteName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        return !GameSession.HasLocalPlayer || !string.Equals(name.Trim(), GameSession.PlayerName, StringComparison.OrdinalIgnoreCase);
    }

    public static bool RefusesRemoteSender(string commandName)
    {
        if (RemoteSender == null) return false;

        Debug.Error($"{commandName} works only for the host's own player");
        return true;
    }

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
            if (AcceptsSender(Engine.Commands.CommandManager.GetCommand(CommandName(commandLine)))) Debug.ExecuteCommand(commandLine);
            else Debug.Error($"{CommandName(commandLine)} works only for the host's own player");
        }
        finally
        {
            Debug.MessageLogged -= Capture;
            CurrentSender = null;
            _captureThread = 0;
        }
        return _output;
    }

    public static bool AcceptsSender(Engine.Commands.CommandBase command)
    {
        return command == null || command.AvailableInTerminal || command is IRemoteSenderCommand || RemoteSender == null;
    }

    public static bool IsEchoOfChat(string text) => text.StartsWith("[Chat] ", StringComparison.Ordinal);

    private static void Capture(string text, Debug.MessageType type)
    {
        if (Environment.CurrentManagedThreadId != _captureThread || _output.Count >= MaxOutputLines || IsEchoOfChat(text)) return;
        _output.Add(new CommandOutputLine(text, type));
    }
}
