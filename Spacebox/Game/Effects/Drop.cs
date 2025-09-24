using OpenTK.Mathematics;

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

    public bool IsExpired => TimeRemaining <= 0f;
    public bool CanBePickedUp => _age >= PickupDelay;
    public bool IsStopped => Velocity.LengthSquared < 0.01f;

    public void Initialize(Vector3 position, Item item, int quantity, float lifetime, float pickupDelay = 0f)
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
        _age = 0f;
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
            Position += Velocity * deltaTime;

            if (Deceleration > 0f && !IsMovingToPlayer)
            {
                var currentSpeed = Velocity.Length;
                var newSpeed = Math.Max(0f, currentSpeed - Deceleration * deltaTime);

                if (newSpeed > 0f)
                {
                    Velocity = Velocity.Normalized() * newSpeed;
                }
                else
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
        ResetPickupTimer();
    }

    public void Dispose()
    {
        Reset();
    }
}