using Engine.Commands;
using Spacebox.Game.Player;
using Spacebox.Network.Admin;

namespace Spacebox.Game.Commands.Teleport;

public sealed class TeleportCommand : CommandBase, IArgumentSuggestions
{
    public const string Usage = "tp <x> <y> <z> | tp <target> | tp <player> <target> | tp <player> <x> <y> <z>";

    private readonly LocalAstronaut _localPlayer;
    private readonly string _name;

    public TeleportCommand(LocalAstronaut localPlayer, string name = "tp")
    {
        _localPlayer = localPlayer;
        _name = name;
    }

    public override string Name => _name;

    public override string Description => $"Teleport players. Usage: {Usage}. Players: a name, @s (you) or @a (everyone); ~ is relative to the teleported player, e.g. tp ~ ~10 ~";

    public void SuggestArguments(int argumentIndex, IReadOnlyList<string> previousArguments, List<string> results)
    {
        if (argumentIndex < 2)
        {
            CommandSuggestionSources.AddPlayerNames(results);
            TeleportSelector.AddSuggestions(results);
        }

        if (argumentIndex < 4) results.Add(TeleportCoordinate.RelativePrefix.ToString());
    }

    public override void Execute(string[] args)
    {
        if (ServerCommandForwarder.TryForward(Name, args)) return;

        if (!TeleportRequest.TryParse(args, out var request))
        {
            Engine.Debug.Error($"Usage: {Usage}");
            return;
        }

        TeleportExecutor.Run(request, _localPlayer, Usage);
    }
}
