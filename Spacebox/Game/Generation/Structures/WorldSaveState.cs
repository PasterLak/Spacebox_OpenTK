using System.Text.Json;
using System.Text.Json.Nodes;
using Spacebox.Game.Player.GameModes;

namespace Spacebox.Game.Generation;

public static class WorldSaveState
{
    private static bool _worldChanged;
    private static string _savedPlayerJson;
    private static bool _tracking;

    public static bool IsTracking => _tracking;

    public static void MarkChanged()
    {
        if (_tracking) _worldChanged = true;
    }

    public static void MarkSaved(string playerJson)
    {
        _worldChanged = false;
        _savedPlayerJson = Fingerprint(playerJson);
        _tracking = true;
    }

    public static bool HasUnsavedChanges(string currentPlayerJson)
    {
        if (!_tracking) return false;
        return _worldChanged || !string.Equals(_savedPlayerJson, Fingerprint(currentPlayerJson), StringComparison.Ordinal);
    }

    public static bool HasUnsavedChanges(string currentPlayerJson, GameMode currentMode, GameMode savedMode)
    {
        if (!_tracking) return false;
        return currentMode != savedMode || HasUnsavedChanges(currentPlayerJson);
    }

    public static string Fingerprint(string playerJson)
    {
        if (string.IsNullOrEmpty(playerJson)) return "";

        try
        {
            if (JsonNode.Parse(playerJson) is not JsonObject data) return playerJson;
            data.Remove("Statistics");
            return data.ToJsonString();
        }
        catch (JsonException)
        {
            return playerJson;
        }
    }

    public static void Reset()
    {
        _worldChanged = false;
        _savedPlayerJson = null;
        _tracking = false;
    }
}
