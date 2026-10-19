using System.Text.Json;
using Engine;

namespace Spacebox.Network.Auth;

public sealed class AccountStore
{
    public const string FileName = "accounts.json";

    private sealed class AccountFile
    {
        public List<PlayerAccount> Accounts { get; set; } = new List<PlayerAccount>();
    }

    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { WriteIndented = true };

    private readonly string _path;
    private readonly Dictionary<string, PlayerAccount> _accounts = new Dictionary<string, PlayerAccount>(StringComparer.OrdinalIgnoreCase);

    public AccountStore(string path)
    {
        _path = path;
        Load();
    }

    public int Count => _accounts.Count;

    public bool TryGet(string name, out PlayerAccount account)
    {
        account = null;
        return !string.IsNullOrWhiteSpace(name) && _accounts.TryGetValue(name.Trim(), out account);
    }

    public bool Register(string name, byte[] salt, byte[] key, int iterations)
    {
        if (string.IsNullOrWhiteSpace(name) || !PasswordCrypto.IsValidKey(key) || salt == null || salt.Length == 0) return false;

        string trimmed = name.Trim();
        if (_accounts.ContainsKey(trimmed)) return false;

        _accounts[trimmed] = new PlayerAccount
        {
            Name = trimmed,
            Salt = Convert.ToBase64String(salt),
            Key = Convert.ToBase64String(key),
            Iterations = iterations,
            Created = DateTime.UtcNow.ToString("u")
        };
        Save();
        return true;
    }

    public bool Remove(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || !_accounts.Remove(name.Trim())) return false;
        Save();
        return true;
    }

    private void Load()
    {
        if (string.IsNullOrEmpty(_path) || !File.Exists(_path)) return;

        try
        {
            var file = JsonSerializer.Deserialize<AccountFile>(File.ReadAllText(_path));
            if (file?.Accounts == null) return;

            foreach (var account in file.Accounts)
            {
                if (!string.IsNullOrWhiteSpace(account?.Name)) _accounts[account.Name.Trim()] = account;
            }
        }
        catch (Exception ex)
        {
            Debug.Error($"[Auth] Failed to read {_path}: {ex.Message}");
        }
    }

    private void Save()
    {
        if (string.IsNullOrEmpty(_path)) return;

        try
        {
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(_path, JsonSerializer.Serialize(new AccountFile { Accounts = _accounts.Values.ToList() }, Options));
        }
        catch (Exception ex)
        {
            Debug.Error($"[Auth] Failed to write {_path}: {ex.Message}");
        }
    }
}
