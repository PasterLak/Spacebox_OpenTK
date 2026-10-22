using Engine.Utils;
using Xunit;

namespace Engine.Tests;

public class AtomicFileTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "engine_atomic_" + Guid.NewGuid().ToString("N"));

    public AtomicFileTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        Directory.Delete(_folder, true);
    }

    [Fact]
    public void WriteAllText_ReplacesExistingFile()
    {
        string path = Path.Combine(_folder, "data.json");
        File.WriteAllText(path, "old");

        AtomicFile.WriteAllText(path, "new");

        Assert.Equal("new", File.ReadAllText(path));
        Assert.False(File.Exists(path + AtomicFile.TempSuffix));
    }

    [Fact]
    public void FailedWrite_KeepsPreviousFile()
    {
        string path = Path.Combine(_folder, "data.json");
        File.WriteAllText(path, "old");

        Assert.Throws<InvalidOperationException>(() => AtomicFile.Write(path, temp =>
        {
            File.WriteAllText(temp, "partial");
            throw new InvalidOperationException();
        }));

        Assert.Equal("old", File.ReadAllText(path));
        Assert.False(File.Exists(path + AtomicFile.TempSuffix));
    }
}
