using System.Text;
using Spacebox.Game.Protection;

namespace Spacebox.Game.Resource;

public class ClaimBeaconBlockData : BlockData, IBlockTooltip
{
    public ClaimBeaconBlockData(BlockData baseBlock) : base(baseBlock)
    {
    }

    public void AppendTooltip(StringBuilder text)
    {
        var settings = ClaimSettings.Current;
        if (!settings.Enabled)
        {
            text.Append("\nClaims are disabled on this server");
            return;
        }

        text.Append($"\nProtects a ship completely, or {settings.Radius * 2 + 1}x{settings.Radius * 2 + 1}x{settings.Radius * 2 + 1} blocks of an asteroid");
        foreach (var payment in settings.Payments)
            text.Append($"\n{payment.Key}: +{ClaimCost.FormatDuration(payment.Value * 60f)}");
        text.Append($"\nStores up to {ClaimCost.FormatDuration(settings.MaxStoredSeconds)}");
    }
}
