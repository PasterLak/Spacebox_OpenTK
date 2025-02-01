using Engine;
using Spacebox.Game.Generation;

namespace Spacebox.Game
{
    public class ProcessResourceTask : TickTask
    {
        private ResourceProcessingBlock _block;
        public ProcessResourceTask(int requiredTicks, ResourceProcessingBlock block) : base(requiredTicks)
        {
            _block = block;
            base.OnComplete += OnComplete;
        }

        public void OnComplete()
        {
            if (_block == null)
            {
                base.Stop();
                return;
            }
            if (_block.Durability == 0)
            {
                base.Stop();
                return;
            }
            if (!_block.IsRunning)
            {
                base.Stop();
                return;
            }

            _block.Craft();
        }
    }
}
