namespace Spacebox.Game.Resource.Content;

public sealed class ExtensionResolution
{
    public IReadOnlyList<InstalledGameSet> Order { get; }
    public IReadOnlyList<string> Errors { get; }
    public bool Success => Errors.Count == 0;

    public ExtensionResolution(IReadOnlyList<InstalledGameSet> order, IReadOnlyList<string> errors)
    {
        Order = order;
        Errors = errors;
    }

    public string ErrorText => string.Join("\n", Errors);
}

public sealed class ExtensionResolver
{
    private enum VisitState { Visiting, Done }

    private readonly InstalledGameSet _baseSet;
    private readonly IReadOnlyList<InstalledGameSet> _available;
    private readonly Dictionary<InstalledGameSet, VisitState> _states = new Dictionary<InstalledGameSet, VisitState>();
    private readonly List<InstalledGameSet> _order = new List<InstalledGameSet>();
    private readonly List<string> _errors = new List<string>();
    private readonly List<InstalledGameSet> _path = new List<InstalledGameSet>();

    private ExtensionResolver(InstalledGameSet baseSet, IReadOnlyList<InstalledGameSet> available)
    {
        _baseSet = baseSet;
        _available = available;
    }

    public static ExtensionResolution Resolve(InstalledGameSet baseSet, IReadOnlyList<InstalledGameSet> available, IReadOnlyList<string> requested)
    {
        var resolver = new ExtensionResolver(baseSet, available);
        resolver.Run(requested ?? Array.Empty<string>());
        return new ExtensionResolution(resolver._order, resolver._errors);
    }

    private void Run(IReadOnlyList<string> requested)
    {
        if (_baseSet == null)
        {
            _errors.Add("The base GameSet is not installed");
            return;
        }

        if (_baseSet.IsExtension)
        {
            _errors.Add($"'{_baseSet.Id}' is an extension of '{_baseSet.Config.Extends}' and cannot be used as the base GameSet");
            return;
        }

        foreach (var id in requested)
        {
            if (string.IsNullOrWhiteSpace(id)) continue;

            var extension = InstalledGameSets.Find(_available, id);
            if (extension == null)
            {
                _errors.Add($"Extension '{id}' is not installed");
                continue;
            }

            Visit(extension);
        }
    }

    private void Visit(InstalledGameSet extension)
    {
        if (_states.TryGetValue(extension, out var state))
        {
            if (state == VisitState.Visiting) _errors.Add($"Extension cycle: {CyclePath(extension)}");
            return;
        }

        if (!extension.IsExtension)
        {
            _errors.Add(extension.IsBasedOnEmpty
                ? $"'{extension.Id}' is a GameSet built on Empty, not an extension"
                : $"'{extension.Id}' is not an extension (its config.json has no \"Extends\")");
            _states[extension] = VisitState.Done;
            return;
        }

        if (!_baseSet.Matches(extension.Config.Extends))
        {
            _errors.Add($"Extension '{extension.Id}' extends '{extension.Config.Extends}', but the base GameSet is '{_baseSet.Id}'");
            _states[extension] = VisitState.Done;
            return;
        }

        _states[extension] = VisitState.Visiting;
        _path.Add(extension);

        foreach (var includeId in extension.Includes)
        {
            if (string.IsNullOrWhiteSpace(includeId)) continue;

            var include = InstalledGameSets.Find(_available, includeId);
            if (include == null)
            {
                _errors.Add($"Extension '{extension.Id}' includes '{includeId}', which is not installed");
                continue;
            }

            Visit(include);
        }

        _path.RemoveAt(_path.Count - 1);
        _states[extension] = VisitState.Done;
        _order.Add(extension);
    }

    private string CyclePath(InstalledGameSet repeated)
    {
        int start = _path.IndexOf(repeated);
        var names = new List<string>();
        for (int i = Math.Max(0, start); i < _path.Count; i++) names.Add(_path[i].Id);
        names.Add(repeated.Id);
        return string.Join(" -> ", names);
    }
}
