using Engine;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;
using Spacebox.Network;

namespace Spacebox.Game.Ships;

public static class ShipKeyMemory
{
    public static readonly string FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "ship_console_keys.json");

    private static ShipKeyPresets _presets;

    private static ShipKeyPresets Presets => _presets ??= Load();

    public static void Remember(ShipConsoleBlock console, ConsoleBindings bindings, int group, int key)
    {
        string id = ConsoleId(console);
        if (id == null || bindings == null) return;

        Presets.Remember(id, ShipKeyPresets.WithKey(bindings, group, key));
        Save();
    }

    public static void OnPlacedLocally(Block block)
    {
        if (block is not ShipConsoleBlock console || console.Entity == null) return;
        if (!Presets.TryGet(ConsoleId(console), out var keys)) return;

        var bindings = console.Entity.ShipControl.GetBindings(console.LocalCell);
        if (bindings == null) return;

        int count = Math.Min(keys.Length, bindings.GroupCount);
        for (int group = 0; group < count; group++)
        {
            if (bindings.GetKey(group) != keys[group])
                ShipNetwork.RequestSetKey(console.Entity, console.LocalCell, group, keys[group]);
        }
    }

    private static string ConsoleId(ShipConsoleBlock console)
    {
        return console == null ? null : GameAssets.GetBlockDataById(console.Id)?.Id_string;
    }

    private static ShipKeyPresets Load()
    {
        try
        {
            return File.Exists(FilePath) ? ShipKeyPresets.Deserialize(File.ReadAllText(FilePath)) : new ShipKeyPresets();
        }
        catch (IOException exception)
        {
            Debug.Warning($"[Ship] Could not read {FilePath}: {exception.Message}");
            return new ShipKeyPresets();
        }
    }

    private static void Save()
    {
        try
        {
            Engine.Utils.AtomicFile.WriteAllText(FilePath, Presets.Serialize());
        }
        catch (IOException exception)
        {
            Debug.Warning($"[Ship] Could not save {FilePath}: {exception.Message}");
        }
    }
}
