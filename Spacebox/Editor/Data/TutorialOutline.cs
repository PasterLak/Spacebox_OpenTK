using System.Text.Json.Nodes;
using Spacebox.Game.Tutorial;

namespace Spacebox.Editor.Data;

public enum TutorialNodeKind
{
    Tutorial,
    Chapter,
    Step
}

public readonly record struct TutorialNode(TutorialNodeKind Kind, int Chapter = -1, int Step = -1)
{
    public static readonly TutorialNode Settings = new TutorialNode(TutorialNodeKind.Tutorial);
}

public sealed class TutorialOutline
{
    private const string ChaptersKey = "Chapters";
    private const string StepsKey = "Steps";

    private readonly JsonObject _root;

    public TutorialOutline(JsonObject root)
    {
        _root = root;
    }

    public List<TutorialNode> Nodes()
    {
        var nodes = new List<TutorialNode> { TutorialNode.Settings };
        var chapters = Chapters(false);
        if (chapters == null) return nodes;

        for (int c = 0; c < chapters.Count; c++)
        {
            nodes.Add(new TutorialNode(TutorialNodeKind.Chapter, c));
            var steps = Steps(c, false);
            if (steps == null) continue;
            for (int s = 0; s < steps.Count; s++) nodes.Add(new TutorialNode(TutorialNodeKind.Step, c, s));
        }
        return nodes;
    }

    public bool Exists(TutorialNode node)
    {
        return node.Kind switch
        {
            TutorialNodeKind.Tutorial => true,
            TutorialNodeKind.Chapter => Chapters(false) is { } chapters && node.Chapter >= 0 && node.Chapter < chapters.Count,
            _ => Steps(node.Chapter, false) is { } steps && node.Step >= 0 && node.Step < steps.Count
        };
    }

    public JsonObject Get(TutorialNode node)
    {
        if (!Exists(node)) return null;
        return node.Kind switch
        {
            TutorialNodeKind.Tutorial => _root,
            TutorialNodeKind.Chapter => Chapters(false)[node.Chapter] as JsonObject,
            _ => Steps(node.Chapter, false)[node.Step] as JsonObject
        };
    }

    public string Label(TutorialNode node)
    {
        if (node.Kind == TutorialNodeKind.Tutorial) return "Tutorial settings";
        var obj = Get(node);
        string title = obj == null ? "" : EditorJson.GetString(obj, "Title");
        if (string.IsNullOrWhiteSpace(title)) title = "(no title)";
        int number = (node.Kind == TutorialNodeKind.Chapter ? node.Chapter : node.Step) + 1;
        bool optional = node.Kind == TutorialNodeKind.Chapter && obj != null && EditorJson.Get(obj, "Optional") is JsonValue value && value.TryGetValue(out bool flag) && flag;
        return $"{number}. {title}{(optional ? " (optional)" : "")}";
    }

    public string Signature()
    {
        var parts = new List<string>();
        foreach (var node in Nodes()) parts.Add(node.Kind + Label(node));
        return string.Join("\n", parts);
    }

    public TutorialNode AddChapter(TutorialNode selected)
    {
        var chapters = Chapters(true);
        int index = selected.Kind == TutorialNodeKind.Tutorial || !Exists(new TutorialNode(TutorialNodeKind.Chapter, selected.Chapter)) ? chapters.Count : selected.Chapter + 1;
        var chapter = (JsonObject)JsonSchema.CreateEmpty(typeof(TutorialChapterJSON));
        chapter["Id"] = UniqueId(chapters, "chapter");
        chapter["Title"] = "New chapter";
        chapter[StepsKey] = new JsonArray();
        chapters.Insert(index, chapter);
        return new TutorialNode(TutorialNodeKind.Chapter, index);
    }

    public TutorialNode AddStep(TutorialNode selected)
    {
        int chapter = selected.Kind == TutorialNodeKind.Tutorial ? (Chapters(false)?.Count ?? 0) - 1 : selected.Chapter;
        if (chapter < 0 || !Exists(new TutorialNode(TutorialNodeKind.Chapter, chapter))) chapter = AddChapter(TutorialNode.Settings).Chapter;

        var steps = Steps(chapter, true);
        int index = selected.Kind == TutorialNodeKind.Step && selected.Chapter == chapter ? selected.Step + 1 : steps.Count;
        var step = (JsonObject)JsonSchema.CreateEmpty(typeof(TutorialStepJSON));
        step["Id"] = UniqueId(steps, "step");
        step["Title"] = "New step";
        step["Tasks"] = new JsonArray();
        steps.Insert(index, step);
        return new TutorialNode(TutorialNodeKind.Step, chapter, index);
    }

    public bool CanMove(TutorialNode node, int delta)
    {
        var owner = Owner(node, out int index);
        return owner != null && index + delta >= 0 && index + delta < owner.Count;
    }

    public TutorialNode Move(TutorialNode node, int delta)
    {
        if (!CanMove(node, delta)) return node;
        var owner = Owner(node, out int index);
        var item = owner[index];
        owner.RemoveAt(index);
        owner.Insert(index + delta, item);
        return With(node, index + delta);
    }

    public TutorialNode Duplicate(TutorialNode node)
    {
        var owner = Owner(node, out int index);
        if (owner == null) return node;
        var copy = EditorJson.Clone(owner[index]);
        if (copy is JsonObject obj && EditorJson.GetString(obj, "Id") is { Length: > 0 } id) obj["Id"] = UniqueId(owner, id);
        owner.Insert(index + 1, copy);
        return With(node, index + 1);
    }

    public TutorialNode Remove(TutorialNode node)
    {
        var owner = Owner(node, out int index);
        if (owner == null) return node;
        owner.RemoveAt(index);
        if (owner.Count > 0) return With(node, Math.Min(index, owner.Count - 1));
        return node.Kind == TutorialNodeKind.Step ? new TutorialNode(TutorialNodeKind.Chapter, node.Chapter) : TutorialNode.Settings;
    }

    private JsonArray Owner(TutorialNode node, out int index)
    {
        index = -1;
        if (!Exists(node) || node.Kind == TutorialNodeKind.Tutorial) return null;
        index = node.Kind == TutorialNodeKind.Chapter ? node.Chapter : node.Step;
        return node.Kind == TutorialNodeKind.Chapter ? Chapters(false) : Steps(node.Chapter, false);
    }

    private static TutorialNode With(TutorialNode node, int index)
    {
        return node.Kind == TutorialNodeKind.Chapter ? new TutorialNode(TutorialNodeKind.Chapter, index) : new TutorialNode(TutorialNodeKind.Step, node.Chapter, index);
    }

    private JsonArray Chapters(bool create)
    {
        if (EditorJson.Get(_root, ChaptersKey) is JsonArray chapters) return chapters;
        if (!create) return null;
        chapters = new JsonArray();
        _root[ChaptersKey] = chapters;
        return chapters;
    }

    private JsonArray Steps(int chapter, bool create)
    {
        var chapters = Chapters(false);
        if (chapters == null || chapter < 0 || chapter >= chapters.Count || chapters[chapter] is not JsonObject obj) return null;
        if (EditorJson.Get(obj, StepsKey) is JsonArray steps) return steps;
        if (!create) return null;
        steps = new JsonArray();
        obj[StepsKey] = steps;
        return steps;
    }

    private static string UniqueId(JsonArray siblings, string baseId)
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sibling in siblings)
            if (sibling is JsonObject obj) used.Add(EditorJson.GetString(obj, "Id"));

        if (!used.Contains(baseId)) return baseId;
        for (int i = 2; ; i++)
        {
            string id = $"{baseId}_{i}";
            if (!used.Contains(id)) return id;
        }
    }
}
