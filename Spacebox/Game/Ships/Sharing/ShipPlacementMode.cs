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

    private const float CheckInterval = 0.2f;

    private static readonly Color4 OutlineColor = new Color4(0.45f, 0.85f, 1f, 0.9f);
    private static readonly Color4 BlockedColor = new Color4(1f, 0.25f, 0.2f, 0.9f);

    private static ShipFile _ship;
    private static Vector3 _localCenter;
    private static Vector3 _halfSize;
    private static Quaternion _rotation = Quaternion.Identity;
    private static Quaternion _frame = Quaternion.Identity;
    private static bool _frameKnown;
    private static bool _turned;
    private static float _distance;
    private static bool _unblockPending;
    private static bool _alignPending;
    private static ShipPlacementPreview _preview;
    private static string _blockedReason;
    private static float _nextCheck;

    public static bool IsActive => _ship != null;

    public static ShipPlacementPreview CreatePreview()
    {
        _preview = new ShipPlacementPreview();
        return _preview;
    }

    public static void Begin(ShipFile ship)
    {
        if (ship?.Entity == null) return;

        _ship = ship;
        _localCenter = ((Vector3)ship.Min + (Vector3)ship.Max) * 0.5f;
        _halfSize = Vector3.ComponentMax((Vector3)ship.Size * 0.5f, new Vector3(0.5f));
        _rotation = Quaternion.Identity;
        _frame = Quaternion.Identity;
        _frameKnown = false;
        _turned = false;
        _alignPending = true;
        _blockedReason = null;
        _nextCheck = 0f;
        _distance = Math.Clamp(_halfSize.Length + 4f, MinDistance, MaxDistance);
        if (_unblockPending) _unblockPending = false;
        else PanelUI.BlockHands();
        _preview?.Show(ship.Entity);
        Chat.Write($"Placing '{ship.Name}': left click to place, {Key(YawLeftAction, "Left")} / {Key(YawRightAction, "Right")} to turn, {Key(PitchUpAction, "Up")} / {Key(PitchDownAction, "Down")} to tilt, {Key(RollLeftAction, "PageUp")} / {Key(RollRightAction, "PageDown")} to roll ({Key(FineAction, "Alt")}: {ShipPlacementRotation.FineStep:0} degrees), {Key(AlignAction, "Backspace")} to line up with you, mouse wheel for distance, right click or Escape to cancel", Color4.LightBlue);
    }

    public static void Cancel()
    {
        if (!IsActive) return;
        _ship = null;
        _unblockPending = true;
        _preview?.Hide();
    }

    public static void Reset()
    {
        if (IsActive || _unblockPending) PanelUI.UnblockHands();
        _ship = null;
        _unblockPending = false;
        _preview = null;
    }

    public static void Update(LocalAstronaut player)
    {
        if (_unblockPending && !Input.IsMouseButton(MouseButton.Left) && !Input.IsMouseButton(MouseButton.Right))
        {
            _unblockPending = false;
            PanelUI.UnblockHands();
        }

        if (!IsActive || player == null) return;

        if (!_frameKnown && _preview != null && _preview.TryGetFrame(out var frame))
        {
            _frame = frame;
            _frameKnown = true;
            if (!_turned) _alignPending = true;
        }

        if (_alignPending)
        {
            _rotation = ShipPlacementRotation.AlignedTo(player.Right, player.Up, player.Front, _frame);
            _alignPending = false;
        }

        if (!UIManager.IsUIMode && !Chat.FocusInput && !Debug.IsVisible) HandleInput(player);
        if (!IsActive) return;

        var center = player.Position + player.Front * _distance;
        if (Time.Total >= _nextCheck)
        {
            _nextCheck = Time.Total + CheckInterval;
            _blockedReason = Spacebox.Network.ShipSharingNetwork.CheckLocal(new ShipPlacementBounds(center, _halfSize, _rotation), player.Position);
        }

        var color = _blockedReason == null ? OutlineColor : BlockedColor;
        WorldLines.DrawBox(_localCenter - _halfSize, _localCenter + _halfSize, color, Thickness, local => center + Vector3.Transform(local - _localCenter, _rotation));
        _preview?.SetPose(_localCenter, _rotation, center);
    }

    public const string YawLeftAction = "ship_place_yaw_left";
    public const string YawRightAction = "ship_place_yaw_right";
    public const string PitchUpAction = "ship_place_pitch_up";
    public const string PitchDownAction = "ship_place_pitch_down";
    public const string RollLeftAction = "ship_place_roll_left";
    public const string RollRightAction = "ship_place_roll_right";
    public const string FineAction = "ship_place_fine";
    public const string AlignAction = "ship_place_align";

    private static string Key(string action, string fallback) => Spacebox.Game.GUI.KeyNames.ForAction(action) ?? fallback;

    private static bool Pressed(string action, Keys fallback) => Engine.InputPro.InputManager.Instance.HasAction(action) ? Input.IsActionDown(action) : Input.IsKeyDown(fallback);

    private static bool Held(string action, Keys fallback) => Engine.InputPro.InputManager.Instance.HasAction(action) ? Input.IsAction(action) : Input.IsKey(fallback);

    private static void HandleInput(LocalAstronaut player)
    {
        float step = Held(FineAction, Keys.LeftAlt) ? ShipPlacementRotation.FineStep : ShipPlacementRotation.CoarseStep;
        if (Pressed(YawLeftAction, Keys.Left)) TurnAround(Vector3.UnitY, step);
        if (Pressed(YawRightAction, Keys.Right)) TurnAround(Vector3.UnitY, -step);
        if (Pressed(PitchUpAction, Keys.Up)) TurnAround(Vector3.UnitX, step);
        if (Pressed(PitchDownAction, Keys.Down)) TurnAround(Vector3.UnitX, -step);
        if (Pressed(RollLeftAction, Keys.PageUp)) TurnAround(Vector3.UnitZ, step);
        if (Pressed(RollRightAction, Keys.PageDown)) TurnAround(Vector3.UnitZ, -step);
        if (Pressed(AlignAction, Keys.Backspace))
        {
            _rotation = ShipPlacementRotation.AlignedTo(player.Right, player.Up, player.Front, _frame);
            _turned = false;
        }

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
        string blocked = Spacebox.Network.ShipSharingNetwork.CheckLocal(new ShipPlacementBounds(center, _halfSize, _rotation), player.Position);
        if (blocked != null)
        {
            _blockedReason = blocked;
            Chat.Write(blocked, Color4.OrangeRed);
            return;
        }

        Spacebox.Network.ShipSharingNetwork.RequestPlace(ship.Entity, center, _rotation, player.Position, (message, ok) => Chat.Write(message, ok ? Color4.LightGreen : Color4.OrangeRed));
        Cancel();
    }

    private static void TurnAround(Vector3 frameAxis, float degrees)
    {
        _rotation = ShipPlacementRotation.Turn(_rotation, _frame, frameAxis, degrees);
        _turned = true;
    }
}
