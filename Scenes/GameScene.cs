using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Entities;


namespace Spacebox.Scenes
{
    internal class GameScene : Scene
    {
        private int _vertexBufferObject;

        private int _vaoModel;

        private int _vaoLamp;

        private Shader _lampShader;

        private Shader _lightingShader;

        private Texture _diffuseMap;

        private Texture _specularMap;
        private List<Light> _lights;

        private Player player;
  private CollisionManager collisionManager;

        private bool flashLight = true;

        AudioSource audio;

        AudioManager audioManager;


        BitmapFont font;
   
        TextRenderer textRenderer;

        Model[] planes = new Model[4];
        Renderer renderer = new Renderer();

        Model arrow;

        public GameScene()
        {
        }

        public override void LoadContent()
        {


            GL.ClearColor(0.400f, 0.339f, 0.216f, 1f); 

          

            //GL.ClearColor(Lighting.BackgroundColor);

            GL.Enable(EnableCap.DepthTest);


            _vertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, GameData._vertices.Length * sizeof(float), GameData._vertices, BufferUsageHint.StaticDraw);

            _lightingShader = new Shader("Shaders/lighting"); // shader vert lighting frag
            _lampShader = new Shader("Shaders/shader");

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

            _diffuseMap = Texture.LoadFromFile("Resources/Textures/container2.png");
            _specularMap = Texture.LoadFromFile("Resources/Textures/container2_specular.png");

            //_camera = new Camera(Vector3.UnitZ * 3, Window.Instance.Size.X / (float)Window.Instance.Size.Y);
            player = new Player(new Vector3(5,1,5), Window.Instance.Size.X / (float)Window.Instance.Size.Y);


            Input.SetCursorState(CursorState.Grabbed);

            audioManager = new AudioManager();
            audio = new AudioSource("Resources/Audio/flashlight.wav", audioManager.Device, audioManager.Context);


            font = new BitmapFont("Resources/Font/arial.png", 256, 256, 16, 16);



            textRenderer = new TextRenderer(font, Window.Instance.Size.X, Window.Instance.Size.Y);

            // var texture2 = new Texture2D("Resources/Textures/tile.png");
            collisionManager = new CollisionManager(10);
            LoadModels();

            


        }

        private void LoadModels()
        {
            Material mat = new Material(_lightingShader, new Texture2D("Resources/Textures/base.png", true));


            for (int i = 0; i < 4; i++)
            {
                planes[i] = new Model("Resources/Models/plane.obj", mat);
                renderer.AddDrawable(planes[i]);

                
            }



            planes[0].Transform.Position = new Vector3(0, 0, 3);
            planes[1].Transform.Position = new Vector3(1, 0, 3);
            planes[2].Transform.Position = new Vector3(0, 0, 4);
            planes[3].Transform.Position = new Vector3(1, 0, 4);

            //planes[0].Transform.Scale = new Vector3(5, 0, 5);
            //planes[0].Material.Tiling = new Vector2(5,5);

           

            Texture2D skyboxTexture = new Texture2D("Resources/Textures/dom.png", true);
            Shader skyboxShader = new Shader("Shaders/skybox");

            Material skyboxMaterial = new Material(skyboxShader, skyboxTexture);

            Skybox skybox = new Skybox("Resources/Models/dom.obj", skyboxShader, skyboxTexture);


             arrow = new Model("Resources/Models/arrow.obj");
            arrow.Material.Color = new Vector4(0, 1, 0, 1);
            arrow.Transform.Position = new Vector3(1,1,1);

            Model terrain = new Model("Resources/Models/terrain.obj", new Material(_lightingShader, new Texture2D("Resources/Textures/grass1.jpg", false)));
            terrain.Material.Tiling = new Vector2(50, 50);

            Model tv = new Model("Resources/Models/tv.obj", new Material(_lightingShader, new Texture2D("Resources/Textures/tv.png", true)));
            tv.Transform.Position = new Vector3(2, 0.5f, 3);
            tv.Transform.Rotation = new Vector3(0, 45, 0);

            renderer.AddDrawable(arrow);
            renderer.AddDrawable(skybox);
            renderer.AddDrawable(terrain);
            renderer.AddDrawable(tv);
            player.Transform.Name = "Player";
            //collisionManager.Add(terrain);
            collisionManager.Add(tv.Collision);
            collisionManager.Add(arrow.Collision);
            collisionManager.Add(player.Collision);

            Console.WriteLine(arrow.Collision.BoundingVolume.ToString());
            Console.WriteLine(arrow.Mesh.GetBounds().ToString());
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

            
            Debug.Render();
            GL.Disable(EnableCap.CullFace);

            GL.BindVertexArray(_vaoModel);

            _lightingShader.SetVector2("offset", Vector2.Zero);
            _lightingShader.SetVector2("tiling", Vector2.One);

            _diffuseMap.Use(TextureUnit.Texture0);
            //_specularMap.Use(TextureUnit.Texture1);
            _lightingShader.Use();

            _lightingShader.SetMatrix4("view", player.Camera.GetViewMatrix());
            _lightingShader.SetMatrix4("projection", player.Camera.GetProjectionMatrix());

            _lightingShader.SetVector3("viewPos", player.Camera.Position);

           
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
            _lightingShader.SetVector3("dirLight.direction", new Vector3(-0.2f, -1.0f, -0.3f));
            _lightingShader.SetVector3("dirLight.ambient", Lighting.AmbientColor); // new Vector3(0.05f, 0.05f, 0.05f)
            _lightingShader.SetVector3("dirLight.diffuse", new Vector3(0.4f, 0.4f, 0.4f));
            _lightingShader.SetVector3("dirLight.specular", new Vector3(0.5f, 0.5f, 0.5f));

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
            _lightingShader.SetVector3("spotLight.position", player.Camera.Position);
            _lightingShader.SetVector3("spotLight.direction", player.Camera.Front);

            if (flashLight)
            {
                _lightingShader.SetVector3("spotLight.ambient", new Vector3(0.0f, 0.0f, 0.0f));
                _lightingShader.SetVector3("spotLight.diffuse", new Vector3(0f, 0f, 0f));
                _lightingShader.SetVector3("spotLight.specular", new Vector3(0f, 0f, 0f));

            }
            else
            {
                _lightingShader.SetVector3("spotLight.ambient", new Vector3(0.0f, 0.0f, 0.0f));
                _lightingShader.SetVector3("spotLight.diffuse", new Vector3(1.0f, 1.0f, 1.0f));
                _lightingShader.SetVector3("spotLight.specular", new Vector3(1.0f, 1.0f, 1.0f));


            }


            _lightingShader.SetFloat("spotLight.constant", 1.0f);
            _lightingShader.SetFloat("spotLight.linear", 0.09f);
            _lightingShader.SetFloat("spotLight.quadratic", 0.032f);
            _lightingShader.SetFloat("spotLight.cutOff", MathF.Cos(MathHelper.DegreesToRadians(12.5f)));
            _lightingShader.SetFloat("spotLight.outerCutOff", MathF.Cos(MathHelper.DegreesToRadians(17.5f)));

            for (int i = 0; i < GameData._cubePositions.Length; i++)
            {
                Matrix4 model = Matrix4.CreateTranslation(GameData._cubePositions[i]);
                float angle = 20.0f * i;
                model = model * Matrix4.CreateFromAxisAngle(new Vector3(1.0f, 0.3f, 0.5f), angle);
                _lightingShader.SetMatrix4("model", model);

                GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
            }

            GL.BindVertexArray(_vaoLamp);

            _lampShader.Use();

            _lampShader.SetMatrix4("view", player.Camera.GetViewMatrix());
            _lampShader.SetMatrix4("projection", player.Camera.GetProjectionMatrix());
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
           
            renderer.RenderAll(player.Camera); //model.Transform.Rotation -= new Vector3(0,0f,0.0f);
            // Re-enable face culling if it was enabled before
            //GL.Enable(EnableCap.CullFace);
            textRenderer.RenderText("FPS: " + Time.FPS, 10, 50, 1f, new Vector3(252 / 256f, 186 / 256f, 3 / 256f));


            collisionManager.CheckCollisions();

            SceneManager.Instance.GameWindow.SwapBuffers();
        }

       

        public override void UnloadContent()
        {
            audioManager.Dispose();
            audio.Dispose();
        }
        Random rnd = new Random();

        public override void Update()
        {
            if (Input.IsKeyDown(Keys.Enter))
            {
                SceneManager.LoadScene(typeof(MenuScene));
            }

            
            Debug.ProjectionMatrix = player.Camera.GetProjectionMatrix();
            Debug.ViewMatrix = player.Camera.GetViewMatrix();

           

           /* for (int x = 0; x < 10000; x++)
            {
                Debug.DrawPoint(new Vector3((float)rnd.NextDouble() * 10, (float)rnd.NextDouble() * 10, (float)rnd.NextDouble() * 10), 2, Color4.Red);
                Debug.DrawLine(new Vector3((float)rnd.NextDouble() * 100, (float)rnd.NextDouble() * 100, (float)rnd.NextDouble() * 100), new Vector3((float)rnd.NextDouble() * 10, (float)rnd.NextDouble() * 10, (float)rnd.NextDouble() * 10), Color4.Red);
            }*/

       

            Debug.DrawLine(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(100f, 0, 0), Color4.Red);
            Debug.DrawLine(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0, 100, 0), Color4.Green);
            Debug.DrawLine(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0, 0,100), Color4.Blue);
   
            player.Update();

            // Обновление всех объектов и их коллизий
            foreach (var obj in collisionManager.Collidables)
            {
                // Сохраняем старый BoundingVolume для обновления
                BoundingVolume oldVolume = obj.BoundingVolume;

                // Обновляем объект (перемещение, вращение и т.д.)

                obj.UpdateBounding();

                // Обновляем CollisionManager с новым BoundingVolume
                collisionManager.Update(obj, oldVolume);
            }

            if (Input.IsKeyDown(Keys.F))
            {
                flashLight = !flashLight;
                audio.Play();
            }

         
        }
    }
}
