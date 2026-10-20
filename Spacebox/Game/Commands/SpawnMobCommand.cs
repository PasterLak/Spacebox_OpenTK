using Engine;
using Engine.Commands;
using Engine.Networking;
using Spacebox.Game.Mobs;
using Spacebox.Game.Player;

namespace Spacebox.Game.Commands;

internal sealed class SpawnMobCommand : CommandBase, Engine.Commands.IArgumentSuggestions
{
    public void SuggestArguments(int argumentIndex, IReadOnlyList<string> previousArguments, List<string> results)
    {
        switch (argumentIndex)
        {
            case 0:
                CommandSuggestionSources.AddMobIds(results);
                results.Add("list");
                results.Add("clear");
                break;
            case 1:
                results.Add("1");
                results.Add("5");
                break;
            case 2:
                CommandSuggestionSources.AddPlayerNames(results);
                break;
        }
    }

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
        if (Spacebox.Network.Admin.ServerCommandForwarder.TryForward(Name, args)) return;

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

        string target = args.Length > 2 ? args[2] : Application.IsHeadless ? Spacebox.Network.Admin.ChatCommands.CurrentSender : null;
        if (target != null)
        {
            if (Spacebox.Network.Players.PlayerLocator.TryFind(target, out var player) &&
                Spacebox.Network.Players.PlayerLocator.TryGetPose(player.Peer, out var playerPosition, out var front))
            {
                position = playerPosition + front * SpawnDistance;
                return true;
            }

            Debug.Error($"Player not found: {target}");
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
