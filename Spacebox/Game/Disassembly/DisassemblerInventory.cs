namespace Spacebox.Game.Disassembly;

public delegate bool DisassemblyRecipeLookup(Item item, out DisassemblyRecipe recipe);

public sealed class DisassemblerInventory
{
    public const byte Width = 3;
    public const byte Height = 2;

    private readonly DisassemblyRecipeLookup _lookup;
    private readonly byte[] _simulatedCounts = new byte[Width * Height];
    private readonly short[] _simulatedIds = new short[Width * Height];

    public Storage Input { get; } = new Storage(Width, Height);
    public Storage Output { get; } = new Storage(Width, Height);

    public DisassemblerInventory(DisassemblyRecipeLookup lookup)
    {
        _lookup = lookup;

        Input.Name = "DisassemblerInput";
        Output.Name = "DisassemblerOutput";

        for (byte y = 0; y < Height; y++)
        for (byte x = 0; x < Width; x++)
        {
            var input = Input.GetSlot(x, y);
            input.Name = "Input";
            input.Filter = CanDisassemble;

            var output = Output.GetSlot(x, y);
            output.Name = "Output";
            output.Filter = RejectAll;
        }
    }

    public bool CanDisassemble(Item item)
    {
        return item != null && _lookup(item, out _);
    }

    public DisassemblerStatus FindRecipe(out DisassemblyRecipe? found)
    {
        found = null;
        var status = DisassemblerStatus.Idle;

        for (byte y = 0; y < Height; y++)
        for (byte x = 0; x < Width; x++)
        {
            var slot = Input.GetSlot(x, y);
            if (!slot.HasItem || WasCheckedBefore(x, y, slot.Item)) continue;

            if (!_lookup(slot.Item, out var recipe))
            {
                status = Worse(status, DisassemblerStatus.CannotDisassemble);
                continue;
            }

            if (CountInput(recipe.Input) < recipe.InputQuantity)
            {
                status = Worse(status, DisassemblerStatus.NotEnoughItems);
                continue;
            }

            if (!CanStore(recipe.Products))
            {
                status = Worse(status, DisassemblerStatus.OutputFull);
                continue;
            }

            found = recipe;
            return DisassemblerStatus.Working;
        }

        return status;
    }

    public bool CanProcess(DisassemblyRecipe recipe)
    {
        return CountInput(recipe.Input) >= recipe.InputQuantity && CanStore(recipe.Products);
    }

    public bool TryProcess(DisassemblyRecipe recipe)
    {
        if (!CanProcess(recipe)) return false;

        Input.RemoveItem(recipe.Input, recipe.InputQuantity);
        for (int i = 0; i < recipe.Products.Count; i++)
            Store(recipe.Products[i].Item, recipe.Products[i].Quantity);

        return true;
    }

    public bool HasAnyInput()
    {
        return Input.HasAnyItems();
    }

    public int CountInput(Item item)
    {
        int count = 0;
        for (byte y = 0; y < Height; y++)
        for (byte x = 0; x < Width; x++)
        {
            var slot = Input.GetSlot(x, y);
            if (slot.HasItem && slot.Item.Id == item.Id) count += slot.Count;
        }

        return count;
    }

    public bool CanStore(IReadOnlyList<Product> products)
    {
        for (byte y = 0; y < Height; y++)
        for (byte x = 0; x < Width; x++)
        {
            var slot = Output.GetSlot(x, y);
            int index = y * Width + x;
            _simulatedIds[index] = slot.HasItem ? slot.Item.Id : (short)-1;
            _simulatedCounts[index] = slot.HasItem ? slot.Count : (byte)0;
        }

        for (int i = 0; i < products.Count; i++)
        {
            if (!SimulateStore(products[i].Item, products[i].Quantity)) return false;
        }

        return true;
    }

    private bool SimulateStore(Item item, int quantity)
    {
        for (int i = 0; i < _simulatedIds.Length && quantity > 0; i++)
        {
            if (_simulatedIds[i] != item.Id) continue;

            int added = Math.Min(quantity, item.StackSize - _simulatedCounts[i]);
            if (added <= 0) continue;

            _simulatedCounts[i] += (byte)added;
            quantity -= added;
        }

        for (int i = 0; i < _simulatedIds.Length && quantity > 0; i++)
        {
            if (_simulatedIds[i] != -1) continue;

            int added = Math.Min(quantity, (int)item.StackSize);
            _simulatedIds[i] = item.Id;
            _simulatedCounts[i] = (byte)added;
            quantity -= added;
        }

        return quantity <= 0;
    }

    private void Store(Item item, int quantity)
    {
        for (byte y = 0; y < Height && quantity > 0; y++)
        for (byte x = 0; x < Width && quantity > 0; x++)
        {
            var slot = Output.GetSlot(x, y);
            if (!slot.HasItem || slot.Item.Id != item.Id) continue;

            int added = Math.Min(quantity, item.StackSize - slot.Count);
            if (added <= 0) continue;

            slot.Count += (byte)added;
            quantity -= added;
        }

        for (byte y = 0; y < Height && quantity > 0; y++)
        for (byte x = 0; x < Width && quantity > 0; x++)
        {
            var slot = Output.GetSlot(x, y);
            if (slot.HasItem) continue;

            int added = Math.Min(quantity, (int)item.StackSize);
            slot.SetData(item, (byte)added);
            quantity -= added;
        }
    }

    private bool WasCheckedBefore(byte x, byte y, Item item)
    {
        int index = y * Width + x;
        for (int i = 0; i < index; i++)
        {
            var slot = Input.GetSlot(i % Width, i / Width);
            if (slot.HasItem && slot.Item.Id == item.Id) return true;
        }

        return false;
    }

    private static DisassemblerStatus Worse(DisassemblerStatus current, DisassemblerStatus candidate)
    {
        return Priority(candidate) > Priority(current) ? candidate : current;
    }

    private static int Priority(DisassemblerStatus status)
    {
        return status switch
        {
            DisassemblerStatus.OutputFull => 3,
            DisassemblerStatus.NotEnoughItems => 2,
            DisassemblerStatus.CannotDisassemble => 1,
            _ => 0
        };
    }

    private static bool RejectAll(Item item)
    {
        return false;
    }
}
