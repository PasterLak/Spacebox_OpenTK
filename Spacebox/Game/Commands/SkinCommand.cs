using Spacebox.Game.Player;
using Spacebox.Network;
using Spacebox.Network.Players;

namespace Spacebox.Game.Commands;

public sealed class SkinCommand : PlayerTargetCommand<string>
{
    public SkinCommand(LocalAstronaut localPlayer) : base(localPlayer)
    {
    }

    public override string Name => "skin";

    public override string Description => $"Change a player's suit color ({string.Join(", ", AstronautSkins.Names)}). Usage: {Usage}";

    protected override string Usage => "skin <color> [player]";

    protected override bool TryParseRequest(string[] args, out string request, out string target)
    {
        request = null;
        target = args.Length > 1 ? string.Join(" ", args, 1, args.Length - 1) : null;
        return args.Length > 0 && AstronautSkins.TryNormalize(args[0], out request);
    }

    protected override void ApplyLocal(LocalAstronaut player, string request)
    {
        GameScene.Current?.Skins.Set(GameSession.PlayerName, request);
        if (AstronautNetwork.Local != null) AstronautNetwork.Local.SetSkinByServer(request);
        else player.SetSkinColor(request);
    }

    protected override void ApplyRemote(AstronautNetwork player, string request)
    {
        GameScene.Current?.Skins.Set(player.PlayerName, request);
        player.SetSkinByServer(request);
    }

    protected override string Describe(string request) => $"suit color set to {request}";

    public override void SuggestArguments(int argumentIndex, IReadOnlyList<string> previousArguments, List<string> results)
    {
        if (argumentIndex == 0) results.AddRange(AstronautSkins.Names);
        else if (argumentIndex == 1) CommandSuggestionSources.AddPlayerNames(results);
    }
}
