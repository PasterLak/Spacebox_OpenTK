using OpenTK.Mathematics;
using Spacebox.Game.Player;
using Spacebox.Game.Tutorial;
using Spacebox.Game.Tutorial.Tasks;
using Xunit;

namespace Spacebox.Tests.Tutorial;

public class TutorialTaskTests
{
    private static ITutorialTask Task(string taskJson)
    {
        var tutorial = TutorialTestData.Parse($$"""{ "Chapters": [ { "Steps": [ { "Tasks": [ {{taskJson}} ] } ] } ] }""");
        return TutorialTaskFactory.Create(tutorial.Chapters[0].Steps[0].Tasks[0]);
    }

    [Fact]
    public void Press_CountsPresses()
    {
        var context = new FakeContext();
        var task = Task("""{ "Type": "press", "Action": "forward", "Count": 2 }""");
        task.Start(context);

        task.Update(context, 0.1f);
        context.Pressed.Add("forward");
        task.Update(context, 0.1f);
        Assert.False(task.IsDone);
        task.Update(context, 0.1f);

        Assert.True(task.IsDone);
        Assert.Equal(2, task.Current);
    }

    [Fact]
    public void Hold_AccumulatesSeconds()
    {
        var context = new FakeContext();
        var task = Task("""{ "Type": "hold", "Action": "sprint", "Seconds": 1 }""");
        context.Held.Add("sprint");

        task.Update(context, 0.6f);
        Assert.False(task.IsDone);
        task.Update(context, 0.6f);
        Assert.True(task.IsDone);
        Assert.False(task.ShowsCounter);
    }

    [Fact]
    public void ActivityCount_FiltersByItemAndMachine()
    {
        var task = Task("""{ "Type": "process", "Item": "aluminium_ore", "Machine": "furnace", "Count": 3 }""");

        task.OnActivity(new PlayerActivityEvent(PlayerActivityKind.ItemProcessed, 10, 2, "crusher"));
        task.OnActivity(new PlayerActivityEvent(PlayerActivityKind.ItemProcessed, 11, 2, "furnace"));
        task.OnActivity(new PlayerActivityEvent(PlayerActivityKind.ItemCrafted, 10, 2, "furnace"));
        Assert.Equal(0, task.Current);

        task.OnActivity(new PlayerActivityEvent(PlayerActivityKind.ItemProcessed, 10, 5, "Furnace"));
        Assert.True(task.IsDone);
        Assert.Equal(3, task.Current);
    }

    [Fact]
    public void PlaceAnyBlock_CountsEveryBlock()
    {
        var task = Task("""{ "Type": "place_block", "Count": 2 }""");

        task.OnActivity(new PlayerActivityEvent(PlayerActivityKind.BlockPlaced, 20, 1, null));
        task.OnActivity(new PlayerActivityEvent(PlayerActivityKind.BlockPlaced, 21, 1, null));

        Assert.True(task.IsDone);
    }

    [Fact]
    public void HaveItem_TracksInventoryAndStaysDone()
    {
        var context = new FakeContext();
        var task = Task("""{ "Type": "have_item", "Item": "aluminium_ore", "Count": 5 }""");

        context.Items[10] = 3;
        task.Update(context, 0f);
        Assert.Equal(3, task.Current);

        context.Items[10] = 6;
        task.Update(context, 0f);
        context.Items[10] = 0;
        task.Update(context, 0f);

        Assert.True(task.IsDone);
        Assert.Equal(5, task.Current);
    }

    [Fact]
    public void Move_UsesStatDeltaAndRestoredProgress()
    {
        var context = new FakeContext();
        context.Stats[nameof(PlayerStatistics.DistanceTraveled)] = 100;
        var task = Task("""{ "Type": "move", "Distance": 20 }""");

        task.Restore(5);
        task.Start(context);
        context.Stats[nameof(PlayerStatistics.DistanceTraveled)] = 110;
        task.Update(context, 0f);
        Assert.Equal(15, task.Current);

        context.Stats[nameof(PlayerStatistics.DistanceTraveled)] = 130;
        task.Update(context, 0f);
        Assert.True(task.IsDone);
    }

    [Fact]
    public void Look_SumsTurnedDegrees()
    {
        var context = new FakeContext();
        var task = Task("""{ "Type": "look", "Degrees": 90 }""");

        context.ViewDirection = -Vector3.UnitZ;
        task.Update(context, 0f);
        context.ViewDirection = Vector3.Normalize(new Vector3(1f, 0f, -1f));
        task.Update(context, 0f);
        Assert.Equal(45f, task.Current, 1);

        context.ViewDirection = Vector3.UnitX;
        task.Update(context, 0f);
        Assert.True(task.IsDone);
    }

    [Fact]
    public void Conditions_CheckContextState()
    {
        var context = new FakeContext();
        var magnet = Task("""{ "Type": "toggle", "What": "magnet" }""");
        var window = Task("""{ "Type": "open_ui", "Window": "inventory" }""");
        var hotbar = Task("""{ "Type": "select_hotbar", "Item": "furnace" }""");
        var pilot = Task("""{ "Type": "pilot", "Flight": true }""");
        var equip = Task("""{ "Type": "equip", "Slot": "Helmet" }""");

        magnet.Update(context, 0f);
        window.Update(context, 0f);
        Assert.False(magnet.IsDone || window.IsDone);

        context.IsMagnetEnabled = true;
        context.OpenWindows.Add("inventory");
        context.SelectedItemId = 20;
        context.IsPiloting = true;
        context.Equipped.Add(50);
        magnet.Update(context, 0f);
        window.Update(context, 0f);
        hotbar.Update(context, 0f);
        pilot.Update(context, 0f);
        equip.Update(context, 0f);

        Assert.True(magnet.IsDone && window.IsDone && hotbar.IsDone && equip.IsDone);
        Assert.False(pilot.IsDone);
        context.IsPilotingFlight = true;
        pilot.Update(context, 0f);
        Assert.True(pilot.IsDone);
    }

    [Fact]
    public void Composite_AllAndAny()
    {
        var context = new FakeContext();
        var all = Task("""{ "Type": "all_of", "Tasks": [ { "Type": "have_item", "Item": "aluminium_ore" }, { "Type": "have_item", "Item": "cobaltite_rock" } ] }""");
        var any = Task("""{ "Type": "any_of", "Tasks": [ { "Type": "have_item", "Item": "aluminium_ore" }, { "Type": "have_item", "Item": "cobaltite_rock" } ] }""");

        context.Items[11] = 1;
        all.Update(context, 0f);
        any.Update(context, 0f);

        Assert.False(all.IsDone);
        Assert.Equal(1, all.Current);
        Assert.True(any.IsDone);

        context.Items[10] = 1;
        all.Update(context, 0f);
        Assert.True(all.IsDone);
    }

    [Fact]
    public void Factory_AcceptsNewTypes()
    {
        TutorialTaskFactory.Register("test_custom", d => new WaitTask(d));

        Assert.True(TutorialTaskFactory.IsKnown("TEST_CUSTOM"));
        Assert.NotNull(Task("""{ "Type": "test_custom", "Seconds": 1 }"""));
    }
}
