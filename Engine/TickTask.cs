namespace Engine
{
    public class TickTask
    {
        public int RequiredTicks { get; private set; }
        private int _currentTicks;
        private bool _isPaused = false;

        public bool IsStopped { get; private set; } = false;
        public bool IsComplete => !_isPaused && _currentTicks >= RequiredTicks;
        public bool IsRunning => !_isPaused && _currentTicks < RequiredTicks;

        public bool IsPaused => _isPaused;

        public Action OnComplete { get;  set; }
        public Action OnTick { get; set; }
        private bool loop;

        public TickTask(int requiredTicks, bool loop)
        {
            RequiredTicks = requiredTicks;
            _currentTicks = 0;
            
            this.loop = loop;
        }

        public TickTask(int requiredTicks, Action onComplete, bool loop)
        {
            RequiredTicks = requiredTicks;
            _currentTicks = 0;
            OnComplete = onComplete;
            this.loop = loop;
        }

        public void Update()
        {
            if(IsStopped) return;
            if (_isPaused) return;

            _currentTicks++;
            OnTick?.Invoke();

            if (IsComplete)
            {
                OnComplete?.Invoke();

                if(loop) 
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

        public void Stop()
        {
            IsStopped = true;
        }

        public void Continue()
        {
            _isPaused = false;
        }

        public void Reset()
        {
            _currentTicks = 0;
        }
    }

}
