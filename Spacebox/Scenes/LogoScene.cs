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

            GL.ClearColor(0, 0, 0, 0);

            sprite = new Sprite("Resources/Textures/dust.png", new Vector2(0, 0),
                new Vector2(Window.Instance.Size.X, Window.Instance.Size.Y));

        }

        public override void Start()
        {

            SceneManager.LoadScene(typeof(MenuScene));
            //SceneManager.LoadScene(typeof(TestScene));

            Input.HideCursor();
        }

        public override void Render()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);


            sprite.Render(new Vector2(0, 0), new Vector2(1, 1));

        }

        public override void OnGUI()
        {
        }

        public override void UnloadContent()
        {
            sprite.Dispose();

            BufferAttribute[] attributes = new BufferAttribute[]
{
    new BufferAttribute { Name = "aPos",    Size = 3 },
    new BufferAttribute { Name = "aNormal", Size = 3 }
};

            BufferShader buffer = new BufferShader(attributes);

           // buffer.BindBuffer(ref vertices, ref indices);
          //  buffer.SetAttributes();



        }

        public override void Update()
        {

            sprite.Shader.SetFloat("time", (float)GLFW.GetTime());
            sprite.Shader.SetVector2("screen", new Vector2(Window.Instance.Size.X, Window.Instance.Size.Y));
            sprite.Shader.SetVector2("mouse", new Vector2(0, 0));

            //sprite.UpdateWindowSize(Window.Instance.Size);
            sprite.UpdateSize(Window.Instance.Size);
            //sprite.UpdateSize(new Vector2(Window.Instance.Size.X, Window.Instance.Size.Y));
        
            if (Input.IsKeyDown(Keys.S))
            {
                //SceneManager.LoadScene(typeof(SpaceMenuScene));
            }

            if (Input.IsKeyDown(Keys.G))
            {
               // SceneManager.LoadScene(typeof(OldSpaceScene));
            }

            if (Input.IsKeyDown(Keys.R))
            {
                sprite.Shader.ReloadShader();
            }

        }
    }
}
