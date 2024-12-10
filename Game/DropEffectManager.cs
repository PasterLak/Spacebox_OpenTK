// DropEffectManager.cs
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.Common.Audio;
using Spacebox.Game;
using Spacebox.Scenes;
using System;
using System.Collections.Generic;

namespace Spacebox.Game
{
    public class DropEffectManager : IDisposable
    {
        private List<DropEffect> activeEffects = new List<DropEffect>();
        private Astronaut player;
        private Shader shader;

        public const float DropLifeTime = 20f;
       
        private readonly float maxSpeed = 20f;
        private readonly float moveDistanceSquared;
        private readonly float pickupDistanceSquared;

        private readonly Stack<DropEffect> effectPool = new Stack<DropEffect>();

        private AudioSource[] pickupSound = new AudioSource[3];

        public DropEffectManager(
            Astronaut player,
          
            float maxSpeed = 20f,
            float moveDistance = 2f,
            float pickupDistance = 0.3f)
        {
            this.player = player;
            this.maxSpeed = maxSpeed;
            this.moveDistanceSquared = moveDistance * moveDistance;
            this.pickupDistanceSquared = pickupDistance * pickupDistance;
            shader = ShaderManager.GetShader("Shaders/particle");

            for(int i = 0; i < 3; i++)
            {
                pickupSound[i] = new AudioSource(SoundManager.AddClip("pickup"));
                pickupSound[i].Volume = 0.8f;
            }
         
        }

        public void DestroyBlock(Vector3 position, Vector3 color, Block block)
        {
            Texture2D texture = GameBlocks.ItemIcon[(short)(block.BlockId - 1)];
            DropEffect dropEffect = GetDropEffect();
            dropEffect.Initialize(
                player,
                position + new Vector3(0.5f, 0.5f, 0.5f),
                color,
                texture,
                shader,
                block
            );
            activeEffects.Add(dropEffect);
        }

        public void Update()
        {
            if (activeEffects.Count == 0)
                return;

            Vector3 playerPosition = player.Position;
            Vector3 targetPosition = player.Position - player.Up * 0.3f;

            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = activeEffects[i];
                effect.Update();

                if (effect.IsFinished)
                {
                    ReturnToPool(effect);
                    activeEffects.RemoveAt(i);
                    continue;
                }

                float distanceSquared = (effect.Position - targetPosition).LengthSquared;

                if (distanceSquared < pickupDistanceSquared)
                {
                    Pickup(effect);
                    ReturnToPool(effect);
                    activeEffects.RemoveAt(i);
                }
                else if (distanceSquared < moveDistanceSquared)
                {
                    MoveTowardsPlayer(effect, targetPosition);
                }
            }
        }

        public void Render()
        {
            if (activeEffects.Count == 0)
                return;

            SortEffectsByDistanceDescending();

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.DepthMask(false);

            foreach (var effect in activeEffects)
            {
                effect.Render();
            }

            GL.DepthMask(true);
            GL.Disable(EnableCap.Blend);
        }

        private void SortEffectsByDistanceDescending()
        {
            Vector3 playerPosition = player.Position;

            activeEffects.Sort((a, b) =>
            {
                float distanceASquared = (a.Position - playerPosition).LengthSquared;
                float distanceBSquared = (b.Position - playerPosition).LengthSquared;
                return distanceBSquared.CompareTo(distanceASquared);
            });
        }

        private void MoveTowardsPlayer(DropEffect effect, Vector3 targetPosition)
        {
           
            Vector3 direction = (targetPosition - effect.Position).Normalized();
           
            effect.Velocity = direction * maxSpeed;
           
            effect.Position += effect.Velocity * Time.Delta;
        }

        private void Pickup(DropEffect effect)
        {
            player.Panel.TryAddBlock(effect.Block, 1);
            
            bool canPlay = false;
            foreach(var sound in pickupSound)
            {
                if (sound.IsPlaying) continue;

                sound.Play();
                canPlay = true;
            }

            if (!canPlay)
            {
                pickupSound[0].Stop();
                pickupSound[0].Play();
            }
        }

        private DropEffect GetDropEffect()
        {
            if (effectPool.Count > 0)
            {
                return effectPool.Pop();
            }
            else
            {
                return new DropEffect();
            }
        }

        private void ReturnToPool(DropEffect effect)
        {
            effect.Reset();
            effectPool.Push(effect);
        }

        public void Dispose()
        {
            foreach (var effect in activeEffects)
            {
                effect.Dispose();
            }
            activeEffects.Clear();

            while (effectPool.Count > 0)
            {
                var effect = effectPool.Pop();
                effect.Dispose();
            }
        }
    }
}
