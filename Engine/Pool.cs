namespace Engine
{
    public sealed class Pool<T> where T : class, new()
    {
        public bool AutoExpand { get; set; } = true;

        private readonly Stack<T> _availableObjects;
        private readonly List<T> _allObjects;
        private readonly Func<T, T> _initializeFunc;
        private readonly Action<T> _onTakeFunc;
        private readonly Action<T> _resetFunc;
        private readonly Func<T, bool> _isActiveFunc;
        private readonly Action<T, bool> _setActiveFunc;


        public Pool(int initialCount,
                   Func<T, T> initializeFunc = null,
                   Action<T> onTakeFunc = null,
                   Action<T> resetFunc = null,
                   Func<T, bool> isActiveFunc = null,
                   Action<T, bool> setActiveFunc = null,
                   bool autoExpand = true)
        {
            AutoExpand = autoExpand;
            _availableObjects = new Stack<T>(initialCount);
            _allObjects = new List<T>(initialCount);

            _initializeFunc = initializeFunc ?? (obj => obj);
            _onTakeFunc = onTakeFunc ?? (_ => { });
            _resetFunc = resetFunc ?? (_ => { });
            _isActiveFunc = isActiveFunc ?? (_ => true);
            _setActiveFunc = setActiveFunc ?? ((_, __) => { });

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
            T obj = new T();
            T initializedObj = _initializeFunc(obj);
            _setActiveFunc(initializedObj, isActiveByDefault);
            _allObjects.Add(initializedObj);
            return initializedObj;
        }

        public T Take()
        {
            if (_availableObjects.Count > 0)
            {
                var element = _availableObjects.Pop();
                _setActiveFunc(element, true);
                _onTakeFunc(element);
                return element;
            }
            if (AutoExpand)
            {
                var newElement = CreateObject(true);
                _onTakeFunc(newElement);
                return newElement;
            }
            throw new InvalidOperationException($"No free elements available in the pool of type {typeof(T)}.");
        }

        public void Release(T element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            _resetFunc(element);
            _setActiveFunc(element, false);
            _availableObjects.Push(element);
        }

        public int TotalObjects => _allObjects.Count;
        public int AvailableObjects => _availableObjects.Count;
    }
}