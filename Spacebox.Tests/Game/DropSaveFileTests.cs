using Spacebox.Game.Player;
using Xunit;

namespace Spacebox.Tests.Game;

public class DropSaveFileTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "spacebox_drops_" + Guid.NewGuid().ToString("N"));

    public DropSaveFileTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        Directory.Delete(_folder, true);
    }

    [Fact]
    public void SavingNoDrops_ClearsPreviouslySavedDrops()
    {
        string path = Path.Combine(_folder, "drop.json");
        DropSaveManager.SaveToFile(new List<DropSaveData> { new DropSaveData { ItemId = "iron", Quantity = 5 } }, path);

        DropSaveManager.SaveToFile(new List<DropSaveData>(), path);

        Assert.Empty(DropSaveManager.LoadFromFile(path));
    }
}
