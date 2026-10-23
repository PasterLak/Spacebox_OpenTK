using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.Player;

namespace Spacebox.Network.Players;

public sealed class PlayerSpawnService : IDisposable
{
    public const string PrefabKey = "astronaut";

    private readonly Node3D _root;
    private readonly LocalAstronaut _localPlayer;

    public PlayerSpawnService(Node3D root, LocalAstronaut localPlayer)
    {
        _root = root;
        _localPlayer = localPlayer;
        NetworkManager.Instance.Spawner.Register(PrefabKey, Create, Destroy);
    }

    public NetworkObject SpawnPlayer(int peer, string name, string skinColor, Vector3 position, Quaternion rotation)
    {
        return NetworkManager.Instance.Spawner.Spawn(PrefabKey, peer, writer =>
        {
            writer.Write(name);
            writer.Write(skinColor);
            writer.Write(position);
            writer.Write(rotation);
        });
    }

    public void Dispose()
    {
        NetworkManager.Instance.Spawner.Unregister(PrefabKey);
    }

    private Node3D Create(NetSpawnContext context)
    {
        string name = context.Data.ReadString();
        string skin = context.Data.ReadString();
        var position = context.Data.ReadVector3();
        var rotation = context.Data.ReadQuaternion();

        if (context.IsOwner && _localPlayer != null)
        {
            _localPlayer.AttachComponent(new AstronautNetwork { PlayerName = name, InitialSkin = skin });
            return _localPlayer;
        }

        var remote = new RemoteAstronaut(name, skin, position);
        remote.SetRotation(rotation);
        remote.AttachComponent(new AstronautNetwork { PlayerName = name, InitialSkin = skin });
        _root.AddChild(remote);
        return remote;
    }

    private void Destroy(Node3D node)
    {
        if (node is LocalAstronaut local)
        {
            DetachNetworkComponents(local);
            return;
        }

        if (node is RemoteAstronaut remote) remote.OnDisconnect();

        DetachNetworkComponents(node);
        _root.RemoveChild(node);
    }

    private static void DetachNetworkComponents(Node3D node)
    {
        for (int i = node.Components.Count - 1; i >= 0; i--)
        {
            if (node.Components[i] is NetworkObject || node.Components[i] is AstronautNetwork)
                node.DetachComponent(node.Components[i]);
        }
    }
}
