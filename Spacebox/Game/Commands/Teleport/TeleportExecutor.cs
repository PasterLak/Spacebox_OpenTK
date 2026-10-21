using System.Globalization;
using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Player;

namespace Spacebox.Game.Commands.Teleport;

public static class TeleportExecutor
{
    public const float DestinationOffset = 2f;

    public static void Run(TeleportRequest request, LocalAstronaut localPlayer, string usage)
    {
        var players = new List<ITeleportable>();
        TeleportPlayers.Collect(localPlayer, players);
        var sender = TeleportPlayers.FindSender(players);

        var subjects = new List<ITeleportable>();
        if (!TeleportSelector.TrySelect(request.Subjects ?? TeleportSelector.Self, sender, players, subjects))
        {
            Debug.Error(request.Subjects == null ? $"Usage: {usage}" : $"Player not found: {request.Subjects}");
            return;
        }

        if (request.HasCoordinates)
        {
            for (int i = 0; i < subjects.Count; i++)
            {
                var position = request.ResolveCoordinates(subjects[i].Position);
                subjects[i].Teleport(position);
                Debug.Success($"{subjects[i].Name} teleported to {Format(position)}");
            }
            return;
        }

        var destinations = new List<ITeleportable>();
        if (!TeleportSelector.TrySelect(request.Destination, sender, players, destinations) || destinations.Count != 1)
        {
            Debug.Error($"Player not found: {request.Destination}");
            return;
        }

        var destination = destinations[0];
        var target = destination.Position + destination.Front * DestinationOffset;
        for (int i = 0; i < subjects.Count; i++)
        {
            if (ReferenceEquals(subjects[i], destination)) continue;
            subjects[i].Teleport(target);
            Debug.Success($"{subjects[i].Name} teleported to {destination.Name}");
        }
    }

    private static string Format(Vector3 position)
    {
        return string.Format(CultureInfo.InvariantCulture, "({0:0.##}, {1:0.##}, {2:0.##})", position.X, position.Y, position.Z);
    }
}
