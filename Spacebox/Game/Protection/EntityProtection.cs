using System.Text.Json;
using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Protection;

public static class EntityProtection
{
    public const string FileName = "protected_entities.json";
    public const string DeniedMessage = "This place is protected: only admins can build, break or move it";
    public const float DeniedMessageInterval = 1.5f;

    private static readonly HashSet<long> _ids = new HashSet<long>();
    private static string _folder;
    private static float _lastDenied = float.NegativeInfinity;

    public static bool LocalCanEdit { get; set; } = true;
    public static IReadOnlyCollection<long> Ids => _ids;
    public static event Action Changed;

    public static bool IsProtected(long entityId) => _ids.Contains(entityId);

    public static bool IsProtected(SpaceEntity entity) => entity != null && _ids.Contains(entity.EntityID);

    public static bool CheckLocalEdit(SpaceEntity entity, Vector3i cell)
    {
        return Check(ProtectionRules.CanEdit(Spacebox.Network.GameSession.PlayerName, LocalCanEdit, entity, cell));
    }

    public static bool CheckLocalEditBox(SpaceEntity entity, Vector3i min, Vector3i max)
    {
        return Check(ProtectionRules.CanEditBox(Spacebox.Network.GameSession.PlayerName, LocalCanEdit, entity, min, max));
    }

    public static bool CheckLocalUse(SpaceEntity entity)
    {
        return Check(ProtectionRules.CanUseEntity(Spacebox.Network.GameSession.PlayerName, LocalCanEdit, entity));
    }

    private static bool Check(bool allowed)
    {
        if (allowed) return true;
        NotifyDenied();
        return false;
    }

    public static bool ShouldShowDenied(float now, float lastShown) => now - lastShown >= DeniedMessageInterval;

    public static void NotifyDenied()
    {
        float now = Time.Total;
        if (!ShouldShowDenied(now, _lastDenied)) return;

        _lastDenied = now;
        GUI.Chat.Write(DeniedMessage, Color4.Red);
    }

    public static bool Set(long entityId, bool protect)
    {
        bool changed = protect ? _ids.Add(entityId) : _ids.Remove(entityId);
        if (!changed) return false;

        Save();
        Changed?.Invoke();
        return true;
    }

    public static void ReplaceAll(IEnumerable<long> ids)
    {
        _ids.Clear();
        foreach (var id in ids) _ids.Add(id);
        Changed?.Invoke();
    }

    public static void Load(string worldFolder)
    {
        _ids.Clear();
        _folder = worldFolder;
        LocalCanEdit = true;

        string path = PathFor(worldFolder);
        if (path == null || !File.Exists(path)) return;

        try
        {
            var ids = JsonSerializer.Deserialize<long[]>(File.ReadAllText(path));
            if (ids != null) foreach (var id in ids) _ids.Add(id);
        }
        catch (Exception ex)
        {
            Debug.Error($"[Protection] Failed to read {FileName}: {ex.Message}");
        }
    }

    public static void Reset()
    {
        _ids.Clear();
        _folder = null;
        LocalCanEdit = true;
        _lastDenied = float.NegativeInfinity;
    }

    private static void Save()
    {
        string path = PathFor(_folder);
        if (path == null) return;

        try
        {
            var ids = new long[_ids.Count];
            _ids.CopyTo(ids);
            File.WriteAllText(path, JsonSerializer.Serialize(ids));
        }
        catch (Exception ex)
        {
            Debug.Error($"[Protection] Failed to write {FileName}: {ex.Message}");
        }
    }

    private static string PathFor(string folder) => string.IsNullOrEmpty(folder) ? null : Path.Combine(folder, FileName);
}
