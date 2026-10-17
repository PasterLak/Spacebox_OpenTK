using Engine;
using Engine.Audio;
using global::Client;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Physics;

namespace Spacebox.Game.Player.Interactions;

public static class BlockDestroyer
{
    private const string DefaultDestroySound = "blockDestroyDefault";

    private static readonly Random _random = new Random();
    private static AudioSource _sound;

    public static void Destroy(HitInfo hit, LocalAstronaut player)
    {
        hit.block.OnDestroy?.Invoke(hit);
        hit.chunk.RemoveBlock(hit.blockPositionIndex, hit.normal);

        var chunkOrigin = hit.chunk.PositionIndex * Chunk.Size;
        var localPos = new Vector3(chunkOrigin.X + hit.blockPositionIndex.X, chunkOrigin.Y + hit.blockPositionIndex.Y, chunkOrigin.Z + hit.blockPositionIndex.Z);
        ClientNetwork.Instance?.SendBlockDestroyed((short)localPos.X, (short)localPos.Y, (short)localPos.Z);
        player.PlayerStatistics.BlocksDestroyed++;

        PlaySound(hit.block.Id);
    }

    public static void DestroyInstantly(HitInfo hit, LocalAstronaut player)
    {
        player.PlayerStatistics.BlockDamageDealt += hit.block.Durability;
        Destroy(hit, player);
    }

    private static void PlaySound(short blockId)
    {
        var clip = GameAssets.GetBlockAudioClipFromItemID(blockId, BlockInteractionType.Destroy)
                   ?? Resources.Load<AudioClip>(DefaultDestroySound);
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
