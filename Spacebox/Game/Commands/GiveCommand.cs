using Spacebox.Game.Player;
using Spacebox.Network.Players;

namespace Spacebox.Game.Commands;

public readonly record struct GiveRequest(Item Item, int Count);

public sealed class GiveCommand : PlayerTargetCommand<GiveRequest>
{
    public const int MaxCount = 9999;

    public GiveCommand(LocalAstronaut localPlayer) : base(localPlayer)
    {
    }

    public override string Name => "give";

    public override string Description => $"Give items. Usage: {Usage}";

    protected override string Usage => "give <item> [count] [player]";

    protected override bool TryParseRequest(string[] args, out GiveRequest request, out string target)
    {
        request = default;
        target = null;
        if (args.Length < 1 || !ItemLookup.TryFind(args[0], out var item)) return false;
        if (!CommandArguments.TrySplitAmountAndTarget(args, 1, out int? amount, out target)) return false;

        int count = amount ?? 1;
        if (count < 1 || count > MaxCount) return false;

        request = new GiveRequest(item, count);
        return true;
    }

    protected override void ApplyLocal(LocalAstronaut player, GiveRequest request) => PlayerItemGiver.Give(player, request.Item, request.Count);

    protected override void ApplyRemote(AstronautNetwork player, GiveRequest request) => player.GiveItemByServer(request.Item.Id_string, request.Count);

    protected override string Describe(GiveRequest request) => $"got {request.Count} x {request.Item.Name}";

    public override void SuggestArguments(int argumentIndex, IReadOnlyList<string> previousArguments, List<string> results)
    {
        switch (argumentIndex)
        {
            case 0:
                CommandSuggestionSources.AddItemIds(results);
                break;
            case 1:
                results.Add("1");
                results.Add("16");
                results.Add("64");
                CommandSuggestionSources.AddPlayerNames(results);
                break;
            case 2:
                CommandSuggestionSources.AddPlayerNames(results);
                break;
        }
    }
}
