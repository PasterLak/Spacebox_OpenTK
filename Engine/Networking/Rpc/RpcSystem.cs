namespace Engine.Networking;

public sealed class RpcSystem
{
    private readonly NetworkManager _network;
    private readonly NetWriter _writer = new NetWriter();
    private readonly NetReader _reader = new NetReader();

    internal RpcSystem(NetworkManager network)
    {
        _network = network;
        RegisterHandlers();
    }

    internal void RegisterHandlers()
    {
        _network.Messages.Register<RpcMessage>(OnRpcMessage);
    }

    public void Send(NetworkBehaviour behaviour, string methodName, object[] args)
    {
        var method = RpcRegistry.Get(behaviour.GetType(), methodName);
        if (method == null)
        {
            Debug.Error($"[Rpc] {behaviour.GetType().Name} has no [Rpc] method '{methodName}'");
            return;
        }

        var networkObject = behaviour.NetworkObject;
        if (networkObject == null || !networkObject.IsSpawned)
        {
            Debug.Warning($"[Rpc] {behaviour.GetType().Name}.{methodName} called before the object was spawned");
            return;
        }

        var target = method.Attribute.Target;
        if (target != RpcTarget.Server && !_network.IsServer)
        {
            Debug.Error($"[Rpc] {behaviour.GetType().Name}.{methodName} targets {target} and can only be called on the server");
            return;
        }

        _writer.Reset();
        method.WriteArguments(_writer, args);
        var message = new RpcMessage
        {
            NetId = networkObject.NetId,
            Behaviour = behaviour.BehaviourIndex,
            MethodId = method.Id,
            Payload = _writer.ToArray()
        };

        Route(networkObject, behaviour, method, message, args);
    }

    private void Route(NetworkObject networkObject, NetworkBehaviour behaviour, RpcMethod method, RpcMessage message, object[] args)
    {
        var delivery = method.Attribute.Delivery;
        int local = _network.LocalPeerId;

        switch (method.Attribute.Target)
        {
            case RpcTarget.Server:
                if (_network.IsServer) InvokeLocal(behaviour, method, args, local);
                else _network.SendToServer(message, delivery);
                break;

            case RpcTarget.Owner:
                if (networkObject.OwnerPeer == local) InvokeLocal(behaviour, method, args, local);
                else _network.Send(networkObject.OwnerPeer, message, delivery);
                break;

            case RpcTarget.Clients:
                _network.SendToClients(message, delivery);
                if (_network.IsClient) InvokeLocal(behaviour, method, args, local);
                break;

            case RpcTarget.ClientsExceptOwner:
                _network.SendToClients(message, delivery, networkObject.OwnerPeer);
                if (_network.IsClient && networkObject.OwnerPeer != local) InvokeLocal(behaviour, method, args, local);
                break;

            case RpcTarget.All:
                _network.SendToClients(message, delivery);
                InvokeLocal(behaviour, method, args, local);
                break;
        }
    }

    private void OnRpcMessage(int sender, RpcMessage message)
    {
        if (!_network.Spawner.TryGet(message.NetId, out var networkObject)) return;

        var behaviour = networkObject.GetBehaviour(message.Behaviour);
        if (behaviour == null) return;

        var method = RpcRegistry.Get(behaviour.GetType(), message.MethodId);
        if (method == null)
        {
            Debug.Warning($"[Rpc] Unknown method {message.MethodId} on {behaviour.GetType().Name}");
            return;
        }

        if (!IsAllowed(sender, networkObject, method)) return;

        _reader.Set(message.Payload, 0, message.Payload.Length);
        object[] args;
        try
        {
            args = method.ReadArguments(_reader);
        }
        catch (Exception ex)
        {
            Debug.Error($"[Rpc] Failed to read arguments of {method.Name} from peer {sender}: {ex.Message}");
            return;
        }

        InvokeLocal(behaviour, method, args, sender);
    }

    private bool IsAllowed(int sender, NetworkObject networkObject, RpcMethod method)
    {
        if (!_network.IsServer) return sender == NetworkManager.ServerPeerId;

        if (method.Attribute.Target != RpcTarget.Server)
        {
            Debug.Warning($"[Rpc] Peer {sender} tried to call {method.Name} ({method.Attribute.Target}) on the server");
            return false;
        }

        if (method.Attribute.RequireOwnership && networkObject.OwnerPeer != sender)
        {
            Debug.Warning($"[Rpc] Peer {sender} tried to call {method.Name} on object {networkObject.NetId} it does not own");
            return false;
        }

        return true;
    }

    private static void InvokeLocal(NetworkBehaviour behaviour, RpcMethod method, object[] args, int sender)
    {
        int previous = RpcContext.SenderPeer;
        RpcContext.SenderPeer = sender;
        method.Invoke(behaviour, args);
        RpcContext.SenderPeer = previous;
    }
}
