using Engine;
using Engine.InputPro;
using OpenTK.Mathematics;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Ships.Sharing;

public static class ShipLibraryUI
{
    public const string WindowId = "ships";
    private const string ToggleAction = "ships";

    private static readonly Color4 WarningChatColor = new Color4(1f, 0.6f, 0.3f, 1f);

    private static ShipLibrary _library;
    private static ShipLibraryEntry _selected;

    public static bool IsOpen => UIManager.IsOpen(WindowId);

    public static void Update(LocalAstronaut player)
    {
        ShipPlacementMode.Update(player);

        if (Chat.FocusInput || Debug.IsVisible || player == null) return;
        if (Spacebox.Game.GUI.Canvas.GameCanvas.WantsKeyboard) return;
        if (!InputManager.Instance.HasAction(ToggleAction) || !Input.IsActionDown(ToggleAction)) return;

        if (IsOpen)
        {
            if (UIManager.IsTop(WindowId)) UIManager.CloseTop();
            return;
        }

        if (UIManager.IsUIMode || ShipPlacementMode.IsActive) return;
        if (!player.GameModeRules.ShipLibrary)
        {
            Chat.Write("Your game mode cannot place saved ships", WarningChatColor);
            return;
        }

        Open();
    }

    public static void Open()
    {
        _library ??= new ShipLibrary(ShipFileNames.Folder);
        _library.Refresh();
        if (_selected != null && !_library.Entries.Contains(_selected)) _selected = null;
        _selected ??= _library.Entries.Count > 0 ? _library.Entries[0] : null;
        UIManager.Open(WindowId);
    }

    public static void Reset()
    {
        ShipPlacementMode.Reset();
        _selected = null;
    }

    public static IReadOnlyList<ShipLibraryEntry> Entries => _library?.Entries ?? (IReadOnlyList<ShipLibraryEntry>)Array.Empty<ShipLibraryEntry>();
    public static ShipLibraryEntry Selected => _selected;

    public static void Select(ShipLibraryEntry entry) => _selected = entry;

    public static void PlaceSelected()
    {
        if (_selected?.Ship == null) return;
        Close();
        ShipPlacementMode.Begin(_selected.Ship);
    }

    public static void OpenFolder()
    {
        if (_library == null) return;
        Directory.CreateDirectory(_library.Folder);
        Application.OpenLink(_library.Folder);
    }

    public static void Close()
    {
        if (UIManager.IsTop(WindowId)) UIManager.CloseTop();
    }

    public static IReadOnlyList<string> MissingGameSetsOf(ShipFile ship) => ship == null ? Array.Empty<string>() : MissingGameSets(ship);

    private static List<string> MissingGameSets(ShipFile ship)
    {
        var result = new List<string>();
        var stack = GameSetLoader.Stack;
        if (stack == null) return result;

        foreach (var id in ship.GameSets)
        {
            bool known = stack.Base.Matches(id);
            foreach (var extension in stack.ExtensionIds) known |= string.Equals(extension, id, StringComparison.OrdinalIgnoreCase);
            if (!known) result.Add(id);
        }
        return result;
    }
}
