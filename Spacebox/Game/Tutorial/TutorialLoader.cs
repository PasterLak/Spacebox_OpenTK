using Engine;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Tutorial;

public static class TutorialLoader
{
    public const string FolderName = "Tutorials";

    private sealed class GameAssetsItemResolver : ITutorialItemResolver
    {
        private readonly string _modId;

        public GameAssetsItemResolver(string modId)
        {
            _modId = modId;
        }

        public bool TryResolve(string id, out short itemId)
        {
            itemId = 0;
            if (string.IsNullOrWhiteSpace(id)) return false;

            string fullId = id.Contains(':') ? id : GameSetLoader.CombineId(_modId, id);
            if (!GameAssets.TryGetItemByFullID(fullId, out var item) || item == null) return false;

            itemId = item.Id;
            return true;
        }
    }

    private static void CheckActions(TutorialDefinition tutorial, List<string> warnings)
    {
        foreach (var chapter in tutorial.Chapters)
            foreach (var step in chapter.Steps)
                foreach (var task in step.Tasks)
                    CheckAction(tutorial, step, task, warnings);
    }

    private static void CheckAction(TutorialDefinition tutorial, TutorialStep step, TutorialTaskDefinition task, List<string> warnings)
    {
        string action = task.Data.Action;
        if (!string.IsNullOrWhiteSpace(action) && !Engine.InputPro.InputManager.Instance.HasAction(action))
            warnings.Add($"[Tutorial] {tutorial.Id}: step '{step.Id}' uses unknown input action '{action}'");

        if (task.Type == "stat" && !PlayerStatReader.IsKnown(task.Data.Stat))
            warnings.Add($"[Tutorial] {tutorial.Id}: step '{step.Id}' uses unknown stat '{task.Data.Stat}'");

        foreach (var child in task.Children) CheckAction(tutorial, step, child, warnings);
    }

    public static void Load(string modPath, string defaultModPath, string modId)
    {
        string folder = Path.Combine(modPath ?? "", FolderName);
        if (!Directory.Exists(folder)) folder = Path.Combine(defaultModPath ?? "", FolderName);
        if (!Directory.Exists(folder))
        {
            TutorialLibrary.Clear();
            return;
        }

        var warnings = new List<string>();
        var parser = new TutorialParser(new GameAssetsItemResolver(modId), warnings);
        var tutorials = new List<TutorialDefinition>();

        foreach (var file in Directory.GetFiles(folder, "*.json"))
        {
            try
            {
                var tutorial = parser.Parse(File.ReadAllText(file), Path.GetFileName(file));
                if (tutorial != null) tutorials.Add(tutorial);
            }
            catch (IOException ex)
            {
                warnings.Add($"[Tutorial] {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        foreach (var tutorial in tutorials) CheckActions(tutorial, warnings);
        foreach (var warning in warnings) Debug.Warning(warning);

        TutorialLibrary.Set(tutorials);
        if (tutorials.Count > 0) Debug.Log($"[Tutorial] Loaded {tutorials.Count} tutorial(s)");
    }
}
