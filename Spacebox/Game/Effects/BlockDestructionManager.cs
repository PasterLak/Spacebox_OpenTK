using OpenTK.Mathematics;

using Engine;
using Spacebox.Game.Generation.Blocks;
using Engine.Components;


namespace Spacebox.Game.Effects
{
    public class BlockDestructionManager : Component
    {
        private List<BlockDestructionEffect> activeEffects = new List<BlockDestructionEffect>();

        public BlockDestructionManager()
        { 
           
        }


        public void DestroyBlock(Vector3 worldPosition, Color3Byte color, Block block)
        {

            var texture = GameAssets.BlockDusts[block.Id];

            var destructionEffect = new BlockDestructionEffect(worldPosition +
                new Vector3(0.5f, 0.5f, 0.5f), color, new ParticleMaterial(texture));
            activeEffects.Add(destructionEffect);
        }

        public override void OnUpdate()
        {
           
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = activeEffects[i];
                effect.Update();

                if (effect.IsFinished)
                {
                    effect.Dispose();

                    activeEffects.RemoveAt(i);
                }
            }
        }

        public override void OnRender()
        {
          
            foreach (var effect in activeEffects)
            {
                effect.Render();
            }
        }

        public override void OnDetached()
        {
           
            foreach (var effect in activeEffects)
            {
                effect.Dispose();
            }
            activeEffects.Clear();
        }
    }
}
