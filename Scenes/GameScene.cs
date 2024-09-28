using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;


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

        private Camera _camera;

        private bool _firstMove = true;

        private Vector2 _lastPos;

        private Axes _axes;
        private Axes _axes2;
        private Skybox _skybox;

        private bool flashLight = true;

        AudioSource audio;

        AudioManager audioManager;


        BitmapFont font;
   
        TextRenderer textRenderer;

        public GameScene()
        {
        }

        public override void LoadContent()
        {


            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            _axes = new Axes(new Vector3(0, 0, 0), 1000);
            _axes2 = new Axes(new Vector3(1, 1, 1), 1);


            GL.ClearColor(Lighting.BackgroundColor);

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

            _diffuseMap = Texture.LoadFromFile("Resources/Textures/block.png");
            _specularMap = Texture.LoadFromFile("Resources/Textures/container2_specular.png");

            _camera = new Camera(Vector3.UnitZ * 3, Window.Instance.Size.X / (float)Window.Instance.Size.Y);
            _skybox = new Skybox(_camera, 2, "Resources/Textures/container2.png");

            Input.SetCursorState(CursorState.Grabbed);

            audioManager = new AudioManager();
            audio = new AudioSource("Resources/Audio/flashlight.wav", audioManager.Device, audioManager.Context);


            font = new BitmapFont("Resources/Font/arial.png", 256, 256, 16, 16);



            textRenderer = new TextRenderer(font, Window.Instance.Size.X, Window.Instance.Size.Y);


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

            // _skybox.Render();
            // _axes.Render(_camera.GetViewMatrix(), _camera.GetProjectionMatrix());
            //_axes2.Render(_camera.GetViewMatrix(), _camera.GetProjectionMatrix());
            
            Debug.Render();
           

            GL.BindVertexArray(_vaoModel);

            _diffuseMap.Use(TextureUnit.Texture0);
            _specularMap.Use(TextureUnit.Texture1);
            _lightingShader.Use();

            _lightingShader.SetMatrix4("view", _camera.GetViewMatrix());
            _lightingShader.SetMatrix4("projection", _camera.GetProjectionMatrix());

            _lightingShader.SetVector3("viewPos", _camera.Position);

            _lightingShader.SetInt("material.diffuse", 0);
            _lightingShader.SetInt("material.specular", 1);
            _lightingShader.SetVector3("material.specular", new Vector3(0.5f, 0.5f, 0.5f));
            _lightingShader.SetFloat("material.shininess", 32.0f);

            /*
               Here we set all the uniforms for the 5/6 types of lights we have. We have to set them manually and index
               the proper PointLight struct in the array to set each uniform variable. This can be done more code-friendly
               by defining light types as classes and set their values in there, or by using a more efficient uniform approach
               by using 'Uniform buffer objects', but that is something we'll discuss in the 'Advanced GLSL' tutorial.
            */
            // Directional light
            _lightingShader.SetVector3("dirLight.direction", new Vector3(-0.2f, -1.0f, -0.3f));
            _lightingShader.SetVector3("dirLight.ambient", new Vector3(0.05f, 0.05f, 0.05f));
            _lightingShader.SetVector3("dirLight.diffuse", new Vector3(0.4f, 0.4f, 0.4f));
            _lightingShader.SetVector3("dirLight.specular", new Vector3(0.5f, 0.5f, 0.5f));

            // Point lights
            for (int i = 0; i < GameData._pointLightPositions.Length; i++)
            {
                _lightingShader.SetVector3($"pointLights[{i}].position", GameData._pointLightPositions[i]);
                _lightingShader.SetVector3($"pointLights[{i}].ambient", new Vector3(0.05f, 0.05f, 0.05f));
                _lightingShader.SetVector3($"pointLights[{i}].diffuse", new Vector3(0.8f, 0.8f, 0.8f));
                _lightingShader.SetVector3($"pointLights[{i}].specular", new Vector3(1.0f, 1.0f, 1.0f));
                _lightingShader.SetFloat($"pointLights[{i}].constant", 1.0f);
                _lightingShader.SetFloat($"pointLights[{i}].linear", 0.09f);
                _lightingShader.SetFloat($"pointLights[{i}].quadratic", 0.032f);
            }

            // Spot light
            _lightingShader.SetVector3("spotLight.position", _camera.Position);
            _lightingShader.SetVector3("spotLight.direction", _camera.Front);

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

            _lampShader.SetMatrix4("view", _camera.GetViewMatrix());
            _lampShader.SetMatrix4("projection", _camera.GetProjectionMatrix());
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


            // Re-enable face culling if it was enabled before
            //GL.Enable(EnableCap.CullFace);
            textRenderer.RenderText("FPS: " + Time.FPS, 10, 50, 1f, new Vector3(252 / 256f, 186 / 256f, 3 / 256f));

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

            const float cameraSpeed = 1.5f;
            const float sensitivity = 0.2f;

            Debug.ProjectionMatrix = _camera.GetProjectionMatrix();
            Debug.ViewMatrix = _camera.GetViewMatrix();

            Debug.SetLineWidth(50);
            // Add debug shapes
            Debug.DrawPoint(new Vector3(0.3f, 3.0f, 0.0f),1000, Color4.Red);


           /* for (int x = 0; x < 10000; x++)
            {
                Debug.DrawPoint(new Vector3((float)rnd.NextDouble() * 10, (float)rnd.NextDouble() * 10, (float)rnd.NextDouble() * 10), 2, Color4.Red);
                Debug.DrawLine(new Vector3((float)rnd.NextDouble() * 100, (float)rnd.NextDouble() * 100, (float)rnd.NextDouble() * 100), new Vector3((float)rnd.NextDouble() * 10, (float)rnd.NextDouble() * 10, (float)rnd.NextDouble() * 10), Color4.Red);
            }*/

            Debug.DrawLine(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(-1.0f,- 1.0f,-1.0f), Color4.Purple);
            Debug.DrawSquare(new Vector3(0.0f, 0.0f, 0.0f), new Vector2(1.0f, 1.0f), Color4.Purple);


            Debug.DrawLine(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(10f, 0, 0), Color4.Red);
            Debug.DrawLine(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0, 10, 0), Color4.Green);
            Debug.DrawLine(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0, 0,10), Color4.Blue);
            // Render your main scene here

            // Render debug shapes


            if (Input.IsKey(Keys.W))
            {
                _camera.Position += _camera.Front * cameraSpeed * (float)Time.Delta; // Forward
            }
            if (Input.IsKey(Keys.S))
            {
                _camera.Position -= _camera.Front * cameraSpeed * (float)Time.Delta; // Backwards
            }
            if (Input.IsKey(Keys.A))
            {
                _camera.Position -= _camera.Right * cameraSpeed * (float)Time.Delta; // Left
            }
            if (Input.IsKey(Keys.D))
            {
                _camera.Position += _camera.Right * cameraSpeed * (float)Time.Delta; // Right
            }
            if (Input.IsKey(Keys.Space))
            {
                _camera.Position += _camera.Up * cameraSpeed * (float)Time.Delta; // Up
            }
            if (Input.IsKey(Keys.LeftShift))
            {
                _camera.Position -= _camera.Up * cameraSpeed * (float)Time.Delta; // Down
            }

            if (Input.IsKeyDown(Keys.F))
            {
                flashLight = !flashLight;
                audio.Play();
            }

            var mouse = Input.MouseState;

            if (_firstMove)
            {
                _lastPos = new Vector2(mouse.X, mouse.Y);
                _firstMove = false;
            }
            else
            {
                var deltaX = mouse.X - _lastPos.X;
                var deltaY = mouse.Y - _lastPos.Y;
                _lastPos = new Vector2(mouse.X, mouse.Y);

                _camera.Yaw += deltaX * sensitivity;
                _camera.Pitch -= deltaY * sensitivity;
            }
        }
    }
}
