
using Lidgren.Network;

namespace Spacebox.Client
{
    public class LocalServerFinder
    {
        public static List<ServerInfo> DiscoverServers(string appKey, int port, int timeoutMilliseconds)
        {
            var servers = new List<ServerInfo>();
            var config = new NetPeerConfiguration(appKey);
            config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
            var client = new NetClient(config);
            client.Start();
            client.DiscoverLocalPeers(port);
            var deadline = DateTime.Now.AddMilliseconds(timeoutMilliseconds);
            while (DateTime.Now < deadline)
            {
                NetIncomingMessage msg;
                while ((msg = client.ReadMessage()) != null)
                {
                    if (msg.MessageType == NetIncomingMessageType.DiscoveryResponse)
                    {
                        var name = msg.ReadString();
                        var ip = msg.SenderEndPoint.Address.ToString();
                        var serverPort = msg.ReadInt32();
                       
                        servers.Add(new ServerInfo { Name = name, IP = ip, Port = serverPort });
                    }
                    client.Recycle(msg);
                }
                Thread.Sleep(10);
            }
            client.Shutdown("Discovery complete");
            return servers;
        }
    }
}
