using System.Text;
using Spacebox.Game.Resource;

namespace Spacebox.Tests.Ships;

public class ShipBlockTooltipTests
{
    private static string Tooltip(IBlockTooltip data)
    {
        var text = new StringBuilder();
        data.AppendTooltip(text);
        return text.ToString();
    }

    [Fact]
    public void Thruster_ShowsThrustAndPower()
    {
        var data = new ThrusterBlockData(new BlockData("thruster", "thruster"), new ThrusterJSON { Force = 60, Power = 15 });

        var text = Tooltip(data);

        Assert.Contains("Thrust: 60", text);
        Assert.Contains("Power usage: 15 EU", text);
    }

    [Fact]
    public void WeaponsConsole_ShowsCannonControlAndGroups()
    {
        var data = new ShipConsoleBlockData(new BlockData("console", "shipconsole"), new ShipConsoleJSON { Buttons = 1, Weapons = true });

        var text = Tooltip(data);

        Assert.Contains("Controls: cannons", text);
        Assert.Contains("Groups: 1", text);
    }

    [Fact]
    public void Cannon_ShowsReloadAndAim()
    {
        var data = new CannonBlockData(new BlockData("cannon", "cannon"), new CannonJSON { Projectile = "missing", ReloadTime = 0.3f, AimCone = 45, Power = 45 });

        var text = Tooltip(data);

        Assert.Contains("Reload time: 0.3s", text);
        Assert.Contains("Aim cone: 45°", text);
        Assert.Contains("Power usage: 45 EU", text);
        Assert.DoesNotContain("Damage", text);
    }
}
