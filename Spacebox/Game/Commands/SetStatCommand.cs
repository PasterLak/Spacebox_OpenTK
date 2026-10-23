using Spacebox.Game.Player;
using Spacebox.Network.Players;

namespace Spacebox.Game.Commands;

public readonly record struct StatValue(PlayerStat Stat, int Value);

public sealed class SetStatCommand : PlayerTargetCommand<StatValue>
{
    private static readonly string[] StatNames = { "hp", "health", "power", "air", "oxygen" };

    public SetStatCommand(LocalAstronaut localPlayer) : base(localPlayer)
    {
    }

    public override string Name => "set";

    public override string Description => $"Set health, power or air to a value between 0 and the maximum. Usage: {Usage}";

    protected override string Usage => "set <hp|power|air> <value> [player]";

    public static bool TryParseStat(string name, out PlayerStat stat)
    {
        stat = name?.ToLowerInvariant() switch
        {
            "hp" or "health" => PlayerStat.Health,
            "power" => PlayerStat.Power,
            "air" or "oxygen" => PlayerStat.Air,
            _ => 0
        };
        return stat != 0;
    }

    protected override bool TryParseRequest(string[] args, out StatValue request, out string target)
    {
        request = default;
        target = null;
        if (args.Length < 2 || !TryParseStat(args[0], out var stat)) return false;
        if (!CommandArguments.TrySplitAmountAndTarget(args, 1, out int? amount, out target) || amount == null) return false;

        request = new StatValue(stat, Math.Max(0, amount.Value));
        return true;
    }

    protected override void ApplyLocal(LocalAstronaut player, StatValue request) => PlayerStatsAccess.Restore(player, request.Stat, request.Value);

    protected override void ApplyRemote(AstronautNetwork player, StatValue request) => player.RestoreStatsByServer(request.Stat, request.Value);

    protected override string Describe(StatValue request) => $"{PlayerStatsAccess.Describe(request.Stat)} set to {request.Value}";

    public override void SuggestArguments(int argumentIndex, IReadOnlyList<string> previousArguments, List<string> results)
    {
        if (argumentIndex == 0) results.AddRange(StatNames);
        else if (argumentIndex == 2) CommandSuggestionSources.AddPlayerNames(results);
    }
}
