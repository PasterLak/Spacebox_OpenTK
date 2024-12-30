using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Common.Audio;
using Spacebox.Common.Physics;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.GUI;
using Spacebox.Scenes;

namespace Spacebox.Game.Player
{
    public class Astronaut : Camera360, INotTransparent
    {
        public string Name { get; private set; } = "Player";
        public Inventory Inventory { get; private set; }
        public Storage Panel { get; private set; }

        private float _cameraSpeed = 3.5f;
        private float _shiftSpeed = 7.5f;

        private Axes _axes;

        public void SetCameraSpeed(float speed, float shiftSpeed)
        {
            _cameraSpeed = speed;
            _shiftSpeed = shiftSpeed;
        }

        private bool _firstMove = true;
        private Vector2 _lastMousePosition;

        public bool CameraActive = true;

        public Action<Astronaut> OnMoved { get; set; }

        private bool _canMove = true;

        public bool CanMove
        {
            get => _canMove;
            set
            {
                _canMove = value;
                _lastMousePosition = Input.Mouse.Position;
            }
        }

        public InertiaController InertiaController { get; private set; } = new InertiaController();
        public CameraSway CameraSway { get; private set; } = new CameraSway();
        public Flashlight Flashlight { get; private set; }

        public HitImage HitImage { get; private set; }
        private AudioSource wallhitAudio;
        private AudioSource wallhitAudio2;
        private AudioSource useConsumableAudio;
        private AudioSource flySpeedUpAudio;
        private float speedUpPower = 0;
        public HealthBar HealthBar { get; private set; }
        public PowerBar PowerBar { get; private set; }
        private GameModeBase _gameModeBase;
        private GameMode _gameMode = GameMode.Spectator;

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

        public Astronaut(Vector3 position)
            : base(position)
        {
            FOV = MathHelper.DegreesToRadians(90);
            base.Name = Name;
            Layer = CollisionLayer.Player;
            VisualDebug.RemoveCollisionToDraw(this);
            HealthBar = new HealthBar();
            PowerBar = new PowerBar();

            SetData();
            CameraRelativeRender = true;
            HitImage = new HitImage();
            Flashlight = new Flashlight(this);

            wallhitAudio = new AudioSource(SoundManager.GetClip("wallhit"));
            wallhitAudio2 = new AudioSource(SoundManager.GetClip("wallHit2"));
            flySpeedUpAudio = new AudioSource(SoundManager.GetClip("flySpeedUp"));
            flySpeedUpAudio.IsLooped = true;

            GameMode = GameMode.Creative;
        }

        ~Astronaut()
        {
            Debug.OnVisibilityWasChanged -= OnGameConsole;
        }

        private void SetData()
        {
            Debug.OnVisibilityWasChanged += OnGameConsole;

            Inventory = new Inventory(8, 4);
            Panel = new Storage(1, 10);
            BoundingVolume = new BoundingSphere(Position, 0.4f);
            Inventory.Name = "Inventory";
            Panel.Name = "Panel";

            Panel.ConnectStorage(Inventory, true);
            Inventory.ConnectStorage(Panel);

            _axes = new Axes(Position, 0.01f);
        }

        private void OnGameConsole(bool state)
        {
            CanMove = !state;
            _lastMousePosition = Input.Mouse.Position;
        }

        private void SetGameMode(GameMode mode)
        {
            _gameMode = mode;
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
        }

        public override void Update()
        {
            Matrix4 viewMatrix = GetViewMatrix();
            Matrix4 projectionMatrix = GetProjectionMatrix();
            Frustum.UpdateFrustum(viewMatrix, projectionMatrix);

            VisualDebug.ProjectionMatrix = projectionMatrix;
            VisualDebug.ViewMatrix = viewMatrix;

            if (VisualDebug.ShowDebug)
            {
                VisualDebug.ProjectionMatrix = GetProjectionMatrix();
                VisualDebug.ViewMatrix = GetViewMatrix();
            }

            _gameModeBase.Update(this);
            HitImage.Update();
            if (!CanMove) return;

            if (Input.IsKeyDown(Keys.F5))
            {
                VisualDebug.ShowPlayerCollision = !VisualDebug.ShowPlayerCollision;

                if (VisualDebug.ShowPlayerCollision)
                {
                    VisualDebug.AddCollisionToDraw(this);
                }
                else
                {
                    VisualDebug.RemoveCollisionToDraw(this);
                }
            }

            if (Input.IsKeyDown(Keys.P))
            {
                Console.WriteLine("Saving Player");
                PlayerSaveLoadManager.SavePlayer(this, World.Instance.WorldData.WorldFolderPath);
            }

            if (Input.IsKeyDown(Keys.R))
            {
                CameraRelativeRender = !CameraRelativeRender;
            }

            if (Input.IsKeyDown(Keys.U))
            {
                CollisionEnabled = !CollisionEnabled;
                Debug.Log($"Collision Enabled: {CollisionEnabled}");
            }

            HandleInput();
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

            if (CameraRelativeRender)
            {
                return Matrix4.LookAt(Vector3.Zero, newFront, newUp);
            }

            return Matrix4.LookAt(Position, Position + newFront, newUp);
        }

        private void HandleInput()
        {
            _gameModeBase.HandleInput(this);

            var mouse = Input.Mouse;

            if (_firstMove)
            {
                _lastMousePosition = new Vector2(mouse.Position.X, mouse.Position.Y);
                _firstMove = false;
            }
            else
            {
                if (CameraActive)
                {
                    var deltaX = mouse.Position.X - _lastMousePosition.X;
                    var deltaY = mouse.Position.Y - _lastMousePosition.Y;
                    _lastMousePosition = new Vector2(mouse.Position.X, mouse.Position.Y);

                    Rotate(deltaX, deltaY);
                }
            }
        }

        public void ApplyConsumable(ConsumableItem consumable)
        {
            if (consumable != null)
            {
                if (GameBlocks.TryGetItemSound(consumable.Id, out AudioClip clip))
                {
                    if (useConsumableAudio != null)
                    {
                        useConsumableAudio.Stop();
                    }

                    useConsumableAudio = new AudioSource(clip);
                    useConsumableAudio.Volume = 0.3f;
                    useConsumableAudio.Play();

                    if (consumable.HealAmount > 0)
                        HealthColorOverlay.SetActive(new System.Numerics.Vector3(0, 1, 0), 0.2f);

                    if (consumable.PowerAmount > 0)
                        HealthColorOverlay.SetActive(new System.Numerics.Vector3(0, 0, 1), 0.15f);
                }

                HealthBar.StatsData.Increment(consumable.HealAmount);
                PowerBar.StatsData.Increment(consumable.PowerAmount);
            }
            else
            {
                Debug.Error($"[Astrounaut] ApplyConsumable: consumable was null!");
            }
        }

        public void SetInteraction(InteractionMode mode)
        {
            if (_gameModeBase == null) return;

            _gameModeBase.SetInteraction(mode);
        }

        public void Draw()
        {
            Draw(this);
        }

        public void Draw(Camera camera)
        {
            Flashlight.Draw(camera);

            if (VisualDebug.ShowDebug)
            {
                _axes.Position = Position + Front * 0.1f;
                _axes.Render(camera);
            }
        }

        public void OnGUI()
        {
            PowerBar.OnGUI();
            HealthBar.OnGUI();
            HitImage.Draw();
        }
    }
}