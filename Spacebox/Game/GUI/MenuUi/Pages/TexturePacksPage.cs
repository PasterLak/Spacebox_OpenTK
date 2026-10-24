using Engine;
using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Resource.TexturePacks;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class TexturePacksPage : MenuPage
{
    private const float Width = 620f;
    private const float IconSize = 40f;

    private readonly GameMenu _menu;
    private readonly VBoxContainer _rows = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand };
    private readonly Dictionary<string, Texture2D> _icons = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

    public TexturePacksPage(GameMenu menu)
    {
        _menu = menu;
    }

    protected override void Build()
    {
        var frame = MenuLayout.Window("Texture packs", Width);
        AddChild(frame.Root);
        _rows.AddThemeConstantOverride("separation", 0f);
        frame.Body.AddChild(new Label
        {
            Text = $"Put packs (folders or archives) into {TexturePackService.FolderName}/; archives are unpacked on the next start. Enabled packs override textures of every GameSet; the top one wins. Applied when a world loads.",
            Autowrap = true,
            ThemeTypeVariation = "MutedLabel"
        });

        frame.Body.AddChild(MenuLayout.ScrollList(_rows, 250f));

        frame.Footer.AddChild(MenuLayout.Button(_menu, "Rescan", () =>
        {
            TexturePackService.Rescan();
            Rebuild();
        }));
        frame.Footer.AddChild(MenuLayout.Button(_menu, "Back", _menu.SetStateToMods));
    }

    public override void OnShow() => Rebuild();

    private void Rebuild()
    {
        _rows.ClearChildren();
        var packs = OrderedPacks();
        if (packs.Count == 0) _rows.AddChild(new Label { Text = "No texture packs installed", ThemeTypeVariation = "MutedLabel" });

        var enabled = TexturePackService.Enabled;
        foreach (var pack in packs)
        {
            bool active = TexturePackOrder.IsEnabled(enabled, pack.Id);
            var row = new SelectableRow(IconSize) { Icon = Icon(pack), FlipIcon = true };
            var title = row.AddLine(active ? $"{TexturePackOrder.IndexOf(enabled, pack.Id) + 1}. {pack.DisplayName}  v{pack.Info.Version}" : $"{pack.DisplayName}  v{pack.Info.Version}");
            if (active) title.ThemeTypeVariation = "SuccessLabel";
            row.AddLine($"{pack.Id} by {(string.IsNullOrEmpty(pack.Info.Author) ? "unknown" : pack.Info.Author)}  -  {pack.TextureCount} textures  {pack.Info.Description}", "MutedLabel");

            string id = pack.Id;
            var edit = row.Row.AddChild(new Button { Text = "Edit", ThemeTypeVariation = "SmallButton", SizeFlagsVertical = SizeFlags.ShrinkCenter, TooltipText = "Open the pack in the editor, shown on the Default GameSet." });
            var editedPack = pack;
            edit.Pressed += _ => _menu.OpenTexturePackEditor(editedPack);
            row.Row.AddChild(Small(active ? "Disable" : "Enable", () => TexturePackOrder.Toggle(enabled, id)));
            if (active)
            {
                row.Row.AddChild(Small("Up", () => TexturePackOrder.MoveUp(enabled, id)));
                row.Row.AddChild(Small("Down", () => TexturePackOrder.MoveDown(enabled, id)));
            }
            _rows.AddChild(row);
        }
    }

    private Button Small(string text, Action change)
    {
        var button = new Button { Text = text, ThemeTypeVariation = "SmallButton", SizeFlagsVertical = SizeFlags.ShrinkCenter };
        button.Pressed += _ =>
        {
            _menu.Click1?.Play();
            change();
            TexturePackService.Changed();
            SettingsService.Save(Settings.AsGameSettings());
            Rebuild();
        };
        return button;
    }

    private static List<TexturePack> OrderedPacks()
    {
        var result = new List<TexturePack>(TexturePackService.Active());
        foreach (var pack in TexturePackService.Available)
            if (!result.Contains(pack)) result.Add(pack);
        return result;
    }

    private Texture2D Icon(TexturePack pack)
    {
        if (_icons.TryGetValue(pack.Folder, out var cached)) return cached;
        var icon = MenuIcons.Load(pack.IconPath);
        _icons[pack.Folder] = icon;
        return icon;
    }
}
