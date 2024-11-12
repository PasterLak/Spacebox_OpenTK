using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Common.Audio;
using Spacebox.Common.SceneManagment;
using Spacebox.Entities;
using Spacebox.GUI;
using Spacebox.UI;


namespace Spacebox.Scenes
{
    internal class GameScene : Scene
    {
        private int _vertexBufferObject;

        private int _vaoModel;

        private int _vaoLamp;

        private Shader _lampShader;

        private Shader _lightingShader;

        private Texture2D _diffuseMap;

        private Texture2D _specularMap;
        private List<Light> _lights;

        private Player player;
   

        private bool flashLight = true;

        AudioSource audio;

        BitmapFont font;
      
        TextRenderer textRenderer;

        Model[] planes = new Model[4];
       
        Model arrow;

        Water water;
        Skybox skybox; Skybox skyboxExtern;

        public GameScene()
        {
        }

        public override void LoadContent()
        {


            GL.ClearColor(0,0,0, 1f); 

          

            //GL.ClearColor(Lighting.BackgroundColor);

            GL.Enable(EnableCap.DepthTest);


            _vertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, GameData._vertices.Length * sizeof(float), GameData._vertices, BufferUsageHint.StaticDraw);

            _lightingShader = ShaderManager.GetShader("Shaders/lighting"); // shader vert lighting frag
            _lampShader = ShaderManager.GetShader("Shaders/shader");

            {
                _vaoModel = GL.GenVertexArray();
                GL.BindVertexArray(_vaoModel);

                var positionLocation = _lightingShader.GetAttribLocation("aPos");
                GL.EnableVertexAttribArray(positionLocation);
                GL.VertexAttribPointer(positionLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);

                var normalLocation = _lightingShader.GetAttribLocation("aNormal");
                GL.EnableVertexAttribArray(normalLocation);
                GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));

                var texCoordLocation = _lightingShader.GetAttribLocation("aTexCoords");
                GL.EnableVertexAttribArray(texCoordLocation);
                GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));
            }

            {
                _vaoLamp = GL.GenVertexArray();
                GL.BindVertexArray(_vaoLamp);

                var positionLocation = _lampShader.GetAttribLocation("aPos");
                GL.EnableVertexAttribArray(positionLocation);
                GL.VertexAttribPointer(positionLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
            }

            _diffuseMap = TextureManager.GetTexture("Resources/Textures/Game/container2.png");
            _specularMap = TextureManager.GetTexture("Resources/Textures/Game/container2_specular.png");

            //_camera = new Camera(Vector3.UnitZ * 3, Window.Instance.Size.X / (float)Window.Instance.Size.Y);
            player = new Player(new Vector3(5,2,5), 
                _lightingShader);


            Input.SetCursorState(CursorState.Grabbed);

          
                audio = new AudioSource(new AudioClip("Resources/Audio/flashlight.wav"));


            font = new BitmapFont("Resources/Font/arial.png", 256, 256, 16, 16);



            textRenderer = new TextRenderer(font, Window.Instance.Size.X, Window.Instance.Size.Y);

            // var texture2 = new Texture2D("Resources/Textures/tile.png");
          
            LoadModels();

            LoadLights();

            
        }
        
        private void LoadLights()
        {
            DirectionalLight sun = new DirectionalLight(_lightingShader);
           
            Renderer.AddDrawable(sun);
        }

        private void LoadModels()
        {
            Material mat = new Material(_lightingShader, new Texture2D("Resources/Textures/Game/base.png", true));


            for (int i = 0; i < 4; i++)
            {
                planes[i] = new Model("Resources/Models/plane.obj", mat);
                Renderer.AddDrawable(planes[i]);

                CollisionManager.Add(planes[i]);
            }



            planes[0].Position = new Vector3(0, 0, 3);
            planes[1].Position = new Vector3(1, 0, 3);
            planes[2].Position = new Vector3(0, 0, 4);
            planes[3].Position = new Vector3(1, 0, 4);

            //planes[0].Transform.Scale = new Vector3(5, 0, 5);
            //planes[0].Material.Tiling = new Vector2(5,5);

           

            Texture2D skyboxTexture = new Texture2D("Resources/Textures/Skybox/skybox2.png", true);
            Shader skyboxShader = ShaderManager.GetShader("Shaders/skybox");

            Material skyboxMaterial = new Material(skyboxShader, skyboxTexture);

             skybox = new Skybox("Resources/Models/domBig.obj", skyboxShader, skyboxTexture);


            Texture2D skyboxTexture2 = new Texture2D
                ("Resources/Textures/Skybox/skybox_01.jpg", false);
            Shader skyboxShader2 = ShaderManager.GetShader("Shaders/skybox");

            Material skyboxMaterial2 = new Material(skyboxShader2, skyboxTexture2);

            skyboxExtern = new Skybox("Resources/Models/domBig.obj", skyboxShader2, 
                skyboxTexture2);

            skyboxExtern.Scale = new Vector3 (120, 120, 120);

            arrow = new Model("Resources/Models/arrow.obj");
          
            arrow.Material.Color = new Vector4(0, 0, 0.5f, 1);
            arrow.Position = new Vector3(1,1,1);

            Model terrain = new Model("Resources/Models/terrain.obj", 
                new Material(_lightingShader,
                new Texture2D("Resources/Textures/Game/grass2.jpg", false)));
            terrain.Material.Tiling = new Vector2(25, 25);
          

            Model tv = new Model("Resources/Models/tv.obj", new Material(_lightingShader, new Texture2D("Resources/Textures/Game/tv.png", true)));
            tv.Position = new Vector3(2, 0.5f, 3);
            tv.Rotation = new Vector3(0, 0, 0);

            Model stone1 = new Model("Resources/Models/stone1.obj", new Material(_lightingShader, new Texture2D("Resources/Textures/Game/stone1.jpg", true)));
            stone1.Position = new Vector3(8, 0, 3);
            stone1.Rotation = new Vector3(0,45, 0);

            Model cube = new Model("Resources/Models/cube.obj", new Material(_lightingShader, new Texture2D("Resources/Textures/Game/container2.png", false)));
            cube.Position = new Vector3(8,0,8);
            //cube.Rotation = new Vector3(0,45,0);

             water = new Water(_lightingShader);

            water.Position = new Vector3(50,-4,-15);
            water.Scale = new Vector3(50,0.1f,50);

            //cube.Scale = new Vector3(10,1,10);

            //Tree tree = new Tree(new Vector3(5, 0, 5), _lightingShader);

            Renderer.AddDrawable(player);

            // renderer.AddDrawable(tree);
            Renderer.AddDrawable(stone1);
            Renderer.AddDrawable(cube);
            Renderer.AddDrawable(arrow);
            // renderer.AddDrawable(water);
            //renderer.AddDrawable(skybox);
            //renderer.AddDrawable(skybox2);
            Renderer.AddDrawable(terrain);
            Renderer.AddDrawable(tv);
            player.Name = "Player";


            //collisionManager.Add(terrain);
            
            CollisionManager.Add(stone1);
            CollisionManager.Add(cube);
            CollisionManager.Add(tv);
            CollisionManager.Add(arrow);
            CollisionManager.Add(player);

            Trigger trigger = new Trigger(new Vector3(3,1,5), new Vector3(1,2,1.5f));
            CollisionManager.Add(trigger);

            var points = terrain.Mesh.GetRandomPoints(20);

            foreach (var point in points)
            {

                Tree t = new Tree(point - new Vector3(10,0,10), _lightingShader);

                Renderer.AddDrawable(t);
                //CollisionManager.Add(t.GetModel());
            }
        }

        public override void Awake()
        {

        }
        public override void Start()
        {
            
        }

        public override void Render()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            
            
            GL.Disable(EnableCap.CullFace);

            GL.BindVertexArray(_vaoModel);

            _lightingShader.SetVector2("offset", Vector2.Zero);
            _lightingShader.SetVector2("tiling", Vector2.One);

            _diffuseMap.Use(TextureUnit.Texture0);
            //_specularMap.Use(TextureUnit.Texture1);
            _lightingShader.Use();

            _lightingShader.SetMatrix4("view", player.GetViewMatrix());
            _lightingShader.SetMatrix4("projection", player.GetProjectionMatrix());

            _lightingShader.SetVector3("viewPos", player.Position);

           
            _lightingShader.SetInt("material.diffuse", 0);
            _lightingShader.SetInt("material.specular", 1);
            _lightingShader.SetVector3("material.specular", new Vector3(0.5f, 0.5f, 0.5f));
            _lightingShader.SetFloat("material.shininess", 16.0f);

            /*
               Here we set all the uniforms for the 5/6 types of lights we have. We have to set them manually and index
               the proper PointLight struct in the array to set each uniform variable. This can be done more code-friendly
               by defining light types as classes and set their values in there, or by using a more efficient uniform approach
               by using 'Uniform buffer objects', but that is something we'll discuss in the 'Advanced GLSL' tutorial.
            */
            // Directional light

           
            // Point lights
            for (int j = 0; j < GameData._pointLightPositions.Length; j++)
            {
                _lightingShader.SetVector3($"pointLights[{j}].position", GameData._pointLightPositions[j]);
                _lightingShader.SetVector3($"pointLights[{j}].ambient", Lighting.AmbientColor);
                _lightingShader.SetVector3($"pointLights[{j}].diffuse", new Vector3(0.8f, 0.8f, 0.8f));
                _lightingShader.SetVector3($"pointLights[{j}].specular", new Vector3(1.0f, 1.0f, 1.0f));
                _lightingShader.SetFloat($"pointLights[{j}].constant", 1.0f);
                _lightingShader.SetFloat($"pointLights[{j}].linear", 0.09f);
                _lightingShader.SetFloat($"pointLights[{j}].quadratic", 0.032f);
            }

            // Spot light
           
          
            /*for (int i = 0; i < GameData._cubePositions.Length; i++)
            {
                Matrix4 model = Matrix4.CreateTranslation(GameData._cubePositions[i]);
                float angle = 20.0f * i;
                model = model * Matrix4.CreateFromAxisAngle(new Vector3(1.0f, 0.3f, 0.5f), angle);
                _lightingShader.SetMatrix4("model", model);

                GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
            }*/
            
            GL.BindVertexArray(_vaoLamp);

            _lampShader.Use();

            _lampShader.SetMatrix4("view", player.GetViewMatrix());
            _lampShader.SetMatrix4("projection", player.GetProjectionMatrix());
            // We use a loop to draw all the lights at the proper position
            for (int i = 0; i < GameData._pointLightPositions.Length; i++)
            {
                Matrix4 lampMatrix = Matrix4.CreateScale(0.2f);
                lampMatrix = lampMatrix * Matrix4.CreateTranslation(GameData._pointLightPositions[i]);

                _lampShader.SetMatrix4("model", lampMatrix);

                GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
            }




            //textRenderer.SetProjection(_camera.GetProjectionMatrix());
            //textRenderer.RenderText("ABABABABAB", 50, 50, 500, new Vector3(1, 1, 1));

            //model.Transform.Rotation -= new Vector3(0,0f,0.0f);
            // Re-enable face culling if it was enabled before
            //GL.Enable(EnableCap.CullFace);

            // textRenderer.RenderText("Pos: " + $" {(Vector3i)player.Position}", 10, 110, 2f, new Vector3(0, 0, 0));

            //Debug.ProjectionMatrix = player.GetProjectionMatrix();
            //Debug.ViewMatrix = player.GetViewMatrix();


            CollisionManager.CheckCollisions();

           

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            
            GL.DepthMask(false); // Отключаем запись в буфер глубины


            skyboxExtern.DrawTransparent(player);
           
             skybox.DrawTransparent(player);


            GL.DepthMask(true); // Включаем запись в буфер глубины
            GL.Disable(EnableCap.Blend);
            water.DrawTransparent(player);

            Renderer.RenderAll(player);


            

        }

        public override void OnGUI()
        {
           
            //textRenderer.RenderText("FPS: " + Time.FPS, 10, 50, 2f, new Vector3(1, 1, 1));
            //textRenderer.RenderText("Delta: " + Time.Delta, 10, 80, 2f, new Vector3(1, 1, 1));
            // textRenderer.RenderText("Pos: " + $" {(Vector3i)player.Position}", 10, 110, 2f, new Vector3(1, 1, 1));
            SceneObjectPanel.Render(Renderer.GetObjects());

            
        }



        public override void UnloadContent()
        {
            
            //audio.Dispose();
            
        }
        Random rnd = new Random();

        float x = 0;
        public override void Update()
        {
            if (Input.IsKeyDown(Keys.Backspace))
            {
                SceneManager.LoadScene(typeof(MenuScene));
            }

            if (Input.IsKeyDown(Keys.Tab))
            {
                SceneObjectPanel.IsVisible = !SceneObjectPanel.IsVisible;

               
                if( !SceneObjectPanel.IsVisible )
                {
                    Input.SetCursorState(CursorState.Grabbed);

                    player.CameraActive = true;
                }
                else
                {
                    Input.SetCursorState(CursorState.Normal);
                    player.CameraActive = false;
                }
                   
                
            }


            if (skybox.Rotation.Y == 360) skybox.Rotation = new Vector3(0,0,0);
            skybox.Rotation -= new Vector3(0,1f * Time.Delta,0);

           /* for (int x = 0; x < 10000; x++)
            {
                Debug.DrawPoint(new Vector3((float)rnd.NextDouble() * 10, (float)rnd.NextDouble() * 10, (float)rnd.NextDouble() * 10), 2, Color4.Red);
                Debug.DrawLine(new Vector3((float)rnd.NextDouble() * 100, (float)rnd.NextDouble() * 100, (float)rnd.NextDouble() * 100), new Vector3((float)rnd.NextDouble() * 10, (float)rnd.NextDouble() * 10, (float)rnd.NextDouble() * 10), Color4.Red);
            }*/

       

            VisualDebug.DrawLine(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(100f, 0, 0), Color4.Red);
            VisualDebug.DrawLine(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0, 100, 0), Color4.Green);
            VisualDebug.DrawLine(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0, 0,100), Color4.Blue);
   
            player.Update();

            // Обновление всех объектов и их коллизий
            foreach (var obj in CollisionManager.Collidables)
            {
                // Сохраняем старый BoundingVolume для обновления
                BoundingVolume oldVolume = obj.BoundingVolume.Clone();

                // Обновляем объект (перемещение, вращение и т.д.)

                if (!obj.IsStatic)
                obj.UpdateBounding();

                // Обновляем CollisionManager с новым BoundingVolume
                CollisionManager.Update(obj, oldVolume);
            }

            if (Input.IsKeyDown(Keys.F))
            {
                //spotLight.IsActive = !spotLight.IsActive;
                audio.Play();
            }

         
        }
    }
}
