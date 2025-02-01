namespace Engine
{

    public class Toggi
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public bool IsUI { get; set; } = false;

        private bool _state = false;
        public bool State => _state;

        public Action<bool> OnStateChanged;

        public Toggi(string name)
        {
            Id = Guid.NewGuid();
            Name = name;
        }

        public void Toggle()
        {
            SetState(!_state);
        }

        public void SetState(bool state)
        {
            _state = state;
            OnStateChanged?.Invoke(_state);
        }

        public void Enable()
        {
            SetState(true);
        }

        public void Disable()
        {
            SetState(false);
        }
    }


}
