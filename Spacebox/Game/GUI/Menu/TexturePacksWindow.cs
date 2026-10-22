using System.Numerics;
using Engine;
using ImGuiNET;
using Spacebox.Game.Resource.TexturePacks;

namespace Spacebox.Game.GUI.Menu;

public sealed class TexturePacksWindow : MenuWindow
{
    private static readonly Vector4 TitleColor = new Vector4(1f, 0.8f, 0.2f, 1f);
    private static readonly Vector4 ActiveColor = new Vector4(0.55f, 1f, 0.55f, 1f);

    private readonly GameMenu _menu;
    private readonly Dictionary<string, Texture2D> _icons = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

    public TexturePacksWindow(GameMenu menu)
    {
        _menu = menu;
    }

    public override void OnWindowChanged()
    {
        TexturePackService.Rescan();
    }

    public override void Render()
    {
        var display = ImGui.GetIO().DisplaySize;
        float width = MathF.Max(display.X * 0.5f, 420f);
        float height = display.Y * 0.62f;
        var position = GameMenu.CenterNextWindow2(width, height);

        ImGui.Begin("TexturePacks", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse |
                                    ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        GameMenu.DrawElementColors(position, new Vector2(width, height), display.Y);

        float padding = height * 0.04f;
        float buttonHeight = height * 0.075f;
        float listHeight = height - padding * 3f - buttonHeight - ImGui.GetTextLineHeightWithSpacing() * 2f;

        ImGui.SetCursorPos(new Vector2(padding, padding));
        ImGui.TextColored(TitleColor, "Texture packs");
        ImGui.SetCursorPosX(padding);
        ImGui.PushTextWrapPos(width - padding);
        ImGui.TextDisabled($"Put packs into {TexturePackService.FolderName}/. Enabled packs override textures of every GameSet; the top one wins. Applied when a world loads.");
        ImGui.PopTextWrapPos();

        ImGui.SetCursorPosX(padding);
        if (ImGui.BeginChild("##packs", new Vector2(width - padding * 2f, listHeight), ImGuiChildFlags.Border))
        {
            var packs = OrderedPacks();
            if (packs.Count == 0) ImGui.TextDisabled("No texture packs installed");
            for (int i = 0; i < packs.Count; i++) DrawPack(packs[i], i, buttonHeight * 1.4f);
        }
        ImGui.EndChild();

        float buttonWidth = (width - padding * 4f) / 3f;
        float y = height - padding - buttonHeight;
        _menu.ButtonWithBackground("Rescan", new Vector2(buttonWidth, buttonHeight), new Vector2(padding, y), TexturePackService.Rescan);
        _menu.ButtonWithBackground("Back", new Vector2(buttonWidth, buttonHeight), new Vector2(padding * 3f + buttonWidth * 2f, y), _menu.SetStateToMods);
        ImGui.End();
    }

    private static List<TexturePack> OrderedPacks()
    {
        var result = new List<TexturePack>(TexturePackService.Active());
        foreach (var pack in TexturePackService.Available)
        {
            if (!result.Contains(pack)) result.Add(pack);
        }
        return result;
    }

    private void DrawPack(TexturePack pack, int index, float rowHeight)
    {
        var enabled = TexturePackService.Enabled;
        bool active = TexturePackOrder.IsEnabled(enabled, pack.Id);

        ImGui.PushID(pack.Id);
        var start = ImGui.GetCursorPos();
        var icon = Icon(pack);
        if (icon != null) ImGui.Image(icon.Handle, new Vector2(rowHeight, rowHeight));
        else ImGui.Dummy(new Vector2(rowHeight, rowHeight));

        ImGui.SameLine();
        ImGui.BeginGroup();
        if (active) ImGui.TextColored(ActiveColor, $"{TexturePackOrder.IndexOf(enabled, pack.Id) + 1}. {pack.DisplayName}  v{pack.Info.Version}");
        else ImGui.Text($"{pack.DisplayName}  v{pack.Info.Version}");
        ImGui.TextDisabled($"{pack.Id} by {(string.IsNullOrEmpty(pack.Info.Author) ? "unknown" : pack.Info.Author)}  -  {pack.TextureCount} textures  {pack.Info.Description}");

        if (ImGui.Button(active ? "Disable" : "Enable")) Change(() => TexturePackOrder.Toggle(enabled, pack.Id));
        if (active)
        {
            ImGui.SameLine();
            if (ImGui.Button("Up")) Change(() => TexturePackOrder.MoveUp(enabled, pack.Id));
            ImGui.SameLine();
            if (ImGui.Button("Down")) Change(() => TexturePackOrder.MoveDown(enabled, pack.Id));
        }
        ImGui.EndGroup();

        ImGui.SetCursorPos(new Vector2(start.X, MathF.Max(ImGui.GetCursorPosY(), start.Y + rowHeight) + ImGui.GetStyle().ItemSpacing.Y));
        ImGui.PopID();
    }

    private void Change(Action change)
    {
        _menu.Click1.Play();
        change();
        TexturePackService.Changed();
        SettingsService.Save(Settings.AsGameSettings());
    }

    private Texture2D Icon(TexturePack pack)
    {
        if (_icons.TryGetValue(pack.Folder, out var cached)) return cached;

        var icon = MenuIcons.Load(pack.IconPath);
        _icons[pack.Folder] = icon;
        return icon;
    }
}
