using Spacebox.Game.Generation;
using Spacebox.Game.Resource;

namespace Spacebox.Game.GUI
{
    public static class ItemTooltipText
    {
        private static readonly System.Text.StringBuilder _tooltipBuilder = new System.Text.StringBuilder();

        public static string Details(Item item, bool showStackSize, bool showDescription, bool showId = false)
        {
            if (item == null) return "";

            var text = "";
            var type = item.GetType();

            if (showDescription)
            {
                var desc = item.Description;
                if (!string.IsNullOrWhiteSpace(desc))
                {
                    text += "\n" + desc + "\n";
                }
            }

            if (showId)
            {

                text += "\nID: " + item.Id_string;

            }

            if (showStackSize)
            {
                if (item.IsStackable)
                {

                    text += "\nStack: " + item.StackSize;
                }
            }

            if (type == typeof(DrillItem))
            {
                var itemType = item as DrillItem;
                text += "\nTier: " + itemType.Tier;
                text += "\nPower usage: " + itemType.PowerUsage;
                text += "\nRange: " + itemType.Range + " blocks";
                text += "\nDamage: " + itemType.Damage + "/s";
            }
            else if (type == typeof(WeaponItem))
            {
                var itemType = item as WeaponItem;

                var pj = GameAssets.Projectiles[itemType.ProjectileID];

                text += "\nDamage: " + $"{+pj.Damage}/{+pj.DamageBlocks}";
                text += "\nPenetration: " + pj.Penetration;
                text += "\nProjectile speed: " + pj.Speed + " block/s";
                text += $"\nReload time: {itemType.ReloadSeconds:0.0#}s";
                text += "\nSpread: " + itemType.Spread;
                text += "\nPower usage: " + itemType.PowerUsage;

                var shotsMin = (int)(60f / MathF.Max(0.05f, itemType.ReloadSeconds));
                text += "\nDPM: " + $"{pj.Damage * shotsMin}/{pj.DamageBlocks * shotsMin}";
            }
            else if (type == typeof(BlockItem))
            {
                var itemType = item as BlockItem;
                text += "\nMass: " + itemType.Mass;
                text += "\nDurability: " + itemType.Health;

                BlockData d = GameAssets.GetBlockDataById(itemType.BlockId);
                if (d != null)
                {
                    text += "\nPower to drill: " + d.PowerToDrill;

                    if(d is StorageBlockData storageData)
                    {
                        text += $"\nSize: {storageData.Size.X}x{ storageData.Size.Y} slots";
                    }

                    if (d.Efficiency != 1f && d.Efficiency > 0f)
                    {
                        text += $"\nSpeed: {d.Efficiency * 100f:0}%";
                    }

                    if (d is GeneratorBlockData generatorData)
                    {
                        text += $"\nGenerates: {generatorData.Power} EU";
                    }

                    if (d is IBlockTooltip blockTooltip)
                    {
                        _tooltipBuilder.Clear();
                        blockTooltip.AppendTooltip(_tooltipBuilder);
                        text += _tooltipBuilder.ToString();
                    }
                }

            }
            else if (type == typeof(EquipmentItem))
            {
                var itemType = item as EquipmentItem;
                text += "\nSlot: " + itemType.Slot;
                if (itemType.HealthBonus != 0)
                    text += "\nHealth: +" + itemType.HealthBonus;
                if (itemType.PowerBonus != 0)
                    text += "\nPower: +" + itemType.PowerBonus;
                if (itemType.ExplosionProtection > 0f)
                    text += $"\nExplosion protection: {itemType.ExplosionProtection * 100f:0}%";
                if (itemType.Flashlight != null)
                    text += $"\nBeam range: {itemType.Flashlight.Range:0} m";
                if (itemType.Radar != null)
                    text += $"\nScan range: {itemType.Radar.Range:0} m" + (itemType.Radar.ShowMass ? "\nShows mass" : "");
            }
            else if (type == typeof(ConsumableItem))
            {
                var itemType = item as ConsumableItem;
                if (itemType.HealAmount > 0)
                    text += "\nHealing: +" + itemType.HealAmount;
                if (itemType.PowerAmount > 0)
                    text += "\nPower: +" + itemType.PowerAmount;
                if (itemType.OxygenAmount > 0)
                    text += "\nOxygen: +" + itemType.OxygenAmount;

            }

            if (item is IItemTooltip itemTooltip)
            {
                _tooltipBuilder.Clear();
                itemTooltip.AppendTooltip(_tooltipBuilder);
                text += _tooltipBuilder.ToString();
            }

            return text;
        }
    }
}
