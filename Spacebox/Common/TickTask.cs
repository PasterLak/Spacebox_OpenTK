namespace Spacebox.Common
{
    public class TickTask
    {
        public int RequiredTicks { get; private set; }
        private int _currentTicks;
        private bool _isPaused = false;
        public bool IsComplete => !_isPaused && _currentTicks >= RequiredTicks;
        public bool IsRunning => !_isPaused && _currentTicks < RequiredTicks;

        public bool IsPaused => _isPaused;

        private Action _onComplete;
        private bool _deleteAfterComplete;

        public TickTask(int requiredTicks, Action onComplete, bool deleteAfterComplete = false)
        {
            RequiredTicks = requiredTicks;
            _currentTicks = 0;
            _onComplete = onComplete;
            _deleteAfterComplete = deleteAfterComplete;
        }

        public void Update()
        {
            if (_isPaused) return;

            _currentTicks++;

            if (IsComplete)
            {
                _onComplete?.Invoke();

                if(!_deleteAfterComplete)
                Reset();
            }
        }

        public void SetRequiredTicks(int ticks)
        {
            RequiredTicks = ticks;
        }

        public void ForceComplete()
        {
            _currentTicks = RequiredTicks;
        }

        public void Pause()
        {
            _isPaused = true;
        }

        public void Contnue()
        {
            _isPaused = false;
        }

        public void Reset()
        {
            _currentTicks = 0;
        }
    }

}
