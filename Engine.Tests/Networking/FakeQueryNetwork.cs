using System.Net;
using Engine.Networking.Query;

namespace Engine.Tests.Networking;

public sealed class FakeQueryNetwork
{
    private readonly Dictionary<IPEndPoint, ServerQueryResponder> _servers = new Dictionary<IPEndPoint, ServerQueryResponder>();

    public Func<byte[], int, int, bool> DropServerPacket { get; set; }

    public void AddServer(IPEndPoint endPoint, ServerQueryResponder responder)
    {
        _servers[endPoint] = responder;
    }

    public Socket CreateClient(IPEndPoint endPoint)
    {
        return new Socket(this, endPoint);
    }

    public sealed class Socket : IQuerySocket
    {
        private readonly FakeQueryNetwork _network;
        private readonly IPEndPoint _endPoint;
        private readonly Queue<(IPEndPoint sender, byte[] data)> _inbox = new Queue<(IPEndPoint, byte[])>();

        public Socket(FakeQueryNetwork network, IPEndPoint endPoint)
        {
            _network = network;
            _endPoint = endPoint;
        }

        public event Action<IPEndPoint, byte[], int, int> Received;

        public bool Started { get; private set; }
        public int SentCount { get; private set; }

        public bool Start()
        {
            Started = true;
            return true;
        }

        public void Send(IPEndPoint endPoint, byte[] data, int offset, int length)
        {
            SentCount++;
            if (_network._servers.TryGetValue(endPoint, out var server))
                Deliver(endPoint, server, data, offset, length, false);
        }

        public void Broadcast(int port, byte[] data, int offset, int length)
        {
            SentCount++;
            foreach (var pair in _network._servers)
            {
                if (pair.Key.Port == port) Deliver(pair.Key, pair.Value, data, offset, length, true);
            }
        }

        public void Poll()
        {
            while (_inbox.Count > 0)
            {
                var packet = _inbox.Dequeue();
                Received?.Invoke(packet.sender, packet.data, 0, packet.data.Length);
            }
        }

        public void Dispose()
        {
            Started = false;
        }

        private void Deliver(IPEndPoint serverEndPoint, ServerQueryResponder server, byte[] data, int offset, int length, bool broadcast)
        {
            var copy = new byte[length];
            Buffer.BlockCopy(data, offset, copy, 0, length);
            server.Handle(_endPoint, copy, 0, copy.Length, broadcast, (target, reply, replyOffset, replyLength) =>
            {
                if (_network.DropServerPacket != null && _network.DropServerPacket(reply, replyOffset, replyLength)) return;

                var replyCopy = new byte[replyLength];
                Buffer.BlockCopy(reply, replyOffset, replyCopy, 0, replyLength);
                _inbox.Enqueue((serverEndPoint, replyCopy));
            });
        }
    }
}
