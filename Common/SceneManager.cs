using OpenTK.Windowing.Desktop;
using Spacebox_OpenTK.Scenes;
using System.Reflection;


namespace Spacebox_OpenTK.Common
{

    public class SceneType
    {
        public Type typ;
    }
    public class SceneManager
    {
        public static SceneManager Instance;

        private static HashSet<Type> types = new HashSet<Type>();

        private static SceneType? _currentSceneType = null;
        private static Scene _currentScene;
        public static Scene CurrentScene { get { return _currentScene; } private set { _currentScene = value; } }


        private static SceneType? _nextSceneType = null;
        //private static Scene _nextScene;
      

        public GameWindow GameWindow {  get; private set; }

        public SceneManager(GameWindow gameWindow, Type startScene) 
        {
            GameWindow = gameWindow;

            if (Instance != null)
            {
                Console.WriteLine("Error! There is already a SceneManager!");
                return;
            }


            Instance = this;
            AddScene(typeof(ErrorScene));
            if (!typeof(Scene).IsAssignableFrom(startScene))
            {
                Console.WriteLine("The startSceneType must inherit from Scene.", nameof(startScene));
                
                LoadScene(typeof(ErrorScene));

                return;
            }


            GetAllScenes();
            AddStartScene(startScene);
            LoadScene(startScene);
        }

        private void AddStartScene(Type startSceneType)
        {
            if (!types.Contains(startSceneType))
            {
                types.Add(startSceneType);
                Console.WriteLine($"{startSceneType.Name} added as start scene.");
            }

            _nextSceneType = new SceneType { typ = startSceneType };

        }

        private void GetAllScenes()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            // Find all types that inherit from the Scene class
            List<Type> derivedClasses = assembly.GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(Scene)))
                .ToList();
         
            foreach (var derivedClass in derivedClasses)
            {
                AddScene(derivedClass);
                
            }
        }

        private void AddScene(Type scene)
        {
            if (!types.Contains(scene))
            {
                types.Add (scene);
            }
        }

        public static void LoadScene(Type sceneType)
        {

            if (types.Contains(sceneType))
            {
                //CurrentScene.UnloadContent();

                _nextSceneType = new SceneType { typ = sceneType }; 
            }
            StartNextScene();

        }

        public static void StartNextScene()
        {
            if (_nextSceneType != null)
            {
                if(CurrentScene != null)
                CurrentScene.UnloadContent();

                _currentSceneType = _nextSceneType;
                _nextSceneType = null;

                Scene sceneInstance = Activator.CreateInstance(_currentSceneType.typ) as Scene;

                CurrentScene = sceneInstance;

                Console.WriteLine("Scene Loaded: " + _currentSceneType.typ.Name);

                CurrentScene.LoadContent();
                CurrentScene.Awake();
                CurrentScene.Start();

                _nextSceneType = null;
            }
        }
    }
}
