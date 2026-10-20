using System.Globalization;
using Engine;
using Engine.Commands;
using OpenTK.Mathematics;
using Spacebox.Game.Player;
using Spacebox.Network;
using Spacebox.Network.Admin;

namespace Spacebox.Game.Commands;

public sealed class SetSpawnCommand : CommandBase, IArgumentSuggestions
{
    public const string ClearArgument = "clear";

    private readonly LocalAstronaut _player;

    public SetSpawnCommand(LocalAstronaut player)
    {
        _player = player;
    }

    public override string Name => "setspawn";

    public override string Description => "Set the spawn point of all players to your position or to coordinates. Usage: setspawn [x y z] | setspawn clear";

    public static bool TryParsePosition(string[] args, out Vector3 position)
    {
        position = Vector3.Zero;
        if (args.Length != 3) return false;

        var style = NumberStyles.Float;
        var culture = CultureInfo.InvariantCulture;
        if (!float.TryParse(args[0], style, culture, out float x) || !float.TryParse(args[1], style, culture, out float y) || !float.TryParse(args[2], style, culture, out float z)) return false;

        position = new Vector3(x, y, z);
        return true;
    }

    public override void Execute(string[] args)
    {
        if (ServerCommandForwarder.TryForward(Name, args)) return;

        if (args.Length == 1 && string.Equals(args[0], ClearArgument, StringComparison.OrdinalIgnoreCase))
        {
            WorldSpawnNetwork.Set(null);
            Debug.Success("World spawn removed: new players spawn near a random asteroid again");
            return;
        }

        if (!TryParsePosition(args, out var position) && (args.Length != 0 || !CommandIssuer.TryGetPosition(_player, out position)))
        {
            Debug.Error("Usage: setspawn [x y z] | setspawn clear");
            return;
        }

        WorldSpawnNetwork.Set(position);
        Debug.Success($"World spawn set to {position.X:0.0} {position.Y:0.0} {position.Z:0.0}");
    }

    public void SuggestArguments(int argumentIndex, IReadOnlyList<string> previousArguments, List<string> results)
    {
        if (argumentIndex == 0) results.Add(ClearArgument);
    }
}
