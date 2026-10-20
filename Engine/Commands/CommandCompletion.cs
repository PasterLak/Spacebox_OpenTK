namespace Engine.Commands;

public static class CommandCompletion
{
    public const int MaxSuggestions = 8;

    private static readonly List<string> _candidates = new List<string>();

    public static CommandBase Find(string name, IReadOnlyList<CommandBase> commands)
    {
        for (int i = 0; i < commands.Count; i++)
        {
            if (string.Equals(commands[i].Name, name, StringComparison.OrdinalIgnoreCase)) return commands[i];
        }
        return null;
    }

    public static void Suggest(string line, IReadOnlyList<CommandBase> commands, List<CommandSuggestion> results)
    {
        results.Clear();
        line ??= "";

        var tokens = Tokenize(line);
        bool endsWithSpace = line.Length > 0 && char.IsWhiteSpace(line[^1]);

        if (tokens.Length == 0 || (tokens.Length == 1 && !endsWithSpace))
        {
            SuggestCommands(tokens.Length == 0 ? "" : tokens[0], commands, results);
            return;
        }

        if (Find(tokens[0], commands) is not IArgumentSuggestions arguments) return;

        int argumentIndex = endsWithSpace ? tokens.Length - 1 : tokens.Length - 2;
        string current = endsWithSpace ? "" : tokens[^1];
        var previous = new ArraySegment<string>(tokens, 1, argumentIndex);

        _candidates.Clear();
        arguments.SuggestArguments(argumentIndex, previous, _candidates);
        _candidates.Sort(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < _candidates.Count && results.Count < MaxSuggestions; i++)
        {
            string candidate = _candidates[i];
            if (string.IsNullOrEmpty(candidate) || !candidate.StartsWith(current, StringComparison.OrdinalIgnoreCase)) continue;
            if (Contains(results, candidate)) continue;
            results.Add(new CommandSuggestion(candidate, ""));
        }
    }

    public static string Usage(string line, IReadOnlyList<CommandBase> commands)
    {
        var tokens = Tokenize(line ?? "");
        if (tokens.Length == 0) return "";
        bool complete = tokens.Length > 1 || char.IsWhiteSpace(line[^1]);
        return complete ? Find(tokens[0], commands)?.Description ?? "" : "";
    }

    public static string Apply(string line, string completion)
    {
        line ??= "";
        if (line.Length == 0 || char.IsWhiteSpace(line[^1])) return line + completion + " ";

        int start = line.LastIndexOf(' ') + 1;
        return line.Substring(0, start) + completion + " ";
    }

    private static void SuggestCommands(string prefix, IReadOnlyList<CommandBase> commands, List<CommandSuggestion> results)
    {
        _candidates.Clear();
        for (int i = 0; i < commands.Count; i++)
        {
            if (commands[i].Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) _candidates.Add(commands[i].Name);
        }
        _candidates.Sort(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < _candidates.Count && results.Count < MaxSuggestions; i++)
        {
            if (Contains(results, _candidates[i])) continue;
            results.Add(new CommandSuggestion(_candidates[i], Find(_candidates[i], commands)?.Description ?? ""));
        }
    }

    private static bool Contains(List<CommandSuggestion> results, string text)
    {
        for (int i = 0; i < results.Count; i++)
        {
            if (string.Equals(results[i].Text, text, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static string[] Tokenize(string line) => line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
}
