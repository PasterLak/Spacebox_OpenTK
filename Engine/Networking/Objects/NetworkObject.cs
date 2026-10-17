using Engine.Components;

namespace Engine.Networking;

public sealed class NetworkObject : Component
{
    private readonly List<NetworkBehaviour> _behaviours = new List<NetworkBehaviour>();

    public uint NetId { get; internal set; }
    public int OwnerPeer { get; internal set; } = NetworkManager.ServerPeerId;
    public string PrefabKey { get; internal set; }
    public bool IsSpawned { get; internal set; }
    internal byte[] SpawnData { get; set; }

    public bool IsOwner => OwnerPeer == NetworkManager.Instance.LocalPeerId;
    public IReadOnlyList<NetworkBehaviour> Behaviours => _behaviours;

    internal void CollectBehaviours()
    {
        _behaviours.Clear();
        if (Owner == null) return;

        var components = Owner.Components;
        for (int i = 0; i < components.Count; i++)
        {
            if (components[i] is not NetworkBehaviour behaviour) continue;

            behaviour.NetworkObject = this;
            behaviour.BehaviourIndex = (byte)_behaviours.Count;
            _behaviours.Add(behaviour);
        }
    }

    internal NetworkBehaviour GetBehaviour(byte index)
    {
        return index < _behaviours.Count ? _behaviours[index] : null;
    }

    internal void WriteState(NetWriter writer)
    {
        for (int b = 0; b < _behaviours.Count; b++)
        {
            var variables = _behaviours[b].Variables;
            for (int v = 0; v < variables.Count; v++) variables[v].Write(writer);
        }
    }

    internal void ReadState(NetReader reader)
    {
        for (int b = 0; b < _behaviours.Count; b++)
        {
            var variables = _behaviours[b].Variables;
            for (int v = 0; v < variables.Count; v++) variables[v].Read(reader);
        }
    }

    public T GetBehaviour<T>() where T : NetworkBehaviour
    {
        for (int i = 0; i < _behaviours.Count; i++)
        {
            if (_behaviours[i] is T typed) return typed;
        }
        return null;
    }
}
