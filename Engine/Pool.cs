using System;
using System.Collections.Generic;

namespace Engine
{
    /// <summary>
    /// Interface for objects that support pooling.
    /// </summary>
    /// <typeparam name="T">The type of the pooled object.</typeparam>
    public interface IPoolable<T>
    {
        /// <summary>Indicates whether the object is currently active.</summary>
        bool IsActive { get; set; }

        /// <summary>
        /// Creates a new instance from the pool.
        /// </summary>
        /// <returns>A new instance of type <typeparamref name="T"/>.</returns>
        T CreateFromPool();

        /// <summary>
        /// Resets the object before returning it to the pool.
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// A generic object pool for managing reusable instances.
    /// </summary>
    /// <typeparam name="T">
    /// The type of objects stored in the pool. Must implement <see cref="IPoolable{T}"/> and have a parameterless constructor.
    /// </typeparam>
    public sealed class Pool<T> where T : IPoolable<T>, new()
    {
        /// <summary>
        /// Determines whether the pool expands automatically when no free objects are available.
        /// </summary>
        public bool AutoExpand { get; set; } = true;

        /// <summary>Stack containing available (inactive) objects.</summary>
        private readonly Stack<T> _availableObjects;

        /// <summary>List containing all objects (both active and inactive).</summary>
        private readonly List<T> _allObjects;

        /// <summary>
        /// Retrieves all objects in the pool (both active and inactive).
        /// </summary>
        /// <returns>A list of all objects managed by the pool.</returns>
        public List<T> GetAllObjects() => _allObjects;

        /// <summary>
        /// Constructs the object pool.
        /// </summary>
        /// <param name="initialCount">The initial number of objects in the pool.</param>
        /// <param name="autoExpand">Determines whether the pool should expand automatically if empty.</param>
        public Pool(int initialCount, bool autoExpand = true)
        {
            AutoExpand = autoExpand;
            _availableObjects = new Stack<T>(initialCount);
            _allObjects = new List<T>(initialCount);
            CreatePool(initialCount);
        }

        /// <summary>
        /// Creates the initial pool with a specified number of objects.
        /// </summary>
        /// <param name="count">The number of objects to preallocate in the pool.</param>
        private void CreatePool(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var obj = CreateObject(false);
                _availableObjects.Push(obj);
            }
        }

        /// <summary>
        /// Creates a new object and adds it to the pool.
        /// </summary>
        /// <param name="isActiveByDefault">Indicates whether the object should be active upon creation.</param>
        /// <returns>A new instance of <typeparamref name="T"/>.</returns>
        private T CreateObject(bool isActiveByDefault = false)
        {
            // Creates a new instance and initializes it using CreateFromPool().
            T createdObject = new T().CreateFromPool();
            createdObject.IsActive = isActiveByDefault;
            _allObjects.Add(createdObject);
            return createdObject;
        }

        /// <summary>
        /// Retrieves a free object from the pool.
        /// </summary>
        /// <returns>A free object of type <typeparamref name="T"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no objects are available and AutoExpand is disabled.
        /// </exception>
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
                Debug.Log("Expanding pool: new count " + (TotalObjects + 1));
                return CreateObject(true);
            }

            throw new InvalidOperationException($"No free elements available in the pool of type {typeof(T)}.");
        }

        /// <summary>
        /// Returns an object to the pool for reuse.
        /// </summary>
        /// <param name="element">The object to return.</param>
        /// <exception cref="ArgumentNullException">Thrown when the provided object is null.</exception>
        public void ReturnElement(T element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            element.Reset();
            element.IsActive = false;
            _availableObjects.Push(element);
        }

        /// <summary>Gets the total number of objects managed by the pool.</summary>
        public int TotalObjects => _allObjects.Count;

        /// <summary>Gets the number of available (inactive) objects in the pool.</summary>
        public int AvailableObjects => _availableObjects.Count;
    }
}
