using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Common.Audio;
using Spacebox.Common.SceneManagment;
using Spacebox.Game;
using Spacebox.GUI;

namespace Spacebox.Scenes
{
    internal class LogoScene : Scene
    {

        Sprite sprite;

        public LogoScene()
        {
        }


        public override void LoadContent()
        {
        

           // GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            GL.ClearColor(0,0,0,0);
           
            sprite = new Sprite("Resources/Textures/cat.png", new Vector2(0,0),
                new Vector2(Window.Instance.Size.X, Window.Instance.Size.Y));
            //sprite = new Sprite(iso, new Vector2(0, 0), new Vector2(500, 500));
            //GL.Enable(EnableCap.DepthTest);

        }

        public override void Awake()
        {
          
        }
        public override void Start()
        {
            if(!Directory.Exists("Mods"))
            {
                Directory.CreateDirectory("Mods");
            }
            if (!Directory.Exists("Saves"))
            {
                Directory.CreateDirectory("Saves");
            }

            SceneManager.LoadScene(typeof(SpaceMenuScene));

            Input.HideCursor();
        }

        public override void Render()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);
            

            sprite.Render(new Vector2(0,0), new Vector2(1,1));

        }

        public override void OnGUI()
        {
        }

        public override void UnloadContent()
        {
            sprite.Dispose();
            
        }

        public override void Update()
        {

            sprite.Shader.SetFloat("time", (float)GLFW.GetTime());
            sprite.Shader.SetVector2("screen", new Vector2(Window.Instance.Size.X, Window.Instance.Size.Y));
            sprite.Shader.SetVector2("mouse", new Vector2(0,0));

            //sprite.UpdateWindowSize(Window.Instance.Size);
            sprite.UpdateSize(Window.Instance.Size);
            //sprite.UpdateSize(new Vector2(Window.Instance.Size.X, Window.Instance.Size.Y));
            if (Input.IsKeyDown(Keys.Enter))
            {
                SceneManager.LoadScene(typeof(MenuScene));
            }
            if (Input.IsKeyDown(Keys.S))
            {
                //SceneManager.LoadScene(typeof(SpaceMenuScene));
            }

            if (Input.IsKeyDown(Keys.G))
            {
                SceneManager.LoadScene(typeof(SpaceScene));
            }

            if (Input.IsKeyDown(Keys.R))
            {
                sprite.Shader.ReloadShader();
            }
         
        }
    }
}
