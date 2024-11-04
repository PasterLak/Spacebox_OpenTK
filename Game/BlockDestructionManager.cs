// BlockDestructionManager.cs
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

           
            texture = TextureManager.GetTexture("Resources/Textures/blockDust.png", true); 

            shader = ShaderManager.GetShader("Shaders/particleShader");
        }

    
        public void DestroyBlock(Vector3 position, Vector3 color)
        {
            Console.WriteLine("Block destroyed: " + position);
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
                    Console.WriteLine("Effect stopped: " );
                    activeEffects.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Рендерит все активные эффекты разрушения блоков.
        /// </summary>
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
