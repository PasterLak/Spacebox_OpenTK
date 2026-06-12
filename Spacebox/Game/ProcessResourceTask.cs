using Engine;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game
{
    public class ProcessResourceTask : TickTask
    {
        private ResourceProcessingBlock _block;

        public ProcessResourceTask(int requiredTicks, ResourceProcessingBlock block) : base(requiredTicks, true)
        {
            _block = block;
            base.OnComplete += OnCompleteHandler;
        }

        private void OnCompleteHandler()
        {
            if (_block == null || _block.Durability == 0 || !_block.IsRunning)
            {
                base.Stop();
                return;
            }

            _block.Craft();
        }
    }
}