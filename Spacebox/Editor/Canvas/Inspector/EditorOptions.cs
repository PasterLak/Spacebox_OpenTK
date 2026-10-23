using Spacebox.Editor.Data;
using Spacebox.Game.Generation.Shapes;
using Spacebox.Game.Mobs;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Tutorial;
using Spacebox.Game.Tutorial.Tasks;

namespace Spacebox.Editor.Canvas.Inspector;

public static class EditorOptions
{
    public static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg" };
    public static readonly string[] ModelExtensions = { ".obj" };
    public static readonly string[] FontExtensions = { ".ttf", ".otf" };
    public static readonly string[] Directions = Enum.GetNames(typeof(Spacebox.Game.Generation.Blocks.Direction));
    public static readonly string[] SignAlignments = { "left", "center", "right" };
    public static readonly string[] GameModeNames = Enum.GetNames(typeof(GameMode));

    private static string GameDirectory => AppContext.BaseDirectory;

    public static IReadOnlyList<string> BlockTypes() => GameSetCatalog.BlockTypeNames.OrderBy(t => t).ToArray();

    public static IReadOnlyList<string> BlockComponents() => Spacebox.Game.Resource.BlockComponentRegistry.Names.OrderBy(t => t).ToArray();

    public static IReadOnlyList<string> BlockModels(EditorContext editor) =>
        ProjectFiles.Union(BlockGeometryLibrary.Names, ProjectFiles.Names(editor.Project.RootPath, "Models/Blocks", ModelExtensions));

    public static IReadOnlyList<string> ProjectModels(EditorContext editor) => ProjectFiles.List(editor.Project.RootPath, "", ModelExtensions);

    public static IReadOnlyList<string> ArmorTextures(EditorContext editor) => ProjectFiles.List(editor.Project.RootPath, "Models", ImageExtensions);

    public static IReadOnlyList<string> Pictures(EditorContext editor) => ProjectFiles.List(editor.Project.RootPath, "Pictures", ImageExtensions);

    public static IReadOnlyList<string> MobModels(EditorContext editor) =>
        ProjectFiles.Union(ProjectModels(editor), ProjectFiles.List(GameDirectory, "Resources/Models", ModelExtensions));

    public static IReadOnlyList<string> MobTextures(EditorContext editor) =>
        ProjectFiles.Union(
            ProjectFiles.List(editor.Project.RootPath, "Models", ImageExtensions),
            ProjectFiles.List(editor.Project.RootPath, "Textures", ImageExtensions),
            ProjectFiles.List(GameDirectory, "Resources/Textures/Mobs", ImageExtensions),
            ProjectFiles.List(GameDirectory, "Resources/Models", ImageExtensions));

    public static IReadOnlyList<string> Fonts() => ProjectFiles.List(GameDirectory, "Resources/Fonts", FontExtensions);

    public static IReadOnlyList<string> MobBehaviours() => MobBrainFactory.Types.OrderBy(t => t).ToArray();

    public static IReadOnlyList<string> TutorialTaskTypes() => TutorialTaskFactory.Types.OrderBy(t => t).ToArray();

    public static IReadOnlyList<string> Statistics() => PlayerStatReader.Names;

    public static IReadOnlyList<string> Windows() => TutorialWindows.Known.OrderBy(w => w, StringComparer.OrdinalIgnoreCase).ToArray();

    public static IReadOnlyList<string> ToggleTargets() => TutorialTaskFactory.ToggleTargets;
}
