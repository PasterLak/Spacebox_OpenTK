using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Generation;

namespace Spacebox.Network.Npc;

public sealed class SpacerSpawnService : IDisposable
{
    public const string PrefabKey = "spacer";

    private readonly Node3D _root;

    public SpacerSpawnService(Node3D root)
    {
        _root = root;
        NetworkManager.Instance.Spawner.Register(PrefabKey, Create, Destroy);
    }

    public void Spawn(Vector3 position)
    {
        NetworkManager.Instance.Spawner.Spawn(PrefabKey, NetworkManager.ServerPeerId, writer => writer.Write(position));
    }

    public void Dispose()
    {
        NetworkManager.Instance.Spawner.Unregister(PrefabKey);
    }

    private Node3D Create(NetSpawnContext context)
    {
        var spacer = new Spacer(context.Data.ReadVector3(), World.Instance?.Physics);
        spacer.AttachComponent(new SpacerNetwork());
        _root.AddChild(spacer);
        return spacer;
    }

    private static void Destroy(Node3D node)
    {
        for (int i = node.Components.Count - 1; i >= 0; i--)
        {
            if (node.Components[i] is NetworkObject || node.Components[i] is SpacerNetwork)
                node.DetachComponent(node.Components[i]);
        }

        if (node is Spacer spacer) spacer.Remove();
    }
}
