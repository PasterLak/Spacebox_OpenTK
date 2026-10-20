namespace Spacebox.Game.Player;

public sealed class SuffocationDamage
{
    public const float Interval = 1f;
    public const int DamagePerTick = 5;

    private float _timer;

    public int Update(float deltaTime, bool outOfOxygen)
    {
        if (!outOfOxygen)
        {
            _timer = 0f;
            return 0;
        }

        _timer += deltaTime;
        int damage = 0;
        while (_timer >= Interval)
        {
            _timer -= Interval;
            damage += DamagePerTick;
        }
        return damage;
    }
}
