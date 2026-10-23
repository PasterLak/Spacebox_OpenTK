using Engine.CanvasUI;
using Engine.CanvasUI.Validation;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.GUI.Canvas;
using Spacebox.Game.GUI.Canvas.Items;
using Spacebox.Game.GUI.Canvas.Windows;

namespace Spacebox.Tests.GUI;

[Collection(GameAssetsCollection.Name)]
public sealed class ItemSlotInteractionTests
{
    private static readonly Item Ore = new Item(401, 64, "ore") { Id_string = "test:slot_ore" };

    private sealed class TestWindow : GameWindow
    {
        public TestWindow(Storage storage) : base("Test")
        {
            AddHeaderButton(StorageButtons.CombineText, StorageButtons.CombineTooltip, () => { });
            AddHeaderButton(StorageButtons.SortText, StorageButtons.SortTooltip, () => { });
            Grid = Body.AddChild(new StorageGridControl());
            Grid.Show(storage);
            Visible = true;
        }

        public StorageGridControl Grid { get; }
        public override bool IsOpen => true;
        protected override void Refresh() { }
    }

    private sealed class DropCatcher : Control, ISlotDropTarget
    {
        public ItemSlot Received { get; private set; }
        public DropCatcher() { MouseFilter = MouseFilter.Stop; }
        public void AcceptDrop(ItemSlot source) => Received = source;
    }

    private static (CanvasRoot Root, TestWindow Window) Build(Storage storage)
    {
        var root = new CanvasRoot { Theme = GameCanvasThemes.Create(GameUiDesign.Classic, ShowcaseFixture.Assets.Font) };
        root.Stretch.VirtualSize = GameCanvas.VirtualSize;
        var window = new TestWindow(storage);
        root.Add(window, UiLayer.Windows);
        window.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        root.Resize(GameCanvas.VirtualSize);
        root.Update(0.016f, default);
        return (root, window);
    }

    private static Vector2 CenterOf(CanvasRoot root, Control control) => root.CanvasToScreen(control.Rect.Position + control.Rect.Size * 0.5f);

    private static void Step(CanvasRoot root, Vector2 position, bool down) => root.Update(0.016f, new PointerState { Position = position, LeftDown = down });

    private static void Drag(CanvasRoot root, Vector2 from, Vector2 to)
    {
        Step(root, from, false);
        Step(root, from, true);
        Step(root, (from + to) * 0.5f, true);
        Step(root, to, true);
        Step(root, to, false);
    }

    [Fact]
    public void DraggingAStack_MovesItToTheTargetSlot()
    {
        var storage = new Storage(4, 2);
        storage.SetSlot(0, 0, Ore, 20);
        var (root, window) = Build(storage);

        Drag(root, CenterOf(root, window.Grid.Slots[0]), CenterOf(root, window.Grid.Slots[5]));

        Assert.False(storage.GetSlot(0, 0).HasItem);
        Assert.Equal(20, storage.GetSlot(1, 1).Count);
        Assert.False(SlotDrag.IsActive);
    }

    [Fact]
    public void Clicking_WithoutMoving_ReportsAClickAndKeepsTheItem()
    {
        var storage = new Storage(4, 2);
        storage.SetSlot(2, 0, Ore, 5);
        var (root, window) = Build(storage);
        ItemSlot clicked = null;
        window.Grid.SlotClicked += slot => clicked = slot;

        var point = CenterOf(root, window.Grid.Slots[2]);
        Step(root, point, false);
        Step(root, point, true);
        Step(root, point, false);

        Assert.Same(storage.GetSlot(2, 0), clicked);
        Assert.Equal(5, storage.GetSlot(2, 0).Count);
    }

    [Fact]
    public void RightClick_SplitsTheStack()
    {
        var storage = new Storage(4, 2);
        storage.SetSlot(0, 0, Ore, 20);
        var (root, window) = Build(storage);

        var point = CenterOf(root, window.Grid.Slots[0]);
        root.Update(0.016f, new PointerState { Position = point });
        root.Update(0.016f, new PointerState { Position = point, RightDown = true });
        root.Update(0.016f, new PointerState { Position = point });

        int total = 0, stacks = 0;
        for (int x = 0; x < 4; x++)
            for (int y = 0; y < 2; y++)
                if (storage.GetSlot(x, y).HasItem) { total += storage.GetSlot(x, y).Count; stacks++; }
        Assert.Equal(20, total);
        Assert.Equal(2, stacks);
    }

    [Fact]
    public void DroppingOnAnotherTarget_HandsItTheSourceSlot()
    {
        var storage = new Storage(4, 2);
        storage.SetSlot(0, 0, Ore, 3);
        var (root, window) = Build(storage);
        var catcher = new DropCatcher { CustomMinimumSize = new Vector2(60f, 60f) };
        root.Add(catcher, UiLayer.Windows);
        catcher.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft, 10f, new Vector2(60f, 60f));
        root.Update(0.016f, default);

        Drag(root, CenterOf(root, window.Grid.Slots[0]), CenterOf(root, catcher));

        Assert.Same(storage.GetSlot(0, 0), catcher.Received);
    }

    [Theory]
    [InlineData(GameUiDesign.Classic)]
    [InlineData(GameUiDesign.Modern)]
    public void InventoryWindow_LaysOutCleanlyOnEveryScreen(GameUiDesign design)
    {
        var root = new CanvasRoot { Theme = GameCanvasThemes.Create(design, ShowcaseFixture.Assets.Font) };
        GameCanvas.Configure(root.Stretch);
        var area = root.Add(new SafeArea(), UiLayer.Windows);
        var center = area.AddChild(new CenterContainer());
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        center.AddChild(new TestWindow(new Storage(8, 3)));

        var report = LayoutHarness.ValidateAll(root, ScreenProfiles.All);
        Assert.True(report.IsClean, report.Format());
    }
}
