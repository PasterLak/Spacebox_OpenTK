using Engine;
using Engine.Utils;
using Spacebox.Game.Resource;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Game.Disassembly;

public static class DisassemblySettingsLoader
{
    public const string FileName = "disassembly.json";

    private static readonly Dictionary<string, string> KeyedArrays = new Dictionary<string, string> { ["Items"] = "Item" };

    public static DisassemblySettings Load(GameSetFiles files, string modId)
    {
        var file = files.File(FileName, KeyedArrays);
        if (file == null) return DisassemblySettings.CreateDefault();

        try
        {
            var json = GameSetFiles.Deserialize<DisassemblyJSON>(file.Data);
            return json == null ? DisassemblySettings.CreateDefault() : Create(json, modId);
        }
        catch (Exception ex)
        {
            Debug.Error($"[GameSetLoader] Error loading {FileName}: {ex.Message}");
            return DisassemblySettings.CreateDefault();
        }
    }

    private static DisassemblySettings Create(DisassemblyJSON json, string modId)
    {
        var entries = new List<DisassemblyEntry>();

        if (json.Items != null)
        {
            foreach (var itemJson in json.Items)
            {
                var entry = CreateEntry(itemJson, modId);
                if (entry != null) entries.Add(entry);
            }
        }

        return new DisassemblySettings(json.DefaultReturnPercent, ToTicks(json.DefaultDurationSec), entries);
    }

    private static DisassemblyEntry CreateEntry(DisassemblyItemJSON json, string modId)
    {
        if (json == null) return null;

        var item = FindItem(json.Item, modId);
        if (item == null)
        {
            Debug.Error($"[GameSetLoader] Disassembly: item was not found - {json.Item}. Entry skipped");
            return null;
        }

        if (json.ReturnPercent is < 0 or > 100)
            Debug.Warning($"[GameSetLoader] Disassembly: ReturnPercent of {json.Item} must be 0-100, clamped");

        return new DisassemblyEntry
        {
            Item = item,
            Disabled = json.Disabled,
            ReturnPercent = json.ReturnPercent,
            DurationTicks = json.DurationSec.HasValue ? ToTicks(json.DurationSec.Value) : null,
            InputQuantity = json.InputQuantity,
            Products = CreateProducts(json, item, modId)
        };
    }

    private static List<Product> CreateProducts(DisassemblyItemJSON json, Item item, string modId)
    {
        if (json.Products == null) return null;

        var products = new List<Product>();
        foreach (var productJson in json.Products)
        {
            if (productJson == null || productJson.Quantity <= 0) continue;

            var product = FindItem(productJson.Item, modId);
            if (product == null)
            {
                Debug.Error($"[GameSetLoader] Disassembly: product was not found - {productJson.Item} (in {json.Item}). Product skipped");
                continue;
            }

            if (product.Id == item.Id)
            {
                Debug.Error($"[GameSetLoader] Disassembly: {json.Item} can not give itself. Product skipped");
                continue;
            }

            products.Add(new Product(product, (byte)Math.Min(byte.MaxValue, productJson.Quantity)));
        }

        return products;
    }

    private static Item FindItem(string id, string modId)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        string fullId = id.Contains(':') ? id : GameSetLoader.CombineId(modId, id);
        return GameAssets.GetItemByFullID(fullId);
    }

    private static int ToTicks(float seconds)
    {
        return Math.Max(1, Time.SecondsToTicks(Math.Max(0.05f, seconds)));
    }
}
