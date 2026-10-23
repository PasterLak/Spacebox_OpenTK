using Engine;
using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Resource;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class WorldBadges
{
    private static readonly Color4 CurrentVersionColor = new Color4(0.55f, 0.55f, 0.58f, 1f);
    private static readonly Color4 OldVersionColor = new Color4(0.85f, 0.52f, 0.12f, 1f);

    private readonly GameMenu _menu;
    private readonly Dictionary<string, List<GameModeDefinition>> _modes = new Dictionary<string, List<GameModeDefinition>>(StringComparer.OrdinalIgnoreCase);

    public WorldBadges(GameMenu menu)
    {
        _menu = menu;
    }

    public void Clear() => _modes.Clear();

    public List<ListBadge> For(WorldInfo world)
    {
        var mode = ModeOf(world);
        var modeColor = ColorPickerButton.TryParseHex(mode.TagColor, out var parsed) ? parsed : CurrentVersionColor;
        bool current = world.GameVersion == Application.Version;
        return new List<ListBadge>
        {
            new ListBadge(mode.Tag, modeColor),
            new ListBadge(world.GameVersion ?? "?", current ? CurrentVersionColor : OldVersionColor)
        };
    }

    private GameModeDefinition ModeOf(WorldInfo world)
    {
        foreach (var mode in ModesOf(world.ModId))
            if (mode.Id == world.GameMode) return mode;
        foreach (var mode in ModesOf("default"))
            if (mode.Id == world.GameMode) return mode;
        return GameModeDefinition.BuiltIn(Enum.IsDefined(world.GameMode) ? world.GameMode : GameMode.Survival);
    }

    private List<GameModeDefinition> ModesOf(string modId)
    {
        modId = string.IsNullOrWhiteSpace(modId) ? "default" : modId;
        if (_modes.TryGetValue(modId, out var modes)) return modes;
        var mod = _menu.GetModById(modId);
        modes = mod == null ? new List<GameModeDefinition>() : GameModesLoader.ReadFolder(Path.Combine(mod.ModPath, GameModesLoader.FolderName));
        _modes[modId] = modes;
        return modes;
    }
}
