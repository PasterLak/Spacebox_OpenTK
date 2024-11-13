using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.Game;
using Spacebox.Scenes;


namespace Spacebox.Managers
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


            shader = ShaderManager.GetShader("Shaders/particle");
        }

    
        public void DestroyBlock(Vector3 position, Vector3 color, Block block)
        {
           
            texture = GameBlocks.BlockDust[block.BlockId];

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
