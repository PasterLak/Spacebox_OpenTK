namespace Spacebox.Game.GUI.Sounds;

public readonly record struct UiSoundSpec(string Clip, float Volume, float MinInterval)
{
    public bool IsEmpty => string.IsNullOrEmpty(Clip);
}
