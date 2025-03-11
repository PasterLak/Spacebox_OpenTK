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
    public class CubeParent : SceneNode, IDisposable
    {

        private CubeRenderer2 cube;
        private CubeRenderer2 cube2;
        private CubeRenderer2 cube3;
        private AxesTest axes;

        private Model2 earth;
        private Model2 sun;
        private Model2 atmosphere;
        private Model2 mars;

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

            var matEarth = new PlanetMaterial(Resources.Load<Texture2D>("Resources/Textures/planet2"));
            matEarth.GlowColor = new Vector3(0.9f,1,1);

            var matMars = new PlanetMaterial(Resources.Load<Texture2D>("Resources/Textures/planet3"));
            matMars.GlowColor = new Vector3(0.9f, 0.5f, 0.5f);

            var atmosphereMat = new AtmosphereMaterial(Resources.Load<Texture2D>("Resources/Textures/skybox2"));
            atmosphereMat.GlowColor = new Vector3(0.9f, 1, 1);

            var matMoon = new PlanetMaterial(Resources.Load<Texture2D>("Resources/Textures/moon"));
            matMoon.GlowColor = new Vector3(0.7f, 0.7f, 0.7f);
            //var sunMat = new ColorMaterial();

            var sunMat = new SunMaterial(Resources.Load<Texture2D>("Resources/Textures/sun"));
            sunMat.GlowColor = new Vector3(1f,1,1);
            sunMat.Color = new Color4(1f,1f,1f,1f);
            sun = new Model2("Resources/Models/sphere2.obj", sunMat);
            earth = new Model2("Resources/Models/sphere2.obj",matEarth );
            atmosphere = new Model2("Resources/Models/sphere2.obj", atmosphereMat);
            mars = new Model2("Resources/Models/sphere2.obj", matMars);
          
            sunMat.Shader.Use();
            sunMat.SetUniforms(sun.WorldMatrix);
            moon = new Model2("Resources/Models/sphere2.obj", matMoon);

            mars.SetScale(0.15f);
           // earth.SetScale(0.2f);
            moon.SetScale(0.2f);
            atmosphere.Scale = Vector3.One * 1.01f;

            earth.AddChild(moon);
            moon.Position = new Vector3(2.5f, 0, 0);

            earth.AddChild(atmosphere);

            
            sun.SetScale(1);
            sun.AddChild(mars);
            //sun.AddChild(earth);
            earth.Position = new Vector3(4, 0, 0);
            mars.Position = new Vector3(-7, 0, 0);
            AddChild(sun);

            boxOBB = new BoundingBoxOBB(earth.Position, earth.Scale , earth.Rotation);
        }
        BoundingBoxOBB boxOBB;
        float speed2 = 0.01f;
        public void Update()
        {
            var speed = 2f * Time.Delta;

            //Rotate(new Vector3(0,speed/2f, 0));
            earth.Rotate(new Vector3(0, speed / 4, 0));
            mars.Rotate(new Vector3(0, speed / 3, 0));
            atmosphere.Rotate(new Vector3(0, speed / 2, 0));
            sun.Rotate(new Vector3(0, speed  * speed2, 0));
            // moon.Rotate(new Vector3(0, speed , 0));
            moon.RotateAround(Vector3.Zero, Vector3.UnitY, 2.3f * speed2);
            earth.RotateAround(Vector3.Zero, Vector3.UnitY, 1.3f * speed2);
            mars.RotateAround(Vector3.Zero, Vector3.UnitY, 1.2f * speed2);
            // cube.Translate(new Vector3(0, 0, speed));
            //cube.Update();

            boxOBB.Center = earth.GetWorldPosition();
            boxOBB.Rotation = earth.Rotation;

            if (Input.IsKeyDown(Keys.KeyPadAdd))
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
            if(VisualDebug.Enabled)
            VisualDebug.DrawOBB(boxOBB, Color4.Red);
            // axes.Render(Camera.Main);.\stats.ps1 
            // model.Render(Camera.Main);
            // model2.Render(Camera.Main);
            RenderAll(Camera.Main);
            earth.Render();
            cube.Render();
        }

        public void Dispose()
        {
            cube.Dispose();
            cube2.Dispose();
            cube3.Dispose();
            axes.Dispose();
        }
    }
}
