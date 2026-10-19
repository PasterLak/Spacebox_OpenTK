using System.Net;

namespace Engine.Networking.Query;

public interface IQuerySocket : IDisposable
{
    event Action<IPEndPoint, byte[], int, int> Received;

    bool Start();
    void Send(IPEndPoint endPoint, byte[] data, int offset, int length);
    void Broadcast(int port, byte[] data, int offset, int length);
    void Poll();
}
