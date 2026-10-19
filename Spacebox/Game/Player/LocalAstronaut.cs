using System;
using Engine;
using Engine.Audio;
using Engine.Components;
using Engine.Light;
using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game.Effects;
using Spacebox.Game.Physics;
using Spacebox.Game.GUI;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Player.Interactions;
using Spacebox.Game.Player.Respawn;
using Spacebox.Game.Resource;
using Spacebox.GUI;

using Spacebox.Game.Ships;
namespace Spacebox.Game.Player
{
    public class LocalAstronaut : Astronaut
    {
        public Inventory Inventory { get; private set; }
        public Storage Panel { get; private set; }
        public PlayerEquipment Equipment { get; private set; }
        private EquipmentStatsBinder _equipmentStats;
        private EquipmentFlashlightBinder _flashlightBinder;
        public FallDamage FallDamage { get; private set; }

        public bool IsUsingItem => PanelUI.CurrentSlot() is { HasItem: true, Item: DrillItem } && PanelUI.IsHeldItemAnimating;

        public EquipmentItem? ActiveFlashlightItem => Equipment.FlashlightItem ?? HeldFlashlightItem;
        public bool CanUseFlashlight => GameMode == GameMode.Spectator || ActiveFlashlightItem != null;

        private static EquipmentItem? HeldFlashlightItem =>
            PanelUI.CurrentSlot() is { HasItem: true, Item: EquipmentItem { Flashlight: not null } item } ? item : null;
        public RadarSettings? Radar => GameMode == GameMode.Spectator ? RadarSettings.Unlimited : Equipment.Radar;
        public bool CameraActive = true;
        public Action<LocalAstronaut> OnMoved { get; set; }
        public SphereBody Body { get; private set; }
        public GravityWalker Gravity { get; } = new GravityWalker();

        public bool CanMove => _isAlive && !UIManager.IsUIMode;
        public bool IsFrozen { get; set; }

        private bool _isAlive = true;
        public bool IsAlive
        {
            get => _isAlive;
            private set
            {
                _isAlive = value;
            }
        }

        public Action OnDeath { get; set; }
        public DeathCase LastDeathCase { get; private set; }
        public Action OnRevived { get; set; }

        private const float AttackerMemorySeconds = 10f;
        private int _lastAttackerPeer = -1;
        private double _lastAttackTime = double.MinValue;

        public void RegisterAttacker(int peer)
        {
            _lastAttackerPeer = peer;
            _lastAttackTime = Time.Total;
        }

        public int GetRecentAttacker()
        {
            return Time.Total - _lastAttackTime <= AttackerMemorySeconds ? _lastAttackerPeer : -1;
        }
        public InertiaController InertiaController { get; private set; } = new InertiaController();
        public CameraSway CameraSway { get; private set; } = new CameraSway();
        public ItemSway ItemSway { get; private set; } = new ItemSway();

        public HitImage HitImage { get; private set; }
        public DeathScreen DeathScreen { get; private set; } = new DeathScreen();
        public PlayerRespawner Respawner { get; private set; }
        public HealthBar HealthBar { get; private set; }
        public Mood Mood { get; private set; }
        public PowerBar PowerBar { get; private set; }
        public OxygenBar OxygenBar { get; private set; }

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
        public PointLight ItemLight { get; private set; }

        private bool _collisionEnabled = true;
        public bool CollisionEnabled
        {
            get => _collisionEnabled;
            set => _collisionEnabled = value;
        }

        private Axes _axes;
        private readonly PlayerStatsTracker _statsTracker;

        public LocalAstronaut(Vector3 position) : base(position, true)
        {
            FOV = Settings.Graphics.Fov;
            SpawnPosition = position;
            _statsTracker = new PlayerStatsTracker(this);
            DepthNear = 0.01f;
            DepthFar = Settings.ViewDistance;

            HealthBar = new HealthBar();
            PowerBar = new PowerBar();
            OxygenBar = new OxygenBar();
            if (GameSetLoader.ModInfo != null)
                OxygenBar.ConsumeInterval = GameSetLoader.ModInfo.OxygenConsumeInterval;
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
            _flashlightBinder = new EquipmentFlashlightBinder(this);
            FallDamage = new FallDamage(this);

            AddChild(Effects);

            var audioListener = AttachComponent(new AudioListener());
            audioListener.DistanceModel = DistanceModel.ExponentDistanceClamped;
            audioListener.DopplerFactor = 3;
            audioListener.SpeedOfSound = 1000f;
            audioListener.Gain = 0.9f;

            OnMoved += (a) =>
            {
                audioListener.Velocity = InertiaController.Velocity;
            };

            HealthBar.StatsData.OnIncrement += (v) => { PlayerStatistics.HealthHealed += v; };
            HealthBar.StatsData.OnDecrement += (v) => { PlayerStatistics.RecordDamageTaken(v); };
            Respawner = new PlayerRespawner(this);
            DeathScreen.OnRespawn += Respawner.RespawnAtSpawnPoint;
            DeathScreen.OnReconstruct += ReconstructOrRespawn;
            DeathScreen.ReconstructionBlocker = Respawner.GetReconstructionBlocker;
            Flashlight.OnEnabledChanged += (b) => { PanelUI.SetFlashlight(this); };

        }

        public override void Start()
        {
            base.Start();
        }

        public void EnablePhysics(PhysicsWorld physics)
        {
            if (Body != null) DetachComponent(Body);

            float radius = BoundingVolume is BoundingSphere sphere ? sphere.Radius : 0.4f;
            Body = AttachComponent(new SphereBody(physics, radius) { IsKinematic = true });
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

            Equipment = new PlayerEquipment();
            Equipment.Storage.ConnectStorage(Inventory);
            Equipment.Storage.OnDataWasChanged += _ => ApplyArmorVisuals();
            _equipmentStats = new EquipmentStatsBinder(Equipment, HealthBar.StatsData, PowerBar.StatsData);

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
            _flashlightBinder?.Apply();
            PanelUI.ResetLastSelected();
            PanelUI.SetSelectedSlot(0);
        }

        private void ApplyArmorVisuals()
        {
            if (ArmorVisual == null) return;

            foreach (var slot in Enum.GetValues<EquipmentSlotType>())
                ArmorVisual.Set(slot, Equipment.GetSlot(slot) is { HasItem: true, Item: EquipmentItem item } ? item : null);
        }

        public override void Update()
        {
            base.Update();
            _statsTracker.Update(Time.Delta);
            _flashlightBinder?.Update();
            HasOxygen = OxygenBar.StatsData.Value > 0;

            Matrix4 viewMatrix = GetViewMatrix();
            Matrix4 projectionMatrix = GetProjectionMatrix();
            Frustum.UpdateFrustum(this);
            if (IsFrozen) return;

            if (IsMain) ShipRider.Carry(this);
            UpdateControl();
            if (IsMain) ShipRider.Capture(this);
        }

        private void UpdateControl()
        {
            if (ShipPilot.IsPiloting)
            {
                _gameModeBase.Update(this);
                if (IsMain) ShipPilot.Update(this);
            }
            else
            {
                _gameModeBase.UpdateInteraction(this);
                _gameModeBase.Update(this);
            }

            HitImage.Update();
            DeathScreen.Update();

            if (!IsMain) return;

            if (!ShipPilot.IsPiloting || ShipPilot.AllowsMovement) _gameModeBase.HandleInput(this);

            ItemSway.EnableSway = CanMove;
            ItemSway.Update(InertiaController.Velocity, Front, Right);
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
                FOV = Settings.Graphics.Fov;
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
            Quaternion sway = Settings.Gameplay.CameraSway ? CameraSway.GetSwayRotation() : Quaternion.Identity;
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
                if (Gravity.IsActive) Gravity.Look(this);
                else Rotate();
            }
        }

        public void SetPosition(Vector3 position)
        {
            Position = position;
            _statsTracker.ResetPosition();
        }

        public void Teleport(Vector3 position)
        {
            Position = position;
            _statsTracker.ResetPosition();
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

        public void ApplyImpulse(Vector3 impulse)
        {
            if (impulse == Vector3.Zero) return;

            if (Gravity.IsActive)
                Gravity.AddImpulse(impulse);
            else
                InertiaController.Velocity += impulse;
        }

        public void TakeDamage(int damage, DeathCase? deathCase = null)
        {
            var health = HealthBar.StatsData;
            damage = Math.Min(damage, health.MaxValue);
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
            PlayerStatistics.RecordDeath(deathCase?.CauseOfDeath);
            LastDeathCase = deathCase;

            IsAlive = false;
            InertiaController.Reset();
            FOV = 110;
            HitImage.Hide();
            Settings.ShowInterface = false;
            PanelUI.IsItemModelVisible = false;
            Input.ShowCursor();
            Mood.AddMoodRandom(1, 10);
            Flashlight.Enabled = false;
            HealthBar.StatsGUI.IsVisible = false;
            PowerBar.StatsGUI.IsVisible = false;
            OxygenBar.StatsGUI.IsVisible = false;

            if (deathCase != null)
                DeathScreen.Show(deathCase);
            else
                DeathScreen.Show(new DeathCase(""));

            OnDeath?.Invoke();
        }

        private void ReconstructOrRespawn()
        {
            if (!Respawner.RespawnAtReconstructor())
                Respawner.RespawnAtSpawnPoint();
        }

        public void Revive(Vector3 position)
        {
            if (IsAlive) return;

            IsAlive = true;
            FOV = Settings.Graphics.Fov;
            Settings.ShowInterface = true;
            var health = HealthBar.StatsData;
            var power = PowerBar.StatsData;
            health.Value = health.MaxValue;
            power.Value = power.MaxValue;
            OxygenBar.StatsData.Fill();
            PanelUI.IsItemModelVisible = true;
            Input.HideCursor();
            Flashlight.Enabled = true;
            HealthBar.StatsGUI.IsVisible = true;
            PowerBar.StatsGUI.IsVisible = true;
            OxygenBar.StatsGUI.IsVisible = true;
            SetPosition(position);
            Effects.PlayEffect(PlayerEffectType.Heal);
            _lastAttackerPeer = -1;
            OnRevived?.Invoke();
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

            if (IsMain) ShipBindingTool.Render();
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
            OxygenBar.OnGUI();
            HealthBar.OnGUI();
            GravityIndicatorUI.Draw(Gravity.IsActive);
            HitImage.Draw();
            DeathScreen.Render();
        }
    }
}