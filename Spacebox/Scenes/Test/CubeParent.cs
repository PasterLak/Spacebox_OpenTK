
using Engine;
using Engine.Physics;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;



namespace Spacebox.Scenes.Test
{

    public interface IGameComponent
    {
        void Update();
        void Render();
    }
    public class CubeParent : SceneNode
    {

        private CubeRenderer2 cube;
        private CubeRenderer2 cube2;
        private CubeRenderer2 cube3;
        private AxesTest axes;

        private Model2 earth;
        private Model2 sun;
        private Model2 atmosphere;

        private Model2 moon;
        BoundingBox box;
        public CubeParent() 
        {
            cube = new CubeRenderer2(new Vector3(0,0,0));

            cube2 = new CubeRenderer2(new Vector3(0, 0, 2));
            cube3 = new CubeRenderer2(new Vector3(0, 0, 0));
            //cube2.Color = Color4.LightSteelBlue;
            cube3.Color = Color4.Red;
            axes = new AxesTest(new Vector3(0,0,0), 2);
           // AddChild(cube);
           
            cube.SetScale(0.5f);
            cube.Rotate(new Vector3(0,45,0));
            cube.Position = new Vector3(0, 0, 2);
            box = new BoundingBox(cube.Position, Vector3.One);
           // AddChild(cube2);
            cube2.Position =  new Vector3(0,2,0);
            cube2.AddChild(cube3);
            cube3.Position = new Vector3(0, 0, 1);
            cube3.ScaleBy(0.5f);
            // cube2.Position = cube.GetLocalAxisPoint(Vector3.UnitZ, 2f);
            //  AddChild(axes);

            var mat = new Material(new Shader("Shaders/textured"), new Texture2D("Resources/Textures/planet2.png"));
            var matSun = new Material(new Shader("Shaders/textured"), new Texture2D("Resources/Textures/sun.png"));
            var mat2 = new Material(new Shader("Shaders/textured"), new Texture2D("Resources/Textures/skybox2.png"));
            var mat3 = new Material(new Shader("Shaders/textured"), new Texture2D("Resources/Textures/moon.png"));

            //var sunMat = new ColorMaterial();

            var sunMat = new TextureMaterial(new Texture2D("Resources/Textures/sun.png"));
            sunMat.Color = new Color4(1f,1f,1f,1f);
            sun = new Model2("Resources/Models/sphere.obj", sunMat);
            earth = new Model2("Resources/Models/sphere.obj",mat );
            atmosphere = new Model2("Resources/Models/sphere.obj", new ColorMaterial());
          

            moon = new Model2("Resources/Models/sphere.obj", mat3);

            earth.SetScale(0.2f);
            moon.SetScale(0.1f);
            atmosphere.Scale = Vector3.One * 1.1f;

            earth.AddChild(moon);
            moon.Position = new Vector3(3, 0, 0);

            //earth.AddChild(atmosphere);
           

            sun.SetScale(1);
            sun.AddChild(earth);
            earth.Position = new Vector3(4, 0, 0);
            AddChild(sun);


        }
        float speed2 = 1f;
        public void Update()
        {
            var speed = 2f * Time.Delta;

            //Rotate(new Vector3(0,speed/2f, 0));
            earth.Rotate(new Vector3(0, speed / 4, 0));
            sun.Rotate(new Vector3(0, speed / 6, 0));
            // moon.Rotate(new Vector3(0, speed , 0));
            moon.RotateAround(Vector3.Zero, Vector3.UnitY, 2.3f * speed2);
            earth.RotateAround(Vector3.Zero, Vector3.UnitY, 1.3f * speed2);
            // cube.Translate(new Vector3(0, 0, speed));
            //cube.Update();

            if(Input.IsKeyDown(Keys.KeyPadAdd))
            {
                speed2 += 0.1f;
            }
            if (Input.IsKeyDown(Keys.KeyPadSubtract))
            {
                speed2 -= 0.1f;

                if (speed2 < 0) speed2 = 0;
            }
            //cube.RotateAround(Vector3.Zero, Vector3.UnitY, 1.3f * speed2);
            box.Center = cube.Position;

          
           //cube2.LookAt(cube.LocalToWorld(Vector3.Zero));
           // cube2.LookAt(new Vector3(5,0,5));
           // cube2.Update();
           //cube.Position = this.Position; cube.Rotation = this.Rotation;

            UpdateAll();
        }

        public void Render()
        {
           // VisualDebug.DrawBoundingBox(box, Color4.Red);
            // axes.Render(Camera.Main);
            // model.Render(Camera.Main);
            // model2.Render(Camera.Main);
            RenderAll(Camera.Main);

        }
    }
}
