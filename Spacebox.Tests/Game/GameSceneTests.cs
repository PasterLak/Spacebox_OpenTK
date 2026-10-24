using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Player;
using Spacebox.Game.Player.Building;
using Spacebox.Game.Ships;

namespace Spacebox.Tests.Game;

[Collection(GameAssetsCollection.Name)]
public class GameSceneTests
{
    [Fact]
    public void NewContext_CreatesServicesWithoutBecomingCurrent()
    {
        using var scene = new GameScene();

        Assert.NotNull(scene.Creative);
        Assert.NotNull(scene.Pilot);
        Assert.NotNull(scene.Rider);
        Assert.NotNull(scene.Binding);
        Assert.NotNull(scene.Skins);
        Assert.NotNull(scene.WorldRules);
        Assert.NotNull(scene.Sprint);
        Assert.NotNull(scene.Claims);
        Assert.NotNull(scene.PictureFrames);
        Assert.NotNull(scene.MachineSounds);
        Assert.Null(scene.Tags);
        Assert.False(scene.Pilot.IsPiloting);
        Assert.NotSame(scene, GameScene.Current);
    }

    [Fact]
    public void MakeCurrent_PointsCurrentAtTheContextUntilDisposed()
    {
        var scene = new GameScene();

        scene.MakeCurrent();
        Assert.Same(scene, GameScene.Current);

        scene.Dispose();
        Assert.Null(GameScene.Current);
        Assert.True(scene.IsDisposed);
    }

    [Fact]
    public void DisposingAnOldContext_KeepsTheNewCurrent()
    {
        var first = new GameScene();
        first.MakeCurrent();
        var second = new GameScene();
        second.MakeCurrent();

        first.Dispose();

        Assert.Same(second, GameScene.Current);
        second.Dispose();
        Assert.Null(GameScene.Current);
    }

    [Fact]
    public void Dispose_CanBeCalledTwice()
    {
        var scene = new GameScene();
        scene.MakeCurrent();

        scene.Dispose();
        scene.Dispose();

        Assert.Null(GameScene.Current);
    }

    [Fact]
    public void State_DoesNotLeakIntoTheNextContext()
    {
        var first = new GameScene();
        first.MakeCurrent();
        first.Creative.FillBlockId = 7;
        first.Creative.Clipboard = new BlockClipboard(Vector3i.One, new List<ClipboardCell>());
        first.Creative.SetPoint1(new BlockPointer(1, null, new Vector3i(1, 2, 3)));
        first.Skins.Set("Anna", "red");
        first.Dispose();

        using var second = new GameScene();
        second.MakeCurrent();

        Assert.Equal(0, second.Creative.FillBlockId);
        Assert.Null(second.Creative.Clipboard);
        Assert.Null(second.Creative.Block1);
        Assert.Equal("Blue", second.Skins.Resolve("Anna", "blue"));
        Assert.Equal("Red", first.Skins.Resolve("Anna", "blue"));
    }

    [Fact]
    public void PilotFacade_IsSafeWithoutAScene()
    {
        var scene = new GameScene();
        scene.MakeCurrent();
        scene.Dispose();

        Assert.Null(ShipPilot.Console);
        Assert.False(ShipPilot.IsPiloting);
        Assert.False(ShipPilot.IsUsingCamera);
        Assert.False(ShipPilot.AllowsMovement);
        Assert.False(ShipPilot.IsSeated);
        ShipPilot.Unmount(null);
        ShipPilot.OnConsoleUnavailable(null);
    }

    [Fact]
    public void BindingTool_DoesNotEnterWithoutAConsole()
    {
        var binding = new ShipBindingTool();

        binding.Enter(null);

        Assert.False(binding.IsActive);
        Assert.Null(binding.DescribeHovered(null));
    }
}
