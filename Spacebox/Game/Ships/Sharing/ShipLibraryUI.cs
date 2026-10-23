using Engine;
using Engine.InputPro;
using ImGuiNET;
using OpenTK.Mathematics;
using Spacebox.Game.GUI;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Resource;
using NVector2 = System.Numerics.Vector2;
using NVector4 = System.Numerics.Vector4;

namespace Spacebox.Game.Ships.Sharing;

public static class ShipLibraryUI
{
    public const string WindowId = "ships";
    private const string ToggleAction = "ships";

    private static readonly NVector4 TitleColor = new NVector4(1f, 0.77f, 0.16f, 1f);
    private static readonly NVector4 HintColor = new NVector4(0.62f, 0.64f, 0.7f, 1f);
    private static readonly NVector4 WarningColor = new NVector4(1f, 0.6f, 0.3f, 1f);
    private static readonly Color4 WarningChatColor = new Color4(1f, 0.6f, 0.3f, 1f);

    private static ShipLibrary _library;
    private static ShipLibraryEntry _selected;

    public static bool IsOpen => UIManager.IsOpen(WindowId);

    public static void Update(LocalAstronaut player)
    {
        ShipPlacementMode.Update(player);

        if (Chat.FocusInput || Debug.IsVisible || player == null) return;
        if (ImGui.GetIO().WantTextInput) return;
        if (!InputManager.Instance.HasAction(ToggleAction) || !Input.IsActionDown(ToggleAction)) return;

        if (IsOpen)
        {
            if (UIManager.IsTop(WindowId)) UIManager.CloseTop();
            return;
        }

        if (UIManager.IsUIMode || ShipPlacementMode.IsActive) return;
        if (player.GameMode != GameMode.Creative)
        {
            Chat.Write("Ships can only be placed in creative mode", WarningChatColor);
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

    public static void OnGUI()
    {
        if (!IsOpen || _library == null) return;

        var display = ImGui.GetIO().DisplaySize;
        float width = MathF.Max(display.X * 0.4f, 560f);
        float height = display.Y * 0.56f;
        var position = GameMenu.CenterNextWindow(width, height);

        ImGui.SetNextWindowPos(position);
        ImGui.SetNextWindowSize(new NVector2(width, height));
        ImGui.Begin("Ships", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar);
        GameMenu.DrawElementColors(position, new NVector2(width, height), display.Y, 0.005f);

        float padding = width * 0.035f;
        float buttonHeight = height * 0.085f;
        ImGui.SetCursorPos(new NVector2(padding, padding * 0.7f));
        ImGui.TextColored(TitleColor, "SHIPS");
        ImGui.SameLine();
        ImGui.TextColored(HintColor, "   Export ships in the main menu: Mods > Ship viewer.");

        float top = ImGui.GetCursorPosY() + padding * 0.3f;
        float contentHeight = height - top - buttonHeight - padding * 1.6f;
        float listWidth = (width - padding * 3f) * 0.45f;
        float infoWidth = width - padding * 3f - listWidth;

        ImGui.SetCursorPos(new NVector2(padding, top));
        if (ImGui.BeginChild("##shipList", new NVector2(listWidth, contentHeight)))
        {
            if (_library.Entries.Count == 0) ImGui.TextWrapped("No ships yet. Export one from the Ship viewer, or copy .ship files into the Ships folder.");
            for (int i = 0; i < _library.Entries.Count; i++)
            {
                var entry = _library.Entries[i];
                ImGui.PushID(i);
                if (ImGui.Selectable(entry.Title, ReferenceEquals(entry, _selected))) _selected = entry;
                ImGui.PopID();
            }
        }
        ImGui.EndChild();

        ImGui.SetCursorPos(new NVector2(padding * 2f + listWidth, top));
        if (ImGui.BeginChild("##shipInfo", new NVector2(infoWidth, contentHeight))) DrawInfo(_selected);
        ImGui.EndChild();

        float buttonWidth = (width - padding * 4f) / 3f;
        float buttonY = height - padding - buttonHeight;
        GameMenu.ButtonWithBackground2("Place", new NVector2(buttonWidth, buttonHeight), new NVector2(padding, buttonY), () =>
        {
            if (_selected?.Ship == null) return;
            UIManager.CloseTop();
            ShipPlacementMode.Begin(_selected.Ship);
        });
        GameMenu.ButtonWithBackground2("Open folder", new NVector2(buttonWidth, buttonHeight), new NVector2(padding * 2f + buttonWidth, buttonY), () =>
        {
            Directory.CreateDirectory(_library.Folder);
            Application.OpenLink(_library.Folder);
        });
        GameMenu.ButtonWithBackground2("Close", new NVector2(buttonWidth, buttonHeight), new NVector2(padding * 3f + buttonWidth * 2f, buttonY), () => UIManager.CloseTop());

        ImGui.End();
    }

    private static void DrawInfo(ShipLibraryEntry entry)
    {
        if (entry == null) return;
        if (entry.Ship == null)
        {
            ImGui.TextColored(WarningColor, "This file cannot be read:");
            ImGui.TextWrapped(entry.Error ?? "");
            return;
        }

        var ship = entry.Ship;
        ImGui.TextColored(TitleColor, ship.Name);
        if (!string.IsNullOrWhiteSpace(ship.Author)) ImGui.Text($"by {ship.Author}");
        ImGui.Text($"{ship.Blocks} blocks, {ship.Size.X} x {ship.Size.Y} x {ship.Size.Z}");
        ImGui.TextColored(HintColor, $"Game {ship.GameVersion}, {System.IO.Path.GetFileName(entry.Path)}");
        if (!string.IsNullOrWhiteSpace(ship.Description))
        {
            ImGui.Spacing();
            ImGui.TextWrapped(ship.Description);
        }

        var missing = MissingGameSets(ship);
        if (missing.Count == 0) return;
        ImGui.Spacing();
        ImGui.PushStyleColor(ImGuiCol.Text, WarningColor);
        ImGui.TextWrapped($"Made with GameSets this world does not have: {string.Join(", ", missing)}. Their blocks are left out.");
        ImGui.PopStyleColor();
    }

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
