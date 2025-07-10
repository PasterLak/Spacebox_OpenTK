using Engine.Components.Debug;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Drawing;

namespace Engine.SceneManagement
{
    public class ParticleSystemEditor : Scene
    {

        private Color4 Color4 = Color4.Black;

        OrbitalCamera camera;
        ParticleSystem system;
        public override void LoadContent()
        {

            camera = AddChild(new OrbitalCamera(Vector3.Zero, 10, 1, 100));
            camera.Projection = ProjectionType.Orthographic;
            //AddChild(new CubeRenderer(Vector3.Zero));
            camera.DepthFar = 1000;
            camera.DepthNear = 0.01f;

            var emitter = new SphereEmitter
            {
                Center = Vector3.Zero,
                Radius = 0.5f,
                SpeedMin = 0.2f,
                SpeedMax = 1f,
                LifeMin = 3f,
                LifeMax = 5f,
                SizeMin = 0.1f,
                SizeMax = 0.2f,
                ColorStart = new Vector4(1, 1, 1, 1),
                ColorEnd = new Vector4(1, 1, 1, 0)
            };

            var dust = Resources.Load<Texture2D>("Resources/Textures/dust.png");
            dust.FilterMode = FilterMode.Nearest;

             system = new ParticleSystem(new ParticleMaterial(dust), emitter) { Max = 500, Rate = 100 };




            // system.AttachComponent(new MoverComponent(new Vector3(1,0,0), 20));
            AttachComponent(new AxesDebugComponent(1));
            AddChild(system);

        }

        public override void Start()
        {
           
        }


        public override void Render()
        {
            GL.ClearColor(Color4);
            base.Render();
        }

        public override void OnGUI()
        {
            ParticleSystemUI.ShowParticleSystemEditor(system,camera);
        }

        public override void UnloadContent()
        {
        }

        public override void Update()
        {
            base.Update();
        }
    }
}