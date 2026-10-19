using OpenTK.Mathematics;
using Spacebox.Game.Player.GameModes;

namespace Spacebox.Game.Tutorial;

public interface ITutorialContext
{
    GameMode GameMode { get; }
    Vector3 ViewDirection { get; }
    short SelectedItemId { get; }
    bool IsMagnetEnabled { get; }
    bool IsFlashlightOn { get; }
    bool IsPiloting { get; }
    bool IsPilotingFlight { get; }
    bool IsUsingShipCamera { get; }
    bool IsInGravity { get; }
    bool HasAnyEquipment { get; }

    int CountItem(short itemId);
    bool IsEquipped(short itemId);
    bool IsSlotEquipped(EquipmentSlotType slot);
    bool IsWindowOpen(string window);
    bool IsActionPressed(string action);
    bool IsActionHeld(string action);
    double GetStat(string stat);
}
