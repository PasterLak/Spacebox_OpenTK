using Spacebox.Game.Player;
using Spacebox.Network.Players;

namespace Spacebox.Game.Commands;

public sealed class StatCommand : PlayerTargetCommand<int>
{
    private readonly string _name;
    private readonly PlayerStat _stats;

    public StatCommand(LocalAstronaut localPlayer, string name, PlayerStat stats) : base(localPlayer)
    {
        _name = name;
        _stats = stats;
    }

    public override string Name => _name;

    public override string Description => _stats == PlayerStat.All
        ? $"Restore health, power and air. Usage: {Usage}"
        : $"Restore {PlayerStatsAccess.Describe(_stats)} to full or set a value. Usage: {Usage}";

    protected override string Usage => _stats == PlayerStat.All ? $"{_name} [player]" : $"{_name} [value] [player]";

    protected override bool TryParseRequest(string[] args, out int request, out string target)
    {
        request = PlayerStatsAccess.Full;
        if (!CommandArguments.TrySplitAmountAndTarget(args, 0, out int? amount, out target)) return false;
        if (amount != null && (_stats == PlayerStat.All || amount < 0)) return false;

        if (amount != null) request = amount.Value;
        return true;
    }

    protected override void ApplyLocal(LocalAstronaut player, int request) => PlayerStatsAccess.Restore(player, _stats, request);

    protected override void ApplyRemote(AstronautNetwork player, int request) => player.RestoreStatsByServer(_stats, request);

    protected override string Describe(int request) => request < 0
        ? $"{PlayerStatsAccess.Describe(_stats)} restored"
        : $"{PlayerStatsAccess.Describe(_stats)} set to {request}";

    public override void SuggestArguments(int argumentIndex, IReadOnlyList<string> previousArguments, List<string> results)
    {
        if (argumentIndex < 2) CommandSuggestionSources.AddPlayerNames(results);
    }
}
