using Engine;
using Engine.CanvasUI;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Versioning;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class UpgradeWorldDialog
{
    private readonly UpdateVersionWindow _model;
    private readonly MenuWindowFrame _frame = MenuLayout.Window("Convert world", 520f);
    private readonly MenuModal _modal;
    private WorldInfo _world;
    private WorldUpgradeResult _shownResult;
    private string _shownError;

    public UpgradeWorldDialog(UpdateVersionWindow model)
    {
        _model = model;
        _modal = new MenuModal(_frame.Root);
        _modal.Closed += () =>
        {
            if (_model.Menu.showVersionConvertWindow) _model.Close();
        };
    }

    private GameMenu Menu => _model.Menu;

    public void Sync(CanvasRoot root)
    {
        var world = Menu.selectedWorld;
        bool wanted = Menu.showVersionConvertWindow && world != null;
        if (!wanted)
        {
            if (Menu.showVersionConvertWindow) _model.Close();
            if (_modal.IsOpen) _modal.Close();
            _world = null;
            return;
        }

        if (!ReferenceEquals(world, _world) || _model.Result != _shownResult || _model.Error != _shownError)
        {
            _world = world;
            _model.EnsureChecked(world);
            Rebuild(world);
        }

        if (!_modal.IsOpen) _modal.Open(root);
    }

    private void Rebuild(WorldInfo world)
    {
        _shownResult = _model.Result;
        _shownError = _model.Error;
        _frame.Body.ClearChildren();
        _frame.Footer.ClearChildren();

        if (_model.Result != null) BuildConverted(world, _model.Result);
        else if (_model.Status == WorldUpgradeStatus.Ok) BuildConvertible(world);
        else BuildUnsupported(world);
    }

    private void BuildConvertible(WorldInfo world)
    {
        Text($"\"{world.Name}\" was made with version {world.GameVersion}.");
        Text($"Convert it to version {Application.Version}?");
        foreach (var step in VersionConverter.GetSteps(world.GameVersion, Application.Version)) Text("- " + step.Description, "MutedLabel");
        Text("A backup is saved in Worlds/_backups first.", "MutedLabel");
        if (!string.IsNullOrEmpty(_model.Error)) Text(_model.Error, "ErrorLabel");
        _frame.Footer.AddChild(MenuLayout.Button(null, "Yes, convert", () => _model.Convert(world)));
        _frame.Footer.AddChild(MenuLayout.Button(Menu, "No", _model.Close));
    }

    private void BuildConverted(WorldInfo world, WorldUpgradeResult result)
    {
        Text($"\"{world.Name}\" was converted to version {result.ToVersion}.");
        Text(result.Summary, "MutedLabel");
        foreach (var warning in result.Warnings) Text(warning, "WarningLabel");
        if (!string.IsNullOrEmpty(result.BackupPath)) Text("Backup: " + Path.GetFileName(result.BackupPath), "MutedLabel");
        _frame.Footer.AddChild(MenuLayout.Button(Menu, "Play", () =>
        {
            _model.Close();
            Menu.LoadWorld(world);
        }));
        _frame.Footer.AddChild(MenuLayout.Button(Menu, "Back", _model.Close));
    }

    private void BuildUnsupported(WorldInfo world)
    {
        Text($"\"{world.Name}\" cannot be opened.");
        Text(_model.Reason ?? "Unknown version", "ErrorLabel");
        _frame.Footer.AddChild(MenuLayout.Button(Menu, "Back", _model.Close));
    }

    private void Text(string text, string variation = "Label")
    {
        _frame.Body.AddChild(new Label { Text = text ?? "", Autowrap = true, ThemeTypeVariation = variation });
    }
}
