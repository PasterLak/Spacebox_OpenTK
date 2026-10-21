namespace Engine.Networking;

public sealed class NetMessageRegistry
{
    private sealed class Handler
    {
        public Func<INetMessage> Create;
        public Action<int, INetMessage> Invoke;
        public string Name;
    }

    private readonly Dictionary<uint, Handler> _handlers = new Dictionary<uint, Handler>();
    private readonly HashSet<uint> _openMessages = new HashSet<uint>();

    public static uint GetId<T>() where T : INetMessage => StableHash.Compute(typeof(T).FullName);

    public void Register<T>(Action<int, T> handler) where T : INetMessage, new()
    {
        uint id = GetId<T>();
        if (_handlers.TryGetValue(id, out var existing) && existing.Name != typeof(T).FullName)
            throw new InvalidOperationException($"[NetMessageRegistry] Hash collision between {existing.Name} and {typeof(T).FullName}");

        _handlers[id] = new Handler
        {
            Create = () => new T(),
            Invoke = (peer, message) => handler(peer, (T)message),
            Name = typeof(T).FullName
        };
    }

    public void AllowBeforeAdmission<T>() where T : INetMessage
    {
        _openMessages.Add(GetId<T>());
    }

    public bool IsAllowedBeforeAdmission(uint id) => _openMessages.Contains(id);

    public void Unregister<T>() where T : INetMessage
    {
        _handlers.Remove(GetId<T>());
    }

    public void Clear()
    {
        _handlers.Clear();
        _openMessages.Clear();
    }

    public void Write<T>(NetWriter writer, T message) where T : INetMessage
    {
        writer.Write(GetId<T>());
        message.Serialize(writer);
    }

    public bool Dispatch(int peer, NetReader reader, bool admitted = true)
    {
        uint id = reader.ReadUInt();
        if (!admitted && !_openMessages.Contains(id)) return false;
        if (!_handlers.TryGetValue(id, out var handler))
        {
            Debug.Warning($"[Network] Received unknown message id {id} from peer {peer}");
            return false;
        }

        var message = handler.Create();
        message.Deserialize(reader);
        handler.Invoke(peer, message);
        return true;
    }

    public void DispatchLocal<T>(int peer, T message) where T : INetMessage
    {
        if (_handlers.TryGetValue(GetId<T>(), out var handler))
            handler.Invoke(peer, message);
    }
}
