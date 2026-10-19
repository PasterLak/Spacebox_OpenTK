using System.Text.Json;
using Engine;

namespace Spacebox.Network.Auth;

public sealed class SavedCredentialStore
{
    public sealed class Entry
    {
        public string Server { get; set; } = "";
        public string Name { get; set; } = "";
        public string Salt { get; set; } = "";
        public string Key { get; set; } = "";
    }

    private sealed class CredentialFile
    {
        public List<Entry> Entries { get; set; } = new List<Entry>();
    }

    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { WriteIndented = true };

    private readonly string _path;
    private readonly List<Entry> _entries = new List<Entry>();

    public static string DefaultPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Multiplayer", "credentials.json");

    public SavedCredentialStore(string path = null)
    {
        _path = path ?? DefaultPath;
        Load();
    }

    public bool TryGetKey(string server, string name, byte[] salt, out byte[] key)
    {
        key = null;
        var entry = Find(server, name);
        if (entry == null || salt == null || entry.Salt != Convert.ToBase64String(salt)) return false;

        try
        {
            key = Convert.FromBase64String(entry.Key);
        }
        catch (FormatException)
        {
            return false;
        }

        return PasswordCrypto.IsValidKey(key);
    }

    public void Save(string server, string name, byte[] salt, byte[] key)
    {
        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(name) || salt == null || !PasswordCrypto.IsValidKey(key)) return;

        var entry = Find(server, name);
        if (entry == null)
        {
            entry = new Entry { Server = server, Name = name.Trim() };
            _entries.Add(entry);
        }

        entry.Salt = Convert.ToBase64String(salt);
        entry.Key = Convert.ToBase64String(key);
        Write();
    }

    public bool Remove(string server, string name)
    {
        var entry = Find(server, name);
        if (entry == null) return false;

        _entries.Remove(entry);
        Write();
        return true;
    }

    private Entry Find(string server, string name)
    {
        foreach (var entry in _entries)
        {
            if (string.Equals(entry.Server, server, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(entry.Name, name?.Trim(), StringComparison.OrdinalIgnoreCase))
                return entry;
        }
        return null;
    }

    private void Load()
    {
        if (!File.Exists(_path)) return;

        try
        {
            var file = JsonSerializer.Deserialize<CredentialFile>(File.ReadAllText(_path));
            if (file?.Entries == null) return;

            foreach (var entry in file.Entries)
            {
                if (entry != null) _entries.Add(entry);
            }
        }
        catch (Exception ex)
        {
            Debug.Error($"[Auth] Failed to read saved passwords: {ex.Message}");
        }
    }

    private void Write()
    {
        try
        {
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(_path, JsonSerializer.Serialize(new CredentialFile { Entries = _entries }, Options));
        }
        catch (Exception ex)
        {
            Debug.Error($"[Auth] Failed to save passwords: {ex.Message}");
        }
    }
}
