using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

using Engine.GUI;
using Engine.SceneManagement;

using Engine;
using Engine.Audio;
using Spacebox.GUI;
using Spacebox.Game.GUI;
using Spacebox.Game;

namespace Spacebox.Scenes
{
    public class ServerMessageScene : Scene
    {

    
  
        public override void LoadContent()
        {

        

        }

        public override void Start()
        {

           

        }

        public override void Render()
        {

          

        }

        public override void OnGUI()
        {
            Theme.ApplySpaceboxTheme();
            Settings.ShowInterface = true;
            CenteredText.Show();
            CenteredText.SetText("Unable to connect to the server.\nPress M to return to the main menu.");
            CenteredText.OnGUI();
        }

        public override void UnloadContent()
        {
            //sprite.Dispose();
          //  audio.Dispose();

        }

        public override void Update()
        {

          
          
            if (Input.IsKeyDown(Keys.M))
            {
                SceneManager.Load<MenuScene>();
            }

        }
    }
}
