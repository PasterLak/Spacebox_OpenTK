using Engine;
using Engine.Audio;
using Engine.Components;
using Engine.Light;
using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game.Effects;
using Spacebox.Game.GUI;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Player.Interactions;
using Spacebox.GUI;
using SpaceNetwork;


namespace Spacebox.Game.Player
{
    public class Astronaut : Camera360Base
    {

        public Inventory Inventory { get; private set; }
        public Storage Panel { get; private set; }

        private Axes _axes;

        public bool CameraActive = true;

        public Action<Astronaut> OnMoved { get; set; }

        private bool _canMove = true;

        public bool CanMove
        {
            get => _canMove;

        }
        private bool _isAlive = true;
        public bool IsAlive
        {
            get => _isAlive;
            private set
            {
                _isAlive = value;
                _canMove = _isAlive;
            }
        }
        public Action OnDeath { get; set; }
        public InertiaController InertiaController { get; private set; } = new InertiaController();
        public CameraSway CameraSway { get; private set; } = new CameraSway();
        public Flashlight Flashlight { get; private set; }

        public HitImage HitImage { get; private set; }
        public DeathScreen DeathScreen { get; private set; } = new DeathScreen();
        public HealthBar HealthBar { get; private set; }
        public Mood Mood { get; private set; }
        public PowerBar PowerBar { get; private set; }
        private GameModeBase _gameModeBase;
        private GameMode _gameMode => _gameModeBase.GetGameMode();
        public InteractionMode CurrentInteraction => _gameModeBase.InteractionHandler.Interaction;
        public PlayerStatistics PlayerStatistics { get; set; } = new PlayerStatistics();
        public PlayerEffects Effects { get; private set; } = new PlayerEffects();
        public Vector3 SpawnPosition { get; set; }
        public Vector3 LastStatPosition { get; private set; }

        public void SetPosition(Vector3 position)
        {
            LastStatPosition = position;
            Position = position;
        }

        public GameMode GameMode
        {
            get { return _gameMode; }
            set { SetGameMode(value); }
        }

        private bool _collisionEnabled = true;

        public bool CollisionEnabled
        {
            get => _collisionEnabled;
            set => _collisionEnabled = value;
        }

        public PointLight ItemLight { get; private set; }

        private Toggi toggle;
        ItemWorldModel itemInHand;
        public Astronaut(Vector3 position)
            : base(position)
        {
            FOV = Settings.Graphics.Fov;
            SpawnPosition = position;
            LastStatPosition = position;
            Name = "Player";
            DepthNear = 0.01f;
            DepthFar = Settings.ViewDistance;
            base.Name = Name;
            Layer = CollisionLayer.Player;

            HealthBar = new HealthBar();
            PowerBar = new PowerBar();
          
            Mood = AttachComponent(new Mood(this));
            SetData();
            SetRenderSpace(true);
            HitImage = new HitImage();
            Flashlight = new Flashlight(this);

            AddChild(Flashlight);
            Flashlight.Position = new Vector3(0, 0, 0f); // -0.3
            Flashlight.Rotation = Vector3.Zero;

            ItemLight = new PointLight();
            ItemLight.Diffuse = new Vector3(0.2f, 1, 0.2f);
            ItemLight.Specular = new Vector3(0f);
            ItemLight.Intensity = 1;
            ItemLight.Range = 5f;
            ItemLight.Position = new Vector3(0, 0, 0);
            ItemLight.Enabled = false;

            AddChild(ItemLight);

            CreateModel(0);
            GameMode = GameMode.Creative;

            toggle = ToggleManager.Register("player");
            toggle.OnStateChanged += state =>
            {
                if (IsAlive)
                    _canMove = state;

            };

            itemInHand = AddChild(new ItemWorldModel("Resources/Textures/Old/drill6.png", 0.1f));
            itemInHand.Rotate(new Vector3(10, 90, -10));
            itemInHand.SetScale(0.6f);
            itemInHand.Position = new Vector3(0.35f, -0.47f, -0.25f);

            Flashlight.CutOff = 20;
            Flashlight.OuterCutOff = 35;


            /*var point = AddChild(new PointLight());
            point.Position = new Vector3(0, 0, -0.5f);
            point.Diffuse = new Vector3(56, 204, 209);
            point.Intensity = 0.25f;
            point.Range = 5;*/

            var damageTexture = Resources.Load<Texture2D>("Resources/Textures/damageEffect.png");
            damageTexture.FilterMode = FilterMode.Nearest;


            AddChild(Effects);

            var audioListener = AttachComponent(new AudioListener());
            audioListener.DistanceModel = DistanceModel.ExponentDistanceClamped;
            audioListener.DopplerFactor = 3;
            audioListener.SpeedOfSound = 1000f;
            audioListener.Gain = 0.9f;

            OnMoved += (a) =>
            {
                audioListener.Velocity = InertiaController.Velocity;

                timeToSavePosToStat -= Time.Delta;

                if (timeToSavePosToStat < 0)
                {
                    timeToSavePosToStat = 5;
                    if (GameMode != null && GameMode != GameMode.Spectator)
                        PlayerStatistics.UpdateDistance(LastStatPosition, Position);

                    LastStatPosition = Position;
                }


            };

            HealthBar.StatsData.OnIncrement += (v) =>
                {
                    PlayerStatistics.HealthHealed += v;
                };

            DeathScreen.OnRespawn += Revive;

            Flashlight.OnEnabledChanged += (b) => { PanelUI.SetFlashlight(this); };

        }

        float timeToSavePosToStat = 5;

        ModelRendererComponent spot;
        private void SetData()
        {

            Inventory = new Inventory(8, 3);
            Panel = new Storage(1, 10);
            BoundingVolume = new BoundingSphere(Position, 0.4f);
            Inventory.Name = "Inventory";
            Panel.Name = "Panel";

            Panel.ConnectStorage(Inventory, true);
            Inventory.ConnectStorage(Panel);

            _axes = new Axes(Position, 0.01f);

            
            /* node = new Node3D();

            var mat = new SpotMaterial(Flashlight);
           // mat.RenderMode = RenderMode.Transparent;
            mat.RenderFace = RenderFace.Both;

            Model model = new Model(GenMesh.CreateCone(30, 1, Vector3.UnitZ, Vector3.Zero, 24), mat);
            spot = node.AttachComponent(new ModelRendererComponent(model));
            node.Rotate(0,180,0);
            node.AttachComponent(new SphereCollider());
        
            AddChild(node);*/


        }
        Node3D node;
        private void SetGameMode(GameMode mode)
        {
            if (_gameModeBase != null)
                _gameModeBase.OnDisable();

            switch (mode)
            {
                case GameMode.Survival:

                    _gameModeBase = new SurvivalMode(this);
                    break;
                case GameMode.Creative:
                    _gameModeBase = new CreativeMode(this);
                    break;
                default:
                    _gameModeBase = new SpectatorMode(this);
                    break;
            }

            _gameModeBase.OnEnable();
            PanelUI.ResetLastSelected();
            PanelUI.SetSelectedSlot(0);
        }

        public void SetItemLight(ItemSlot? slot)
        {
            var item = slot.Item;
            if (slot != null && item != null && slot.Count > 0 && item.IsLuminous)
            {
                ItemLight.Diffuse = item.Color.ToVector3();
                ItemLight.Enabled = true;
            }
            else
            {
                ItemLight.Enabled = false;
            }
        }


        private static Dictionary<string, Texture2D> astronautTextures = new Dictionary<string, Texture2D>();
        private static Texture2D GetAstronautTexture(string color)
        {
            if (!astronautTextures.TryGetValue(color, out Texture2D tex))
            {
                string texturePath = $"Resources/Textures/Skins/Astronaut_{color}.jpg";
                tex = Resources.Load<Texture2D>(texturePath);
                tex.FlipY();
                tex.FilterMode = FilterMode.Nearest;

                astronautTextures[color] = tex;
            }
            return tex;
        }
        private ModelRendererComponent astBody;
        private ModelRendererComponent astHelmet;
        private ModelRendererComponent astTank;
        private void CreateModel(int id)
        {
            var colors = new[] { "Yellow", "Orange", "Purple", "Blue", "Green", "Cyan", "Red", "White", "Black" };
            int index = Math.Abs(id) % colors.Length;
            var selectedColor = colors[index];
            Texture2D tex = GetAstronautTexture(selectedColor);
            var mat = new TextureMaterial(tex);

            var mesh1 = Resources.Load<Engine.Mesh>("Resources/Models/Player/Astronaut_Body_Fly.obj");
            var mesh2 = Resources.Load<Engine.Mesh>("Resources/Models/Player/Astronaut_Helmet_Closed.obj");
            var mesh3 = Resources.Load<Engine.Mesh>("Resources/Models/Player/Astronaut_Tank_Fly.obj");

            astBody = AttachComponent(new ModelRendererComponent(new Model(mesh1, mat)));
            astHelmet = AttachComponent(new ModelRendererComponent(new Model(mesh2, mat)));
            astTank = AttachComponent(new ModelRendererComponent(new Model(mesh3, mat)));
        }


        public override void Update()
        {
            astBody.Enabled = !IsMain;
            astHelmet.Enabled = !IsMain;
            astTank.Enabled = !IsMain;

            itemInHand.Enabled = !IsMain;


            base.Update();


            Matrix4 viewMatrix = GetViewMatrix();
            Matrix4 projectionMatrix = GetProjectionMatrix();
            Frustum.UpdateFrustum(this);

            VisualDebug.ProjectionMatrix = projectionMatrix;
            VisualDebug.ViewMatrix = viewMatrix;

            if (VisualDebug.Enabled)
            {
                VisualDebug.ProjectionMatrix = GetProjectionMatrix();
                VisualDebug.ViewMatrix = GetViewMatrix();
            }

            _gameModeBase.UpdateInteraction(this);
            _gameModeBase.Update(this);

            HitImage.Update();
            DeathScreen.Update();

            if (!IsMain) return;
            _gameModeBase.HandleInput(this);


            if (!CanMove) return;

            if (Input.IsAction("zoom"))
            {
                if (PanelUI.IsHolding<CreativeToolItem>()) return;

                if (FOV != 50)
                    FOV = 50;
            }
            if (Input.IsActionUp("zoom"))
            {
                FOV = 90;
              
            }

#if DEBUG
            if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.R))
            {
               
            }

            if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.U))
            {
                CollisionEnabled = !CollisionEnabled;
                Debug.Log($"Collision Enabled: {CollisionEnabled}");
            }

#endif

            HandleMouse();
            UpdateBounding();

            base.Update();
        }

        public void EnableCameraSway(bool enabled)
        {
            CameraSway.EnableSway(enabled);
        }

        public override Matrix4 GetViewMatrix()
        {
            Quaternion sway = CameraSway.GetSwayRotation();
            Quaternion combinedRotation = GetRotation() * sway;
            Vector3 newFront = Vector3.Transform(-Vector3.UnitZ, combinedRotation);
            Vector3 newUp = Vector3.Transform(Vector3.UnitY, combinedRotation);

            Rotation = QuaternionToEulerDegrees(GetRotation());

            if (CameraRelativeRender)
            {
                return Matrix4.LookAt(Vector3.Zero, newFront, newUp);
            }

            return Matrix4.LookAt(Position, Position + newFront, newUp);
        }

        public static Vector3 QuaternionToEulerDegrees(Quaternion q)
        {

            Vector3 eulerRad = q.ToEulerAngles();

            float xDegrees = eulerRad.X * (180f / (float)Math.PI);
            float yDegrees = eulerRad.Y * (180f / (float)Math.PI);
            float zDegrees = eulerRad.Z * (180f / (float)Math.PI);

            return new Vector3(xDegrees, yDegrees, zDegrees);
        }


        private void HandleMouse()
        {
            if (CameraActive)
            {
                Rotate();
            }
        }

        public void Teleport(Vector3 position)
        {
            Position = position;
            LastStatPosition = Position;
            InertiaController.Reset();
            Effects.PlayEffect(PlayerEffectType.Teleport);
            PlayerStatistics.TeleportsUsed++;
            Mood.RemoveMood(5);
            OnMoved?.Invoke(this);
        }

        public void TakeDamage(int damage, DeathCase? deathCase = null)
        {


            var health = HealthBar.StatsData;
            damage = Math.Min(damage, health.MaxValue);
            PlayerStatistics.DamageTaken += damage;
            health.Decrement(damage);
            Mood.AddMood(1);

            if (damage > 12)
                Effects.PlayEffect(PlayerEffectType.Damage);
            if (health.Value > 0)
            {
                ColorOverlay.FadeOut(new System.Numerics.Vector3(1, 0, 0), 0.1f + 1 / (10 - Math.Min(damage, 6)));
                HitImage.Show();
            }
            else
            {
                Death(deathCase);

            }
        }

        private void Death(DeathCase? deathCase = null)
        {
            if (!IsAlive) return;
            PlayerStatistics.DeathsTotal++;
          
            IsAlive = false;
            InertiaController.Reset();
            FOV = 110;
            HitImage.Hide();
            Settings.ShowInterface = false;
            PanelUI.IsVisible = false;
            PanelUI.IsItemModelVisible = false;
            Input.ShowCursor();
            Mood.AddMoodRandom(1,10);
            Flashlight.Enabled = false;
            HealthBar.StatsGUI.IsVisible = false;
            PowerBar.StatsGUI.IsVisible = false;
        
            if (deathCase != null)
                DeathScreen.Show(deathCase);

            else

                DeathScreen.Show(new DeathCase(""));
            OnDeath?.Invoke();
        }

        public void Revive()
        {
            IsAlive = true;
          
            FOV = Settings.Graphics.Fov;
            Settings.ShowInterface = true;
            var health = HealthBar.StatsData;
            var power = PowerBar.StatsData;
            health.Value = health.MaxValue;
            power.Value = power.MaxValue;
            PanelUI.IsVisible = true;
            PanelUI.IsItemModelVisible = true;
            Input.HideCursor();
            Flashlight.Enabled = true;
            //PanelUI.ResetLastSelected();
            HealthBar.StatsGUI.IsVisible = true;
            PowerBar.StatsGUI.IsVisible = true;
            SetPosition( SpawnPosition);
            Effects.PlayEffect(PlayerEffectType.Heal);

        }

        public void SetInteraction(InteractionMode mode)
        {
            if (_gameModeBase == null) return;

            _gameModeBase.SetInteraction(mode);
        }

        public override void Render()
        {

            base.Render();

            if (VisualDebug.Enabled)
            {
                _axes.Position = Position + Front * 0.1f;
                _axes.Render();
            }

            if (_gameModeBase != null)
            {
                _gameModeBase.Render(this);
            }

        }

        public override void OnGUI()
        {
            base.OnGUI();

            PowerBar.OnGUI();
            HealthBar.OnGUI();
            HitImage.Draw();

            DeathScreen.Render();
        }
    }
}