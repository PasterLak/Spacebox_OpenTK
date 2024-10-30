using Spacebox.Common.SceneManagment;
using Spacebox.Entities;
using OpenTK.Mathematics;
using Spacebox.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using Spacebox.Common.Audio;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.GUI;
using Spacebox.Game;
using System.Drawing;
using System.Resources;

namespace Spacebox.Scenes
{
    internal class SpaceScene : Scene
    {

        Player player;
        Skybox skybox;
        private Shader skyboxShader;

        private Chunk chunk;
        Shader blocksShader;
        private Texture2D blockTexture;
        private Texture2D lightAtlas;
        // to base

        public override void LoadContent()
        {
            float q = 5;
            player = new Player(new Vector3(q + 3,0,q), 16/9f);


            skyboxShader = new Shader("Shaders/skybox");
            skybox = new Skybox("Resources/Models/cube.obj", skyboxShader, 
                new Texture2D("Resources/Textures/Skybox/space6.png", true) );
            skybox.Scale = new Vector3(100,100,100);

         


            CollisionManager.Add(player);


        
            //renderer.AddDrawable(skybox);

            Input.SetCursorState(CursorState.Grabbed);

            chunk = new Chunk();

            blocksShader = new Shader("Shaders/block");
            blockTexture = new Texture2D("Resources/Textures/blocks.png", true);

            lightAtlas = new Texture2D("Resources/Textures/lightAtlas.png", true);

            blocksShader.Use();
            blocksShader.SetInt("texture0", 0);
            blocksShader.SetInt("textureAtlas", 1);

            blocksShader.SetVector3("fogColor", new Vector3(0,0,0));
            blocksShader.SetVector3("ambientColor", Lighting.AmbientColor);

            chunk.RemoveBlock(0,0,0);
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
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            skybox.DrawTransparent(player);

            Renderer.RenderAll(player);

            GL.Enable(EnableCap.DepthTest);
           


            Matrix4 model = Matrix4.Identity;
            Matrix4 view = player.GetViewMatrix();
            Matrix4 projection = player.GetProjectionMatrix();

            blocksShader.SetMatrix4("model", model);
            blocksShader.SetMatrix4("view", view);
            blocksShader.SetMatrix4("projection", projection);

            blocksShader.SetVector3("cameraPosition", player.Position);

            blocksShader.SetVector3("ambientColor", Lighting.AmbientColor);

           

            blockTexture.Use(TextureUnit.Texture0);
            lightAtlas.Use(TextureUnit.Texture1);

            chunk.Draw(blocksShader);


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
            blocksShader.Dispose();
            blockTexture.Dispose();
            lightAtlas.Dispose();
        }

    }
}
