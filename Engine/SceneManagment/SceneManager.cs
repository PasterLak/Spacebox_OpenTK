﻿using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using Engine.Audio;


namespace Engine.SceneManagment
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
            var assemblies = AppDomain.CurrentDomain.GetAssemblies(); 

            foreach (var assembly in assemblies)
            {
                List<Type> derivedClasses = assembly.GetTypes()
                    .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(Scene)))
                    .ToList();

                foreach (var derivedClass in derivedClasses)
                {
                    AddScene(derivedClass);
                }
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
            LoadScene(sceneType, new string[0]);
        }
        public static void LoadScene(Type sceneType, string[] args)
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

            StartNextScene(args);

        }

        private static void StartNextScene(string[] args)
        {
            if (_nextSceneType != null)
            {
                if (CurrentScene != null)
                {
                    Debug.Log("[SceneManager] Unloading content ",
                    Color4.White);
                    DisposablesUnloader.Dispose();

                    Scene sceneBase = CurrentScene as Scene;
                    sceneBase.Dispose();

                    
                   
                    InputManager.RemoveAllActions(true);
                    CurrentScene.Dispose();
                    CurrentScene.UnloadContent();

                    SoundManager.Dispose();
             
                    Resources.UnloadAll();
                    VisualDebug.Clear();

                    Camera.Main = null;
                   // Debug.Log("Active Textures: " + GL.GetInteger(GetPName.TextureBinding2D));
                   // Debug.Log("Active Buffers: " + GL.GetInteger(GetPName.ArrayBufferBinding));
                    

                    Debug.Log("[SceneManager] Content was unloaded ",
                    Color4.White);
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                Debug.Log("[SceneManager] Loading scene ", Color4.White);
                _currentSceneType = _nextSceneType;
                _nextSceneType = null;

                Scene sceneInstance = Activator.CreateInstance(_currentSceneType.typ, new object[] { args }) as Scene; 

      

                CurrentScene = sceneInstance;

                CurrentScene.Name = _currentSceneType.typ.Name;

                Debug.Log("[SceneManager] Scene Loaded: [" + CurrentScene.Name + "] ",
                    Color4.Yellow);

                CurrentScene.LoadContent();
                
                CurrentScene.Start();
                Resources.PrintLoadedResources();


            }
        }
    }
}
