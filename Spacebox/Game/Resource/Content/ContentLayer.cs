namespace Spacebox.Game.Resource.Content;

public sealed class ContentLayer
{
    public string Id { get; }
    public string Root { get; }

    public ContentLayer(string id, string root)
    {
        Id = id ?? "";
        Root = root ?? "";
    }

    public string PathOf(params string[] parts)
    {
        var all = new string[parts.Length + 1];
        all[0] = Root;
        Array.Copy(parts, 0, all, 1, parts.Length);
        return Path.Combine(all);
    }

    public override string ToString() => $"{Id} ({Root})";
}
