using Engine.CanvasUI;

namespace Spacebox.Game.GUI.Canvas.Items;

public sealed class StorageGridControl : GridContainer
{
    private readonly List<ItemSlotControl> _slots = new List<ItemSlotControl>();
    private readonly Action<ItemSlotControl> _configure;
    private Storage _storage;
    private float _spacing = -1f;

    public StorageGridControl(Action<ItemSlotControl> configure = null)
    {
        _configure = configure;
        AddThemeConstantOverride("h_separation", 0f);
        AddThemeConstantOverride("v_separation", 0f);
    }

    public Storage Storage => _storage;
    public IReadOnlyList<ItemSlotControl> Slots => _slots;
    public event Action<ItemSlot> SlotClicked;
    public event Action<ItemSlot> SlotHovered;

    public void Show(Storage storage)
    {
        if (!ReferenceEquals(storage, _storage)) Rebuild(storage);
        float spacing = GetThemeConstant("slot_spacing", 0f);
        if (spacing != _spacing)
        {
            _spacing = spacing;
            AddThemeConstantOverride("h_separation", spacing);
            AddThemeConstantOverride("v_separation", spacing);
        }
        for (int i = 0; i < _slots.Count; i++) _slots[i].Refresh();
    }

    private void Rebuild(Storage storage)
    {
        _storage = storage;
        ClearChildren();
        _slots.Clear();
        if (storage == null) return;

        Columns = Math.Max(1, (int)storage.SizeX);
        for (int y = 0; y < storage.SizeY; y++)
        {
            for (int x = 0; x < storage.SizeX; x++)
            {
                var control = new ItemSlotControl();
                control.Bind(storage.GetSlot(x, y));
                control.Clicked += slot => SlotClicked?.Invoke(slot);
                control.Hovered += slot => SlotHovered?.Invoke(slot);
                _configure?.Invoke(control);
                _slots.Add(control);
                AddChild(control);
            }
        }
    }
}
