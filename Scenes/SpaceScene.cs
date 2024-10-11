using Spacebox.Common.SceneManagment;
using Spacebox.Entities;
using OpenTK.Mathematics;
using Spacebox.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using Spacebox.Common.Audio;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.GUI;

namespace Spacebox.Scenes
{
    internal class SpaceScene : Scene
    {

        Player player;
        Skybox skybox;
        private Shader skyboxShader;

        // to base
        
        public override void LoadContent()
        {
            float q = 5;
            player = new Player(new Vector3(q + 3,0,q), 16/9f);
            skyboxShader = new Shader("Shaders/skybox");
            skybox = new Skybox("Resources/Models/cube.obj", skyboxShader, 
                new Texture2D("Resources/Textures/Skybox/space.png", true) );
            skybox.Scale = new Vector3(100,100,100);

                        var cube = new Model("Resources/Models/cube.obj");
            cube.Position = new Vector3(q,0,q);

            var cube2 = new ModelLocal("Resources/Models/cube.obj");
            cube2.Material.Color = new Vector4(0,0.2f,1,1);
            cube2.Position = new Vector3(q, 0, q + 2);


            CollisionManager.Add(player);


            Renderer.AddDrawable(cube);
            Renderer.AddDrawable(cube2);
            //renderer.AddDrawable(skybox);

            Input.SetCursorState(CursorState.Grabbed);
        }

        

        public override void Update()
        {
            player.Update();

            if(Input.IsKeyDown(Keys.Backspace))
            {
                SceneManager.LoadScene(typeof(MenuScene));
            }
            

        }

        public override void Render()
        {
            skybox.DrawTransparent(player);

            Renderer.RenderAll(player);

            Debug.DrawLine(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(100f, 0, 0), Color4.Red);
            Debug.DrawLine(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0, 100, 0), Color4.Green);
            Debug.DrawLine(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0, 0, 100), Color4.Blue);

        }

        public override void OnGUI()
        {
            Overlay.OnGUI(player);
        }

        public override void UnloadContent()
        {
            
        }

    }
}
