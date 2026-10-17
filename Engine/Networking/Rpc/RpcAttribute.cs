namespace Engine.Networking;

public enum RpcTarget : byte
{
    Server,
    Owner,
    Clients,
    ClientsExceptOwner,
    All
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class RpcAttribute : Attribute
{
    public RpcTarget Target { get; }
    public DeliveryMode Delivery { get; set; } = DeliveryMode.Reliable;
    public bool RequireOwnership { get; set; } = true;

    public RpcAttribute(RpcTarget target)
    {
        Target = target;
    }
}
