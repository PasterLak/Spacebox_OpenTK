using Engine;
using Spacebox.Game.Generation.Blocks.Components;

namespace Spacebox.Game
{
    public class ProcessResourceTask : TickTask
    {
        private ProcessingComponent _block;

        public ProcessResourceTask(int requiredTicks, ProcessingComponent block) : base(requiredTicks, true)
        {
            _block = block;
            base.OnComplete += OnCompleteHandler;
        }

        private void OnCompleteHandler()
        {
            if (_block == null || _block.Host.Durability == 0 || !_block.IsRunning || _block.IsEntityDisposed)
            {
                base.Stop();
                return;
            }

            _block.Craft();
        }
    }
}