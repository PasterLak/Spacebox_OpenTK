namespace Engine.Tests.Core;

[Collection(GlobalStateCollection.Name)]
public class ResourcesTests
{
    private sealed class FakeResource : IResource
    {
        public static int Loads;

        public bool IsDisposed { get; set; }
        public string Path { get; private set; } = "";

        public IResource Load(string path)
        {
            if (path.Contains("broken")) throw new IOException("cannot read " + path);

            Loads++;
            Path = path;
            return this;
        }

        public string GetResourceInfo() => Path;
        public int GetHandle() => 0;
        public void Dispose() => IsDisposed = true;
    }

    private sealed class ResourceWithoutFallback : IResource
    {
        public bool IsDisposed { get; set; }
        public IResource Load(string path) => throw new IOException("always fails");
        public string GetResourceInfo() => "";
        public int GetHandle() => 0;
        public void Dispose() => IsDisposed = true;
    }

    [Fact]
    public void Load_CachesByNormalizedPathAndShortName()
    {
        int before = FakeResource.Loads;

        var first = Resources.Load<FakeResource>("Test/Models/ship.obj");
        var sameWithoutExtension = Resources.Load<FakeResource>("Test\\Models\\ship");
        var byShortName = Resources.Load<FakeResource>("ship");

        Assert.Same(first, sameWithoutExtension);
        Assert.Same(first, byShortName);
        Assert.Equal(before + 1, FakeResource.Loads);
        Assert.True(Resources.Has<FakeResource>("Test/Models/ship"));

        Resources.Unload<FakeResource>("Test/Models/ship");

        Assert.True(first.IsDisposed);
        Assert.False(Resources.Has<FakeResource>("Test/Models/ship"));
    }

    [Fact]
    public void UnloadAll_KeepsGlobalUnlessRequested()
    {
        var sceneResource = Resources.Load<FakeResource>("Test/scene_only");
        var globalResource = Resources.Load<FakeResource>("Test/always", true);

        Resources.UnloadAll();

        Assert.True(sceneResource.IsDisposed);
        Assert.False(globalResource.IsDisposed);
        Assert.False(Resources.Has<FakeResource>("Test/scene_only"));
        Assert.True(Resources.Has<FakeResource>("Test/always"));

        Resources.Unload<FakeResource>("Test/always");
    }

    [Fact]
    public void AddResource_RegistersCodeCreatedResource()
    {
        var created = new FakeResource();

        Resources.AddResource("generated_texture", created, false);

        Assert.Same(created, Resources.Get<FakeResource>("generated_texture"));
        Resources.Unload<FakeResource>("generated_texture");
    }

    [Fact]
    public void FailedLoad_ReturnsRegisteredFallback()
    {
        var fallback = new FakeResource();
        ResourceFallbacks.Register("fake_fallback", fallback);

        var loaded = Resources.Load<FakeResource>("Test/broken_file");

        Assert.Same(fallback, loaded);
        Assert.False(Resources.Has<FakeResource>("Test/broken_file"));
        Resources.Unload<FakeResource>("fake_fallback");
    }

    [Fact]
    public void FailedLoad_WithoutFallback_ThrowsClearError()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Resources.Load<ResourceWithoutFallback>("Test/anything"));

        Assert.Contains(nameof(ResourceWithoutFallback), error.Message);
    }
}
