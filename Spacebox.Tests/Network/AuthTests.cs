using System.Text.Json;
using Engine.Networking;
using Spacebox.Network;
using Spacebox.Network.Auth;
using Spacebox.Network.Dedicated;
using Spacebox.Network.Messages;

namespace Spacebox.Tests.Network;

public class AuthTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "spacebox_auth_" + Guid.NewGuid().ToString("N"));

    public AuthTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        ClientAuth.Reset();
        ClientAuth.StoreFactory = () => new SavedCredentialStore();
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }

    private sealed class FakeServer
    {
        public readonly List<AuthChallengeMessage> Challenges = new List<AuthChallengeMessage>();
        public readonly List<string> Rejections = new List<string>();
        public readonly List<string> Accepted = new List<string>();
        public readonly ServerAuthService Service;
        public readonly AccountStore Accounts;

        public FakeServer(string path)
        {
            Accounts = new AccountStore(path);
            Service = new ServerAuthService(Accounts, (_, c) => Challenges.Add(c), (_, r) => Rejections.Add(r), (_, j) => Accepted.Add(j.Name));
        }

        public AuthChallengeMessage Last => Challenges[^1];
    }

    private string AccountsPath => Path.Combine(_folder, AccountStore.FileName);

    private static AuthResponseMessage Login(AuthChallengeMessage challenge, string password, bool register)
    {
        var key = PasswordCrypto.DeriveKey(password, challenge.Salt, challenge.Iterations);
        return new AuthResponseMessage { Key = register ? key : Array.Empty<byte>(), Proof = PasswordCrypto.ComputeProof(key, challenge.Nonce) };
    }

    private static JoinRequestMessage Join(string name) => new JoinRequestMessage { Name = name };

    [Fact]
    public void AdminNames_CanOnlyBeRegisteredFromTheLocalNetwork()
    {
        var rejections = new List<string>();
        var challenges = new List<AuthChallengeMessage>();
        var addresses = new Dictionary<int, string> { [2] = "85.10.20.30", [3] = "192.168.1.7" };
        var service = new ServerAuthService(new AccountStore(AccountsPath), (_, c) => challenges.Add(c), (_, r) => rejections.Add(r), (_, _) => { },
            peer => addresses[peer], isReservedName: name => name == "Boss");

        service.Begin(2, Join("Boss"));
        Assert.Equal(new[] { ServerAuthService.ReservedName }, rejections);

        service.Begin(3, Join("Boss"));
        service.Begin(2, Join("Guest"));
        Assert.Equal(2, challenges.Count);
        Assert.Single(rejections);
    }

    [Fact]
    public void DeriveKey_IsDeterministicAndSaltDependent()
    {
        var salt = PasswordCrypto.NewSalt();

        Assert.Equal(PasswordCrypto.DeriveKey("secret", salt, 10_000), PasswordCrypto.DeriveKey("secret", salt, 10_000));
        Assert.NotEqual(PasswordCrypto.DeriveKey("secret", salt, 10_000), PasswordCrypto.DeriveKey("secret", PasswordCrypto.NewSalt(), 10_000));
        Assert.Equal(PasswordCrypto.KeySize, PasswordCrypto.DeriveKey("secret", salt, 10_000).Length);
    }

    [Fact]
    public void Proof_VerifiesOnlyWithSameKeyAndNonce()
    {
        var key = PasswordCrypto.DeriveKey("secret", PasswordCrypto.NewSalt(), 10_000);
        var nonce = PasswordCrypto.NewNonce();
        var proof = PasswordCrypto.ComputeProof(key, nonce);

        Assert.True(PasswordCrypto.VerifyProof(key, nonce, proof));
        Assert.False(PasswordCrypto.VerifyProof(key, PasswordCrypto.NewNonce(), proof));
        Assert.False(PasswordCrypto.VerifyProof(PasswordCrypto.DeriveKey("other", PasswordCrypto.NewSalt(), 10_000), nonce, proof));
        Assert.False(PasswordCrypto.VerifyProof(key, nonce, null));
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("abc", false)]
    [InlineData("abcd", true)]
    public void PasswordRules_RequireMinimumLength(string password, bool valid)
    {
        Assert.Equal(valid, PasswordCrypto.TryValidatePassword(password, out _));
    }

    [Fact]
    public void AccountStore_PersistsWithoutPlainPassword()
    {
        var salt = PasswordCrypto.NewSalt();
        var key = PasswordCrypto.DeriveKey("hunter22", salt, 10_000);
        var store = new AccountStore(AccountsPath);

        Assert.True(store.Register("Alice", salt, key, 10_000));
        Assert.False(store.Register("alice", salt, key, 10_000));

        var reloaded = new AccountStore(AccountsPath);
        Assert.True(reloaded.TryGet("ALICE", out var account));
        Assert.Equal(key, account.GetKey());
        Assert.DoesNotContain("hunter22", File.ReadAllText(AccountsPath));

        Assert.True(reloaded.Remove("alice"));
        Assert.False(new AccountStore(AccountsPath).TryGet("alice", out _));
    }

    [Fact]
    public void Whitelist_DisabledAllowsEveryone()
    {
        Assert.True(ServerAccessSettings.Open.IsAllowed("anyone"));
        Assert.True(new ServerAccessSettings(false, new[] { "bob" }, false).IsAllowed("alice"));
    }

    [Fact]
    public void Whitelist_EnabledAllowsOnlyListedNames()
    {
        var access = new ServerAccessSettings(true, new[] { " Bob ", "" }, false);

        Assert.True(access.IsAllowed("bob"));
        Assert.False(access.IsAllowed("alice"));
        Assert.False(access.IsAllowed(""));
    }

    [Fact]
    public void Server_RegistersNewPlayer()
    {
        var server = new FakeServer(AccountsPath);

        server.Service.Begin(2, Join("Alice"));
        Assert.False(server.Last.IsRegistered);

        server.Service.OnResponse(2, Login(server.Last, "password1", true));

        Assert.Equal(new[] { "Alice" }, server.Accepted);
        Assert.True(server.Accounts.TryGet("Alice", out _));
        Assert.Equal(0, server.Service.PendingCount);
    }

    [Fact]
    public void Server_LogsInRegisteredPlayer()
    {
        var server = new FakeServer(AccountsPath);
        server.Service.Begin(2, Join("Alice"));
        server.Service.OnResponse(2, Login(server.Last, "password1", true));

        server.Service.Begin(3, Join("Alice"));
        Assert.True(server.Last.IsRegistered);
        server.Service.OnResponse(3, Login(server.Last, "password1", false));

        Assert.Equal(2, server.Accepted.Count);
    }

    [Fact]
    public void Server_RepeatedJoinRequestDoesNotResetAttempts()
    {
        var server = new FakeServer(AccountsPath);
        server.Service.Begin(2, Join("Alice"));
        server.Service.OnResponse(2, Login(server.Last, "password1", true));

        server.Service.Begin(3, Join("Alice"));
        for (int i = 0; i < ServerAuthService.MaxAttempts - 1; i++)
            server.Service.OnResponse(3, Login(server.Last, "wrong", false));

        int challenges = server.Challenges.Count;
        server.Service.Begin(3, Join("Bob"));
        Assert.Equal(challenges, server.Challenges.Count);

        server.Service.OnResponse(3, Login(server.Last, "wrong", false));
        Assert.Equal(new[] { ServerAuthService.TooManyAttempts }, server.Rejections);
    }

    [Fact]
    public void Server_WrongPasswordRetriesThenRejects()
    {
        var server = new FakeServer(AccountsPath);
        server.Service.Begin(2, Join("Alice"));
        server.Service.OnResponse(2, Login(server.Last, "password1", true));

        server.Service.Begin(3, Join("Alice"));
        for (int i = 0; i < ServerAuthService.MaxAttempts; i++)
            server.Service.OnResponse(3, Login(server.Last, "wrong", false));

        Assert.Single(server.Accepted);
        Assert.Equal(ServerAuthService.WrongPassword, server.Challenges[^1].Error);
        Assert.Equal(new[] { ServerAuthService.TooManyAttempts }, server.Rejections);
    }

    [Fact]
    public void Server_ReplayedProofIsRejected()
    {
        var server = new FakeServer(AccountsPath);
        server.Service.Begin(2, Join("Alice"));
        server.Service.OnResponse(2, Login(server.Last, "password1", true));

        server.Service.Begin(3, Join("Alice"));
        var captured = Login(server.Last, "password1", false);
        server.Service.Begin(4, Join("Alice"));
        server.Service.OnResponse(4, captured);

        Assert.Single(server.Accepted);
    }

    [Fact]
    public void Server_RegistrationWithoutValidKeyIsRejected()
    {
        var server = new FakeServer(AccountsPath);
        server.Service.Begin(2, Join("Alice"));

        server.Service.OnResponse(2, new AuthResponseMessage { Key = new byte[3], Proof = new byte[32] });

        Assert.Empty(server.Accepted);
        Assert.Equal(new[] { ServerAuthService.InvalidAccount }, server.Rejections);
        Assert.False(server.Accounts.TryGet("Alice", out _));
    }

    [Fact]
    public void Server_PendingLoginTimesOut()
    {
        var server = new FakeServer(AccountsPath);
        server.Service.Begin(2, Join("Alice"));
        Assert.True(server.Service.IsPending("alice"));

        server.Service.Update(ServerAuthService.TimeoutSeconds + 1f);

        Assert.Equal(new[] { ServerAuthService.TimedOut }, server.Rejections);
        Assert.False(server.Service.IsPending("alice"));
    }

    [Fact]
    public void Client_SavedPasswordJoinsAutomatically()
    {
        var credentialsPath = Path.Combine(_folder, "credentials.json");
        ClientAuth.StoreFactory = () => new SavedCredentialStore(credentialsPath);
        GameSession.SetPlayer("Alice", "Red");
        var server = new FakeServer(AccountsPath);

        AuthResponseMessage sent = default;
        ClientAuth.Begin("host:7777", response => sent = response);
        server.Service.Begin(2, Join("Alice"));
        ClientAuth.HandleChallenge(server.Last);
        Assert.True(ClientAuth.IsAwaitingPassword);
        Assert.True(ClientAuth.Submit("password1", "password1", true, out _));
        server.Service.OnResponse(2, sent);
        ClientAuth.OnJoinAccepted();

        ClientAuth.Begin("host:7777", response => sent = response);
        server.Service.Begin(3, Join("Alice"));
        ClientAuth.HandleChallenge(server.Last);
        Assert.False(ClientAuth.IsAwaitingPassword);
        server.Service.OnResponse(3, sent);

        Assert.Equal(2, server.Accepted.Count);
        Assert.DoesNotContain("password1", File.ReadAllText(credentialsPath));
    }

    [Fact]
    public void Client_WrongSavedPasswordAsksAgainAndForgetsIt()
    {
        var credentialsPath = Path.Combine(_folder, "credentials.json");
        ClientAuth.StoreFactory = () => new SavedCredentialStore(credentialsPath);
        GameSession.SetPlayer("Alice", "Red");
        var server = new FakeServer(AccountsPath);
        server.Service.Begin(2, Join("Alice"));
        server.Service.OnResponse(2, Login(server.Last, "password1", true));
        new SavedCredentialStore(credentialsPath).Save("host:7777", "Alice", server.Last.Salt, PasswordCrypto.DeriveKey("old-pass", server.Last.Salt, server.Last.Iterations));

        AuthResponseMessage sent = default;
        ClientAuth.Begin("host:7777", response => sent = response);
        server.Service.Begin(3, Join("Alice"));
        ClientAuth.HandleChallenge(server.Last);
        Assert.False(ClientAuth.IsAwaitingPassword);
        server.Service.OnResponse(3, sent);
        ClientAuth.HandleChallenge(server.Last);

        Assert.True(ClientAuth.IsAwaitingPassword);
        Assert.Equal(ServerAuthService.WrongPassword, ClientAuth.Error);
        Assert.False(new SavedCredentialStore(credentialsPath).TryGetKey("host:7777", "Alice", server.Last.Salt, out _));
    }

    [Fact]
    public void Client_RegistrationNeedsMatchingPasswords()
    {
        ClientAuth.StoreFactory = () => new SavedCredentialStore(Path.Combine(_folder, "credentials.json"));
        var server = new FakeServer(AccountsPath);
        ClientAuth.Begin("host:7777", _ => { });
        server.Service.Begin(2, Join("Alice"));
        ClientAuth.HandleChallenge(server.Last);

        Assert.False(ClientAuth.Submit("password1", "password2", false, out var error));
        Assert.NotNull(error);
        Assert.False(ClientAuth.Submit("abc", "abc", false, out _));
        Assert.True(ClientAuth.IsAwaitingPassword);
    }

    [Fact]
    public void Messages_RoundTrip()
    {
        var challenge = new AuthChallengeMessage { IsRegistered = true, Salt = new byte[] { 1, 2 }, Iterations = 12345, Nonce = new byte[] { 3 }, Error = "x" };
        var writer = new NetWriter();
        challenge.Serialize(writer);
        var copy = new AuthChallengeMessage();
        copy.Deserialize(new NetReader(writer.ToArray()));

        Assert.True(copy.IsRegistered);
        Assert.Equal(challenge.Salt, copy.Salt);
        Assert.Equal(12345, copy.Iterations);
        Assert.Equal(challenge.Nonce, copy.Nonce);
        Assert.Equal("x", copy.Error);
    }

    [Fact]
    public void ServerConfig_OldFileKeepsAccessDisabled()
    {
        var config = JsonSerializer.Deserialize<ServerConfig>("{\"ServerName\":\"Old\",\"Port\":7777}");

        Assert.False(config.WhitelistEnabled);
        Assert.Empty(config.Whitelist);
        Assert.False(config.RegistrationEnabled);
    }

    [Fact]
    public void ServerConfig_NewFileUsesRegistrationAndCreative()
    {
        var config = ServerConfig.CreateNew();

        Assert.True(config.RegistrationEnabled);
        Assert.Equal((byte)Spacebox.Game.Player.GameModes.GameMode.Creative, config.GameMode);
        Assert.False(string.IsNullOrEmpty(config.Seed));
    }

    [Fact]
    public void Login_IsLockedAfterRepeatedFailuresAcrossConnections()
    {
        var server = new FakeServer(AccountsPath);
        server.Service.Begin(1, Join("Alice"));
        server.Service.OnResponse(1, Login(server.Last, "hunter22", true));
        Assert.Single(server.Accepted);

        for (int peer = 2; peer <= 3; peer++)
        {
            server.Service.Begin(peer, Join("Alice"));
            for (int i = 0; i < ServerAuthService.MaxAttempts && server.Service.PendingCount > 0; i++)
                server.Service.OnResponse(peer, Login(server.Last, "wrong", false));
        }

        server.Service.Begin(9, Join("alice"));
        Assert.Equal(ServerAuthService.LockedOut, server.Rejections[^1]);
        Assert.Equal(0, server.Service.PendingCount);
    }

    [Fact]
    public void Login_PendingNameDoesNotBlockTheRealPlayer()
    {
        var server = new FakeServer(AccountsPath);
        server.Service.Begin(1, Join("Alice"));
        server.Service.OnResponse(1, Login(server.Last, "hunter22", true));

        server.Service.Begin(2, Join("Alice"));
        server.Service.Begin(3, Join("Alice"));
        server.Service.OnResponse(3, Login(server.Last, "hunter22", false));

        Assert.Equal(new[] { "Alice", "Alice" }, server.Accepted);
        Assert.Contains(ServerAuthService.NameTaken, server.Rejections);
        Assert.Equal(0, server.Service.PendingCount);
    }
}
