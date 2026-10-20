using System.Globalization;

namespace Spacebox.Game.Commands;

public static class CommandArguments
{
    public static bool TrySplitAmountAndTarget(string[] args, int start, out int? amount, out string target)
    {
        amount = null;
        target = null;

        for (int i = start; i < args.Length; i++)
        {
            if (int.TryParse(args[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int number))
            {
                if (amount != null) return false;
                amount = number;
            }
            else
            {
                if (target != null) return false;
                target = args[i];
            }
        }

        return true;
    }
}
