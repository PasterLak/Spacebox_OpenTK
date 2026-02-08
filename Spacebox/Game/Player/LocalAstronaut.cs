using System;
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
using Client;
using Spacebox.Client;

namespace Spacebox.Game.Player
{
    public class LocalAstronaut : Astronaut
    {
        public Inventory Inventory { get; private set; }
        public Storage Panel { get; private set; }
        public bool CameraActive = true;
        public Action<LocalAstronaut> OnMoved { get; set; }

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

        public HitImage HitImage { get; private set; }
        public DeathScreen DeathScreen { get; private set; } = new DeathScreen();
        public HealthBar HealthBar { get; private set; }
        public Mood Mood { get; private set; }
        public PowerBar PowerBar { get; private set; }

        private GameModeBase _gameModeBase;
        private GameMode _gameMode => _gameModeBase.GetGameMode();
        public GameMode GameMode
        {
            get { return _gameMode; }
            set { SetGameMode(value); }
        }

        public InteractionMode CurrentInteraction => _gameModeBase.InteractionHandler.Interaction;
        public PlayerStatistics PlayerStatistics { get; set; } = new PlayerStatistics();
        public PlayerEffects Effects { get; private set; } = new PlayerEffects();
        public Vector3 SpawnPosition { get; set; }
        public Vector3 LastStatPosition { get; private set; }
        public PointLight ItemLight { get; private set; }

        private bool _collisionEnabled = true;
        public bool CollisionEnabled
        {
            get => _collisionEnabled;
            set => _collisionEnabled = value;
        }

        private Toggi _toggle;

        private Axes _axes;
        private float _timeToSavePosToStat = 5;

        public LocalAstronaut(Vector3 position) : base(position, true)
        {
            FOV = Settings.Graphics.Fov;
            SpawnPosition = position;
            LastStatPosition = position;
            DepthNear = 0.01f;
            DepthFar = Settings.ViewDistance;

            HealthBar = new HealthBar();
            PowerBar = new PowerBar();
            Mood = AttachComponent(new Mood(this));

            SetData();
            SetRenderSpace(true);

            HitImage = new HitImage();


            ItemLight = new PointLight();
            ItemLight.Diffuse = new Vector3(0.2f, 1, 0.2f);
            ItemLight.Specular = Vector3.Zero;
            ItemLight.Intensity = 1;
            ItemLight.Range = 5f;
            ItemLight.Enabled = false;
            AddChild(ItemLight);

            CreateModel(0, "Yellow");

            this.Flashlight.AddToggleToManager(this);
            GameMode = GameMode.Creative;

            _toggle = ToggleManager.Register("player");
            _toggle.OnStateChanged += state =>
            {
                if (IsAlive) _canMove = state;
            };



            AddChild(Effects);

            var audioListener = AttachComponent(new AudioListener());
            audioListener.DistanceModel = DistanceModel.ExponentDistanceClamped;
            audioListener.DopplerFactor = 3;
            audioListener.SpeedOfSound = 1000f;
            audioListener.Gain = 0.9f;

            OnMoved += (a) =>
            {
                audioListener.Velocity = InertiaController.Velocity;
                _timeToSavePosToStat -= Time.Delta;

                if (_timeToSavePosToStat < 0)
                {
                    _timeToSavePosToStat = 5;
                    if (GameMode != null && GameMode != GameMode.Spectator)
                        PlayerStatistics.UpdateDistance(LastStatPosition, Position);

                    LastStatPosition = Position;
                }
            };

            HealthBar.StatsData.OnIncrement += (v) => { PlayerStatistics.HealthHealed += v; };
            DeathScreen.OnRespawn += Revive;
            Flashlight.OnEnabledChanged += (b) => { PanelUI.SetFlashlight(this); };


            AttachComponent(new NetworkNode3DComponent(true));

            if (ClientNetwork.Instance != null)
                rpc = AttachComponent(new NetworkIdentity(ClientNetwork.Instance.LocalPlayerId, true));
          


        }

        public override void Start()
        {
            base.Start();

            
        }

        private void SetData()
        {
            Inventory = new Inventory(8, 3);
            Panel = new Storage(1, 10);
            BoundingVolume = new BoundingSphere(Position, 0.4f);
            Inventory.Name = "Inventory";
            Panel.Name = "Panel";
            Name = "LocalAstronaut";
            Panel.ConnectStorage(Inventory, true);
            Inventory.ConnectStorage(Panel);

            _axes = new Axes(Position, 0.01f);
        }

        private void SetGameMode(GameMode mode)
        {
            if (_gameModeBase != null) _gameModeBase.OnDisable();

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



        public override void Update()
        {
            base.Update();


            Matrix4 viewMatrix = GetViewMatrix();
            Matrix4 projectionMatrix = GetProjectionMatrix();
            Frustum.UpdateFrustum(this);


            _gameModeBase.UpdateInteraction(this);
            _gameModeBase.Update(this);

            HitImage.Update();
            DeathScreen.Update();

            if (!IsMain) return;

            _gameModeBase.HandleInput(this);

            if (!CanMove) return;

            if (Input.IsAction("zoom"))
            {
                if (!PanelUI.IsHolding<CreativeToolItem>())
                {
                    if (FOV != 50) FOV = 50;
                }
            }
            if (Input.IsActionUp("zoom"))
            {
                FOV = 90;
            }

            if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.I))
            {
                Debug.Log("RPC Call send!");
                rpc?.Call(nameof(RpcColor));
            }
         
#if DEBUG
            if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.U))
            {
                CollisionEnabled = !CollisionEnabled;
                Debug.Log($"Collision Enabled: {CollisionEnabled}");
            }
#endif

            HandleMouse();
            UpdateBounding();
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

            Rotation = QuaternionToEuler(GetRotation());

            if (CameraRelativeRender)
            {
                return Matrix4.LookAt(Vector3.Zero, newFront, newUp);
            }

            return Matrix4.LookAt(Position, Position + newFront, newUp);
        }

        private void HandleMouse()
        {
            if (CameraActive)
            {
                Rotate();
            }
        }

        public void SetPosition(Vector3 position)
        {
            LastStatPosition = position;
            Position = position;
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

        public void TakeDamage(Projectile projectile, DeathCase? deathCase = null)
        {
            TakeDamage(projectile.Parameters.Damage, deathCase);
        }
        public void TakeDamage(int damage, DeathCase? deathCase = null)
        {
            var health = HealthBar.StatsData;
            damage = Math.Min(damage, health.MaxValue);
            PlayerStatistics.DamageTaken += damage;
            health.Decrement(damage);
            Mood.AddMood(1);

            if (damage > 12) Effects.PlayEffect(PlayerEffectType.Damage);

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
            Mood.AddMoodRandom(1, 10);
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
            if (IsAlive) return;

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
            HealthBar.StatsGUI.IsVisible = true;
            PowerBar.StatsGUI.IsVisible = true;
            SetPosition(SpawnPosition);
            Effects.PlayEffect(PlayerEffectType.Heal);
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

        public override void Destroy()
        {
            base.Destroy();
            _axes.Destroy();


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