using Engine.CanvasUI;
using Spacebox.Game;
using Spacebox.Game.GUI.Canvas;
using Spacebox.Game.GUI.Canvas.Windows;

namespace Spacebox.Tests.GUI;

public class GameWindowsPlacementTests
{
    private static (CanvasRoot Root, GameWindowsView View) Build()
    {
        var root = new CanvasRoot { Theme = GameCanvasThemes.Create(GameUiDesign.Classic, ShowcaseFixture.Assets.Font) };
        GameCanvas.Configure(root.Stretch);
        var view = root.Add(new GameWindowsView(), UiLayer.Windows);
        view.SetAnchorsPreset(LayoutPreset.FullRect);
        root.Resize(new OpenTK.Mathematics.Vector2(1280f, 720f));
        return (root, view);
    }

    private static T Window<T>(GameWindowsView view) where T : GameWindow => view.Windows.OfType<T>().Single();

    [Theory]
    [InlineData(typeof(StorageWindow))]
    [InlineData(typeof(CreativeWindow))]
    [InlineData(typeof(EquipmentWindow))]
    [InlineData(typeof(ProcessingWindow))]
    public void Inventory_StaysCenteredWhenOtherWindowsOpen(Type other)
    {
        var (root, view) = Build();
        view.Inventory.Visible = true;
        root.Layout();
        var alone = view.Inventory.Rect;

        view.Windows.Single(w => w.GetType() == other).Visible = true;
        root.Layout();

        Assert.Equal(alone, view.Inventory.Rect);
        Assert.Equal(root.ViewportSize.X * 0.5f, alone.X + alone.Width * 0.5f, 1);
    }

    [Fact]
    public void SideWindows_DoNotOverlapTheInventory()
    {
        var (root, view) = Build();
        view.Inventory.Visible = true;
        Window<CreativeWindow>(view).Visible = true;
        Window<EquipmentWindow>(view).Visible = true;
        Window<StorageWindow>(view).Visible = true;
        Window<CraftingWindow>(view).Visible = true;
        root.Layout();

        var open = view.Windows.Where(w => w.Visible).ToList();
        for (int i = 0; i < open.Count; i++)
            for (int j = i + 1; j < open.Count; j++)
                Assert.False(open[i].Rect.Intersects(open[j].Rect), $"{open[i].GetType().Name} / {open[j].GetType().Name}");
    }
}
