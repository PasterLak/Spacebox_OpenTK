
using System.Reflection;
using SpaceNetwork.Messages;

namespace SpaceNetwork
{
    public static class MessageRegistry
    {
        static Dictionary<int, Type> idToType = new Dictionary<int, Type>();
        static Dictionary<Type, int> typeToId = new Dictionary<Type, int>();
        static int nextId = 1;

        static MessageRegistry()
        {
            var baseType = typeof(BaseMessage);
            var assembly = Assembly.GetAssembly(baseType);
            var types = assembly.GetTypes().Where(t => !t.IsAbstract && baseType.IsAssignableFrom(t));
            foreach (var type in types)
            {
                Register(type);
            }
        }

        public static void Register<T>() where T : BaseMessage, new()
        {
            Register(typeof(T));
        }

        public static void Register(Type t)
        {
            if (!typeToId.ContainsKey(t))
            {
                typeToId[t] = nextId;
                idToType[nextId] = t;
                nextId++;
            }
        }

        public static int GetId(Type t)
        {
            return typeToId[t];
        }

        public static BaseMessage CreateMessage(int id)
        {
            var t = idToType[id];
            return (BaseMessage)Activator.CreateInstance(t);
        }
    }
}
