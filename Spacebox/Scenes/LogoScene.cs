using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

using Engine.GUI;
using Engine.SceneManagment;

using Engine;

namespace Spacebox.Scenes
{
    public class LogoScene : Scene
    {

        Sprite sprite;

        public LogoScene(string[] args) : base(args)
        {
        }


        public override void LoadContent()
        {

            //GL.ClearColor(0, 0, 0, 0);

            sprite = new Sprite("Resources/Textures/planet2.png", new Vector2(0, 0),
                new Vector2(Window.Instance.Size.X, Window.Instance.Size.Y));

        }

        public override void Start()
        {

            //SceneManager.LoadScene(typeof(MenuScene));
            //SceneManager.LoadScene(typeof(TestScene));

            Input.HideCursor();
        }

        public override void Render()
        {
            //GL.Clear(ClearBufferMask.ColorBufferBit);


            sprite.Render(new Vector2(0, 0), new Vector2(1, 1));

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

            sprite.UpdateWindowSize(Window.Instance.ClientSize);
            sprite.UpdateSize(Window.Instance.Size);
            //sprite.UpdateSize(new Vector2(Window.Instance.Size.X, Window.Instance.Size.Y));
        
            if (Input.IsKeyDown(Keys.S))
            {
                SceneManager.LoadScene(typeof(MenuScene));
            }

            if (Input.IsKeyDown(Keys.T))
            {
                SceneManager.LoadScene(typeof(TestScene));
            }

            if (Input.IsKeyDown(Keys.R))
            {
                sprite.Shader.ReloadShader();
            }

        }
    }
}
