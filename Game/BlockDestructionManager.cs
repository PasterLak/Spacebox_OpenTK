// BlockDestructionManager.cs
using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.Scenes;


namespace Spacebox.Managers
{
    public class BlockDestructionManager : IDisposable
    {
        private List<BlockDestructionEffect> activeEffects = new List<BlockDestructionEffect>();
        private Camera camera;

        public BlockDestructionManager(Camera camera)
        {
            this.camera = camera;
        }

        /// <summary>
        /// Инициирует разрушение блока на заданной позиции.
        /// </summary>
        /// <param name="position">Позиция разрушенного блока.</param>
        public void DestroyBlock(Vector3 position)
        {
            Console.WriteLine("Block destroyed: " + position);
            var destructionEffect = new BlockDestructionEffect(camera, position + 
                new Vector3(0.5f, 0.5f, 0.5f));
            activeEffects.Add(destructionEffect);
        }

        /// <summary>
        /// Обновляет все активные эффекты разрушения блоков.
        /// </summary>
        /// <param name="deltaTime">Прошедшее время с последнего обновления.</param>
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
