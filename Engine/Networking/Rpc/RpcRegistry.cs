using System.Reflection;

namespace Engine.Networking;

public sealed class RpcMethod
{
    public uint Id { get; }
    public string Name { get; }
    public RpcAttribute Attribute { get; }
    public Type[] ParameterTypes { get; }

    private readonly MethodInfo _method;
    private readonly Action<object, object[]> _invoke;

    public RpcMethod(MethodInfo method, RpcAttribute attribute)
    {
        _method = method;
        Name = method.Name;
        Attribute = attribute;
        Id = StableHash.Compute(method.DeclaringType.FullName + "." + method.Name);

        var parameters = method.GetParameters();
        _invoke = CompileInvoker(method, parameters);
        ParameterTypes = new Type[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            ParameterTypes[i] = parameters[i].ParameterType;
            if (!NetSerializer.CanSerialize(ParameterTypes[i]))
                Debug.Error($"[Rpc] {method.DeclaringType.Name}.{method.Name}: parameter type {ParameterTypes[i].Name} is not serializable");
        }
    }

    public void WriteArguments(NetWriter writer, object[] args)
    {
        if (args.Length != ParameterTypes.Length)
            throw new ArgumentException($"[Rpc] {Name} expects {ParameterTypes.Length} arguments, got {args.Length}");

        for (int i = 0; i < args.Length; i++)
            NetSerializer.Write(writer, ParameterTypes[i], args[i]);
    }

    public object[] ReadArguments(NetReader reader)
    {
        var args = new object[ParameterTypes.Length];
        for (int i = 0; i < args.Length; i++)
            args[i] = NetSerializer.Read(reader, ParameterTypes[i]);
        return args;
    }

    public void Invoke(object target, object[] args)
    {
        try
        {
            _invoke(target, args);
        }
        catch (Exception ex)
        {
            Debug.Error($"[Rpc] {Name} threw: {ex}");
        }
    }

    private static Action<object, object[]> CompileInvoker(MethodInfo method, ParameterInfo[] parameters)
    {
        var target = System.Linq.Expressions.Expression.Parameter(typeof(object), "target");
        var args = System.Linq.Expressions.Expression.Parameter(typeof(object[]), "args");
        var arguments = new System.Linq.Expressions.Expression[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            var item = System.Linq.Expressions.Expression.ArrayIndex(args, System.Linq.Expressions.Expression.Constant(i));
            arguments[i] = System.Linq.Expressions.Expression.Convert(item, parameters[i].ParameterType);
        }

        var instance = System.Linq.Expressions.Expression.Convert(target, method.DeclaringType);
        var call = System.Linq.Expressions.Expression.Call(instance, method, arguments);
        return System.Linq.Expressions.Expression.Lambda<Action<object, object[]>>(call, target, args).Compile();
    }
}

public static class RpcRegistry
{
    private sealed class TypeMethods
    {
        public readonly Dictionary<string, RpcMethod> ByName = new Dictionary<string, RpcMethod>();
        public readonly Dictionary<uint, RpcMethod> ById = new Dictionary<uint, RpcMethod>();
    }

    private static readonly Dictionary<Type, TypeMethods> _types = new Dictionary<Type, TypeMethods>();

    public static RpcMethod Get(Type type, string name)
    {
        return GetMethods(type).ByName.TryGetValue(name, out var method) ? method : null;
    }

    public static RpcMethod Get(Type type, uint id)
    {
        return GetMethods(type).ById.TryGetValue(id, out var method) ? method : null;
    }

    private static TypeMethods GetMethods(Type type)
    {
        if (_types.TryGetValue(type, out var cached)) return cached;

        var methods = new TypeMethods();
        for (var current = type; current != null && current != typeof(object); current = current.BaseType)
        {
            var declared = current.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            for (int i = 0; i < declared.Length; i++)
            {
                var attribute = declared[i].GetCustomAttribute<RpcAttribute>();
                if (attribute == null || methods.ByName.ContainsKey(declared[i].Name)) continue;

                var rpc = new RpcMethod(declared[i], attribute);
                methods.ByName[rpc.Name] = rpc;
                methods.ById[rpc.Id] = rpc;
            }
        }

        _types[type] = methods;
        return methods;
    }
}
