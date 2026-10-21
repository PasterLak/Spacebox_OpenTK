using Engine.Commands;
using Spacebox.Game.Player;
using Spacebox.Network.Admin;

namespace Spacebox.Game.Commands.Teleport;

public sealed class TeleportHereCommand : CommandBase, IArgumentSuggestions
{
    private const string Usage = "tphere <player>";

    private readonly LocalAstronaut _localPlayer;

    public TeleportHereCommand(LocalAstronaut localPlayer)
    {
        _localPlayer = localPlayer;
    }

    public override string Name => "tphere";

    public override string Description => $"Teleport a player to you. Usage: {Usage} (@a brings everyone)";

    public void SuggestArguments(int argumentIndex, IReadOnlyList<string> previousArguments, List<string> results)
    {
        if (argumentIndex != 0) return;
        CommandSuggestionSources.AddPlayerNames(results);
        results.Add(TeleportSelector.All);
    }

    public override void Execute(string[] args)
    {
        if (ServerCommandForwarder.TryForward(Name, args)) return;

        if (args.Length != 1)
        {
            Engine.Debug.Error($"Usage: {Usage}");
            return;
        }

        TeleportExecutor.Run(TeleportRequest.ToPlayer(args[0], TeleportSelector.Self), _localPlayer, Usage);
    }
}
