using Engine.Networking;

namespace Spacebox.Game.Generation.Blocks.Components;

public sealed class ComponentStateChannel
{
    private readonly INetworkStateComponent[] _components;

    public ComponentStateChannel(ComponentBlock host)
    {
        var found = new List<INetworkStateComponent>();
        for (int i = 0; i < host.ComponentCount; i++)
        {
            if (host.GetComponent(i) is INetworkStateComponent state) found.Add(state);
        }
        _components = found.ToArray();
    }

    public void Write(NetWriter writer)
    {
        for (int i = 0; i < _components.Length; i++)
            _components[i].WriteNetworkState(writer);
    }

    public void Read(NetReader reader)
    {
        for (int i = 0; i < _components.Length; i++)
            _components[i].ReadNetworkState(reader);
    }
}
