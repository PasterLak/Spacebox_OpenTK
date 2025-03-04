using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

using Engine.GUI;
using Engine.SceneManagment;

using Engine;

using Spacebox.Game;

namespace Spacebox.Scenes
{
    public static class SceneSwitcher
    {
        static float max = 1.5f;
        static float _time = max;
        static int counter = 0;
        public static void Update(Type type)
        {
            _time -= Time.Delta;

            if(_time < 0)
            {
                _time = max;
                counter++;

                Debug.Log($"<<< {counter} >>>");
                SceneManager.LoadScene(type);
            }
        }
    }
    public class BScene : Scene
    {

        public BScene(string[] args) : base(args)
        {
        }

        // Mesh no problems
        // Texture no problems ++ 
        // Buffer no problems
        // AudioClip no problems
        // Shader no problems, fixed, was +20% pro min 
        public override void LoadContent()
        {
            /*Resources.LoadAll<Texture2D>(
              new[]
              {
                    "Resources/Textures/arSphere.png",
                    "Resources/Textures/icon.png",
            
                      "Resources/Textures/moon.png",
                    "Resources/Textures/spacer.png",
                     "Resources/Textures/skybox2.png"
              });*/
            //var clip = Resources.Get<AudioClip>("music");

           /* Resources.LoadAll<AudioClip>(
             new[]
             {
                    "Resources/Audio/scroll.ogg",
                    "Resources/Audio/death2.ogg",
                    "Resources/Audio/screenshot.ogg",
                     "Resources/Audio/radarScanning.ogg",
                    "Resources/Audio/wallhit.ogg",
                    "Resources/Audio/flySpeedUp.ogg",
                    "Resources/Audio/Music/ambientMain.ogg",
                     "Resources/Audio/Music/music.ogg",

             });*/

            /* Resources.LoadAll<Engine.Mesh>(
              new[]
              {
                     "Resources/Models/sphere2.obj",
                     "Resources/Models/spacer.obj",

                     "Resources/Models/sphere.obj",

                     "Resources/Models/cube.obj",

                     "Resources/Models/skybox.obj",
                       "Resources/Models/test.obj",


              });*/
            /*Resources.LoadAll<Shader>(
             GetShaderFiles().ToArray()
             );*/

            for(int i = 0;i< 100; i++)
            {
                var b = BuffersData.CreateBlockBuffer();
                b.Dispose();
            }
          

        }

        public static List<string> GetShaderFiles(string rootFolder = "Shaders")
        {
            var files = Directory.GetFiles(rootFolder, "*.*", SearchOption.AllDirectories);
            var result = new List<string>();
            foreach (var file in files)
            {
                string relative = Path.GetRelativePath(Directory.GetCurrentDirectory(), file)
                                    .Replace('\\', '/');
                relative = Path.ChangeExtension(relative, null);
                result.Add(relative);
            }
            return result;
        }

        public override void Start()
        {

        }

        public override void Render()
        {
            //GL.Clear(ClearBufferMask.ColorBufferBit);



        }

        public override void OnGUI()
        {
        }

        public override void UnloadContent()
        {
         

        }

        public override void Update()
        {

            SceneSwitcher.Update(typeof(AScene));

            if (Input.IsKeyDown(Keys.S))
            {
                SceneManager.LoadScene(typeof(MenuScene));
            }

            if (Input.IsKeyDown(Keys.T))
            {
                SceneManager.LoadScene(typeof(TestScene));
            }

            if (Input.IsKeyDown(Keys.A))
            {
                SceneManager.LoadScene(typeof(AScene));
            }

          

        }
    }
}
