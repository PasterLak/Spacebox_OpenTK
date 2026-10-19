namespace Engine.Networking;

public sealed class ReplicationSystem
{
    private readonly NetworkManager _network;
    private readonly NetWriter _reliable = new NetWriter();
    private readonly NetWriter _unreliable = new NetWriter();
    private readonly NetReader _reader = new NetReader();

    internal ReplicationSystem(NetworkManager network)
    {
        _network = network;
        RegisterHandlers();
    }

    internal void RegisterHandlers()
    {
        _network.Messages.Register<VariablesMessage>(OnVariablesMessage);
    }

    internal void Tick()
    {
        foreach (var networkObject in _network.Spawner.Objects)
        {
            if (!networkObject.IsSpawned) continue;

            bool canSend = _network.IsServer || networkObject.IsOwner;
            if (!canSend) continue;

            _reliable.Reset();
            _unreliable.Reset();
            CollectDirty(networkObject);

            if (_reliable.Length > 0) SendVariables(networkObject, _reliable, DeliveryMode.Reliable);
            if (_unreliable.Length > 0) SendVariables(networkObject, _unreliable, DeliveryMode.Sequenced);
        }
    }

    private void CollectDirty(NetworkObject networkObject)
    {
        bool isServer = _network.IsServer;
        var behaviours = networkObject.Behaviours;

        for (int b = 0; b < behaviours.Count; b++)
        {
            var variables = behaviours[b].Variables;
            for (int v = 0; v < variables.Count; v++)
            {
                var variable = variables[v];
                if (!variable.IsDirty) continue;

                variable.ClearDirty();
                if (!isServer && variable.Permission != NetworkWritePermission.Owner) continue;

                var writer = IsReliable(variable.Delivery) ? _reliable : _unreliable;
                writer.Write((byte)b);
                writer.Write((byte)v);
                variable.Write(writer);
            }
        }
    }

    private void SendVariables(NetworkObject networkObject, NetWriter writer, DeliveryMode delivery)
    {
        var message = new VariablesMessage { NetId = networkObject.NetId, Payload = writer.ToArray() };

        if (_network.IsServer)
            _network.SendToClients(message, delivery);
        else
            _network.SendToServer(message, delivery);
    }

    private void OnVariablesMessage(int sender, VariablesMessage message)
    {
        if (!_network.Spawner.TryGet(message.NetId, out var networkObject)) return;

        bool fromOwner = _network.IsServer;
        if (fromOwner && networkObject.OwnerPeer != sender) return;

        _reader.Set(message.Payload, 0, message.Payload.Length);
        bool hasReliable = false;

        try
        {
            while (!_reader.EndOfData)
            {
                var behaviour = networkObject.GetBehaviour(_reader.ReadByte());
                byte index = _reader.ReadByte();
                if (behaviour == null || index >= behaviour.Variables.Count) return;

                var variable = behaviour.Variables[index];
                if (fromOwner && variable.Permission != NetworkWritePermission.Owner) return;

                hasReliable |= IsReliable(variable.Delivery);
                variable.Read(_reader);
            }
        }
        catch (Exception ex)
        {
            Debug.Error($"[Replication] Bad variables message for object {message.NetId} from peer {sender}: {ex.Message}");
            return;
        }

        if (fromOwner) _network.SendToClients(message, hasReliable ? DeliveryMode.Reliable : DeliveryMode.Sequenced, sender);
    }

    private static bool IsReliable(DeliveryMode delivery)
    {
        return delivery == DeliveryMode.Reliable || delivery == DeliveryMode.ReliableUnordered;
    }
}
