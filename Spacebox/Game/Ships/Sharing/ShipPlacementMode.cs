using Engine;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;

namespace Spacebox.Game.Ships.Sharing;

public static class ShipPlacementMode
{
    private const float MinDistance = 4f;
    private const float MaxDistance = 200f;
    private const float Thickness = 0.08f;

    private static readonly Color4 OutlineColor = new Color4(0.45f, 0.85f, 1f, 0.9f);

    private static ShipFile _ship;
    private static Vector3 _localCenter;
    private static Vector3 _halfSize;
    private static Quaternion _rotation = Quaternion.Identity;
    private static float _distance;
    private static bool _unblockPending;

    public static bool IsActive => _ship != null;

    public static void Begin(ShipFile ship)
    {
        if (ship?.Entity == null) return;

        _ship = ship;
        _localCenter = ((Vector3)ship.Min + (Vector3)ship.Max) * 0.5f;
        _halfSize = Vector3.ComponentMax((Vector3)ship.Size * 0.5f, new Vector3(0.5f));
        _rotation = Quaternion.Identity;
        _distance = Math.Clamp(_halfSize.Length + 4f, MinDistance, MaxDistance);
        if (_unblockPending) _unblockPending = false;
        else PanelUI.BlockHands();
        Chat.Write($"Placing '{ship.Name}': left click to place, R / T to turn, mouse wheel for distance, right click or Escape to cancel", Color4.LightBlue);
    }

    public static void Cancel()
    {
        if (!IsActive) return;
        _ship = null;
        _unblockPending = true;
    }

    public static void Reset()
    {
        if (IsActive || _unblockPending) PanelUI.UnblockHands();
        _ship = null;
        _unblockPending = false;
    }

    public static void Update(LocalAstronaut player)
    {
        if (_unblockPending && !Input.IsMouseButton(MouseButton.Left) && !Input.IsMouseButton(MouseButton.Right))
        {
            _unblockPending = false;
            PanelUI.UnblockHands();
        }

        if (!IsActive || player == null) return;

        if (!UIManager.IsUIMode && !Chat.FocusInput && !Debug.IsVisible) HandleInput(player);
        if (!IsActive) return;

        var center = player.Position + player.Front * _distance;
        WorldLines.DrawBox(_localCenter - _halfSize, _localCenter + _halfSize, OutlineColor, Thickness, local => center + Vector3.Transform(local - _localCenter, _rotation));
    }

    private static void HandleInput(LocalAstronaut player)
    {
        if (Input.IsKeyDown(Keys.R)) _rotation = Quaternion.FromAxisAngle(Vector3.UnitY, MathHelper.PiOver2) * _rotation;
        if (Input.IsKeyDown(Keys.T)) _rotation = Quaternion.FromAxisAngle(Vector3.UnitX, MathHelper.PiOver2) * _rotation;

        float scroll = Input.MouseScrollDelta.Y;
        if (scroll != 0f) _distance = Math.Clamp(_distance + scroll * MathF.Max(1f, _distance * 0.1f), MinDistance, MaxDistance);

        if (Input.IsMouseButtonDown(MouseButton.Right))
        {
            Cancel();
            return;
        }

        if (!Input.IsMouseButtonDown(MouseButton.Left)) return;

        var ship = _ship;
        var center = player.Position + player.Front * _distance;
        Spacebox.Network.ShipSharingNetwork.RequestPlace(ship.Entity, center, _rotation, (message, ok) => Chat.Write(message, ok ? Color4.LightGreen : Color4.OrangeRed));
        Cancel();
    }
}
