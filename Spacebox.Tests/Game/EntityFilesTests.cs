using Spacebox.Game.Generation.Tools;

namespace Spacebox.Tests;

public class EntityFilesTests
{
    [Theory]
    [InlineData("123", "123")]
    [InlineData("123.entity", "123")]
    [InlineData("123.entity.entity", "123")]
    [InlineData("ship_1", "ship_1")]
    public void NameOf_StripsAllEntityExtensions(string input, string expected)
    {
        Assert.Equal(expected, EntityFiles.NameOf(input));
        Assert.Equal(expected + ".entity", EntityFiles.FileName(input));
    }

    [Fact]
    public void RepairDoubleExtensions_KeepsNewestSave()
    {
        string folder = Directory.CreateTempSubdirectory().FullName;
        try
        {
            string oldFile = Path.Combine(folder, "7.entity");
            string newFile = Path.Combine(folder, "7.entity.entity");
            File.WriteAllText(oldFile, "old");
            File.WriteAllText(newFile, "new");
            File.SetLastWriteTimeUtc(oldFile, DateTime.UtcNow.AddHours(-1));

            EntityFiles.RepairDoubleExtensions(folder);

            Assert.False(File.Exists(newFile));
            Assert.Equal("new", File.ReadAllText(oldFile));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void RepairDoubleExtensions_DropsOutdatedDuplicate()
    {
        string folder = Directory.CreateTempSubdirectory().FullName;
        try
        {
            string current = Path.Combine(folder, "7.entity");
            string stale = Path.Combine(folder, "7.entity.entity");
            File.WriteAllText(current, "current");
            File.WriteAllText(stale, "stale");
            File.SetLastWriteTimeUtc(stale, DateTime.UtcNow.AddHours(-1));

            EntityFiles.RepairDoubleExtensions(folder);

            Assert.False(File.Exists(stale));
            Assert.Equal("current", File.ReadAllText(current));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
