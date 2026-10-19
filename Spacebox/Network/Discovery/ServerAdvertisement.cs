namespace Spacebox.Network.Discovery;

public sealed class ServerAdvertisement
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string IconPath { get; init; } = "";
    public bool LanDiscovery { get; init; } = true;
    public int MaxPlayers { get; init; }
    public bool IsDedicated { get; init; }
}
