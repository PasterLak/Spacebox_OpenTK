using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;

namespace Spacebox.Game.Waypoints;

public static class WaypointService
{
    private static readonly List<Tag> _waypoints = new List<Tag>();
    private static readonly List<string> _names = new List<string>();

    public static IReadOnlyList<Tag> Collect()
    {
        _waypoints.Clear();
        TagManager.Instance?.CollectStaticTags(_waypoints);
        _waypoints.Sort((a, b) => string.Compare(a.Text, b.Text, StringComparison.OrdinalIgnoreCase));
        return _waypoints;
    }

    public static bool Contains(Tag tag)
    {
        if (tag == null) return false;

        var waypoints = Collect();
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] == tag) return true;
        }
        return false;
    }

    public static Tag Add(Vector3 position)
    {
        if (TagManager.Instance == null) return null;

        var waypoints = Collect();
        _names.Clear();
        for (int i = 0; i < waypoints.Count; i++) _names.Add(waypoints[i].Text);

        var tag = TagManager.Instance.CreateTag(WaypointDefaults.NextName(_names), position, WaypointDefaults.ColorFor(waypoints.Count), true);
        PlayerActivity.Raise(PlayerActivityKind.WaypointCreated);
        Save();
        return tag;
    }

    public static void Remove(Tag tag)
    {
        if (tag == null || TagManager.Instance == null) return;

        TagManager.Instance.ReleaseTag(tag);
        Save();
    }

    public static void Rename(Tag tag, string name)
    {
        if (tag != null) tag.Text = WaypointDefaults.CleanName(name, tag.Text);
    }

    public static void SetColor(Tag tag, Color4 color)
    {
        tag?.UpdateColor(color);
    }

    public static void SetVisible(Tag tag, bool visible)
    {
        if (tag != null) tag.Visible = visible;
    }

    public static void Save()
    {
        var folder = World.WorldData?.WorldFolderPath;
        if (!string.IsNullOrEmpty(folder)) TagsSaveLoader.SaveTags(folder);
    }
}
