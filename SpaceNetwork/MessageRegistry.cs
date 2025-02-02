
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
            Register<InitMessage>();
            Register<PlayersMessage>();
            Register<PositionMessage>();
            Register<RotationMessage>();
            Register<KickMessage>();
            Register<ChatMessage>();
            Register<SeedMessage>();
        }

        public static void Register<T>() where T : BaseMessage, new()
        {
            var t = typeof(T);
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
