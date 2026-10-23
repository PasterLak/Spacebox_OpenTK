namespace Engine.Networking;

public sealed class NetMessageRegistry
{
    private abstract class Handler
    {
        public string Name;
        public abstract Delegate Callback { get; }
        public abstract void Dispatch(int peer, NetReader reader);
    }

    private sealed class Handler<T> : Handler where T : INetMessage, new()
    {
        public Action<int, T> Invoke;

        public override Delegate Callback => Invoke;

        public override void Dispatch(int peer, NetReader reader)
        {
            var message = new T();
            message.Deserialize(reader);
            Invoke(peer, message);
        }
    }

    private static class MessageId<T> where T : INetMessage
    {
        public static readonly uint Value = StableHash.Compute(typeof(T).FullName);
    }

    private readonly Dictionary<uint, Handler> _handlers = new Dictionary<uint, Handler>();
    private readonly HashSet<uint> _openMessages = new HashSet<uint>();

    public static uint GetId<T>() where T : INetMessage => MessageId<T>.Value;

    public void Register<T>(Action<int, T> handler) where T : INetMessage, new()
    {
        uint id = GetId<T>();
        if (_handlers.TryGetValue(id, out var existing) && existing.Name != typeof(T).FullName)
            throw new InvalidOperationException($"[NetMessageRegistry] Hash collision between {existing.Name} and {typeof(T).FullName}");

        _handlers[id] = new Handler<T>
        {
            Invoke = handler,
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

        handler.Dispatch(peer, reader);
        return true;
    }

    public void DispatchLocal<T>(int peer, T message) where T : INetMessage
    {
        if (_handlers.TryGetValue(GetId<T>(), out var handler) && handler.Callback is Action<int, T> callback)
            callback(peer, message);
    }
}
