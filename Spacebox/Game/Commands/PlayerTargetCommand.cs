using Engine;
using Engine.Commands;
using Engine.Networking;
using Spacebox.Game.Player;
using Spacebox.Network;
using Spacebox.Network.Admin;
using Spacebox.Network.Players;

namespace Spacebox.Game.Commands;

public abstract class PlayerTargetCommand<TRequest> : CommandBase, IArgumentSuggestions
{
    private readonly LocalAstronaut _localPlayer;

    protected PlayerTargetCommand(LocalAstronaut localPlayer)
    {
        _localPlayer = localPlayer;
    }

    protected abstract string Usage { get; }

    protected abstract bool TryParseRequest(string[] args, out TRequest request, out string target);

    protected abstract void ApplyLocal(LocalAstronaut player, TRequest request);

    protected abstract void ApplyRemote(AstronautNetwork player, TRequest request);

    protected abstract string Describe(TRequest request);

    public abstract void SuggestArguments(int argumentIndex, IReadOnlyList<string> previousArguments, List<string> results);

    public override void Execute(string[] args)
    {
        if (ServerCommandForwarder.TryForward(Name, args)) return;

        if (!TryParseRequest(args, out var request, out string target))
        {
            Debug.Error($"Usage: {Usage}");
            return;
        }

        if (target == null && !GameSession.HasLocalPlayer) target = ChatCommands.CurrentSender;

        if (target == null || IsLocalName(target))
        {
            if (!GameSession.HasLocalPlayer || _localPlayer == null)
            {
                Debug.Error($"Usage: {Usage}");
                return;
            }

            ApplyLocal(_localPlayer, request);
            Debug.Success($"{GameSession.PlayerName}: {Describe(request)}");
            return;
        }

        if (!NetworkManager.Instance.IsServer || !PlayerLocator.TryFind(target, out var player))
        {
            Debug.Error($"Player not found: {target}");
            return;
        }

        var network = AstronautNetwork.Find(player.Peer);
        if (network == null)
        {
            Debug.Error($"{player.Name} is not spawned yet");
            return;
        }

        ApplyRemote(network, request);
        Debug.Success($"{player.Name}: {Describe(request)}");
    }

    private static bool IsLocalName(string name)
    {
        return GameSession.HasLocalPlayer && string.Equals(name.Trim(), GameSession.PlayerName, StringComparison.OrdinalIgnoreCase);
    }
}
