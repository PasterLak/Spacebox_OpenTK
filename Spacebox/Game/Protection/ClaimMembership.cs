namespace Spacebox.Game.Protection;

public static class ClaimMembership
{
    public static string Sanitize(string name)
    {
        string trimmed = name?.Trim() ?? "";
        return trimmed.Length > ClaimSettings.MaxNameLength ? trimmed.Substring(0, ClaimSettings.MaxNameLength) : trimmed;
    }

    public static bool SameName(string a, string b) => string.Equals(Sanitize(a), Sanitize(b), StringComparison.OrdinalIgnoreCase);

    public static bool IsAllowed(string owner, IReadOnlyList<string> members, string player)
    {
        if (string.IsNullOrWhiteSpace(player)) return false;
        if (SameName(owner, player)) return true;

        for (int i = 0; i < members.Count; i++)
        {
            if (SameName(members[i], player)) return true;
        }
        return false;
    }

    public static bool TryAdd(List<string> members, string owner, string name, int maxMembers)
    {
        string clean = Sanitize(name);
        if (clean.Length == 0 || SameName(owner, clean) || members.Count >= maxMembers) return false;

        for (int i = 0; i < members.Count; i++)
        {
            if (SameName(members[i], clean)) return false;
        }

        members.Add(clean);
        return true;
    }

    public static bool Remove(List<string> members, string name)
    {
        for (int i = members.Count - 1; i >= 0; i--)
        {
            if (!SameName(members[i], name)) continue;
            members.RemoveAt(i);
            return true;
        }
        return false;
    }
}
