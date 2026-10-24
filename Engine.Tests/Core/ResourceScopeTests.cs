namespace Engine.Tests.Core;

[Collection(GlobalStateCollection.Name)]
public class ResourceScopeTests
{
    private sealed class CountingResource : IResource
    {
        private readonly string _name;

        public CountingResource(string name)
        {
            _name = name;
        }

        public bool IsDisposed { get; set; }
        public int Disposals { get; private set; }

        public IResource Load(string path) => this;
        public string GetResourceInfo() => _name;
        public int GetHandle() => 0;

        public void Dispose()
        {
            Disposals++;
            IsDisposed = true;
        }
    }

    [Fact]
    public void EndScene_DisposesSceneResourcesButNotAppResources()
    {
        var sceneResource = ResourceScopes.Track(new CountingResource("scene"));
        var appResource = ResourceScopes.KeepForApp(ResourceScopes.Track(new CountingResource("app")));

        ResourceScopes.EndScene();

        Assert.True(sceneResource.IsDisposed);
        Assert.False(appResource.IsDisposed);
        Assert.True(ResourceScopes.App.Contains(appResource));

        ResourceScopes.Forget(appResource);
    }

    [Fact]
    public void OwnedResources_SurviveSceneEnd()
    {
        var owned = ResourceScopes.Own(ResourceScopes.Track(new CountingResource("owned")));

        ResourceScopes.EndScene();

        Assert.False(owned.IsDisposed);
        Assert.False(ResourceScopes.Scene.Contains(owned));
    }

    [Fact]
    public void DisposedResources_AreForgottenAndNotReportedAsLeaks()
    {
        var resource = ResourceScopes.Track(new CountingResource("disposed"));
        resource.Dispose();
        ResourceScopes.Forget(resource);

        var leaks = ResourceScopes.EndScene();

        Assert.DoesNotContain(leaks, leak => leak.Contains("disposed"));
        Assert.Equal(1, resource.Disposals);
    }

    [Fact]
    public void LeakedResources_AreReportedWithTheirOrigin()
    {
        var scope = new ResourceScope("Scene", true);
        var leaked = new CountingResource("leaked texture");
        scope.Track(leaked, "Owner.Create");

        var leaks = scope.DisposeAll();

        Assert.Single(leaks);
        Assert.Contains("leaked texture", leaks[0]);
        Assert.Contains("created by Owner.Create", leaks[0]);
        Assert.True(leaked.IsDisposed);
        Assert.Equal(0, scope.Count);
    }

    [Fact]
    public void AppScope_DisposesWithoutReportingLeaks()
    {
        var scope = new ResourceScope("App", false);
        var resource = new CountingResource("font atlas");
        scope.Track(resource, "Font.Bake");

        var leaks = scope.DisposeAll();

        Assert.Empty(leaks);
        Assert.True(resource.IsDisposed);
    }

#if DEBUG
    [Fact]
    public void Track_RecordsTheCreatorInDebugBuilds()
    {
        var resource = ResourceScopes.Track(new CountingResource("with origin"));

        string origin = ResourceScopes.Scene.OriginOf(resource);

        Assert.Contains(nameof(ResourceScopeTests), origin);
        ResourceScopes.Forget(resource);
    }
#endif

    [Fact]
    public void CachedResource_IsUntrackedAndReleasedOnlyByTheCache()
    {
        var resource = ResourceScopes.Track(new CountingResource("cached"));

        Resources.AddResource("Test/Scopes/cached", resource, false);

        Assert.False(ResourceScopes.Scene.Contains(resource));
        Assert.True(Resources.IsCacheOwned(resource));
        Assert.True(Resources.BlocksDispose(resource));
        Assert.False(Resources.Release(resource));
        Assert.False(resource.IsDisposed);

        Resources.Unload<CountingResource>("Test/Scopes/cached");

        Assert.Equal(1, resource.Disposals);
        Assert.False(Resources.IsCacheOwned(resource));
    }

    [Fact]
    public void CacheEntriesSharingOneResource_DisposeItOnceAfterTheLastRelease()
    {
        var resource = new CountingResource("shared");
        Resources.AddResource("Test/Scopes/first", resource, false);
        Resources.AddResource("Test/Scopes/second", resource, false);

        Resources.Unload<CountingResource>("Test/Scopes/first");
        Assert.Equal(0, resource.Disposals);

        Resources.Unload<CountingResource>("Test/Scopes/second");
        Resources.Unload<CountingResource>("Test/Scopes/second");
        Resources.Unload<CountingResource>("Test/Scopes/first");

        Assert.Equal(1, resource.Disposals);
    }

    [Fact]
    public void ReplacingACacheEntry_ReleasesThePreviousResource()
    {
        var first = new CountingResource("first");
        var second = new CountingResource("second");

        Resources.AddResource("Test/Scopes/replaced", first, false);
        Resources.AddResource("Test/Scopes/replaced", first, false);
        Resources.AddResource("Test/Scopes/replaced", second, false);

        Assert.Equal(1, first.Disposals);
        Assert.False(second.IsDisposed);

        Resources.Unload<CountingResource>("Test/Scopes/replaced");
        Assert.Equal(1, second.Disposals);
    }

    [Fact]
    public void Release_DisposesUncachedResourcesOnceAndIsSafeTwice()
    {
        var resource = new CountingResource("uncached");

        Assert.True(Resources.Release(resource));
        Resources.Release(resource);

        Assert.True(resource.IsDisposed);
        Assert.Equal(1, resource.Disposals);
    }
}
