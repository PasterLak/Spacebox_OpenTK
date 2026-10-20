using Engine.Commands;
using Spacebox.Network;
using Spacebox.Network.Admin;
using Spacebox.Network.Dedicated;

namespace Spacebox.Game.GUI;

public static class ChatCommandInput
{
    private static readonly List<CommandBase> _commands = new List<CommandBase>();
    private static readonly List<CommandSuggestion> _suggestions = new List<CommandSuggestion>();
    private static readonly ChatHistory _history = new ChatHistory();
    private static string _lastText;

    public static IReadOnlyList<CommandSuggestion> Suggestions => _suggestions;
    public static int Selected { get; private set; }
    public static string Usage { get; private set; } = "";

    public static void Update(string text)
    {
        if (text == _lastText) return;
        _lastText = text;
        Selected = 0;
        _suggestions.Clear();
        Usage = "";

        if (!ChatCommands.IsCommandStart(text)) return;

        string line = text.Substring(1);
        CollectCommands();
        CommandCompletion.Suggest(line, _commands, _suggestions);
        Usage = CommandCompletion.Usage(line, _commands);

        if (_suggestions.Count == 1 && string.Equals(ChatCommands.LastWord(line), _suggestions[0].Text, StringComparison.OrdinalIgnoreCase)) _suggestions.Clear();
    }

    public static string Complete(string text)
    {
        Update(text);
        if (_suggestions.Count == 0) return null;

        return ChatCommands.Prefix + CommandCompletion.Apply(text.Substring(1), _suggestions[Selected].Text);
    }

    public static string Navigate(string text, bool up)
    {
        Update(text);
        if (_suggestions.Count > 0)
        {
            int count = _suggestions.Count;
            Selected = (Selected + (up ? count - 1 : 1)) % count;
            return null;
        }

        return up ? _history.Older(text) : _history.Newer(text);
    }

    public static void Sent(string text)
    {
        _history.Add(text);
        Reset();
    }

    public static void Reset()
    {
        _lastText = null;
        _suggestions.Clear();
        Usage = "";
        Selected = 0;
        _history.ResetCursor();
    }

    private static void CollectCommands()
    {
        _commands.Clear();
        foreach (var command in CommandManager.GetCommands())
        {
            if (CommandManager.IsAvailable(command)) _commands.Add(command);
        }

        if (!GameSession.IsRemoteClient) return;

        var server = ServerCommandCatalog.Reference;
        for (int i = 0; i < server.Count; i++)
        {
            if (CommandCompletion.Find(server[i].Name, _commands) == null) _commands.Add(server[i]);
        }
    }
}
