namespace Engine
{
    public interface IPoolable<T>
    {
        bool IsActive { get; set; }
        T CreateFromPool();
        void Reset();
    }

    public sealed class Pool<T> where T : IPoolable<T>, new()
    {
        public bool AutoExpand { get; set; } = true;

        private readonly Stack<T> _availableObjects;
        private readonly List<T> _allObjects;

        public List<T> GetAllObjects() => _allObjects;

        public Pool(int initialCount, bool autoExpand = true)
        {
            AutoExpand = autoExpand;
            _availableObjects = new Stack<T>(initialCount);
            _allObjects = new List<T>(initialCount);
            CreatePool(initialCount);
        }

        private void CreatePool(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var obj = CreateObject(false);
                _availableObjects.Push(obj);
            }
        }

        private T CreateObject(bool isActiveByDefault = false)
        {
            T createdObject = new T().CreateFromPool();
            createdObject.IsActive = isActiveByDefault;
            _allObjects.Add(createdObject);
            return createdObject;
        }

        public T GetFreeElement()
        {
            if (_availableObjects.Count > 0)
            {
                var element = _availableObjects.Pop();
                element.IsActive = true;
                return element;
            }

            if (AutoExpand)
            {
                Debug.Log("Expand pool new count: " + (TotalObjects + 1));
                return CreateObject(true);
            }

            throw new InvalidOperationException($"No free elements available in the pool of type {typeof(T)}.");
        }

        public void ReturnElement(T element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            element.Reset();
            element.IsActive = false;
            _availableObjects.Push(element);
        }

        public int TotalObjects => _allObjects.Count;
        public int AvailableObjects => _availableObjects.Count;
    }
}