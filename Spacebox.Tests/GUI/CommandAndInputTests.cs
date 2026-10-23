using System.Text.Json;
using Spacebox.Game.Ships.Sharing;
using Spacebox.Network.Players;

namespace Spacebox.Tests.GUI;

public class CommandAndInputTests
{
    [Fact]
    public void KillReason_IsAnnouncedAsKilledByTheCommand()
    {
        Assert.Equal("Ann was killed by the command", DeathMessageFormatter.Format("Ann", null, DeathMessageFormatter.CommandReason));
        Assert.Equal("Ann died: Fell", DeathMessageFormatter.Format("Ann", null, "Fell"));
    }

    [Theory]
    [InlineData(ShipPlacementMode.YawAction)]
    [InlineData(ShipPlacementMode.PitchAction)]
    [InlineData(ShipPlacementMode.RollAction)]
    [InlineData(ShipPlacementMode.ReverseAction)]
    [InlineData(ShipPlacementMode.FineAction)]
    [InlineData(ShipPlacementMode.AlignAction)]
    public void ShipPlacementKeys_AreInTheInputFile(string action)
    {
        Assert.Contains(action, InputActionIds());
    }

    private static HashSet<string> InputActionIds()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Spacebox", "Resources", "default_input.json"))) dir = dir.Parent;
        Assert.NotNull(dir);
        var root = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir.FullName, "Spacebox", "Resources", "default_input.json"))).RootElement;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var profile in root.EnumerateObject())
            foreach (var action in profile.Value.GetProperty("Actions").EnumerateObject())
                ids.Add(action.Name);
        return ids;
    }
}
