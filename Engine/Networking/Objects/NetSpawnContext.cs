namespace Engine.Networking;

public readonly struct NetSpawnContext
{
    public string PrefabKey { get; }
    public uint NetId { get; }
    public int OwnerPeer { get; }
    public NetReader Data { get; }

    public bool IsOwner => OwnerPeer == NetworkManager.Instance.LocalPeerId;

    public NetSpawnContext(string prefabKey, uint netId, int ownerPeer, NetReader data)
    {
        PrefabKey = prefabKey;
        NetId = netId;
        OwnerPeer = ownerPeer;
        Data = data;
    }
}
