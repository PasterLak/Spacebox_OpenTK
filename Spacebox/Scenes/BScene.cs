using OpenTK.Windowing.GraphicsLibraryFramework;
using Engine.SceneManagement;

using Engine;

using Spacebox.Game;
using Spacebox.Game.Player;
using Engine.Audio;


namespace Spacebox.Scenes
{
    public static class SceneSwitcher
    {
        static float max = 3f;
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
               // SceneManager.LoadScene(type);
            }
        }
    }
    public class BScene : Scene
    {

        private FreeCamera0 player;
        public BScene(string[] args) 
        {
        }

        // Mesh no problems
        // Texture no problems ++ 
        // Buffer no problems
        // AudioClip no problems
        // Shader no problems, fixed, was +20% pro min 

        Skybox skybox;
        SpaceTexture skyboxTexture2;
        AudioSource audio;
        public override void LoadContent()
        {

            //var skyboxTexture = Resources.Load<Texture2D>("Resources/Textures/planet2.png");
            /* var mesh = Resources.Load<Engine.Mesh>("Resources/Models/test.obj");
              skyboxTexture2 = new SpaceTexture(2048, 2048, 12215);
             skybox = new Skybox(mesh, skyboxTexture2);
             player = new FreeCamera(new Vector3(0, 0, 5));
             player.DepthNear = 0.01f;
             player.CameraRelativeRender = false;
             skybox.IsAmbientAffected = false;


             var axes = new Axes(new Vector3(0, 0, 0), 100);

            var  cube = new CubeParent();*/
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

            /* for (int i = 0;i< 100; i++)
             {
                 var b = BuffersData.CreateBlockBuffer();
                 b.Dispose();
             }

             */
            Resources.LoadAllAsync<AudioClip>(
              new[]
              {
                    "Resources/Audio/Music/ambientMain.ogg"

              });

            var clip = Resources.Get<AudioClip>("ambientMain");
            audio = new AudioSource(clip);
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
            audio.Play();
        }

        public override void Render()
        {
            //GL.Clear(ClearBufferMask.ColorBufferBit);

            //skybox.DrawTransparent(player);

        }

        public override void OnGUI()
        {
        }

        public override void UnloadContent()
        {

            audio.Dispose();
        }

        public override void Update()
        {
           // player.Update();
            SceneSwitcher.Update(typeof(AScene));

            
          

        }
    }
}
