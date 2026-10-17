using OpenTK.Mathematics;

namespace Engine.Networking;

public static class NetSerializer
{
    private sealed class Entry
    {
        public Action<NetWriter, object> Write;
        public Func<NetReader, object> Read;
    }

    private static readonly Dictionary<Type, Entry> _entries = new Dictionary<Type, Entry>();
    private static readonly object _lock = new object();

    static NetSerializer()
    {
        Register<byte>((w, v) => w.Write(v), r => r.ReadByte());
        Register<sbyte>((w, v) => w.Write(v), r => r.ReadSByte());
        Register<bool>((w, v) => w.Write(v), r => r.ReadBool());
        Register<short>((w, v) => w.Write(v), r => r.ReadShort());
        Register<ushort>((w, v) => w.Write(v), r => r.ReadUShort());
        Register<int>((w, v) => w.Write(v), r => r.ReadInt());
        Register<uint>((w, v) => w.Write(v), r => r.ReadUInt());
        Register<long>((w, v) => w.Write(v), r => r.ReadLong());
        Register<ulong>((w, v) => w.Write(v), r => r.ReadULong());
        Register<float>((w, v) => w.Write(v), r => r.ReadFloat());
        Register<double>((w, v) => w.Write(v), r => r.ReadDouble());
        Register<string>((w, v) => w.Write(v), r => r.ReadString());
        Register<Vector2>((w, v) => w.Write(v), r => r.ReadVector2());
        Register<Vector3>((w, v) => w.Write(v), r => r.ReadVector3());
        Register<Vector3i>((w, v) => w.Write(v), r => r.ReadVector3i());
        Register<Quaternion>((w, v) => w.Write(v), r => r.ReadQuaternion());
        Register<byte[]>((w, v) => w.WriteBytes(v ?? Array.Empty<byte>()), r => r.ReadBytes());
    }

    public static void Register<T>(Action<NetWriter, T> write, Func<NetReader, T> read)
    {
        lock (_lock)
        {
            _entries[typeof(T)] = new Entry
            {
                Write = (w, v) => write(w, (T)v),
                Read = r => read(r)
            };
        }
    }

    public static bool CanSerialize(Type type)
    {
        return GetEntry(type) != null;
    }

    public static void Write<T>(NetWriter writer, T value)
    {
        Write(writer, typeof(T), value);
    }

    public static T Read<T>(NetReader reader)
    {
        return (T)Read(reader, typeof(T));
    }

    public static void Write(NetWriter writer, Type type, object value)
    {
        var entry = GetEntry(type) ?? throw new NotSupportedException($"[NetSerializer] Type {type.FullName} is not registered");
        entry.Write(writer, value);
    }

    public static object Read(NetReader reader, Type type)
    {
        var entry = GetEntry(type) ?? throw new NotSupportedException($"[NetSerializer] Type {type.FullName} is not registered");
        return entry.Read(reader);
    }

    private static Entry GetEntry(Type type)
    {
        lock (_lock)
        {
            if (_entries.TryGetValue(type, out var entry)) return entry;

            entry = CreateEntry(type);
            if (entry != null) _entries[type] = entry;
            return entry;
        }
    }

    private static Entry CreateEntry(Type type)
    {
        if (type.IsEnum)
        {
            var underlying = Enum.GetUnderlyingType(type);
            var inner = GetEntryUnlocked(underlying);
            if (inner == null) return null;

            return new Entry
            {
                Write = (w, v) => inner.Write(w, Convert.ChangeType(v, underlying)),
                Read = r => Enum.ToObject(type, inner.Read(r))
            };
        }

        if (typeof(INetSerializable).IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) != null)
        {
            return new Entry
            {
                Write = (w, v) => ((INetSerializable)v).Serialize(w),
                Read = r =>
                {
                    var instance = (INetSerializable)Activator.CreateInstance(type);
                    instance.Deserialize(r);
                    return instance;
                }
            };
        }

        if (type.IsArray && type.GetArrayRank() == 1)
        {
            var elementType = type.GetElementType();
            var element = GetEntryUnlocked(elementType);
            if (element == null) return null;

            return new Entry
            {
                Write = (w, v) =>
                {
                    var array = (Array)v;
                    if (array == null)
                    {
                        w.Write(-1);
                        return;
                    }

                    w.Write(array.Length);
                    for (int i = 0; i < array.Length; i++) element.Write(w, array.GetValue(i));
                },
                Read = r =>
                {
                    int length = r.ReadInt();
                    if (length < 0) return null;

                    var array = Array.CreateInstance(elementType, length);
                    for (int i = 0; i < length; i++) array.SetValue(element.Read(r), i);
                    return array;
                }
            };
        }

        return null;
    }

    private static Entry GetEntryUnlocked(Type type)
    {
        if (_entries.TryGetValue(type, out var entry)) return entry;

        entry = CreateEntry(type);
        if (entry != null) _entries[type] = entry;
        return entry;
    }
}
