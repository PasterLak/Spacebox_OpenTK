using System.Globalization;
using Engine;
using Engine.CanvasUI;

namespace Spacebox.Game.GUI.Showcase;

public sealed class ShowcaseItem
{
    public string Name { get; init; } = "";
    public Texture2D Icon { get; init; }
}

public sealed class ShowcaseAssets : IDisposable
{
    private const string FontPath = "Resources/Fonts/FusionPixel/FusionPixel12pxMonospacedLatin.ttf";
    private const string ItemsFolder = "GameSets/Default/Items";
    private const string EquipmentFolder = "Resources/Textures/UI/Equipment";

    public SkiaCanvasFont Font { get; }
    public IReadOnlyList<ShowcaseItem> Items { get; }
    public IReadOnlyDictionary<string, Texture2D> EquipmentIcons { get; }
    public Texture2D Slot { get; }
    public Texture2D SelectedSlot { get; }

    public ShowcaseAssets()
    {
        Font = new SkiaCanvasFont(FontPath, 12);
        Items = LoadItems();
        EquipmentIcons = LoadFolder(EquipmentFolder);
        Slot = LoadTexture("Resources/Textures/UI/slot.png");
        SelectedSlot = LoadTexture("Resources/Textures/UI/selectedSlot.png");
    }

    public ShowcaseItem Item(int index) => Items.Count == 0 ? null : Items[index % Items.Count];

    public void Dispose()
    {
        Font.Dispose();
    }

    private static List<ShowcaseItem> LoadItems()
    {
        var items = new List<ShowcaseItem>();
        if (!Directory.Exists(ItemsFolder)) return items;

        foreach (var file in Directory.GetFiles(ItemsFolder, "*.png", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal))
        {
            if (file.Contains("_Ignored")) continue;
            items.Add(new ShowcaseItem { Name = Prettify(Path.GetFileNameWithoutExtension(file)), Icon = LoadTexture(file) });
        }
        return items;
    }

    private static Dictionary<string, Texture2D> LoadFolder(string folder)
    {
        var result = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(folder)) return result;
        foreach (var file in Directory.GetFiles(folder, "*.png"))
            result[Path.GetFileNameWithoutExtension(file)] = LoadTexture(file);
        return result;
    }

    private static Texture2D LoadTexture(string path)
    {
        var texture = Resources.Load<Texture2D>(path);
        texture.FilterMode = FilterMode.Nearest;
        return texture;
    }

    public static string Prettify(string name)
    {
        var chars = new List<char>(name.Length + 8);
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (c == '_' || c == '-')
            {
                chars.Add(' ');
                continue;
            }
            if (i > 0 && char.IsUpper(c) && char.IsLower(name[i - 1])) chars.Add(' ');
            if (i > 0 && char.IsDigit(c) && !char.IsDigit(name[i - 1])) chars.Add(' ');
            chars.Add(c);
        }
        var text = new string(chars.ToArray()).Trim();
        return text.Length == 0 ? text : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text.ToLowerInvariant());
    }
}
