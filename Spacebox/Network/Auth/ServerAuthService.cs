using Spacebox.Network.Messages;

namespace Spacebox.Network.Auth;

public sealed class ServerAuthService
{
    public const int MaxAttempts = 3;
    public const float TimeoutSeconds = 180f;
    public const string WrongPassword = "Wrong password";
    public const string TooManyAttempts = "Too many wrong passwords";
    public const string TimedOut = "Login timed out";
    public const string InvalidAccount = "Could not create the account";
    public const string LockedOut = "Too many wrong passwords, try again in a few minutes";
    public const string NameTaken = "Name is already taken";

    private sealed class PendingLogin
    {
        public JoinRequestMessage Request;
        public byte[] Salt;
        public int Iterations;
        public byte[] Nonce;
        public bool IsRegistered;
        public int Attempts;
        public float Age;
    }

    private readonly AccountStore _accounts;
    private readonly Action<int, AuthChallengeMessage> _sendChallenge;
    private readonly Action<int, string> _reject;
    private readonly Action<int, JoinRequestMessage> _accept;
    private readonly Dictionary<int, PendingLogin> _pending = new Dictionary<int, PendingLogin>();
    private readonly List<int> _expired = new List<int>();
    private readonly Dashboard.LoginThrottle _throttle;
    private readonly Func<int, string> _addressOf;

    public ServerAuthService(AccountStore accounts, Action<int, AuthChallengeMessage> sendChallenge, Action<int, string> reject, Action<int, JoinRequestMessage> accept,
        Func<int, string> addressOf = null, Dashboard.LoginThrottle throttle = null)
    {
        _accounts = accounts;
        _sendChallenge = sendChallenge;
        _reject = reject;
        _accept = accept;
        _addressOf = addressOf;
        _throttle = throttle ?? new Dashboard.LoginThrottle();
    }

    public int PendingCount => _pending.Count;

    public bool IsPending(string name)
    {
        foreach (var login in _pending.Values)
        {
            if (string.Equals(login.Request.Name?.Trim(), name?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    public void Begin(int peer, JoinRequestMessage request)
    {
        if (_throttle.IsLocked(NameKey(request.Name)) || _throttle.IsLocked(AddressKey(peer)))
        {
            _reject(peer, LockedOut);
            return;
        }

        var login = new PendingLogin { Request = request };

        if (_accounts.TryGet(request.Name, out var account))
        {
            login.IsRegistered = true;
            login.Salt = account.GetSalt();
            login.Iterations = account.Iterations;
        }
        else
        {
            login.Salt = PasswordCrypto.NewSalt();
            login.Iterations = PasswordCrypto.DefaultIterations;
        }

        _pending[peer] = login;
        SendChallenge(peer, login, null);
    }

    public void OnResponse(int peer, AuthResponseMessage response)
    {
        if (!_pending.TryGetValue(peer, out var login)) return;

        if (login.IsRegistered ? VerifyLogin(login, response) : TryRegister(login, response))
        {
            _pending.Remove(peer);
            _throttle.Reset(NameKey(login.Request.Name));
            RejectOtherLogins(peer, login.Request.Name);
            _accept(peer, login.Request);
            return;
        }

        if (!login.IsRegistered)
        {
            _pending.Remove(peer);
            _reject(peer, InvalidAccount);
            return;
        }

        login.Attempts++;
        _throttle.RecordFailure(NameKey(login.Request.Name));
        _throttle.RecordFailure(AddressKey(peer));
        if (_throttle.IsLocked(NameKey(login.Request.Name)) || _throttle.IsLocked(AddressKey(peer)))
        {
            _pending.Remove(peer);
            _reject(peer, LockedOut);
            return;
        }

        if (login.Attempts >= MaxAttempts)
        {
            _pending.Remove(peer);
            _reject(peer, TooManyAttempts);
            return;
        }

        SendChallenge(peer, login, WrongPassword);
    }

    public bool RemoveAccount(string name) => _accounts.Remove(name);

    public void Remove(int peer)
    {
        _pending.Remove(peer);
    }

    public void Update(float deltaTime)
    {
        if (_pending.Count == 0) return;

        _expired.Clear();
        foreach (var pair in _pending)
        {
            pair.Value.Age += deltaTime;
            if (pair.Value.Age >= TimeoutSeconds) _expired.Add(pair.Key);
        }

        foreach (int peer in _expired)
        {
            _pending.Remove(peer);
            _reject(peer, TimedOut);
        }
    }

    private void RejectOtherLogins(int acceptedPeer, string name)
    {
        _expired.Clear();
        foreach (var pair in _pending)
        {
            if (pair.Key != acceptedPeer && string.Equals(pair.Value.Request.Name?.Trim(), name?.Trim(), StringComparison.OrdinalIgnoreCase))
                _expired.Add(pair.Key);
        }

        foreach (int other in _expired)
        {
            _pending.Remove(other);
            _reject(other, NameTaken);
        }
    }

    private static string NameKey(string name) => "name:" + (name ?? "").Trim().ToLowerInvariant();

    private string AddressKey(int peer) => "ip:" + (_addressOf?.Invoke(peer) ?? peer.ToString());

    private bool VerifyLogin(PendingLogin login, AuthResponseMessage response)
    {
        return _accounts.TryGet(login.Request.Name, out var account) &&
               PasswordCrypto.VerifyProof(account.GetKey(), login.Nonce, response.Proof);
    }

    private bool TryRegister(PendingLogin login, AuthResponseMessage response)
    {
        if (!PasswordCrypto.IsValidKey(response.Key) || !PasswordCrypto.VerifyProof(response.Key, login.Nonce, response.Proof)) return false;
        return _accounts.Register(login.Request.Name, login.Salt, response.Key, login.Iterations);
    }

    private void SendChallenge(int peer, PendingLogin login, string error)
    {
        login.Nonce = PasswordCrypto.NewNonce();
        _sendChallenge(peer, new AuthChallengeMessage
        {
            IsRegistered = login.IsRegistered,
            Salt = login.Salt,
            Iterations = login.Iterations,
            Nonce = login.Nonce,
            Error = error
        });
    }
}
