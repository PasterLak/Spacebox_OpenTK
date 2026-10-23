namespace Spacebox.Game.Resource.Content;

public static class ExtensionTargets
{
    public const string Any = "*";

    public static IReadOnlyList<string> Parse(string extends)
    {
        var targets = new List<string>();
        if (string.IsNullOrWhiteSpace(extends)) return targets;
        foreach (var part in extends.Split(','))
        {
            string target = part.Trim();
            if (target.Length > 0) targets.Add(target);
        }
        return targets;
    }

    public static bool IsAny(string extends)
    {
        foreach (var target in Parse(extends))
            if (target == Any) return true;
        return false;
    }

    public static bool Matches(string extends, InstalledGameSet baseSet)
    {
        if (baseSet == null || baseSet.IsEmpty || baseSet.IsExtension) return false;
        foreach (var target in Parse(extends))
            if (target == Any || baseSet.Matches(target)) return true;
        return false;
    }

    public static string Describe(string extends) => IsAny(extends) ? "any GameSet" : string.Join(", ", Parse(extends));
}
