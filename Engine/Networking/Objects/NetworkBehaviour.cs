using Engine.Components;

namespace Engine.Networking;

public abstract class NetworkBehaviour : Component
{
    private readonly List<INetworkVariable> _variables = new List<INetworkVariable>();

    public NetworkObject NetworkObject { get; internal set; }
    internal byte BehaviourIndex { get; set; }
    internal IReadOnlyList<INetworkVariable> Variables => _variables;

    public bool IsSpawned => NetworkObject != null && NetworkObject.IsSpawned;
    public bool IsServer => NetworkManager.Instance.IsServer;
    public bool IsClient => NetworkManager.Instance.IsClient;
    public bool IsOwner => NetworkObject != null && NetworkObject.IsOwner;
    public int OwnerPeer => NetworkObject?.OwnerPeer ?? NetworkManager.ServerPeerId;
    public uint NetId => NetworkObject?.NetId ?? 0;

    protected NetworkVariable<T> CreateVariable<T>(T initial = default, NetworkWritePermission permission = NetworkWritePermission.Server, DeliveryMode delivery = DeliveryMode.Reliable)
    {
        var variable = new NetworkVariable<T>(this, initial, permission, delivery);
        _variables.Add(variable);
        return variable;
    }

    protected void SendRpc(string methodName, params object[] args)
    {
        NetworkManager.Instance.Rpcs.Send(this, methodName, args);
    }

    public virtual void OnNetworkSpawn()
    {
    }

    public virtual void OnNetworkDespawn()
    {
    }

    public virtual void OnOwnershipChanged(int previousOwner)
    {
    }
}
