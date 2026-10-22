using Spacebox.Editor.Canvas.Inspector;
using Spacebox.Editor.Canvas.Pages;
using Spacebox.Editor.Data;
using Spacebox.Editor.UI.Fields;

namespace Spacebox.Editor.Canvas;

public static class EditorPages
{
    public static List<EditorPage> CreateDefault(GameSetProject project)
    {
        var widgets = FieldWidgetRegistry.CreateDefault();
        return new List<EditorPage>
        {
            new OverviewPage(widgets),
            new JsonFilePage("Player spawn", Spacebox.Game.Player.Respawn.SpawnParameters.RelativePath, EditorScopes.PlayerSpawn, widgets,
                "Start items and where they go, start health/power/oxygen, and which asteroid new players start next to. Used for new players and respawns.")
        };
    }
}
