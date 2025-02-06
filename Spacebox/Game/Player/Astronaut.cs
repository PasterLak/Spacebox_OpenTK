using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

using Engine.Audio;
using Engine.Physics;

using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Engine;
using Spacebox.GUI;


namespace Spacebox.Game.Player
{
    public class Astronaut : Camera360, INotTransparent
    {
        public string Name { get; private set; } = "Player";
        public Inventory Inventory { get; private set; }
        public Storage Panel { get; private set; }

        private Axes _axes;

        public void SetCameraSpeed(float speed, float shiftSpeed)
        {

        }

        private bool _firstMove = true;
        private Vector2 _lastMousePosition;
        bool needResetNextFrame = false;
        public bool CameraActive = true;

        public Action<Astronaut> OnMoved { get; set; }

        private bool _canMove = true;

        public bool CanMove
        {
            get => _canMove;

        }

        public InertiaController InertiaController { get; private set; } = new InertiaController();
        public CameraSway CameraSway { get; private set; } = new CameraSway();
        public Flashlight Flashlight { get; private set; }

        public HitImage HitImage { get; private set; }

        private AudioSource useConsumableAudio;

        public HealthBar HealthBar { get; private set; }
        public PowerBar PowerBar { get; private set; }
        private GameModeBase _gameModeBase;
        private GameMode _gameMode => _gameModeBase.GetGameMode();
        public InteractionMode CurrentInteraction => _gameModeBase.InteractionHandler.Interaction;

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

        private Toggi toggle;
        public Astronaut(Vector3 position)
            : base(position)
        {
            FOV = 90;
            DepthNear = 0.01f;
            DepthFar = Settings.ViewDistance;
            base.Name = Name;
            Layer = CollisionLayer.Player;
            VisualDebug.RemoveCollisionToDraw(this);
            HealthBar = new HealthBar();
            PowerBar = new PowerBar();

            SetData();
            CameraRelativeRender = true;
            HitImage = new HitImage();
            Flashlight = new Flashlight(this);

            GameMode = GameMode.Creative;

            toggle = ToggleManager.Register("player");
            toggle.OnStateChanged += state =>
            {
                needResetNextFrame = state;
                _canMove = state;
            };
        }

        public void ResetMousePosition()
        {
            _firstMove = true;
            Input.MoveCursorToCenter();
            _lastMousePosition = Input.Mouse.Position;

        }


        private void SetData()
        {

            Inventory = new Inventory(8, 4);
            Panel = new Storage(1, 10);
            BoundingVolume = new BoundingSphere(Position, 0.4f);
            Inventory.Name = "Inventory";
            Panel.Name = "Panel";

            Panel.ConnectStorage(Inventory, true);
            Inventory.ConnectStorage(Panel);

            _axes = new Axes(Position, 0.01f);
        }

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
        }

        public void Save()
        {

            PlayerSaveLoadManager.SavePlayer(this, World.Data.WorldFolderPath);


        }

        public override void Update()
        {
            if (needResetNextFrame)
            {
                ResetMousePosition();

                needResetNextFrame = false;
            }

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

            _gameModeBase.HandleInput(this);

            if (!CanMove) return;

            if (Input.IsMouseButton(MouseButton.Middle))
            {
                if (PanelUI.IsHolding<CreativeToolItem>()) return;

                if (FOV != 50)
                    FOV = 50;
            }
            if (Input.IsMouseButtonUp(MouseButton.Middle))
            {
                FOV = 90;
            }

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



            if (Input.IsKeyDown(Keys.R))
            {
                CameraRelativeRender = !CameraRelativeRender;
            }

            if (Input.IsKeyDown(Keys.U))
            {
                CollisionEnabled = !CollisionEnabled;
                Debug.Log($"Collision Enabled: {CollisionEnabled}");
            }

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


            var mouse = Input.Mouse;

            if (_firstMove)
            {
                _lastMousePosition = new Vector2(mouse.Position.X, mouse.Position.Y);
                // _lastMousePosition = Input.Mouse.Delta;
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

            if (_gameModeBase != null)
            {
                _gameModeBase.Render(this);
            }
        }

        public void Draw(Camera camera)
        {
            Flashlight.Draw(camera);

            if (VisualDebug.Enabled)
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