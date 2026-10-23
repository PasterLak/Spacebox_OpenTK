using Engine;
using Engine.Utils;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Game.Ships;

public static class ShipPhysicsSettingsLoader
{
    public const string FileName = "ships.json";

    public static ShipPhysicsSettings Load(GameSetFiles files)
    {
        var settings = new ShipPhysicsSettings();
        var file = files.File(FileName);

        if (file != null)
        {
            try
            {
                settings = GameSetFiles.Deserialize<ShipPhysicsSettings>(file.Data) ?? settings;
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
