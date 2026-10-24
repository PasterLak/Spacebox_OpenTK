namespace Engine;

public static class ResourceScopes
{
    public static ResourceScope App { get; private set; } = new ResourceScope("App", false);
    public static ResourceScope Scene { get; private set; } = new ResourceScope("Scene", true);

    public static T Track<T>(T resource) where T : IResource
    {
        Scene.Track(resource, CaptureOrigin());
        return resource;
    }

    public static T Own<T>(T resource) where T : IResource
    {
        Forget(resource);
        return resource;
    }

    public static T KeepForApp<T>(T resource) where T : IResource
    {
        if (resource == null) return resource;
        string origin = Scene.OriginOf(resource);
        Scene.Untrack(resource);
        App.Track(resource, origin);
        return resource;
    }

    public static void Forget(IResource resource)
    {
        if (resource == null) return;
        Scene.Untrack(resource);
        App.Untrack(resource);
    }

    public static List<string> EndScene()
    {
        var ended = Scene;
        Scene = new ResourceScope("Scene", true);
        return Report(ended);
    }

    public static List<string> Shutdown()
    {
        var leaks = EndScene();
        App.DisposeAll();
        App = new ResourceScope("App", false);
        return leaks;
    }

    private static List<string> Report(ResourceScope scope)
    {
        var leaks = scope.DisposeAll();
        for (int i = 0; i < leaks.Count; i++)
            Debug.Error($"[Resources] Leaked {scope.Name.ToLowerInvariant()} resource disposed by the safety net: {leaks[i]}");
        return leaks;
    }

    private static string CaptureOrigin()
    {
#if DEBUG
        return ResourceOrigin.Describe(new System.Diagnostics.StackTrace(2, false));
#else
        return null;
#endif
    }
}
