namespace Spacebox.Network.Moderation;

public sealed class BannedAddressEntry
{
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
}

public sealed class ModerationData
{
    public List<string> BannedNames { get; set; } = new List<string>();
    public List<string> BannedAddresses { get; set; } = new List<string>();
    public List<string> MutedNames { get; set; } = new List<string>();
    public List<BannedAddressEntry> BannedAddressOwners { get; set; } = new List<BannedAddressEntry>();

    public void ApplyMissingDefaults()
    {
        BannedNames ??= new List<string>();
        BannedAddresses ??= new List<string>();
        MutedNames ??= new List<string>();
        BannedAddressOwners ??= new List<BannedAddressEntry>();
        BannedNames.RemoveAll(string.IsNullOrWhiteSpace);
        BannedAddresses.RemoveAll(string.IsNullOrWhiteSpace);
        MutedNames.RemoveAll(string.IsNullOrWhiteSpace);
        BannedAddressOwners.RemoveAll(entry => entry == null || string.IsNullOrWhiteSpace(entry.Name) || string.IsNullOrWhiteSpace(entry.Address));
    }

    public bool IsBanned(string name, string address)
    {
        return Contains(BannedNames, name?.Trim()) || (!string.IsNullOrEmpty(address) && Contains(BannedAddresses, address));
    }

    public bool IsMuted(string name) => Contains(MutedNames, name?.Trim());

    public bool BanName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        return AddUnique(BannedNames, name.Trim());
    }

    public void BanAddress(string name, string address)
    {
        if (string.IsNullOrWhiteSpace(address)) return;

        AddUnique(BannedAddresses, address);
        if (string.IsNullOrWhiteSpace(name) || HasOwner(name.Trim(), address)) return;
        BannedAddressOwners.Add(new BannedAddressEntry { Name = name.Trim(), Address = address });
    }

    public bool UnbanName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        string trimmed = name.Trim();
        bool removed = BannedNames.RemoveAll(entry => Same(entry, trimmed)) > 0;
        for (int i = BannedAddressOwners.Count - 1; i >= 0; i--)
        {
            var entry = BannedAddressOwners[i];
            if (!Same(entry.Name, trimmed)) continue;

            BannedAddressOwners.RemoveAt(i);
            if (!IsAddressOwnedByBannedName(entry.Address) && BannedAddresses.RemoveAll(address => Same(address, entry.Address)) > 0) removed = true;
        }
        return removed;
    }

    public bool UnbanAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address)) return false;

        BannedAddressOwners.RemoveAll(entry => Same(entry.Address, address));
        return BannedAddresses.RemoveAll(entry => Same(entry, address)) > 0;
    }

    public bool SetMuted(string name, bool muted)
    {
        if (string.IsNullOrWhiteSpace(name) || IsMuted(name) == muted) return false;

        string trimmed = name.Trim();
        if (muted) AddUnique(MutedNames, trimmed);
        else MutedNames.RemoveAll(entry => Same(entry, trimmed));
        return true;
    }

    public ModerationData Copy()
    {
        var copy = new ModerationData
        {
            BannedNames = new List<string>(BannedNames),
            BannedAddresses = new List<string>(BannedAddresses),
            MutedNames = new List<string>(MutedNames)
        };
        foreach (var entry in BannedAddressOwners) copy.BannedAddressOwners.Add(new BannedAddressEntry { Name = entry.Name, Address = entry.Address });
        return copy;
    }

    private bool IsAddressOwnedByBannedName(string address)
    {
        foreach (var entry in BannedAddressOwners)
        {
            if (Same(entry.Address, address) && Contains(BannedNames, entry.Name)) return true;
        }
        return false;
    }

    private bool HasOwner(string name, string address)
    {
        foreach (var entry in BannedAddressOwners)
        {
            if (Same(entry.Name, name) && Same(entry.Address, address)) return true;
        }
        return false;
    }

    private static bool Same(string? a, string? b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static bool Contains(List<string> list, string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;

        for (int i = 0; i < list.Count; i++)
        {
            if (Same(list[i], value)) return true;
        }

        return false;
    }

    private static bool AddUnique(List<string> list, string value)
    {
        if (Contains(list, value)) return false;
        list.Add(value);
        return true;
    }
}
