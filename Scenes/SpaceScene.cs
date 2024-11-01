using Spacebox.Common.SceneManagment;
using OpenTK.Mathematics;
using Spacebox.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.GUI;
using Spacebox.Game;
using Spacebox.Entities;
using Spacebox.Common.Audio;

namespace Spacebox.Scenes
{
    internal class SpaceScene : Scene
    {

        Astronaut player;
        Skybox skybox;
        World world;
        private Shader skyboxShader;

        private Chunk chunk;
        private Sector sector;
        Shader blocksShader;
        private Texture2D blockTexture;
        private Texture2D lightAtlas;
        // to base
        private Sprite sprite;
        private AudioSource blockPlace;
        private AudioSource blockDestroy;
        private bool ShowBlocksList = false;
        public override void LoadContent()
        {
            float q = 5;
            player = new Astronaut(new Vector3(q + 3,0,q), 16/9f);

            PlayerSaveLoadManager.LoadPlayer(player);


            skyboxShader = new Shader("Shaders/skybox");
            skybox = new Skybox("Resources/Models/cube.obj", skyboxShader, 
                new SpaceTexture(512,512) );
            skybox.Scale = new Vector3(100,100,100);
            skybox.IsAmbientAffected = false;

            Lighting.AmbientColor = new Vector3(0.5f, 0.5f, 0.5f);


            CollisionManager.Add(player);

            SoundManager.AddAudioClip("blockPlace");
            SoundManager.AddAudioClip("blockDestroy");
            blockPlace = new AudioSource(SoundManager.GetClip("blockPlace"));
            blockDestroy = new AudioSource(SoundManager.GetClip("blockDestroy"));
           
            //audio2 = new AudioSource(new AudioClip("shooting", SoundManager));
            
            //renderer.AddDrawable(skybox);

            Input.SetCursorState(CursorState.Grabbed);

            //chunk = new Chunk(new Vector3(0,0,0));
            

            blocksShader = new Shader("Shaders/block");
            blockTexture = GameBlocks.AtlasTexture;

            lightAtlas = new Texture2D("Resources/Textures/lightAtlas.png", true);

            blocksShader.Use();
            blocksShader.SetInt("texture0", 0);
            blocksShader.SetInt("textureAtlas", 1);

            blocksShader.SetVector3("fogColor", new Vector3(0,0,0));
            blocksShader.SetVector3("ambientColor", Lighting.AmbientColor);

            Texture2D block = UVAtlas.GetBlockTexture(blockTexture, 0,0);

            //sprite = new Sprite(block, new Vector2(0,0) , new Vector2(500,500));
            sprite = new Sprite("Resources/Textures/cat.png", new Vector2(0, 0), new Vector2(Window.Instance.Size.X, Window.Instance.Size.Y));
            //chunk.RemoveBlock(0,0,0);

            world = new World(player);

            sector = new Sector(new Vector3(0, 0, 0), new Vector3i(0, 0, 0), world);


            AmbientSaveLoadManager.LoadAmbient();
        }

        

        public override void Update()
        {
            player.Update();

            if(Input.IsKeyDown(Keys.Backspace))
            {
                SceneManager.LoadScene(typeof(MenuScene));
            }

            if (Input.IsKeyDown(Keys.R))
            {
                player.Position = new Vector3(0,0,0);
            }

            if(Input.IsKeyDown(Keys.Tab))
            {
                ShowBlocksList = !ShowBlocksList;
            }

            if (Input.IsMouseButtonDown(MouseButton.Right))
            {
                blockPlace.Play();
            }
            if (Input.IsMouseButtonDown(MouseButton.Left))
            {
                blockDestroy.Play();
            }

            //chunk.Test(player);

            sprite.Shader.SetFloat("time", (float)GLFW.GetTime());
            sprite.Shader.SetVector2("screen", new Vector2(Window.Instance.Size.X, Window.Instance.Size.Y));
            sprite.Shader.SetVector2("mouse", new Vector2(0, 0));

            sprite.UpdateSize(Window.Instance.Size);

            //world.Update();

            sector.Update();
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

            //blocksShader.SetMatrix4("model", model);
            blocksShader.SetMatrix4("view", view);
            blocksShader.SetMatrix4("projection", projection);

            blocksShader.SetVector3("cameraPosition", player.Position);

            blocksShader.SetVector3("ambientColor", Lighting.AmbientColor);

           

            blockTexture.Use(TextureUnit.Texture0);
            lightAtlas.Use(TextureUnit.Texture1);

            //chunk.Draw(blocksShader);
            sector.Render(blocksShader);

            //world.Render(blocksShader);

            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            //sprite.Render(new Vector2(0, 0), new Vector2(1, 1));

            Debug.DrawLine(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(100f, 0, 0), Color4.Red);
            Debug.DrawLine(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0, 100, 0), Color4.Green);
            Debug.DrawLine(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0, 0, 100), Color4.Blue);

        }

        public override void OnGUI()
        {
            
            Overlay.OnGUI(player);

            if(ShowBlocksList)
            BlocksOverlay.OnGUI(player);
        }

        public override void UnloadContent()
        {
            blocksShader.Dispose();
            blockTexture.Dispose();
            lightAtlas.Dispose();
            sprite.Dispose();
            blockPlace.Dispose();
            blockDestroy.Dispose();
        }

    }
}
