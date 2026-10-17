using Client;
using Engine;
using Engine.Components;
using Lidgren.Network;
using SpaceNetwork.Messages;
using System.Reflection;

namespace Spacebox.Client
{
    public class NetworkIdentity : Component
    {
        public int NetworkId { get; private set; }
        public bool IsMine { get; private set; }

        private static readonly Dictionary<int, NetworkIdentity> _activeIdentities = new Dictionary<int, NetworkIdentity>();
        private static readonly Dictionary<Type, Dictionary<string, MethodInfo>> _rpcCache = new Dictionary<Type, Dictionary<string, MethodInfo>>();

        public NetworkIdentity(int id, bool isMine)
        {
            NetworkId = id;
            IsMine = isMine;
        }

        public override void OnAttached(Node3D onOwner)
        {
            base.OnAttached(onOwner);

            if (_activeIdentities.ContainsKey(NetworkId))
            {
                _activeIdentities.Remove(NetworkId);
            }
            _activeIdentities.Add(NetworkId, this);

            CacheMethods();
        }

        public override void OnDetached()
        {
            if (_activeIdentities.ContainsKey(NetworkId))
            {
                _activeIdentities.Remove(NetworkId);
            }
            base.OnDetached();
        }

        public void TargetRpc(int targetId, string methodName, params object[] args)
        {
            Call(targetId, methodName, NetDeliveryMethod.ReliableOrdered, args);
        }

        public void Call(string methodName, params object[] args)
        {
            Call(methodName, NetDeliveryMethod.ReliableOrdered, args);
        }

        public void Call(string methodName, NetDeliveryMethod method, params object[] args)
        {
            Call(-1, methodName, method, args);
        }
        private void Call(int targetId, string methodName, NetDeliveryMethod method, params object[] args)
        {
            if (ClientNetwork.Instance == null) return;

            var msg = new RpcMessage
            {
                NetworkId = NetworkId,
                TargetId = targetId,
                MethodName = methodName,
                Parameters = args
            };

            ClientNetwork.Instance.Send(msg, method);
        }

        public void ExecuteRpcLocal(string methodName, object[] args)
        {
            if (Owner == null) return;

            if (TryInvoke(Owner, methodName, args)) return;

            for (int i = 0; i < Owner.Components.Count; i++)
            {

                if (Owner.Components[i] == this) continue;

                if (TryInvoke(Owner.Components[i], methodName, args)) return;
            }

            Debug.Warning($"[NetworkIdentity] RPC method '{methodName}' not found on object {NetworkId}");
        }

        private bool TryInvoke(object target, string methodName, object[] args)
        {
            var type = target.GetType();

            if (!_rpcCache.TryGetValue(type, out var methods))
            {
                methods = CacheMethodsForType(type);
            }

            if (methods.TryGetValue(methodName, out var methodInfo))
            {
                try
                {
                    methodInfo.Invoke(target, args);
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.Error($"[NetworkIdentity] Error invoking {methodName}: {ex.InnerException?.Message ?? ex.Message}");
                    return true; 
                }
            }
            return false; 
        }

        private void CacheMethods()
        {
            if (Owner == null) return;

            CacheMethodsForType(Owner.GetType());

            for (int i = 0; i < Owner.Components.Count; i++)
            {
                CacheMethodsForType(Owner.Components[i].GetType());
            }
        }

        private static Dictionary<string, MethodInfo> CacheMethodsForType(Type type)
        {
            if (_rpcCache.TryGetValue(type, out var existing)) return existing;

            var methodDict = new Dictionary<string, MethodInfo>();
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            for (int i = 0; i < methods.Length; i++)
            {
                var method = methods[i];
                if (method.GetCustomAttribute<RpcAttribute>() != null)
                {
                    methodDict[method.Name] = method;
                }
            }

            _rpcCache[type] = methodDict;
            return methodDict;
        }

        public static NetworkIdentity Get(int netId)
        {
            _activeIdentities.TryGetValue(netId, out var identity);
            return identity;
        }
    }
}