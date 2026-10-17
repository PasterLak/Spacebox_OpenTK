namespace Spacebox.Game.Player;

public class EquipmentStatsBinder
{
    private readonly PlayerEquipment _equipment;
    private readonly StatsData _health;
    private readonly StatsData _power;
    private readonly int _baseMaxHealth;
    private readonly int _baseMaxPower;

    public EquipmentStatsBinder(PlayerEquipment equipment, StatsData health, StatsData power)
    {
        _equipment = equipment;
        _health = health;
        _power = power;
        _baseMaxHealth = health.MaxValue;
        _baseMaxPower = power.MaxValue;

        _equipment.OnChanged += Apply;
        Apply();
    }

    private void Apply()
    {
        _health.MaxValue = _baseMaxHealth + _equipment.HealthBonus;
        _power.MaxValue = _baseMaxPower + _equipment.PowerBonus;
    }
}
