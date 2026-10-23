using Engine;
using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Resource;
using Spacebox.Game.Versioning;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class WorldBadges
{
    private static readonly Color4 CurrentVersionColor = new Color4(1f, 0.72f, 0.1f, 1f);
    private static readonly Color4 OldVersionColor = new Color4(0.55f, 0.55f, 0.58f, 1f);
    private static readonly Color4 UnsupportedVersionColor = new Color4(0.8f, 0.2f, 0.15f, 1f);

    private readonly GameMenu _menu;
    private readonly Dictionary<string, List<GameModeDefinition>> _modes = new Dictionary<string, List<GameModeDefinition>>(StringComparer.OrdinalIgnoreCase);

    public WorldBadges(GameMenu menu)
    {
        _menu = menu;
    }

    public void Clear() => _modes.Clear();

    public List<ListBadge> For(WorldInfo world)
    {
        return new List<ListBadge>
        {
            ModeBadge(world.ModId, world.GameMode),
            new ListBadge(string.IsNullOrEmpty(world.GameVersion) ? "?" : world.GameVersion, VersionColor(world))
        };
    }

    private static Color4 VersionColor(WorldInfo world)
    {
        if (world.GameVersion == Application.Version) return CurrentVersionColor;
        return VersionConverter.GetSupport(world, out _) == WorldUpgradeStatus.Ok ? OldVersionColor : UnsupportedVersionColor;
    }

    public ListBadge ModeBadge(string modId, GameMode gameMode)
    {
        var mode = ModeOf(modId, gameMode);
        return new ListBadge(mode.Tag, ColorPickerButton.TryParseHex(mode.TagColor, out var color) ? color : OldVersionColor);
    }

    private GameModeDefinition ModeOf(string modId, GameMode gameMode)
    {
        foreach (var mode in ModesOf(modId))
            if (mode.Id == gameMode) return mode;
        foreach (var mode in ModesOf("default"))
            if (mode.Id == gameMode) return mode;
        return GameModeDefinition.BuiltIn(Enum.IsDefined(gameMode) ? gameMode : GameMode.Survival);
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
