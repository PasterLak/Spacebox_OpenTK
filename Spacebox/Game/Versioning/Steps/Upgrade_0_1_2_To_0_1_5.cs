namespace Spacebox.Game.Versioning.Steps;

public sealed class Upgrade_0_1_2_To_0_1_5 : WorldUpgradeStep
{
    public Upgrade_0_1_2_To_0_1_5() : base("0.1.2", "0.1.5")
    {
    }

    public const string RemovedCobaltCrystal = "default:cobalt_crystal";
    public const string CobaltIngot = "default:cobalt_ingot";
    public const string RemovedBasicEngine = "default:basic_engine";
    public const string RemovedBasicCannon = "default:basic_cannon";
    public const string RemovedObscuriumAmmo = "default:obscurium_ammo";

    public override string Description => "0.1.2 -> 0.1.5: new save fields are optional (tutorial progress, local player file), cobalt crystals become cobalt ingots, decorative basic engine and basic cannon blocks and obscurium ammo are removed, entity files move from the sector folders to Entities/ (with entities.index) and DNA reconstructors are registered in reconstructors.nbt";

    public override string Check(WorldUpgradeContext world)
    {
        return WorldFormatProbe.Detect(world.WorldFolder) switch
        {
            WorldSaveFormat.Legacy => "This world uses the save format from before 0.1.2 (an old converter only changed its version number), so it cannot be converted",
            WorldSaveFormat.WorldSpacePositions => "This world was saved by an early 0.1.2 build (before December 2025), so it cannot be converted",
            _ => null
        };
    }

    public override void Apply(WorldUpgradeContext world)
    {
        world.RemoveItem(RemovedCobaltCrystal, CobaltIngot);
        world.RemoveBlock(RemovedBasicEngine);
        world.RemoveBlock(RemovedBasicCannon);
        world.RemoveItem(RemovedObscuriumAmmo);
        EntityStorageLayout.Upgrade(world);
    }
}
