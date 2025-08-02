using OpenTK.Mathematics;

using Engine;
using Spacebox.Game.Generation.Blocks;


namespace Spacebox.Game.Effects
{
    public class BlockDestructionManager : IDisposable
    {
        private List<BlockDestructionEffect> activeEffects = new List<BlockDestructionEffect>();
        private Camera camera;
        Texture2D texture;
        Shader shader;
        public BlockDestructionManager(Camera camera)
        {
            this.camera = camera;


            shader = Resources.Load<Shader>("Shaders/particle");
        }


        public void DestroyBlock(Vector3 position, Color3Byte color, Block block)
        {

            texture = GameAssets.BlockDusts[block.BlockId];

            var destructionEffect = new BlockDestructionEffect(camera, position +
                new Vector3(0.5f, 0.5f, 0.5f), color, texture, shader);
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
