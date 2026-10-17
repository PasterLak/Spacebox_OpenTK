namespace Engine.Networking;

public static class RpcContext
{
    public static int SenderPeer { get; internal set; }
    public static bool IsRemote => SenderPeer != NetworkManager.Instance.LocalPeerId;
}
