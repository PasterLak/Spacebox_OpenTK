using Engine;
using Engine.Utils;

namespace Spacebox.Game.Ships;

public static class ShipPhysicsSettingsLoader
{
    public const string FileName = "ships.json";

    public static ShipPhysicsSettings Load(string modPath)
    {
        var settings = new ShipPhysicsSettings();
        string file = Path.Combine(modPath, FileName);

        if (File.Exists(file))
        {
            try
            {
                settings = JsonFixer.LoadJsonSafe<ShipPhysicsSettings>(file) ?? settings;
            }
            catch (Exception ex)
            {
                Debug.Error($"[GameSetLoader] Error loading {FileName}: {ex.Message}");
            }
        }

        settings.Validate();
        return settings;
    }
}
