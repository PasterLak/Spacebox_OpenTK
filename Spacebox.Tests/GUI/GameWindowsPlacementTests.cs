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
        var view = root.Add(new GameWindowsView(new Spacebox.Game.GUI.GameUi()), UiLayer.Windows);
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
        Assert.Equal(root.ViewportSize.Y * 0.5f + GameWindowsView.InventoryDrop, alone.Y + alone.Height * 0.5f, 1);
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

    [Fact]
    public void WaypointsWindow_LaysOutCleanly()
    {
        var (root, view) = Build();
        Window<WaypointsWindow>(view).Visible = true;
        var report = Engine.CanvasUI.Validation.LayoutHarness.ValidateAll(root, Engine.CanvasUI.Validation.ScreenProfiles.All, new Engine.CanvasUI.Validation.LayoutRules { MinHitTargetPixels = 12f });
        Assert.True(report.IsClean, report.Format());
    }

    public static IEnumerable<object[]> ShipLibraryCases() => new[] { new object[] { 0 }, new object[] { 1 } };

    [Theory]
    [MemberData(nameof(ShipLibraryCases))]
    public void ShipLibraryWindow_LaysOutCleanly(int sample)
    {
        var (root, view) = Build();
        var window = Window<ShipLibraryWindow>(view);
        window.Visible = true;
        var entries = new List<Spacebox.Game.Ships.Sharing.ShipLibraryEntry>();
        if (sample == 1)
        {
            entries.Add(new Spacebox.Game.Ships.Sharing.ShipLibraryEntry("Ships/a.ship", DateTime.UnixEpoch, new Spacebox.Game.Ships.Sharing.ShipFile { Name = "A rather long ship name that does not fit into the list", Author = "Pilot", Description = "A small mining ship with two thrusters, a cargo hold and a console that controls everything.", GameVersion = "0.1.5", Blocks = 1234, Max = new OpenTK.Mathematics.Vector3i(12, 5, 20) }, null));
            entries.Add(new Spacebox.Game.Ships.Sharing.ShipLibraryEntry("Ships/broken.ship", DateTime.UnixEpoch, null, "The file ends too early and cannot be read by this version of the game"));
        }
        window.Show(entries, entries.FirstOrDefault(), sample == 1 ? new[] { "SomeExtension", "AnotherVeryLongExtensionName" } : null);

        var report = Engine.CanvasUI.Validation.LayoutHarness.ValidateAll(root, Engine.CanvasUI.Validation.ScreenProfiles.All, new Engine.CanvasUI.Validation.LayoutRules { MinHitTargetPixels = 12f });
        Assert.True(report.IsClean, report.Format());
    }

    [Fact]
    public void BlockWindowSound_WinsOverSilentWindowsOpenedInTheSameFrame()
    {
        var (_, view) = Build();
        var sounds = new GameWindowSounds();
        var creative = Window<CreativeWindow>(view);
        var processing = Window<ProcessingWindow>(view);
        creative.Visible = true;
        processing.Visible = true;
        sounds.Track(creative, false);
        sounds.Track(view.Inventory, false);
        sounds.Track(processing, false);
        Assert.Same(processing, sounds.PendingOpen);
    }
}
