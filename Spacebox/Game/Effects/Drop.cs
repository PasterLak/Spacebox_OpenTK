using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Effects;

public class Drop : IDisposable
{
    public struct DropInfo
    {
        public Item item;
        public int quantity;
    }

    public Vector3 Position;
    public Vector3 Velocity;
    public DropInfo Info;
    public float TimeRemaining;
    public float PickupDelay;
    public float Deceleration;
    public bool IsMovingToPlayer;
    public bool IsActive;
    public bool IsThrown;

    private float _age;
    public Vector3 _initialVelocity;
    private float _throwTime;
    public SpaceEntity SourceEntity;

    public bool IsExpired => TimeRemaining <= 0f;
    public bool CanBePickedUp => _age >= PickupDelay;
    public bool IsStopped => Velocity.LengthSquared < 0.01f;

    public void Initialize(Vector3 position, Item item, int quantity, float lifetime, float pickupDelay = 0f, SpaceEntity sourceEntity = null)
    {
        Position = position;
        Velocity = Vector3.Zero;
        Info = new DropInfo { item = item, quantity = quantity };
        TimeRemaining = lifetime;
        PickupDelay = pickupDelay;
        Deceleration = 0f;
        IsMovingToPlayer = false;
        IsActive = true;
        IsThrown = false;
        SourceEntity = sourceEntity;
        _age = 0f;
        _initialVelocity = Vector3.Zero;
        _throwTime = 0f;
    }

    public void AddQuantity(int amount, float lifetime)
    {
        Info = new DropInfo { item = Info.item, quantity = Info.quantity + amount };
        TimeRemaining = lifetime;
    }

    public void Update(float deltaTime)
    {
        if (!IsActive) return;

        TimeRemaining -= deltaTime;
        _age += deltaTime;

        if (Velocity.LengthSquared > 0.01f)
        {
            var movement = Velocity * deltaTime;
            var newPosition = Position + movement;

            if (IsThrown && !IsMovingToPlayer)
            {
                var ray = new Ray(Position, Velocity.Normalized(), movement.Length);
                bool hasHit = false;

                if (SourceEntity != null)
                {
                    hasHit = SourceEntity.Raycast(ray, out var entityHit);
                    if (hasHit)
                    {
                        HandleCollision(entityHit.hitPosition, entityHit.normal.ToVector3());
                        return;
                    }
                }
                else
                {
                    hasHit = World.CurrentSector.Raycast(ray, out var worldHit);
                    if (hasHit)
                    {
                        HandleCollision(worldHit.hitPosition, worldHit.normal.ToVector3());
                        return;
                    }
                }
            }

            Position = newPosition;

            if (Deceleration > 0f && !IsMovingToPlayer && IsThrown)
            {
                _throwTime += deltaTime;
                float decayFactor = MathHelper.Clamp(1f - (_throwTime * Deceleration * 0.2f), 0f, 1f);
                decayFactor = decayFactor * decayFactor;

                Velocity = _initialVelocity * decayFactor;

                if (Velocity.LengthSquared < 0.01f)
                {
                    Velocity = Vector3.Zero;
                }
            }
        }
    }

    public void ResetPickupTimer()
    {
        _age = 0f;
    }

    private void HandleCollision(Vector3 hitPosition, Vector3 normal)
    {
        Position = hitPosition + normal * 0.05f;

        var reflectedVelocity = Velocity - 2f * Vector3.Dot(Velocity, normal) * normal;
        reflectedVelocity *= 0.8f;

        Velocity = reflectedVelocity;
        _initialVelocity = reflectedVelocity;
        _throwTime = 0f;

        if (Velocity.LengthSquared < 1f)
        {
            Velocity = Vector3.Zero;
        }
    }

    public void Reset()
    {
        Position = Vector3.Zero;
        Velocity = Vector3.Zero;
        Info = default;
        TimeRemaining = 0f;
        PickupDelay = 0f;
        Deceleration = 0f;
        IsMovingToPlayer = false;
        IsActive = false;
        IsThrown = false;
        SourceEntity = null;
        _age = 0f;
        _initialVelocity = Vector3.Zero;
        _throwTime = 0f;
    }

    public void Dispose()
    {
        Reset();
    }
}