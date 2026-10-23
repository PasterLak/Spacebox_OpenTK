using OpenTK.Mathematics;
using Spacebox.Game.GUI.Menu;
using Spacebox.Network.Discovery;

namespace Spacebox.Game.GUI.MenuUi;

public static class ServerTextColors
{
    public static Color4 State(ServerListItem item) => From(ServerText.StateColor(item));

    public static Color4 Error => From(ServerText.ErrorColor);

    public static Color4 From(System.Numerics.Vector4 color) => new Color4(color.X, color.Y, color.Z, color.W);
}
