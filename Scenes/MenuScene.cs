using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;

namespace Spacebox.Scenes
{
    internal class MenuScene : Scene
    {

        AudioSource audio;
        AudioSource audio2;
        AudioManager audioManager;
        public MenuScene()
        {
        }

         ~MenuScene()
        {
            audio.Dispose();
            audio2.Dispose();
        }


        public override void LoadContent()
        {

            GL.ClearColor(0.8f, 0.5f, 0.3f, 1.0f);
            audioManager = new AudioManager();
            audio = new AudioSource("Resources/Audio/music.wav", audioManager.Device, audioManager.Context);
            audio.IsLooped = true;
            audio2 = new AudioSource("Resources/Audio/shooting.wav", audioManager.Device, audioManager.Context);

        }

        public override void Awake()
        {
            audio.Play();
        }
        public override void Start()
        {
            
        }

        public override void Render()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);

            SceneManager.Instance.GameWindow.SwapBuffers();
        }

        public override void UnloadContent()
        {
            audio.Dispose();
            audio2.Dispose();
            audioManager.Dispose();
        }

        public override void Update()
        {
          if(Input.IsKeyDown(Keys.Enter))
            {
                SceneManager.LoadScene(typeof(GameScene));
            }
           

            if (Input.IsKeyDown(Keys.T))
            {
                audio2.Play();
            }
        }
    }
}
