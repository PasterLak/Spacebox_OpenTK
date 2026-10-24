using System.Net;
using Engine.Networking.Query;
using Spacebox.Game.GUI.Menu;
using Spacebox.Network;
using Spacebox.Network.Discovery;

namespace Spacebox.Tests.Network;

public class ServerDiscoveryTests
{
    private sealed class FakeServerNetwork
    {
        private readonly Dictionary<IPEndPoint, ServerQueryResponder> _servers = new Dictionary<IPEndPoint, ServerQueryResponder>();

        public void Add(IPEndPoint endPoint, ServerStatus status, byte[] icon = null)
        {
            _servers[endPoint] = new ServerQueryResponder { InfoProvider = status.Serialize, BlobProvider = () => icon };
        }

        public FakeSocket CreateSocket() => new FakeSocket(this);

        public sealed class FakeSocket : IQuerySocket
        {
            private readonly FakeServerNetwork _network;
            private readonly Queue<(IPEndPoint, byte[])> _inbox = new Queue<(IPEndPoint, byte[])>();
            private static readonly IPEndPoint Self = new IPEndPoint(IPAddress.Parse("192.168.1.50"), 40000);

            public FakeSocket(FakeServerNetwork network) => _network = network;

            public event Action<IPEndPoint, byte[], int, int> Received;

            public bool Start() => true;

            public void Send(IPEndPoint endPoint, byte[] data, int offset, int length)
            {
                if (_network._servers.TryGetValue(endPoint, out var server)) Deliver(endPoint, server, data, offset, length, false);
            }

            public void Broadcast(int port, byte[] data, int offset, int length)
            {
                foreach (var pair in _network._servers)
                    if (pair.Key.Port == port) Deliver(pair.Key, pair.Value, data, offset, length, true);
            }

            public void Poll()
            {
                while (_inbox.Count > 0)
                {
                    var (sender, data) = _inbox.Dequeue();
                    Received?.Invoke(sender, data, 0, data.Length);
                }
            }

            public void Dispose() { }

            private void Deliver(IPEndPoint serverEndPoint, ServerQueryResponder server, byte[] data, int offset, int length, bool broadcast)
            {
                var copy = data.AsSpan(offset, length).ToArray();
                server.Handle(Self, copy, 0, copy.Length, broadcast, (_, reply, replyOffset, replyLength) =>
                    _inbox.Enqueue((serverEndPoint, reply.AsSpan(replyOffset, replyLength).ToArray())));
            }
        }
    }

    private static readonly IPEndPoint Saved = new IPEndPoint(IPAddress.Parse("10.0.0.1"), 7777);
    private static readonly IPEndPoint Lan = new IPEndPoint(IPAddress.Parse("192.168.1.20"), 7777);

    private double _now;

    private static ServerStatus Status(long id, string name = "Server", string description = "Hello") => new ServerStatus
    {
        InstanceId = id,
        Name = name,
        Description = description,
        GameVersion = Engine.Application.Version,
        ModId = "default",
        WorldName = "World",
        GameMode = 1,
        Players = 2,
        MaxPlayers = 8,
        IsDedicated = true
    };

    private ServerBrowser CreateBrowser(FakeServerNetwork network) => new ServerBrowser(network.CreateSocket(), () => _now);

    [Fact]
    public void ServerStatus_RoundTrips()
    {
        var status = Status(42, "Name", "Line 1\nLine 2");

        Assert.True(ServerStatus.TryDeserialize(status.Serialize(), out var copy));
        Assert.Equal(42, copy.InstanceId);
        Assert.Equal("Name", copy.Name);
        Assert.Equal("Line 1\nLine 2", copy.Description);
        Assert.Equal(Engine.Application.Version, copy.GameVersion);
        Assert.Equal("default", copy.ModId);
        Assert.Equal("World", copy.WorldName);
        Assert.Equal(1, copy.GameMode);
        Assert.Equal(2, copy.Players);
        Assert.Equal(8, copy.MaxPlayers);
        Assert.True(copy.IsDedicated);
    }

    [Fact]
    public void ServerStatus_LongTextFitsInfoLimit()
    {
        var status = Status(1, new string('N', 500), new string('ж', 5000));

        var bytes = status.Serialize();

        Assert.True(bytes.Length <= ServerQueryProtocol.MaxInfoSize);
        Assert.True(ServerStatus.TryDeserialize(bytes, out var copy));
        Assert.Equal(ServerStatus.MaxNameLength, copy.Name.Length);
    }

    [Fact]
    public void ServerStatus_RejectsGarbage()
    {
        Assert.False(ServerStatus.TryDeserialize(null, out _));
        Assert.False(ServerStatus.TryDeserialize(new byte[] { 99, 1, 2 }, out _));
        Assert.False(ServerStatus.TryDeserialize(new byte[] { ServerStatus.FormatVersion, 1 }, out _));
    }

    [Fact]
    public void Browser_PingsKnownLocalServerOutsideBroadcastRange()
    {
        var known = new IPEndPoint(IPAddress.Parse("192.168.1.30"), 7790);
        var network = new FakeServerNetwork();
        network.Add(known, Status(9, "Known"));
        var browser = CreateBrowser(network);
        browser.SetSavedServers(new List<ServerEntry>());
        browser.KnownLocal = new LocalServerHistory(new List<string> { "192.168.1.30:7790" });

        browser.Refresh();
        browser.Update(0.01f);

        Assert.Single(browser.Local);
        Assert.Equal("Known", browser.Local[0].DisplayName);
    }

    [Fact]
    public void Browser_RemembersDiscoveredLanServer()
    {
        var network = new FakeServerNetwork();
        network.Add(Lan, Status(7));
        var browser = CreateBrowser(network);
        browser.SetSavedServers(new List<ServerEntry>());
        var addresses = new List<string>();
        browser.KnownLocal = new LocalServerHistory(addresses);
        int changes = 0;
        browser.KnownLocalChanged += () => changes++;

        browser.Refresh();
        browser.Update(0.01f);
        browser.Refresh();
        browser.Update(0.01f);

        Assert.Equal(new[] { "192.168.1.20:7777" }, addresses);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void LocalServerHistory_MovesToFrontAndLimitsSize()
    {
        var addresses = new List<string> { "bad", "10.0.0.1:7777" };
        var history = new LocalServerHistory(addresses);
        Assert.Equal(new[] { "10.0.0.1:7777" }, addresses);

        for (int i = 0; i < LocalServerHistory.MaxEntries + 4; i++)
            history.Remember(new IPEndPoint(IPAddress.Parse("10.0.1." + i), 7777));
        Assert.Equal(LocalServerHistory.MaxEntries, addresses.Count);

        Assert.True(history.Remember(new IPEndPoint(IPAddress.Parse("10.0.1.5"), 7777)));
        Assert.Equal("10.0.1.5:7777", addresses[0]);
        Assert.False(history.Remember(new IPEndPoint(IPAddress.Parse("10.0.1.5"), 7777)));
        Assert.False(history.Remember(new IPEndPoint(IPAddress.Loopback, 7777)));
    }

    [Fact]
    public void IconFile_AcceptsOnlySmallPng()
    {
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0 };
        var big = new byte[ServerIconFile.MaxBytes + 1];
        png.CopyTo(big, 0);

        Assert.True(ServerIconFile.IsValid(png));
        Assert.False(ServerIconFile.IsValid(big));
        Assert.False(ServerIconFile.IsValid(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
        Assert.False(ServerIconFile.IsValid(null));
    }

    [Fact]
    public void IconFile_AcceptsJpeg()
    {
        var jpeg = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0 };

        Assert.True(ServerIconFile.IsJpeg(jpeg));
        Assert.True(ServerIconFile.IsValid(jpeg));
        Assert.False(ServerIconFile.IsPng(jpeg));
    }

    [Fact]
    public void IconFile_ResolvesJpegWhenPngIsMissing()
    {
        string folder = Path.Combine(Path.GetTempPath(), "spacebox_icon_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            string jpg = Path.Combine(folder, "server-icon.jpg");
            File.WriteAllBytes(jpg, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0 });

            Assert.Equal(jpg, ServerIconFile.ResolvePath(Path.Combine(folder, "server-icon.png")));
            Assert.NotNull(ServerIconFile.TryRead(Path.Combine(folder, "server-icon.png")));
            Assert.Null(ServerIconFile.ResolvePath(Path.Combine(folder, "missing.png")));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void IconHash_FormatAndParseRoundTrip()
    {
        Assert.Equal("", ServerIconHash.Format(0));
        Assert.Equal(0UL, ServerIconHash.Parse(""));
        Assert.Equal(0UL, ServerIconHash.Parse("not hex"));
        Assert.Equal(0xABCDEF0123456789UL, ServerIconHash.Parse(ServerIconHash.Format(0xABCDEF0123456789UL)));
    }

    [Theory]
    [InlineData("127.0.0.1", 7777, true)]
    [InlineData("play.example.com", 1, true)]
    [InlineData("", 7777, false)]
    [InlineData("a b", 7777, false)]
    [InlineData("127.0.0.1", 0, false)]
    [InlineData("127.0.0.1", 70000, false)]
    public void AddressValidator_ChecksHostAndPort(string host, int port, bool expected)
    {
        Assert.Equal(expected, ServerAddressValidator.TryValidate(host, port, out var error));
        Assert.Equal(expected, error == null);
    }

    [Fact]
    public void AddressValidator_UsesHostWhenNameEmpty()
    {
        Assert.Equal("10.0.0.1", ServerAddressValidator.ResolveName("  ", " 10.0.0.1 "));
        Assert.Equal("My server", ServerAddressValidator.ResolveName(" My server ", "10.0.0.1"));
        Assert.Equal(ServerAddressValidator.MaxNameLength, ServerAddressValidator.ResolveName(new string('x', 100), "h").Length);
    }

    [Fact]
    public void Browser_SavedServerGoesOnline()
    {
        var network = new FakeServerNetwork();
        network.Add(Saved, Status(1, "Remote", "Welcome"));
        var browser = CreateBrowser(network);
        var entry = new ServerEntry { Name = "Mine", IP = "10.0.0.1", Port = 7777 };
        browser.SetSavedServers(new List<ServerEntry> { entry });
        int changes = 0;
        browser.SavedEntriesChanged += () => changes++;

        browser.Refresh();
        Assert.Equal(ServerOnlineState.Pinging, browser.Saved[0].State);
        browser.Update(0.01f);

        var item = browser.Saved[0];
        Assert.Equal(ServerOnlineState.Online, item.State);
        Assert.Equal("Mine", item.DisplayName);
        Assert.Equal(2, item.Status.Players);
        Assert.Equal("Welcome", entry.Description);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void Browser_SavedServerWithoutAnswerGoesOffline()
    {
        var browser = CreateBrowser(new FakeServerNetwork());
        browser.SetSavedServers(new List<ServerEntry> { new ServerEntry { IP = "10.0.0.3", Port = 7777 } });

        browser.Refresh();
        _now = 10;
        browser.Update(0.01f);

        Assert.Equal(ServerOnlineState.Offline, browser.Saved[0].State);
        Assert.Null(browser.Saved[0].Status);
    }

    [Fact]
    public void Browser_DiscoversLanServerOnce()
    {
        var network = new FakeServerNetwork();
        network.Add(Lan, Status(7, "Lan game"));
        var browser = CreateBrowser(network);
        browser.SetSavedServers(new List<ServerEntry>());

        browser.Refresh();
        browser.Update(0.01f);
        browser.Refresh();
        browser.Update(0.01f);

        Assert.Single(browser.Local);
        var item = browser.Local[0];
        Assert.False(item.IsSaved);
        Assert.Equal("Lan game", item.DisplayName);
        Assert.Equal("192.168.1.20", item.Entry.IP);
        Assert.Equal(7777, item.Entry.Port);
    }

    [Fact]
    public void Browser_DiscoversLanServerOnAnotherPortOfTheScannedRange()
    {
        var endPoint = new IPEndPoint(IPAddress.Parse("192.168.1.21"), Engine.Networking.NetworkManager.DefaultPort + 3);
        var network = new FakeServerNetwork();
        network.Add(endPoint, Status(11, "Second server"));
        var browser = CreateBrowser(network);
        browser.SetSavedServers(new List<ServerEntry>());

        browser.Refresh();
        browser.Update(0.01f);

        Assert.Single(browser.Local);
        Assert.Equal(endPoint.Port, browser.Local[0].Entry.Port);
    }

    [Fact]
    public void LanPorts_TryThePreferredPortFirstThenTheScannedRange()
    {
        int first = Engine.Networking.NetworkManager.DefaultPort;
        var candidates = LanPorts.Candidates(first + 2);

        Assert.Equal(first + 2, candidates[0]);
        Assert.Equal(LanPorts.Count, candidates.Count);
        Assert.Equal(candidates.Count, candidates.Distinct().Count());
        Assert.True(LanPorts.IsScanned(first + LanPorts.Count - 1));
        Assert.False(LanPorts.IsScanned(first + LanPorts.Count));
        Assert.Equal(25000, LanPorts.Candidates(25000)[0]);
        Assert.Equal(LanPorts.Count + 1, LanPorts.Candidates(25000).Count);
    }

    [Fact]
    public void JoinPrecheck_RefusesOtherGameVersions()
    {
        var status = Status(1);

        Assert.Null(JoinPrecheck.Check(status, Engine.Application.Version));
        Assert.Null(JoinPrecheck.Check(null, Engine.Application.Version));

        status.GameVersion = "0.0.1";
        string error = JoinPrecheck.Check(status, Engine.Application.Version);
        Assert.Contains("0.0.1", error);
        Assert.Contains(Engine.Application.Version, error);
    }

    [Fact]
    public void Browser_HidesLanServerThatIsAlreadySaved()
    {
        var network = new FakeServerNetwork();
        network.Add(Lan, Status(7));
        var browser = CreateBrowser(network);
        browser.SetSavedServers(new List<ServerEntry> { new ServerEntry { IP = "192.168.1.20", Port = 7777 } });

        browser.Refresh();
        browser.Update(0.01f);

        Assert.Single(browser.Local);
        Assert.True(browser.IsHiddenLocal(browser.Local[0]));
    }

    [Fact]
    public void Browser_RemovesLanServerThatStoppedAnswering()
    {
        var network = new FakeServerNetwork();
        network.Add(Lan, Status(7));
        var browser = CreateBrowser(network);
        browser.SetSavedServers(new List<ServerEntry>());
        browser.Refresh();
        browser.Update(0.01f);

        browser.Update(ServerBrowser.LocalServerLifetime + 1f);

        Assert.Empty(browser.Local);
    }

    [Fact]
    public void Browser_DownloadsIconAndCachesHash()
    {
        var icon = new byte[3000];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(icon, 0);
        var network = new FakeServerNetwork();
        network.Add(Saved, Status(1), icon);
        var browser = CreateBrowser(network);
        var entry = new ServerEntry { IP = "10.0.0.1", Port = 7777 };
        browser.SetSavedServers(new List<ServerEntry> { entry });
        byte[] received = null;
        browser.IconReceived += (_, data) => received = data;

        browser.Refresh();
        browser.Update(0.01f);
        browser.Update(0.01f);

        ulong hash = ServerQueryProtocol.ComputeHash(icon);
        Assert.Equal(icon, received);
        Assert.Equal(hash, browser.Saved[0].IconHash);
        Assert.Equal(ServerIconHash.Format(hash), entry.IconHash);
    }

    [Fact]
    public void Browser_SkipsIconAlreadyCached()
    {
        var icon = new byte[100];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(icon, 0);
        var network = new FakeServerNetwork();
        network.Add(Saved, Status(1), icon);
        var browser = CreateBrowser(network);
        browser.HasIcon = _ => true;
        browser.SetSavedServers(new List<ServerEntry> { new ServerEntry { IP = "10.0.0.1", Port = 7777 } });
        int received = 0;
        browser.IconReceived += (_, _) => received++;

        browser.Refresh();
        browser.Update(0.01f);
        browser.Update(0.01f);

        Assert.Equal(0, received);
    }

    [Fact]
    public void Browser_SetSavedServersKeepsExistingState()
    {
        var network = new FakeServerNetwork();
        network.Add(Saved, Status(1));
        var browser = CreateBrowser(network);
        var entry = new ServerEntry { IP = "10.0.0.1", Port = 7777 };
        var list = new List<ServerEntry> { entry };
        browser.SetSavedServers(list);
        browser.Refresh();
        browser.Update(0.01f);
        var item = browser.Saved[0];

        list.Add(new ServerEntry { IP = "10.0.0.2" });
        browser.SetSavedServers(list);

        Assert.Same(item, browser.Saved[0]);
        Assert.Equal(2, browser.Saved.Count);
    }

    [Fact]
    public void ServerText_FormatsStateAndPlayers()
    {
        var item = new ServerListItem("k", new ServerEntry { IP = "1.2.3.4", Port = 5 }, false);
        Assert.Equal("-", ServerText.StateLabel(item));
        Assert.Equal("? players", ServerText.PlayersLabel(item));
        Assert.Equal("1.2.3.4:5  (LAN)", ServerText.AddressLine(item));
        Assert.Equal("first", ServerText.FirstLine("first\r\nsecond"));
        Assert.Equal("Creative", ServerText.GameModeLabel(1));
        Assert.Equal("Unknown", ServerText.GameModeLabel(200));
    }

    [Theory]
    [InlineData(ServerOnlineState.Offline, 10, 0)]
    [InlineData(ServerOnlineState.Online, 10, 4)]
    [InlineData(ServerOnlineState.Online, 100, 3)]
    [InlineData(ServerOnlineState.Online, 200, 2)]
    [InlineData(ServerOnlineState.Online, 500, 1)]
    public void SignalBars_DependOnPing(ServerOnlineState state, int ping, int expected)
    {
        var item = new ServerListItem("k", new ServerEntry(), true);
        typeof(ServerListItem).GetProperty(nameof(ServerListItem.State))!.SetValue(item, state);
        typeof(ServerListItem).GetProperty(nameof(ServerListItem.PingMs))!.SetValue(item, ping);

        Assert.Equal(expected, ServerSignalBars.GetLevel(item));
    }

    [Fact]
    public void ClientConfig_LoadsLegacyFileAndFillsDefaults()
    {
        string path = Path.Combine(Path.GetTempPath(), $"client_{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{\"PlayerNickname\":\"wow\",\"Servers\":[{\"Name\":\"Home\",\"Description\":null,\"ModFolderName\":null,\"MaxPlayers\":0,\"IP\":\"192.168.56.1\",\"Port\":5544}]}");
        try
        {
            var config = ClientConfigStore.Load(path);

            Assert.Equal("wow", config.PlayerNickname);
            Assert.Equal(ClientConfigStore.DefaultSkinColor, config.SkinColor);
            var server = Assert.Single(config.Servers);
            Assert.Equal("Home", server.Name);
            Assert.Equal("", server.Description);
            Assert.Equal("", server.IconHash);
            Assert.Equal(5544, server.Port);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
