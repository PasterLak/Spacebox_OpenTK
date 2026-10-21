namespace Spacebox.Game.GUI.Menu;

public abstract class SettingsCategoryWindow : MenuWindow, IOptionsCategory
{
    public virtual string CaptureState() => SettingsService.Serialize(Settings.AsGameSettings());

    public virtual void Save() => SettingsService.Save(Settings.AsGameSettings());

    public virtual void Discard() => OptionsMenu.DiscardUnsavedChanges();
}
