using Engine.Light;
using OpenTK.Mathematics;
using System.Xml.Linq;

namespace Engine.SceneManagement
{
    public interface ISceneWithArgs<T>
    {
        void Initialize(T param);
    }
    public static class SceneManager
    {
        static readonly HashSet<Type> _registeredScenes = new();
        private static readonly Stack<Func<Scene>> _history = new();
        static Func<Scene> _nextFactory;
        static Scene _current;
        static Func<Scene> _lastFactory;

        public static Scene Current => _current;

        public static void Initialize( Action registerScenes)
        {
            
            registerScenes();
            Register<ErrorScene>();

            Load<ErrorScene>();
        }

        public static void Load(Type sceneType)
        {
            if (!_registeredScenes.Contains(sceneType))
            {
                Debug.Error($"[SceneManager] Scene {sceneType.Name} not registered.");
                Load<ErrorScene>();
                return;
            }

            if (_nextFactory != null)
                _history.Push(_nextFactory);

           
            _nextFactory = () =>
            {
                var s = (Scene)Activator.CreateInstance(sceneType)!;
                s.Name = sceneType.Name;
                return s;
            };
            _lastFactory = _nextFactory;
            Switch(sceneType.Name);
        }

        public static void Load<TScene>() where TScene : Scene, new()
        {
            
            if (!_registeredScenes.Contains(typeof(TScene)))
            {
                Debug.Error($"[SceneManager] Scene {typeof(TScene).Name} not registered.");
                Load<ErrorScene>();
                return;
            }
            if (_nextFactory != null)
                _history.Push(_nextFactory);
            
            _nextFactory = () =>
            {
                var s = new TScene();
                s.Name = s.GetType().Name;
                return s;
            };
            _lastFactory = _nextFactory;

            Switch(typeof(TScene).Name);
        }

        public static void Load<TScene, TParam>(TParam param)
            where TScene : Scene, ISceneWithArgs<TParam>, new()
        {
            if (!_registeredScenes.Contains(typeof(TScene)))
            {
                Debug.Error($"[SceneManager] Scene {typeof(TScene).Name} not registered.");
                Load<ErrorScene>();
                return;
            }

            if (_nextFactory != null)
                _history.Push(_nextFactory);
          
            _nextFactory = () =>
            {
                var s = new TScene();
                s.Name = s.GetType().Name;
                s.Initialize(param);
                return s;
            };
            _lastFactory = _nextFactory;

            Switch(typeof(TScene).Name);
        }

        public static void Reload()
        {
            if (_history.Count == 0) return;
            _nextFactory = _lastFactory;
            Switch(_current.GetType().Name);
        }

        public static void Register<TScene>() where TScene : Scene, new()
        {
            _registeredScenes.Add(typeof(TScene));
        }

        public static void LoadPrevious()
        {
            if (_history.Count == 0) return;
            _nextFactory = _history.Pop();
            Switch("Previous");
        }
        private static void Switch(string newSceneName)
        {
            if (_current != null)
            {
       
                var name = _current.Name;
                Debug.Log("[SceneManager] Unloading scene: " + name, Color4.White);

                _current.Destroy();
                _current.UnloadContent();
                LightSystem.Clear();
                InputManager0.RemoveAllActions(true);
                EventBus.Clear();
                Camera.Main = null;
                Resources.UnloadAll();
                VisualDebug.Clear();
                _current = null;
                

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                Debug.Log("[SceneManager] Scene was unloaded: " + name, Color4.White);
            }
           
            Debug.Log("------------------------------------------------------------------", Color4.White);
            Debug.Log("     [SceneManager] Loading scene: " + newSceneName  +" >>>", Color4.Yellow);
            Debug.Log("------------------------------------------------------------------", Color4.White);
            Debug.Log("[SceneManager] Constructor", Color4.Yellow);
            _current = _nextFactory();
            Debug.Log("[SceneManager] Loading content", Color4.Yellow);
            _current.LoadContent();
            Debug.Log("[SceneManager] Loaded: " + newSceneName, Color4.Yellow);
            _current.Start();
        }

        public static void Update() => _current?.Update();
        public static void Render() => _current?.Render();
    }

    
}