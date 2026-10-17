namespace Engine.Networking;

public enum NetworkWritePermission : byte
{
    Server,
    Owner
}

public interface INetworkVariable
{
    bool IsDirty { get; }
    NetworkWritePermission Permission { get; }
    DeliveryMode Delivery { get; }
    void Write(NetWriter writer);
    void Read(NetReader reader);
    void ClearDirty();
}

public sealed class NetworkVariable<T> : INetworkVariable
{
    private readonly NetworkBehaviour _behaviour;
    private T _value;

    public event Action<T, T> Changed;

    public bool IsDirty { get; private set; }
    public NetworkWritePermission Permission { get; }
    public DeliveryMode Delivery { get; }

    internal NetworkVariable(NetworkBehaviour behaviour, T initial, NetworkWritePermission permission, DeliveryMode delivery)
    {
        _behaviour = behaviour;
        _value = initial;
        Permission = permission;
        Delivery = delivery;
    }

    public T Value
    {
        get => _value;
        set
        {
            if (EqualityComparer<T>.Default.Equals(_value, value)) return;

            if (!CanWrite())
            {
                Debug.Warning($"[NetworkVariable] {_behaviour.GetType().Name} tried to write a {typeof(T).Name} without permission");
                return;
            }

            var old = _value;
            _value = value;
            IsDirty = true;
            Changed?.Invoke(old, value);
        }
    }

    public void Write(NetWriter writer)
    {
        NetSerializer.Write(writer, _value);
    }

    public void Read(NetReader reader)
    {
        var old = _value;
        _value = NetSerializer.Read<T>(reader);
        if (!EqualityComparer<T>.Default.Equals(old, _value)) Changed?.Invoke(old, _value);
    }

    public void ClearDirty()
    {
        IsDirty = false;
    }

    private bool CanWrite()
    {
        var network = NetworkManager.Instance;
        if (!_behaviour.IsSpawned || network.IsServer) return true;
        return Permission == NetworkWritePermission.Owner && _behaviour.IsOwner;
    }
}
