using Spacebox.Network.Messages;

namespace Spacebox.Network.Auth;

public static class ClientAuth
{
    private sealed class PendingSave
    {
        public string Server;
        public string Name;
        public byte[] Salt;
        public byte[] Key;
    }

    private static string _server;
    private static AuthChallengeMessage _challenge;
    private static bool _triedSaved;
    private static PendingSave _pendingSave;
    private static Action<AuthResponseMessage> _send;

    public static bool IsAwaitingPassword { get; private set; }
    public static bool IsRegistered => _challenge.IsRegistered;
    public static string Error { get; private set; }
    public static string PlayerName => GameSession.PlayerName;
    public static string Server => _server;

    public static Func<SavedCredentialStore> StoreFactory { get; set; } = () => new SavedCredentialStore();

    public static void Begin(string server, Action<AuthResponseMessage> send)
    {
        Reset();
        _server = server;
        _send = send;
    }

    public static void HandleChallenge(AuthChallengeMessage challenge)
    {
        _challenge = challenge;
        Error = string.IsNullOrEmpty(challenge.Error) ? null : challenge.Error;

        var store = StoreFactory();
        if (_triedSaved && Error != null)
            store.Remove(_server, PlayerName);

        if (!_triedSaved && challenge.IsRegistered && store.TryGetKey(_server, PlayerName, challenge.Salt, out var savedKey))
        {
            _triedSaved = true;
            Send(savedKey, false);
            return;
        }

        IsAwaitingPassword = true;
    }

    public static bool Submit(string password, string confirmation, bool savePassword, out string error)
    {
        error = null;
        if (!IsAwaitingPassword) return false;
        if (!PasswordCrypto.TryValidatePassword(password, out error)) return false;
        if (!_challenge.IsRegistered && password != confirmation)
        {
            error = "The passwords do not match";
            return false;
        }

        var key = PasswordCrypto.DeriveKey(password, _challenge.Salt, _challenge.Iterations);
        _pendingSave = savePassword ? new PendingSave { Server = _server, Name = PlayerName, Salt = _challenge.Salt, Key = key } : null;
        IsAwaitingPassword = false;
        Error = null;
        Send(key, !_challenge.IsRegistered);
        return true;
    }

    public static void OnJoinAccepted()
    {
        if (_pendingSave != null)
            StoreFactory().Save(_pendingSave.Server, _pendingSave.Name, _pendingSave.Salt, _pendingSave.Key);
        Reset();
    }

    public static void Reset()
    {
        IsAwaitingPassword = false;
        Error = null;
        _challenge = default;
        _triedSaved = false;
        _pendingSave = null;
        _send = null;
        _server = null;
    }

    private static void Send(byte[] key, bool includeKey)
    {
        _send?.Invoke(new AuthResponseMessage
        {
            Key = includeKey ? key : Array.Empty<byte>(),
            Proof = PasswordCrypto.ComputeProof(key, _challenge.Nonce)
        });
    }
}
