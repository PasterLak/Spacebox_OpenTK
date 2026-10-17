namespace Spacebox.Network;

public class ServerEntry
{
    public string Name { get; set; } = "Server";
    public string Description { get; set; } = "";
    public string IP { get; set; } = "127.0.0.1";
    public int Port { get; set; } = Engine.Networking.NetworkManager.DefaultPort;
}
