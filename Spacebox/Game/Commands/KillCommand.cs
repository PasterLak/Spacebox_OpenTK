using Spacebox.Game.Player;
using Spacebox.Network.Players;

namespace Spacebox.Game.Commands;

public sealed class KillCommand : PlayerTargetCommand<bool>
{
    public KillCommand(LocalAstronaut localPlayer) : base(localPlayer)
    {
    }

    public override string Name => "kill";

    public override string Description => $"Kill a player. Usage: {Usage}";

    protected override string Usage => "kill [player]";

    protected override bool TryParseRequest(string[] args, out bool request, out string target)
    {
        request = true;
        target = args.Length > 0 ? string.Join(" ", args) : null;
        return args.Length <= 1 || target.Length > 0;
    }

    protected override void ApplyLocal(LocalAstronaut player, bool request) => player.Kill(new DeathCase(DeathMessageFormatter.CommandReason));

    protected override void ApplyRemote(AstronautNetwork player, bool request) => player.KillByServer();

    protected override string Describe(bool request) => "killed by the command";

    protected override string Report(string playerName, bool request) => DeathMessageFormatter.Killed(playerName);

    public override void SuggestArguments(int argumentIndex, IReadOnlyList<string> previousArguments, List<string> results)
    {
        if (argumentIndex == 0) CommandSuggestionSources.AddPlayerNames(results);
    }
}
