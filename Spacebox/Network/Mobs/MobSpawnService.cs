using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Generation;
using Spacebox.Game.Mobs;

namespace Spacebox.Network.Mobs;

public sealed class MobSpawnService : IDisposable
{
    public const string PrefabKey = "mob";

    private readonly Node3D _root;
    private readonly Random _random = new Random();

    public MobSpawnService(Node3D root)
    {
        _root = root;
        NetworkManager.Instance.Spawner.Register(PrefabKey, Create, Destroy);
    }

    public void Spawn(MobDefinition definition, Vector3 position, bool isTimid)
    {
        int seed = _random.Next();
        NetworkManager.Instance.Spawner.Spawn(PrefabKey, NetworkManager.ServerPeerId, writer =>
        {
            writer.Write(definition.Id);
            writer.Write(position);
            writer.Write(isTimid);
            writer.Write(seed);
        });
    }

    public void Dispose()
    {
        NetworkManager.Instance.Spawner.Unregister(PrefabKey);
    }

    private Node3D Create(NetSpawnContext context)
    {
        string id = context.Data.ReadString();
        var position = context.Data.ReadVector3();
        bool isTimid = context.Data.ReadBool();
        int seed = context.Data.ReadInt();

        if (!GameAssets.Mobs.TryGetValue(id, out var definition))
        {
            Debug.Error($"[MobSpawnService] Unknown mob '{id}'");
            return null!;
        }

        var mob = new Mob(definition, position, World.Instance?.Physics, isTimid, seed);
        mob.AttachComponent(new MobNetwork());
        _root.AddChild(mob);
        return mob;
    }

    private static void Destroy(Node3D node)
    {
        for (int i = node.Components.Count - 1; i >= 0; i--)
        {
            if (node.Components[i] is NetworkObject || node.Components[i] is MobNetwork)
                node.DetachComponent(node.Components[i]);
        }

        if (node is Mob mob) mob.Remove();
    }
}
