using Engine;
using Engine.Audio;
using Spacebox.Network;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Physics;

namespace Spacebox.Game.Player.Interactions;

public static class BlockDestroyer
{
    private static readonly Random _random = new Random();
    private static AudioSource _sound;

    public static void Destroy(HitInfo hit, LocalAstronaut player)
    {
        hit.block.OnDestroy?.Invoke(hit);
        hit.chunk.RemoveBlock(hit.blockPositionIndex, hit.normal);
        WorldEditNetwork.NotifyRemoved(hit.chunk, hit.blockPositionIndex);
        player.PlayerStatistics.BlocksDestroyed++;
        PlayerActivity.RaiseBlock(PlayerActivityKind.BlockDestroyed, hit.block.Id);

        PlaySound(hit.block.Id);
    }

    public static void DestroyInstantly(HitInfo hit, LocalAstronaut player)
    {
        player.PlayerStatistics.BlockDamageDealt += hit.block.Durability;
        Destroy(hit, player);
    }

    private static void PlaySound(short blockId)
    {
        var clip = Spacebox.Game.Effects.BlockSounds.GetClip(blockId, BlockInteractionType.Destroy);
        if (clip == null) return;

        if (_sound == null || _sound.Clip != clip)
        {
            _sound?.Stop();
            _sound = new AudioSource(clip);
        }

        _sound.Pitch = _random.Next(9, 12) * 0.1f;
        _sound.Play();
    }
}
