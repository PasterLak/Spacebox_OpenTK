using OpenTK.Mathematics;

namespace Spacebox.Game
{
    public class WeaponItem : Item
    {
        public int ReloadTime = 500;
        public byte Spread  = 0;
        public byte Pushback  = 0;
        public short ProjectileID = 0;
        public float AnimationSpeed = 0;
        public byte PowerUsage = 0;
        public string ShotSound = "";

        public WeaponItem(byte stackSize, string name, float modelDepth) : base(stackSize, name, modelDepth)
        {
        }

        public static Vector3 CalculateSpreadCone(WeaponItem weaponItem, Vector3 direction)
        {
            if (weaponItem.Spread <= 0) return direction;

            Random r = new Random();

            float spreadAngle = weaponItem.Spread / 255f * MathF.PI / 12f;

            Vector3 right = Vector3.Cross(direction, Vector3.UnitY).Normalized();
            Vector3 up = Vector3.Cross(right, direction).Normalized();

            float angle = (float)(r.NextDouble() * 2 * Math.PI);
            float radius = (float)(r.NextDouble() * spreadAngle);

            Vector3 offset = right * MathF.Cos(angle) * radius + up * MathF.Sin(angle) * radius;

            return (direction + offset).Normalized();
        }
    }
}
