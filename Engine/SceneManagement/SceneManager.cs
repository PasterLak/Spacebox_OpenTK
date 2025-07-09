using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using Engine.Audio;
using Engine.Light;
using Engine.Physics;
using Engine.SceneManagement;

namespace Engine.SceneManagement
{
    public interface ISceneWithParam<T>
    {
        void Initialize(T param);
    }
    public static class SceneManager
    {
        static readonly HashSet<Type> _registeredScenes = new();
        static Func<Scene> _nextFactory;
        static Func<Scene> _lastFactory;
        static Scene _current;

        public static Scene Current => _current;
        public static GameWindow GameWindow { get; private set; }

        public static void Initialize(GameWindow window, Action registerScenes)
        {
            GameWindow = window;
            registerScenes();
            Register<ErrorScene>();
            Load<ErrorScene>();
        }

 
        public static void Load<TScene>() where TScene : Scene, new()
        {
            if (!_registeredScenes.Contains(typeof(TScene)))
                throw new InvalidOperationException($"Scene {typeof(TScene).Name} not registered.");
            Debug.Log("[SceneManager] Loading scene ", Color4.White);
            _nextFactory = () =>
            {
                var s = new TScene();
                s.Name = s.GetType().Name;
                return s;
            };
            _lastFactory = _nextFactory;
            Switch();
        }

        public static void Load<TScene, TParam>(TParam param)
            where TScene : Scene, ISceneWithParam<TParam>, new()
        {
            if (!_registeredScenes.Contains(typeof(TScene)))
                throw new InvalidOperationException($"Scene {typeof(TScene).Name} not registered.");
            Debug.Log("[SceneManager] Loading scene ", Color4.White);
            _nextFactory = () =>
            {
                var s = new TScene();
                s.Name = s.GetType().Name;
                s.Initialize(param);
                return s;
            };
            _lastFactory = _nextFactory;
            Switch();
        }

        public static void Reload()
        {
            if (_lastFactory == null) return;
            _nextFactory = _lastFactory;
            Switch();
        }



        public static void Register<TScene>() where TScene : Scene, new()
        {
            _registeredScenes.Add(typeof(TScene));
        }

       

        static void Switch()
        {
            if (_current != null)
            {
                var name = _current.Name;
                Debug.Log("[SceneManager] Unloading scene: " + name, Color4.White);
                DisposablesUnloader.Dispose();

                _current.Destroy();
                _current.UnloadContent();
                _current.Dispose();
                LightSystem.Clear();
                InputManager.RemoveAllActions();
                EventBus.Clear();
                Camera.Main = null;
                Resources.UnloadAll();
                VisualDebug.Clear();
                _current = null;
                Debug.Log("[SceneManager] Scene was unloaded: " + name, Color4.White);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            _current = _nextFactory();
            _current.LoadContent();
            _current.Start();
        }

        public static void Update() => _current?.Update();
        public static void Render() => _current?.Render();
    }

    
}