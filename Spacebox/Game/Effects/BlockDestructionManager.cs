using OpenTK.Mathematics;

using Engine;
using Spacebox.Game.Generation.Blocks;


namespace Spacebox.Game.Effects
{
    public class BlockDestructionManager : IDisposable
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

        public void Update()
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

        public void Render()
        {
           
            foreach (var effect in activeEffects)
            {
                effect.Render();
            }
        }

        public void Dispose()
        {
            foreach (var effect in activeEffects)
            {
                effect.Dispose();
            }
            activeEffects.Clear();
        }
    }
}
