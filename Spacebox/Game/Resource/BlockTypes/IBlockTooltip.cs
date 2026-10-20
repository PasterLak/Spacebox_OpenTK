using System.Text;

namespace Spacebox.Game.Resource;

public interface IBlockTooltip
{
    void AppendTooltip(StringBuilder text);
}
