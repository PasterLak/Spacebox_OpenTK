using Engine;
using Engine.Commands;
using Engine.Networking;
using Spacebox.Game.Mobs;
using Spacebox.Game.Player;

namespace Spacebox.Game.Commands;

internal sealed class SpawnMobCommand : CommandBase
{
    private const float SpawnDistance = 3f;
    private const int MaxCount = 32;

    private readonly LocalAstronaut _astronaut;

    public SpawnMobCommand(LocalAstronaut astronaut)
    {
        _astronaut = astronaut;
    }

    public override string Name => "mob";

    public override string Description => Application.IsHeadless
        ? "Mobs. Usage: mob <id> <count> <player> | mob list | mob clear"
        : "Mobs. Usage: mob <id> [count] [player] | mob list | mob clear";

    public override void Execute(string[] args)
    {
        var system = MobSystem.Instance;
        if (system == null || !NetworkManager.Instance.IsServer)
        {
            Debug.Error("Mobs can be spawned only on the server");
            return;
        }

        if (args.Length < 1)
        {
            Debug.Error($"Usage: {Name} <id> [count] | {Name} list | {Name} clear");
            return;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "list":
                List(system);
                return;
            case "clear":
                Clear(system);
                return;
        }

        if (!GameAssets.Mobs.TryGetValue(args[0], out var definition))
        {
            Debug.Error($"Unknown mob '{args[0]}'. Use '{Name} list'");
            return;
        }

        int count = args.Length > 1 && int.TryParse(args[1], out int parsed) ? Math.Clamp(parsed, 1, MaxCount) : 1;
        if (!TryGetSpawnPosition(args, out var position)) return;


        for (int i = 0; i < count; i++)
            system.TrySpawn(definition, position, MobRandom.Chance(Random.Shared, definition.FleeChance));

        Debug.Success($"Spawned {count} x {definition.Name}");
    }

    private bool TryGetSpawnPosition(string[] args, out OpenTK.Mathematics.Vector3 position)
    {
        position = OpenTK.Mathematics.Vector3.Zero;

        if (args.Length > 2)
        {
            if (Spacebox.Network.Players.PlayerLocator.TryFind(args[2], out var player) &&
                Spacebox.Network.Players.PlayerLocator.TryGetPose(player.Peer, out var playerPosition, out var front))
            {
                position = playerPosition + front * SpawnDistance;
                return true;
            }

            Debug.Error($"Player not found: {args[2]}");
            return false;
        }

        if (Application.IsHeadless || _astronaut == null)
        {
            Debug.Error($"Usage on the server: {Name} <id> <count> <player>");
            return false;
        }

        position = _astronaut.Position + _astronaut.Front * SpawnDistance;
        return true;
    }

    private static void List(MobSystem system)
    {
        foreach (var definition in GameAssets.Mobs.Values)
            Debug.Log($"{definition.Id} ({definition.Name}): {system.Count(definition)} alive");
    }

    private static void Clear(MobSystem system)
    {
        var mobs = system.Mobs;
        for (int i = mobs.Count - 1; i >= 0; i--)
        {
            var mob = mobs[i];
            if (mob.Link != null) mob.Link.Despawn();
            else mob.Remove();
        }

        Debug.Success("Mobs removed");
    }
}
