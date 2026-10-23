using System.Numerics;
using ImGuiNET;
using static Spacebox.Game.Resource.GameSetLoader;

namespace Spacebox.Game.GUI.Menu;

public class ModsWindow : MenuWindow
{
    private readonly GameMenu _menu;
    private int _selected;

    public ModsWindow(GameMenu menu)
    {
        _menu = menu;
    }

    protected override Spacebox.Game.GUI.MenuUi.MenuPage CreatePage() => new Spacebox.Game.GUI.MenuUi.ModsPage(_menu);

    public override void Render()
    {
        var display = ImGui.GetIO().DisplaySize;
        float width = MathF.Max(display.X * 0.5f, 420f);
        float height = display.Y * 0.62f;
        var position = GameMenu.CenterNextWindow2(width, height);

        ImGui.Begin("Mods", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse |
                            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        GameMenu.DrawElementColors(position, new Vector2(width, height), display.Y);

        float padding = height * 0.04f;
        float buttonHeight = height * 0.075f;
        float listHeight = height - padding * 3f - buttonHeight * 2f - ImGui.GetTextLineHeightWithSpacing();

        ImGui.SetCursorPos(new Vector2(padding, padding));
        ImGui.TextColored(new Vector4(1f, 0.8f, 0.2f, 1f), "GameSets");

        var gameSets = _menu.AllGameSets;
        _selected = Math.Clamp(_selected, 0, Math.Max(0, gameSets.Count - 1));

        ImGui.SetCursorPosX(padding);
        if (ImGui.BeginChild("##gameSets", new Vector2(width - padding * 2f, listHeight), ImGuiChildFlags.Border))
        {
            for (int i = 0; i < gameSets.Count; i++)
                DrawGameSet(gameSets[i], i, buttonHeight * (!Spacebox.Game.Resource.Content.GameSetRoles.IsExtension(gameSets[i].Extends) ? 1.4f : 2f));
        }
        ImGui.EndChild();

        float buttonWidth = (width - padding * 2f - padding * 2f) / 3f;
        float y = height - padding - buttonHeight * 2f - padding * 0.5f;
        var selected = gameSets.Count > 0 ? gameSets[_selected] : null;

        ImGui.BeginDisabled();
        _menu.ButtonWithBackground("Download mods", new Vector2(buttonWidth, buttonHeight), new Vector2(padding, y), () => { });
        ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) ImGui.SetTooltip("Mod hub - coming soon: a place to find and download mods, texture packs and ships from other players");

        _menu.ButtonWithBackground("GameSet editor", new Vector2(buttonWidth, buttonHeight), new Vector2(padding * 2f + buttonWidth, y), () =>
        {
            if (selected != null) _menu.OpenGameSetEditor(selected, "Overview");
        });

        _menu.ButtonWithBackground("Ship viewer", new Vector2(buttonWidth, buttonHeight), new Vector2(padding, y + buttonHeight + padding * 0.5f), _menu.Open<ShipViewerWindow>);

        _menu.ButtonWithBackground("Back", new Vector2(buttonWidth, buttonHeight), new Vector2((width - buttonWidth) * 0.5f, y + buttonHeight + padding * 0.5f), _menu.SetStateToMain);

        _menu.ButtonWithBackground("Texture packs", new Vector2(buttonWidth, buttonHeight), new Vector2(padding * 3f + buttonWidth * 2f, y + buttonHeight + padding * 0.5f), _menu.Open<TexturePacksWindow>);
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Global texture packs: enable several and order them, the top pack wins");

        ImGui.End();
    }

    private void DrawGameSet(ModConfig mod, int index, float rowHeight)
    {
        ImGui.PushID(index);
        var start = ImGui.GetCursorPos();
        if (ImGui.Selectable("##row", index == _selected, ImGuiSelectableFlags.AllowDoubleClick, new Vector2(0f, rowHeight)))
        {
            _selected = index;
            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) _menu.OpenGameSetEditor(mod, "Overview");
        }

        ImGui.SetCursorPos(start);
        if (mod.Icon != null) ImGui.Image(mod.Icon.Handle, new Vector2(rowHeight, rowHeight));
        else ImGui.Dummy(new Vector2(rowHeight, rowHeight));

        ImGui.SameLine();
        ImGui.BeginGroup();
        ImGui.Text($"{mod.ModName}  v{mod.Version}");
        ImGui.TextDisabled($"{mod.ModId} by {(string.IsNullOrEmpty(mod.Author) ? "unknown" : mod.Author)}  -  {mod.Description}");
        if (Spacebox.Game.Resource.Content.GameSetRoles.IsExtension(mod.Extends))
        {
            string includes = mod.Includes != null && mod.Includes.Count > 0 ? $", includes {string.Join(", ", mod.Includes)}" : "";
            ImGui.TextColored(new Vector4(0.6f, 0.8f, 1f, 1f), $"Extension of {Spacebox.Game.Resource.Content.ExtensionTargets.Describe(mod.Extends)}{includes} - pick it when creating a world");
        }
        ImGui.EndGroup();
        ImGui.SetCursorPos(start + new Vector2(0f, rowHeight + ImGui.GetStyle().ItemSpacing.Y + 2f));
        ImGui.PopID();
    }
}
