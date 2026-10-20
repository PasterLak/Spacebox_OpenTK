using System.Text;

namespace Spacebox.Game;

public interface IItemTooltip
{
    void AppendTooltip(StringBuilder text);
}
