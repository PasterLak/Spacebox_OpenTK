namespace Spacebox.Game.GUI.Showcase;

public sealed class ShowcaseSlot
{
    public ShowcaseItem Item { get; set; }
    public int Count { get; set; }
}

public sealed class ShowcaseState
{
    public const int HotbarSize = 10;
    public const int InventoryColumns = 10;
    public const int InventoryRows = 4;

    private float _time;

    public float Health { get; private set; } = 100f;
    public float Power { get; private set; } = 100f;
    public float Oxygen { get; private set; } = 100f;
    public float MaxHealth => 100f;
    public float MaxPower => 100f;
    public float MaxOxygen => 100f;
    public int SelectedHotbar { get; set; }
    public ShowcaseSlot[] Hotbar { get; } = CreateSlots(HotbarSize);
    public ShowcaseSlot[] Inventory { get; } = CreateSlots(InventoryColumns * InventoryRows);
    public IReadOnlyList<(string Slot, string Icon)> ArmorSlots { get; } = new[] { ("Helmet", "helmet"), ("Shoulders", "shoulders"), ("Chest", "chest"), ("Arms", "arms"), ("Legs", "legs"), ("Boots", "boots") };
    public IReadOnlyList<(string Slot, string Icon)> DeviceSlots { get; } = new[] { ("Flashlight", "flashlight"), ("Scanner", "scanner"), ("Generator", "powerGenerator") };

    public void Fill(ShowcaseAssets assets)
    {
        var random = new Random(7);
        for (int i = 0; i < Hotbar.Length; i++)
        {
            if (i >= 7) continue;
            Hotbar[i].Item = assets.Item(i * 5);
            Hotbar[i].Count = i == 0 ? 1 : random.Next(1, 100);
        }
        for (int i = 0; i < Inventory.Length; i++)
        {
            if (random.NextDouble() < 0.35) continue;
            Inventory[i].Item = assets.Item(random.Next(0, 1000));
            Inventory[i].Count = random.Next(1, 100);
        }
    }

    public void Update(float deltaTime)
    {
        _time += deltaTime;
        Health = 55f + 40f * MathF.Sin(_time * 0.4f);
        Power = 50f + 50f * MathF.Sin(_time * 0.25f + 1.3f);
        Oxygen = 100f - (_time * 6f % 100f);
    }

    private static ShowcaseSlot[] CreateSlots(int count)
    {
        var slots = new ShowcaseSlot[count];
        for (int i = 0; i < count; i++) slots[i] = new ShowcaseSlot();
        return slots;
    }
}
