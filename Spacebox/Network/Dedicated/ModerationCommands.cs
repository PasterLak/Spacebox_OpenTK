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
        if (args.Length == 0)
        {
            Debug.Error("Usage: ban <name>");
            return;
        }

        string name = string.Join(' ', args);
        if (ServerModeration.BanName(name)) Debug.Log($"Banned {name}");
        else Debug.Error($"{name} cannot be banned");
    }
}

public sealed class UnbanCommand : CommandBase
{
    public override string Name => "unban";
    public override string Description => "Remove a name ban. Usage: unban <name>";

    public override void Execute(string[] args)
    {
        if (args.Length == 0)
        {
            Debug.Error("Usage: unban <name>");
            return;
        }

        string name = string.Join(' ', args);
        if (ServerModeration.Unban(name)) Debug.Log($"Unbanned {name}");
        else Debug.Error($"{name} is not banned");
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
        if (args.Length == 0)
        {
            Debug.Error($"Usage: {Name} <name>");
            return;
        }

        string name = string.Join(' ', args);
        if (!ServerModeration.SetMuted(name, _muted)) Debug.Warning(_muted ? $"{name} is already muted" : $"{name} is not muted");
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
        if (args.Length == 0)
        {
            Debug.Error("Usage: unregister <name>");
            return;
        }

        string name = string.Join(' ', args);
        if (NetworkSession.Current != null && NetworkSession.Current.RemoveAccount(name)) Debug.Log($"Account {name} was removed");
        else Debug.Error($"{name} has no account or registration is disabled");
    }
}
