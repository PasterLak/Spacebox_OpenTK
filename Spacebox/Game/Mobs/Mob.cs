using Engine;
using Engine.Components;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Mobs.Feeding;
using Spacebox.Game.Mobs.Navigation;
using Spacebox.Game.Physics;
using Spacebox.Game.Physics.Explosions;
using Spacebox.Game.Player;
using Spacebox.Network;

namespace Spacebox.Game.Mobs;

public sealed class Mob : Node3D, IDamageable, IDrillable, IExplosionDamageable, IPushable
{
    private const float AlertDuration = 10f;
    private const float MinFacingSpeed = 0.2f;
    private const float DropSpeed = 1.5f;

    private readonly StatsData _health;
    private readonly SphereBody _body;
    private readonly MobAnimator _animator;
    private readonly Random _random;
    private IMobBrain? _brain;
    private Quaternion _orientation = Quaternion.Identity;
    private Vector3 _lookTarget;
    private Vector3 _moveDirection;
    private bool _hasLookTarget;
    private bool _hasMoveDirection;
    private float _pendingDrillDamage;
    private MobAction _action;
    private bool _isRemoved;

    public MobDefinition Definition { get; }
    public MobContext Context { get; }
    public MobNavigationGrid Navigation { get; } = new MobNavigationGrid();
    public MobLocomotion Locomotion { get; }
    public MobSenses Senses { get; }
    public IMobLink? Link { get; set; }
    public bool IsTimid { get; }
    public bool IsDead { get; private set; }
    public bool IsReplica => Link != null && Link.IsReplica;
    public string StateName => _brain?.StateName ?? "";
    public Vector3 Forward => Vector3.Transform(Vector3.UnitZ, _orientation);
    public float HealthPercentage => _health.Percentage;

    public MobAction Action
    {
        get => _action;
        set
        {
            if (_action == value) return;
            _action = value;
            if (!IsReplica) Link?.SyncAction(value);
        }
    }

    public Mob(MobDefinition definition, Vector3 position, PhysicsWorld? physics, bool isTimid, int seed)
    {
        Definition = definition;
        Name = definition.Id;
        Position = position;
        IsTimid = isTimid;
        _random = new Random(seed);

        _health = new StatsData(definition.Name, definition.Health, definition.Health);
        _health.OnEqualZero += Die;

        _body = AttachComponent(new SphereBody(physics, definition.Radius) { Bounciness = 0f });
        AttachComponent(new ModelRendererComponent(MobModels.Get(definition)));
        _animator = new MobAnimator(this, definition.Scale, (float)_random.NextDouble() * 10f);

        Navigation.Bind(position);
        Locomotion = new MobLocomotion(this, _body, Navigation);
        Senses = new MobSenses(this, definition, Navigation, _random);
        Context = new MobContext(this, _random);

        FaceDirection(MobRandom.Direction(_random));

        MobSystem.Instance?.Register(this);
    }

    public void MakeReplica()
    {
        _body.IsKinematic = true;
        _body.Velocity = Vector3.Zero;
        Locomotion.Stop();
    }

    public void ApplyReplicatedAction(MobAction action)
    {
        _action = action;
    }

    public void LookAt(Vector3 worldPoint)
    {
        _lookTarget = worldPoint;
        _hasLookTarget = true;
    }

    public void SetMoveDirection(Vector3 direction)
    {
        _moveDirection = direction;
        _hasMoveDirection = true;
    }

    public override void Update()
    {
        base.Update();
        float deltaTime = Time.Delta;

        if (!IsReplica && !IsDead) UpdateAuthority(deltaTime);

        _animator.Update(deltaTime, _action);
    }

    public void TakeDamage(Projectile projectile)
    {
        if (IsReplica) return;
        ApplyDamage(projectile.Parameters.Damage);
    }

    public void TakeExplosionDamage(int damage)
    {
        if (IsReplica) return;
        ApplyDamage(damage);
    }

    public void TakeDrillDamage(float damage)
    {
        _pendingDrillDamage += damage;
        int wholeDamage = (int)_pendingDrillDamage;
        if (wholeDamage <= 0) return;

        _pendingDrillDamage -= wholeDamage;
        if (IsReplica) Link!.SendDrillDamage(wholeDamage);
        else ApplyDamage(wholeDamage);
    }

    public void ApplyDamage(int damage)
    {
        if (damage <= 0 || IsDead) return;

        if (Link != null) Link.BroadcastHurt();
        else PlayHurt();

        Senses.Alert(AlertDuration);
        _health.Decrement(damage);
    }

    public void Push(Vector3 normal, float targetSpeed)
    {
        if (IsReplica) Link!.SendPush(normal, targetSpeed);
        else _body.Push(normal, targetSpeed);
    }

    public void Bite(int targetPeer)
    {
        if (IsReplica || IsDead) return;

        int damage = Definition.RollDamage(_random);
        if (Link != null) Link.BroadcastBite(targetPeer, damage);
        else PlayBite();
    }

    public bool EatBlock(FoodTarget food)
    {
        if (IsReplica || !food.IsEdible(Navigation, Definition)) return false;
        if (!Navigation.TryGetBlock(food.Cell, out var chunk, out var index, out _)) return false;

        var normal = food.Normal;
        chunk.RemoveBlock(index.X, index.Y, index.Z, (sbyte)normal.X, (sbyte)normal.Y, (sbyte)normal.Z, false);
        WorldEditNetwork.NotifyRemoved(chunk, index);
        return true;
    }

    public void PlayHurt()
    {
        _animator.PlayHurt();
    }

    public void PlayBite()
    {
        _animator.PlayBite();
    }

    public void PlayDeathEffect(short blockId)
    {
        if (World.DestructionManager == null || !GameAssets.Blocks.ContainsKey(blockId)) return;
        World.DestructionManager.DestroyBlock(Position - new Vector3(0.5f), Color3Byte.White, GameAssets.CreateBlockFromId(blockId));
    }

    public void Remove()
    {
        if (_isRemoved) return;
        _isRemoved = true;

        Locomotion.Stop();
        DetachComponent(_body);
        Parent?.RemoveChild(this);
        MobSystem.Instance?.Unregister(this);
    }

    private void UpdateAuthority(float deltaTime)
    {
        Context.Tick(deltaTime);
        Senses.Update(deltaTime);

        _brain ??= MobBrainFactory.Create(Context);
        _brain.Update(deltaTime);

        Locomotion.Update(deltaTime);
        UpdateFacing(deltaTime);
    }

    private void UpdateFacing(float deltaTime)
    {
        Vector3 direction;
        if (_hasLookTarget) direction = _lookTarget - Position;
        else if (_hasMoveDirection) direction = _moveDirection;
        else if (_body.Velocity.LengthSquared > MinFacingSpeed * MinFacingSpeed) direction = _body.Velocity;
        else direction = Vector3.Zero;

        _hasLookTarget = false;
        _hasMoveDirection = false;

        if (direction.LengthSquared < 1e-6f) return;

        var target = LookRotation(direction.Normalized());
        float t = 1f - MathF.Exp(-Definition.TurnSpeed * deltaTime);
        _orientation = Quaternion.Slerp(_orientation, target, t).Normalized();
        Rotation = QuaternionToEuler(_orientation);
    }

    private void FaceDirection(Vector3 direction)
    {
        _orientation = LookRotation(direction);
        Rotation = QuaternionToEuler(_orientation);
    }

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;

        Locomotion.Stop();
        var drop = Definition.RollDrop(_random);
        short effectBlock = drop is BlockItem blockItem ? blockItem.BlockId : (short)0;

        if (drop != null && !IsReplica)
            World.DropEffectManager?.DropItem(Position, MobRandom.Direction(_random), DropSpeed, drop, 1);

        if (Link != null)
        {
            Link.BroadcastDeath(effectBlock);
            Link.Despawn();
            return;
        }

        PlayDeathEffect(effectBlock);
        Remove();
    }

    private static Quaternion LookRotation(Vector3 direction)
    {
        float yaw = MathF.Atan2(direction.X, direction.Z);
        float pitch = -MathF.Asin(Math.Clamp(direction.Y, -1f, 1f));
        return Quaternion.FromAxisAngle(Vector3.UnitY, yaw) * Quaternion.FromAxisAngle(Vector3.UnitX, pitch);
    }
}
