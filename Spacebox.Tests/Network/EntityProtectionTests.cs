using Engine.Networking;
using Spacebox.Game.Protection;
using Spacebox.Network;

namespace Spacebox.Tests.Network;

public class EntityProtectionTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "spacebox-protection-" + Guid.NewGuid().ToString("N"));

    public EntityProtectionTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        EntityProtection.Reset();
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }

    [Fact]
    public void ProtectedIds_AreSavedAndLoadedWithTheWorld()
    {
        EntityProtection.Load(_folder);
        Assert.True(EntityProtection.Set(42, true));
        Assert.False(EntityProtection.Set(42, true));
        Assert.True(EntityProtection.Set(7, true));
        Assert.True(EntityProtection.Set(7, false));

        EntityProtection.Reset();
        Assert.False(EntityProtection.IsProtected(42));

        EntityProtection.Load(_folder);
        Assert.True(EntityProtection.IsProtected(42));
        Assert.False(EntityProtection.IsProtected(7));
        Assert.True(File.Exists(Path.Combine(_folder, EntityProtection.FileName)));
    }

    [Fact]
    public void WorldWithoutFile_HasNothingProtected()
    {
        EntityProtection.Load(_folder);

        Assert.Empty(EntityProtection.Ids);
        Assert.True(EntityProtection.LocalCanEdit);
    }

    [Fact]
    public void BrokenFile_IsIgnored()
    {
        File.WriteAllText(Path.Combine(_folder, EntityProtection.FileName), "not json");

        EntityProtection.Load(_folder);

        Assert.Empty(EntityProtection.Ids);
    }

    [Fact]
    public void ReplaceAll_TakesTheServerList()
    {
        EntityProtection.ReplaceAll(new long[] { 1, 2, 3 });

        Assert.Equal(3, EntityProtection.Ids.Count);
        Assert.True(EntityProtection.IsProtected(2));
    }

    [Fact]
    public void DeniedMessage_IsThrottled()
    {
        Assert.True(EntityProtection.ShouldShowDenied(10f, float.NegativeInfinity));
        Assert.False(EntityProtection.ShouldShowDenied(10.5f, 10f));
        Assert.True(EntityProtection.ShouldShowDenied(10f + EntityProtection.DeniedMessageInterval, 10f));
    }

    [Fact]
    public void StateMessage_RoundTrips()
    {
        var writer = new NetWriter();
        new ProtectionStateMessage { EntityIds = new long[] { 5, long.MaxValue }, CanEdit = true }.Serialize(writer);

        var copy = new ProtectionStateMessage();
        copy.Deserialize(new NetReader(writer.ToArray()));

        Assert.Equal(new long[] { 5, long.MaxValue }, copy.EntityIds);
        Assert.True(copy.CanEdit);
    }
}
