using Engine;
using Engine.Animation;
using Engine.Components;
using Engine.Components.Debug;
using Engine.Physics;
using Engine.Utils;
using OpenTK.Mathematics;
using Spacebox.Game.Effects;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.GUI;
using Spacebox.Game.Physics.Explosions;


namespace Spacebox.Game
{
    public interface IDamageable
    {
        public void TakeDamage(Projectile projectile);

    }
    public class Spacer : Node3D, IDamageable, IDrillable, IExplosionDamageable
    {
        public Storage Storage { get; private set; }

        private StatsData _health;
        private Animator _animator;
        private const float Radius = 0.5f;
        private const float InteractDistance = 3f;
        private const float InteractRayLength = 5f;
        private static readonly Vector3 InitialVelocity = new Vector3(0, 0, 0.2f);

        private SphereBody _body;
        private readonly Ray _interactRay = new Ray(Vector3.Zero, Vector3.UnitZ, InteractRayLength);
        private PlayerEffects _effects;
        private bool _lootWasGenerated = false;
        private float _pendingDrillDamage;

        public Spacer(Vector3 pos, PhysicsWorld physics = null)
        {

            Position = pos;
            Texture2D spacerTex = GameAssets.LoadResource<Texture2D>("Resources/Textures/Skins/spacer.png");
            spacerTex.FlipY();
            spacerTex.FilterMode = FilterMode.Nearest;
            Name = nameof(Spacer);
            _health = new StatsData();
            _health.MaxValue = 20;
            _health.Value = 20;

            _health.OnEqualZero += OnKilled;
            Storage = new Storage(3, 3);
            Storage.Name = Name;

            _body = AttachComponent(new SphereBody(physics, Radius) { Velocity = InitialVelocity });

            Model spacerModel = new Model(GameAssets.LoadResource<Mesh>("Resources/Models/spacer.obj"),
                new TextureMaterial(spacerTex));
            AttachComponent(new ModelRendererComponent(spacerModel));
            AttachComponent(new AxesDebugComponent());

            _animator = new Animator(this);
            _animator.AddAnimation(new RotateAnimation(Vector3.UnitX, 5f, 0f));
            _animator.AddAnimation(new RotateAnimation(Vector3.UnitY, 5f, 0f));

            /*
            var mo = AddChild(new ItemWorldModel("Resources/Textures/Old/drill6.png", 0.1f));
            mo.Rotate(new Vector3(0,90,0));
            mo.SetScale(0.5f);
            mo.Position = new Vector3(0.3f,-0.2f,-0.2f);
            */

            _effects = AddChild(new PlayerEffects());

        }

        public void TakeDamage(Projectile projectile)
        {
            _health.Decrement(projectile.Parameters.Damage);
            _effects.PlayEffect(PlayerEffectType.Damage);
        }


        public void TakeExplosionDamage(int damage)
        {
            _health.Decrement(damage);
            _effects.PlayEffect(PlayerEffectType.Damage);
        }

        public void TakeDrillDamage(float damage)
        {
            _pendingDrillDamage += damage;
            int wholeDamage = (int)_pendingDrillDamage;
            if (wholeDamage <= 0) return;

            _pendingDrillDamage -= wholeDamage;
            _health.Decrement(wholeDamage);
            _effects.PlayEffect(PlayerEffectType.Damage);
        }

        private void GenerateLoot()
        {
            Storage.Clear();
            AddItems(Storage);
            _lootWasGenerated = true;
        }

        private void OnKilled()
        {

            if (!_lootWasGenerated && World.CurrentSector != null)
                GenerateLoot();

            World.DropEffectManager?.DropStorage(Storage, Position);
            Storage.Clear();
            DetachComponent(_body);
            Parent?.RemoveChild(this);
        }
        private void AddItems(Storage storage)
        {
            var items = GameAssets.LootConfig.GenerateLoot("spacer", SeedHelper.ToIntSeed(World.CurrentSector.Seed), storage.SlotsCount);

            foreach (var item in items)
            {
                storage.TryAddItem(item.Item, (byte)item.Quantity);

            }
        }

        public override void Update()
        {
            base.Update();

            _animator?.Update();

            var cam = Camera.Main as LocalAstronaut;

            if (cam == null) return;

            _interactRay.Origin = cam.Position;
            _interactRay.Direction = cam.Front;

            if (_interactRay.IntersectsSphere(Position, Radius, out float distance))
            {

                if (distance < InteractDistance)
                {
                    CenteredText.SetText("Press RMB to open");
                    CenteredText.Show();

                    if (Input.IsActionDown("use"))
                    {
                        if (!_lootWasGenerated)
                            GenerateLoot();
                        StorageUI.OpenStorage(Storage, cam);
                        CenteredText.Hide();
                    }
                }

            }



        }

        public override void Render()
        {

            base.Render();

        }

    }
}
