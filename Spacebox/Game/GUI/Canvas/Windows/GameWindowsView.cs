using Engine.CanvasUI;

namespace Spacebox.Game.GUI.Canvas.Windows;

public sealed class GameWindowsView : SafeArea
{
    public const float HudReserve = 120f;
    public const float Gap = 10f;
    public const float InventoryDrop = Items.ItemSlotControl.DefaultSize;

    private readonly List<GameWindow> _windows = new List<GameWindow>();
    private readonly List<GameWindow> _left = new List<GameWindow>();
    private readonly List<GameWindow> _middle = new List<GameWindow>();
    private readonly List<GameWindow> _right = new List<GameWindow>();
    private readonly List<GameWindow> _leftStack = new List<GameWindow>();
    private readonly List<GameWindow> _middleStack = new List<GameWindow>();
    private float _inventoryHeight;

    public GameWindowsView()
    {
        Name = "GameWindows";
        Inventory = new InventoryWindow();
        Add(_left, new CreativeWindow());
        Add(_middle, new StorageWindow());
        Add(_middle, new CraftingWindow());
        Add(_middle, new ProcessingWindow());
        Add(_middle, new OxygenGeneratorWindow());
        Add(_middle, new DnaReconstructorWindow());
        Add(_middle, new GeneratorWindow());
        Add(_middle, new GravityGeneratorWindow());
        Add(_middle, new AnalyzerWindow());
        Add(_middle, new DisassemblerWindow());
        Add(_middle, new ClaimBeaconWindow());
        Add(_middle, new RadarWindow());
        Add(_middle, new ShipConsoleWindow());
        Add(_middle, new SignWindow());
        Add(_middle, new WaypointsWindow());
        Add(null, Inventory);
        Add(_right, new EquipmentWindow());
    }

    public InventoryWindow Inventory { get; }

    public IReadOnlyList<GameWindow> Windows => _windows;

    public void Sync()
    {
        for (int i = 0; i < _windows.Count; i++) _windows[i].Sync();
    }

    private void Add(List<GameWindow> side, GameWindow window)
    {
        _windows.Add(window);
        side?.Add(window);
        AddChild(window);
    }

    protected override void LayoutChildren()
    {
        base.LayoutChildren();
        var area = ContentRect;
        var free = new Rect2(area.X, area.Y, area.Width, MathF.Max(0f, area.Height - HudReserve));
        float centerX = area.X + area.Width * 0.5f;

        Collect(_middle, _middleStack);
        Collect(_left, _leftStack);

        Rect2 anchor;
        _inventoryHeight = Inventory.Visible ? Inventory.GetCombinedMinimumSize().Y : 0f;
        if (Inventory.Visible)
        {
            var size = Inventory.GetCombinedMinimumSize();
            float y = Math.Clamp(MathF.Round(area.Y + (area.Height - size.Y) * 0.5f + InventoryDrop), free.Y, MathF.Max(free.Y, free.End.Y - size.Y));
            anchor = new Rect2(MathF.Round(centerX - size.X * 0.5f), y, size.X, size.Y);
            Container.FitChildInRect(Inventory, anchor);

            float height = StackHeight(_middleStack);
            if (height <= anchor.Y - Gap - free.Y)
            {
                PlaceStack(_middleStack, centerX, anchor.Y - Gap - height, size.X);
                float width = StackWidth(_middleStack, size.X);
                anchor = new Rect2(MathF.Round(centerX - width * 0.5f), anchor.Y, width, anchor.Height);
            }
            else _leftStack.AddRange(_middleStack);
        }
        else
        {
            float width = StackWidth(_middleStack, 0f);
            float height = StackHeight(_middleStack);
            float top = MathF.Max(free.Y, MathF.Round(free.Y + (free.Height - height) * 0.5f));
            anchor = new Rect2(MathF.Round(centerX - width * 0.5f), top, width, height);
            PlaceStack(_middleStack, centerX, top, width);
        }

        PlaceSide(_leftStack, anchor, free, true);
        Collect(_right, _middleStack);
        PlaceSide(_middleStack, anchor, free, false);
    }

    private static void Collect(List<GameWindow> source, List<GameWindow> target)
    {
        target.Clear();
        for (int i = 0; i < source.Count; i++)
            if (source[i].Visible) target.Add(source[i]);
    }

    private float HeightOf(GameWindow window)
    {
        float height = window.GetCombinedMinimumSize().Y;
        return window.MatchesInventoryHeight && _inventoryHeight > 0f ? MathF.Max(height, _inventoryHeight) : height;
    }

    private static float StackWidth(List<GameWindow> stack, float minimum)
    {
        float width = minimum;
        for (int i = 0; i < stack.Count; i++) width = MathF.Max(width, stack[i].GetCombinedMinimumSize().X);
        return width;
    }

    private float StackHeight(List<GameWindow> stack)
    {
        float height = 0f;
        for (int i = 0; i < stack.Count; i++) height += HeightOf(stack[i]) + (i > 0 ? Gap : 0f);
        return height;
    }

    private void PlaceStack(List<GameWindow> stack, float centerX, float top, float matchWidth)
    {
        float y = top;
        for (int i = 0; i < stack.Count; i++)
        {
            var window = stack[i];
            var size = window.GetCombinedMinimumSize();
            float width = window.SizeFlagsHorizontal.HasFlag(SizeFlags.Fill) ? MathF.Max(size.X, matchWidth) : size.X;
            float height = HeightOf(window);
            Container.FitChildInRect(window, new Rect2(MathF.Round(centerX - width * 0.5f), y, width, height));
            y += height + Gap;
        }
    }

    private void PlaceSide(List<GameWindow> stack, Rect2 anchor, Rect2 free, bool left)
    {
        if (stack.Count == 0) return;
        float height = StackHeight(stack);
        float top = Math.Clamp(anchor.End.Y - height, free.Y, MathF.Max(free.Y, free.End.Y - height));
        for (int i = 0; i < stack.Count; i++)
        {
            var window = stack[i];
            var size = window.GetCombinedMinimumSize();
            float windowHeight = HeightOf(window);
            float x = left ? anchor.X - Gap - size.X : anchor.End.X + Gap;
            Container.FitChildInRect(window, new Rect2(MathF.Round(x), MathF.Round(top), size.X, windowHeight));
            top += windowHeight + Gap;
        }
    }
}
