using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Spacebox.Network.Auth;

namespace Spacebox.Network.Dashboard;

public sealed class DashboardAdmin
{
    public string Name { get; set; } = "";
    public string Salt { get; set; } = "";
    public string Key { get; set; } = "";
    public int Iterations { get; set; } = PasswordCrypto.DefaultIterations;
    public string Created { get; set; } = "";
}

public sealed class DashboardAdminStore
{
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;
    public const int MaxNameLength = 32;
    public const int HashIterations = 600_000;

    private const string KeyAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int KeyGroups = 4;
    private const int KeyGroupLength = 5;

    private readonly string _adminFile;
    private readonly string _setupKeyFile;
    private readonly object _lock = new object();
    private DashboardAdmin _admin;

    public DashboardAdminStore(string adminFile, string setupKeyFile)
    {
        _adminFile = adminFile;
        _setupKeyFile = setupKeyFile;
        _admin = Load(adminFile);
    }

    public bool HasAdmin
    {
        get
        {
            lock (_lock) return _admin != null;
        }
    }

    public string AdminName
    {
        get
        {
            lock (_lock) return _admin?.Name;
        }
    }

    public string SetupKeyFile => _setupKeyFile;

    public string EnsureSetupKey()
    {
        lock (_lock)
        {
            if (_admin != null) return null;

            if (File.Exists(_setupKeyFile))
            {
                string existing = File.ReadAllText(_setupKeyFile).Trim();
                if (existing.Length > 0) return existing;
            }

            string key = NewSetupKey();
            PrivateFile.WriteAllText(_setupKeyFile, key);
            return key;
        }
    }

    public bool TryRegister(string setupKey, string name, string password, out string error)
    {
        lock (_lock)
        {
            if (_admin != null)
            {
                error = "An admin is already registered";
                return false;
            }

            if (!IsSetupKey(setupKey))
            {
                error = "Wrong setup key";
                return false;
            }

            if (!ValidateName(name, out error) || !ValidatePassword(password, out error)) return false;

            _admin = Create(name.Trim(), password);
            Save(_admin);
            TryDelete(_setupKeyFile);
            return true;
        }
    }

    public bool Verify(string name, string password)
    {
        DashboardAdmin admin;
        lock (_lock) admin = _admin;
        if (admin == null || name == null || password == null) return false;

        byte[] key = PasswordCrypto.DeriveKey(password, Convert.FromBase64String(admin.Salt), admin.Iterations);
        bool nameMatches = string.Equals(admin.Name, name.Trim(), StringComparison.OrdinalIgnoreCase);
        bool valid = CryptographicOperations.FixedTimeEquals(key, Convert.FromBase64String(admin.Key)) && nameMatches;
        if (valid && admin.Iterations < HashIterations) Rehash(admin, password);
        return valid;
    }

    private void Rehash(DashboardAdmin admin, string password)
    {
        lock (_lock)
        {
            if (_admin != admin) return;
            var upgraded = Create(admin.Name, password);
            upgraded.Created = admin.Created;
            _admin = upgraded;
            Save(upgraded);
        }
    }

    public bool TryChangePassword(string oldPassword, string newPassword, out string error)
    {
        string name = AdminName;
        if (!Verify(name, oldPassword))
        {
            error = "The current password is wrong";
            return false;
        }

        if (!ValidatePassword(newPassword, out error)) return false;

        lock (_lock)
        {
            _admin = Create(name, newPassword);
            Save(_admin);
        }
        return true;
    }

    public static bool ValidatePassword(string password, out string error)
    {
        error = null;
        if (password == null || password.Length < MinPasswordLength) error = $"The password needs at least {MinPasswordLength} characters";
        else if (password.Length > MaxPasswordLength) error = $"The password can have at most {MaxPasswordLength} characters";
        return error == null;
    }

    public static bool ValidateName(string name, out string error)
    {
        error = null;
        string trimmed = name?.Trim() ?? "";
        if (trimmed.Length == 0) error = "Enter a name";
        else if (trimmed.Length > MaxNameLength) error = $"The name can have at most {MaxNameLength} characters";
        return error == null;
    }

    public static string NewSetupKey()
    {
        var builder = new StringBuilder();
        for (int group = 0; group < KeyGroups; group++)
        {
            if (group > 0) builder.Append('-');
            builder.Append(RandomNumberGenerator.GetString(KeyAlphabet, KeyGroupLength));
        }
        return builder.ToString();
    }

    private bool IsSetupKey(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate) || !File.Exists(_setupKeyFile)) return false;

        var expected = Encoding.UTF8.GetBytes(Normalize(File.ReadAllText(_setupKeyFile)));
        var actual = Encoding.UTF8.GetBytes(Normalize(candidate));
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    private static string Normalize(string key) => key.Trim().Replace(" ", "").ToUpperInvariant();

    private static DashboardAdmin Create(string name, string password)
    {
        var salt = PasswordCrypto.NewSalt();
        return new DashboardAdmin
        {
            Name = name,
            Salt = Convert.ToBase64String(salt),
            Key = Convert.ToBase64String(PasswordCrypto.DeriveKey(password, salt, HashIterations)),
            Iterations = HashIterations,
            Created = DateTime.UtcNow.ToString("O")
        };
    }

    private void Save(DashboardAdmin admin)
    {
        PrivateFile.WriteAllText(_adminFile, JsonSerializer.Serialize(admin, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static DashboardAdmin Load(string path)
    {
        if (!File.Exists(path)) return null;

        try
        {
            var admin = JsonSerializer.Deserialize<DashboardAdmin>(File.ReadAllText(path));
            return admin != null && !string.IsNullOrEmpty(admin.Key) && !string.IsNullOrEmpty(admin.Salt) ? admin : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (IOException)
        {
        }
    }
}
