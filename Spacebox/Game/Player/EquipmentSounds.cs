using Spacebox.Game.Effects;

namespace Spacebox.Game.Player;

public static class EquipmentSounds
{
    public const float Volume = 0.55f;

    private static readonly LocalSoundPlayer _player = new LocalSoundPlayer();

    public static void Play(EquipmentItem item)
    {
        if (item == null) return;
        _player.Play(item.EquipSound, Volume);
    }

    public static void Clear()
    {
        _player.Clear();
    }
}
