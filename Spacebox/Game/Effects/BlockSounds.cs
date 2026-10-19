using Engine;
using Engine.Audio;

namespace Spacebox.Game.Effects;

public static class BlockSounds
{
    public const string DefaultPlaceSound = "blockPlaceDefault";
    public const string DefaultDestroySound = "blockDestroyDefault";

    public static AudioClip GetClip(short blockId, BlockInteractionType type)
    {
        var data = GameAssets.GetBlockDataById(blockId);
        string name = data == null ? null : type == BlockInteractionType.Place ? data.SoundPlace : data.SoundDestroy;

        if (name != null && GameAssets.Sounds.TryGetValue(name, out var clip) && clip != null) return clip;
        return Resources.Load<AudioClip>(type == BlockInteractionType.Place ? DefaultPlaceSound : DefaultDestroySound);
    }
}
