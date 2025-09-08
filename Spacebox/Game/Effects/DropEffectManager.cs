using OpenTK.Mathematics;
using Engine;
using Engine.Audio;
using Spacebox.Game.Player;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Effects
{
    public class DropEffectManager : IDisposable
    {
        public const float DropLifeTime = 20f;

        private readonly List<DropEffect> activeEffects = new List<DropEffect>();
        private readonly Stack<DropEffect> effectPool = new Stack<DropEffect>();
        private readonly AudioSource[] pickupSounds = new AudioSource[3];

        private readonly Astronaut player;
        private readonly float maxSpeed;
        private readonly float moveDistanceSquared;
        private readonly float pickupDistanceSquared;

        public DropEffectManager(
            Astronaut player,
            float maxSpeed = 20f,
            float moveDistance = 2f,
            float pickupDistance = 0.3f)
        {
            this.player = player;
            this.maxSpeed = maxSpeed;
            moveDistanceSquared = moveDistance * moveDistance;
            pickupDistanceSquared = pickupDistance * pickupDistance;

            InitializeAudioSources();
        }

        private void InitializeAudioSources()
        {
            for (int i = 0; i < pickupSounds.Length; i++)
            {
                pickupSounds[i] = new AudioSource(Resources.Load<AudioClip>("pickupDefault"));
                pickupSounds[i].Volume = 0.8f;
            }
        }

        public void DestroyBlock(Vector3 position, Color3Byte color, Block block)
        {
            var blockData = GameAssets.GetBlockDataById(block.Id);

            if(blockData.Id == 0 || blockData.Drop.Count <= 0 ) return;

            var texture = GameAssets.ItemIcons[(short)blockData.Drop.Item.Id];
            var dropEffect = GetOrCreateDropEffect();

            dropEffect.Initialize(
                player,
                position + Vector3.One * 0.5f,
                color,
                texture,
                blockData.Drop.Item,
                blockData.Drop.Count
            );

            activeEffects.Add(dropEffect);
        }

        public void Update()
        {
            if (activeEffects.Count == 0) return;

            var targetPosition = player.Position - player.Up * 0.3f;

            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = activeEffects[i];
                effect.Update();

                if (effect.IsFinished)
                {
                    RemoveEffect(i);
                    continue;
                }

                var distanceSquared = (effect.Position - targetPosition).LengthSquared;

                if (ShouldPickup(distanceSquared))
                {
                    ProcessPickup(effect);
                    RemoveEffect(i);
                }
                else if (ShouldMoveTowardsPlayer(distanceSquared))
                {
                    MoveTowardsPlayer(effect, targetPosition);
                }
            }
        }

        public void Render()
        {
            if (activeEffects.Count == 0) return;

            SortEffectsByDistanceDescending();

            foreach (var effect in activeEffects)
            {
                effect.Render();
            }
        }

        private void SortEffectsByDistanceDescending()
        {
            var playerPosition = player.Position;

            activeEffects.Sort((a, b) =>
            {
                var distanceA = (a.Position - playerPosition).LengthSquared;
                var distanceB = (b.Position - playerPosition).LengthSquared;
                return distanceB.CompareTo(distanceA);
            });
        }

        private bool ShouldPickup(float distanceSquared) => distanceSquared < pickupDistanceSquared;

        private bool ShouldMoveTowardsPlayer(float distanceSquared) => distanceSquared < moveDistanceSquared;

        private void MoveTowardsPlayer(DropEffect effect, Vector3 targetPosition)
        {
            var direction = (targetPosition - effect.Position).Normalized();
            effect.Velocity = direction * maxSpeed;
            effect.Position += effect.Velocity * Time.Delta;
        }

        private void ProcessPickup(DropEffect effect)
        {
            player.Panel.TryAddItem(effect.Drop.item, effect.Drop.quantity);
            PlayPickupSound();
        }

        private void PlayPickupSound()
        {
            var availableSound = FindAvailableSound();
            if (availableSound != null)
            {
                availableSound.Play();
            }
            else
            {
                pickupSounds[0].Stop();
                pickupSounds[0].Play();
            }
        }

        private AudioSource FindAvailableSound()
        {
            foreach (var sound in pickupSounds)
            {
                if (!sound.IsPlaying) return sound;
            }
            return null;
        }

        private void RemoveEffect(int index)
        {
            var effect = activeEffects[index];
            ReturnToPool(effect);
            activeEffects.RemoveAt(index);
        }

        private DropEffect GetOrCreateDropEffect()
        {
            return effectPool.Count > 0 ? effectPool.Pop() : new DropEffect();
        }

        private void ReturnToPool(DropEffect effect)
        {
            effect.Reset();
            effectPool.Push(effect);
        }

        public void Dispose()
        {
            DisposeActiveEffects();
            DisposePooledEffects();
            DisposeAudioSources();
        }

        private void DisposeActiveEffects()
        {
            foreach (var effect in activeEffects)
            {
                effect.Dispose();
            }
            activeEffects.Clear();
        }

        private void DisposePooledEffects()
        {
            while (effectPool.Count > 0)
            {
                effectPool.Pop().Dispose();
            }
        }

        private void DisposeAudioSources()
        {
            foreach (var sound in pickupSounds)
            {
                sound?.Dispose();
            }
        }
    }
}