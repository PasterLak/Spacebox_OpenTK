using Spacebox.Game.Player;
using Spacebox.Network.Players;

namespace Spacebox.Game.Commands;

public sealed class HitCommand : PlayerTargetCommand<int>
{
    public const int MaxDamage = 100000;

    public HitCommand(LocalAstronaut localPlayer) : base(localPlayer)
    {
    }

    public override string Name => "hit";

    public override string Description => $"Damage a player like a real hit. Usage: {Usage}";

    protected override string Usage => "hit [player] <damage>";

    protected override bool TryParseRequest(string[] args, out int request, out string target)
    {
        request = 0;
        if (!CommandArguments.TrySplitAmountAndTarget(args, 0, out int? amount, out target)) return false;
        if (amount == null || amount <= 0) return false;

        request = Math.Min(amount.Value, MaxDamage);
        return true;
    }

    protected override void ApplyLocal(LocalAstronaut player, int request) => player.TakeDamage(request, new DeathCase(DeathMessageFormatter.HitReason));

    protected override void ApplyRemote(AstronautNetwork player, int request) => player.HitByServer(request);

    protected override string Describe(int request) => $"hit for {request} damage";

    public override void SuggestArguments(int argumentIndex, IReadOnlyList<string> previousArguments, List<string> results)
    {
        if (argumentIndex == 0) CommandSuggestionSources.AddPlayerNames(results);
    }
}
