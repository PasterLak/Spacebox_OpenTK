namespace Engine;

public sealed class ResourceScope
{
    private readonly object _sync = new object();
    private readonly Dictionary<IResource, string> _tracked = new Dictionary<IResource, string>(ReferenceEqualityComparer.Instance);
    private readonly List<KeyValuePair<IResource, string>> _drain = new List<KeyValuePair<IResource, string>>();

    public ResourceScope(string name, bool reportsLeaks)
    {
        Name = name;
        ReportsLeaks = reportsLeaks;
    }

    public string Name { get; }
    public bool ReportsLeaks { get; }

    public int Count
    {
        get
        {
            lock (_sync) return _tracked.Count;
        }
    }

    public void Track(IResource resource, string origin = null)
    {
        if (resource == null) return;
        lock (_sync) _tracked[resource] = origin;
    }

    public bool Untrack(IResource resource)
    {
        if (resource == null) return false;
        lock (_sync) return _tracked.Remove(resource);
    }

    public bool Contains(IResource resource)
    {
        if (resource == null) return false;
        lock (_sync) return _tracked.ContainsKey(resource);
    }

    public string OriginOf(IResource resource)
    {
        if (resource == null) return null;
        lock (_sync) return _tracked.TryGetValue(resource, out var origin) ? origin : null;
    }

    public List<string> DisposeAll()
    {
        lock (_sync)
        {
            _drain.AddRange(_tracked);
            _tracked.Clear();
        }

        var leaks = new List<string>();
        for (int i = 0; i < _drain.Count; i++)
        {
            var resource = _drain[i].Key;
            if (resource.IsDisposed) continue;

            if (ReportsLeaks) leaks.Add(Describe(resource, _drain[i].Value));
            resource.Dispose();
        }
        _drain.Clear();
        return leaks;
    }

    public static string Describe(IResource resource, string origin)
    {
        string info = resource.GetResourceInfo();
        return string.IsNullOrEmpty(origin) ? info : info + " (created by " + origin + ")";
    }
}
