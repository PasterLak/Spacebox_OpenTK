using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using Spacebox.Scenes;
using System.Reflection;


namespace Spacebox.Common.SceneManagment
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

        public static GameWindow GameWindow { get; private set; }



        public static void Initialize(GameWindow gameWindow, Type startScene)
        {
            GameWindow = gameWindow;

            if (Instance != null)
            {
                Debug.Error("Error! There is already a SceneManager!");
                return;
            }


            //Instance = this;
            AddScene(typeof(ErrorScene));
            if (!typeof(Scene).IsAssignableFrom(startScene))
            {
                Debug.Error("The startSceneType must inherit from Scene." + nameof(startScene));

                LoadScene(typeof(ErrorScene));

                return;
            }


            GetAllScenes();
            AddStartScene(startScene);
            LoadScene(startScene);
        }


        private static void AddStartScene(Type startSceneType)
        {
            if (!types.Contains(startSceneType))
            {
                types.Add(startSceneType);
                Debug.Log($"{startSceneType.Name} added as start scene.");
            }

            _nextSceneType = new SceneType { typ = startSceneType };

        }

        private static void GetAllScenes()
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

        private static void AddScene(Type scene)
        {
            if (!types.Contains(scene))
            {
                types.Add(scene);
            }
        }

        public static void LoadScene(Type sceneType)
        {

            if (types.Contains(sceneType))
            {

                _nextSceneType = new SceneType { typ = sceneType };
            }
            else
            {
                Debug.Error($"Error: Scene of type {sceneType.Name} must inherit from class Scene!");

                LoadScene(typeof(ErrorScene));
                return;
            }

            StartNextScene();

        }

        public static void StartNextScene()
        {
            if (_nextSceneType != null)
            {
                if (CurrentScene != null)
                {
                    DisposablesUnloader.Dispose();

                    Scene sceneBase = CurrentScene as Scene;
                    sceneBase.Dispose();

                    ShaderManager.Dispose();
                    TextureManager.Dispose();

                    CurrentScene.Dispose();
                    CurrentScene.UnloadContent();
                    VisualDebug.Clear();
                }



                _currentSceneType = _nextSceneType;
                _nextSceneType = null;

                Scene sceneInstance = Activator.CreateInstance(_currentSceneType.typ) as Scene;

                CurrentScene = sceneInstance;


                Debug.Log("[SceneManager] Scene Loaded: [" + _currentSceneType.typ.Name + "] ",
                    Color4.Yellow);

                CurrentScene.LoadContent();
                CurrentScene.Awake();
                CurrentScene.Start();

                

            }
        }
    }
}
