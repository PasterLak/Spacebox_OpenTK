using Engine;
using Engine.Commands;
using Spacebox.Network.Moderation;

namespace Spacebox.Network.Dedicated;

public sealed class BanCommand : CommandBase, Engine.Commands.IArgumentSuggestions
{
    public void SuggestArguments(int argumentIndex, IReadOnlyList<string> previousArguments, List<string> results)
    {
        if (argumentIndex == 0) Spacebox.Game.Commands.CommandSuggestionSources.AddPlayerNames(results);
    }

    public override string Name => "ban";
    public override string Description => "Ban a player by name (also offline). Usage: ban <name>";

    public override void Execute(string[] args)
    {
        if (args.Length == 0) return;
        if (ServerModeration.BanName(args[0])) Debug.Log($"Banned {args[0]}");
        else Debug.Error($"{args[0]} cannot be banned");
    }
}

public sealed class UnbanCommand : CommandBase
{
    public override string Name => "unban";
    public override string Description => "Remove a name ban. Usage: unban <name>";

    public override void Execute(string[] args)
    {
        if (args.Length == 0) return;
        if (ServerModeration.Unban(args[0])) Debug.Log($"Unbanned {args[0]}");
        else Debug.Error($"{args[0]} is not banned");
    }
}

public sealed class MuteCommand : CommandBase, Engine.Commands.IArgumentSuggestions
{
    public void SuggestArguments(int argumentIndex, IReadOnlyList<string> previousArguments, List<string> results)
    {
        if (argumentIndex == 0) Spacebox.Game.Commands.CommandSuggestionSources.AddPlayerNames(results);
    }

    private readonly bool _muted;

    public MuteCommand(bool muted)
    {
        _muted = muted;
    }

    public override string Name => _muted ? "mute" : "unmute";
    public override string Description => _muted ? "Mute a player in chat. Usage: mute <name>" : "Unmute a player. Usage: unmute <name>";

    public override void Execute(string[] args)
    {
        if (args.Length == 0) return;
        ServerModeration.SetMuted(args[0], _muted);
    }
}

public sealed class UnregisterCommand : CommandBase, Engine.Commands.IArgumentSuggestions
{
    public void SuggestArguments(int argumentIndex, IReadOnlyList<string> previousArguments, List<string> results)
    {
        if (argumentIndex == 0) Spacebox.Game.Commands.CommandSuggestionSources.AddPlayerNames(results);
    }

    public override string Name => "unregister";
    public override string Description => "Delete a player account so the name can register again. Usage: unregister <name>";

    public override void Execute(string[] args)
    {
        if (args.Length == 0) return;
        if (NetworkSession.Current != null && NetworkSession.Current.RemoveAccount(args[0])) Debug.Log($"Account {args[0]} was removed");
        else Debug.Error($"{args[0]} has no account or registration is disabled");
    }
}
