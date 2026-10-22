using Engine;
using Engine.Networking;
using Engine.Physics;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Player;

namespace Spacebox.Game.Ships;

public static class ShipPilot
{
    private const float AimDistance = 300f;
    private const float RollSpeed = 1000f;

    private static LocalAstronaut _player;
    private static Vector3 _seatLocal;
    private static bool _skipUseThisFrame;
    private static bool _wasBinding;
    private static ShipCameraBlock _viewCamera;
    private static ShipViewCamera _view;
    private static Quaternion _seatRotationLocal = Quaternion.Identity;
    private static Quaternion _lastViewOrientation = Quaternion.Identity;

    public static ShipConsoleBlock Console { get; private set; }
    public static bool IsPiloting => Console != null;
    public static bool IsUsingCamera { get; private set; }
    public static bool AllowsMovement => IsPiloting && ShipBindingTool.IsActive;
    public static bool IsSeated => IsPiloting && !ShipBindingTool.IsActive;

    public static event Action<ShipConsoleBlock> Mounted;
    public static event Action<ShipConsoleBlock> Unmounted;

    public static void Mount(ShipConsoleBlock console, LocalAstronaut player)
    {
        if (console?.Entity == null || player == null || !player.IsAlive) return;
        if (!Spacebox.Game.Protection.EntityProtection.CheckLocalUse(console.Entity)) return;
        if (IsPiloting) Unmount(player);

        Console = console;
        _player = player;
        _skipUseThisFrame = true;
        _wasBinding = false;
        IsUsingCamera = false;

        ShipRider.Board(player, console.Entity);
        _seatLocal = console.Entity.WorldToEntityLocal(player.Position);
        player.InertiaController.Velocity = Vector3.Zero;
        FaceConsole(player, console);

        Spacebox.Game.GUI.PanelUI.BlockHands();
        Spacebox.Network.ShipNetwork.SendMount(console, true);
        Mounted?.Invoke(console);
    }

    public static void Unmount(LocalAstronaut player)
    {
        var console = Console;
        if (console == null) return;

        if ((IsUsingCamera || _wasBinding) && console.Entity != null && player != null)
        {
            player.SetPosition(console.Entity.EntityLocalToWorld(_seatLocal));
            player.InertiaController.Velocity = Vector3.Zero;
        }

        SetViewCamera(null, player);
        Spacebox.Network.ShipNetwork.SendMount(console, false);
        ShipBindingTool.Exit();
        Spacebox.Game.GUI.PanelUI.UnblockHands();
        Console = null;
        IsUsingCamera = false;
        _wasBinding = false;
        Unmounted?.Invoke(console);
    }

    public static void OnConsoleUnavailable(ShipConsoleBlock console)
    {
        if (Console == console) Unmount(_player);
    }

    public static void Reset()
    {
        ShipBindingTool.Reset();
        Spacebox.Game.GUI.PanelUI.ResetHandsBlock();
        if (_view != null && Camera.Main == _view) Camera.Main = _player;
        Console = null;
        _player = null;
        _viewCamera = null;
        _view = null;
        _wasBinding = false;
        IsUsingCamera = false;
    }

    public static void Update(LocalAstronaut player)
    {
        var console = Console;
        var entity = console?.Entity;
        if (entity == null || entity.IsDisposed || !console.IsPowered || !player.IsAlive)
        {
            Unmount(player);
            return;
        }

        if (_skipUseThisFrame)
        {
            _skipUseThisFrame = false;
        }
        else if (!UIManager.IsUIMode && Input.IsActionDown("use"))
        {
            if (ShipBindingTool.IsActive)
            {
                ShipBindingTool.TryUseDoor(player);
            }
            else
            {
                Unmount(player);
                return;
            }
        }

        Spacebox.Game.GUI.ShipConsoleUI.HandleInput();
        ShipBindingTool.Update(player);

        var bindings = entity.ShipControl.GetBindings(console.LocalCell);
        var command = new ConsoleCommand
        {
            AimLocal = entity.DirectionToEntitySpace(ViewFront(player)).Normalized(),
            AimPointLocal = entity.WorldToEntityLocal(FindAimPoint(player, entity)),
            PilotPeer = NetworkManager.Instance.LocalPeerId
        };

        if (!UIManager.IsUIMode && !ShipBindingTool.IsActive)
        {
            if (bindings != null) command.Buttons = bindings.ReadButtons(ShipKeys.IsHeld);
            if (console.Data.Flight) command.Flight = ReadFlight();
            else HandleViewRoll(player);
        }

        Spacebox.Network.ShipNetwork.SubmitCommand(console, command);
        UpdateView(player, console, bindings);
    }

    public static void UpdateViewCamera(LocalAstronaut player)
    {
        var entity = _viewCamera?.Entity;
        if (!IsUsingCamera || entity == null || _view == null) return;

        var delta = entity.Orientation * Quaternion.Invert(_lastViewOrientation);
        _lastViewOrientation = entity.Orientation;
        _view.SetRotation(delta * _view.GetRotation());
        if (player.CameraActive && !UIManager.IsUIMode) _view.Rotate();

        player.SetRotation(entity.Orientation * _seatRotationLocal);
        _view.Follow(player, entity.EntityLocalToWorld(_viewCamera.ViewPointLocal));
    }

    private static Vector3 ViewFront(LocalAstronaut player)
    {
        return IsUsingCamera && _view != null ? _view.Front : player.Front;
    }

    private static Vector3 FindAimPoint(LocalAstronaut player, SpaceEntity entity)
    {
        var origin = IsUsingCamera && _view != null ? _view.Position : player.Position;
        var front = ViewFront(player);
        var ray = new Ray(origin, front, AimDistance);
        var sector = World.CurrentSector;
        if (sector != null && sector.Raycast(ray, out var hit, entity)) return hit.hitPosition;
        return origin + front * AimDistance;
    }

    private static FlightInput ReadFlight()
    {
        var flight = FlightInput.None;
        if (Input.IsAction("forward")) flight |= FlightInput.Forward;
        if (Input.IsAction("backward")) flight |= FlightInput.Backward;
        if (Input.IsAction("left")) flight |= FlightInput.Left;
        if (Input.IsAction("right")) flight |= FlightInput.Right;
        if (Input.IsAction("up")) flight |= FlightInput.Up;
        if (Input.IsAction("down")) flight |= FlightInput.Down;
        if (Input.IsAction("rollLeft")) flight |= FlightInput.RollLeft;
        if (Input.IsAction("rollRight")) flight |= FlightInput.RollRight;
        if (Input.IsAction("sprint")) flight |= FlightInput.Brake;
        if (!Input.IsKey(Keys.LeftAlt)) flight |= FlightInput.Steer;
        return flight;
    }

    private static void HandleViewRoll(LocalAstronaut player)
    {
        float roll = RollSpeed * Time.Delta;
        Camera360Base view = IsUsingCamera && _view != null ? _view : player;
        if (Input.IsAction("rollLeft")) view.Roll(-roll);
        if (Input.IsAction("rollRight")) view.Roll(roll);
    }

    private static void UpdateView(LocalAstronaut player, ShipConsoleBlock console, ConsoleBindings bindings)
    {
        var entity = console.Entity;

        if (ShipBindingTool.IsActive)
        {
            if (!_wasBinding)
            {
                player.SetPosition(entity.EntityLocalToWorld(_seatLocal));
                player.InertiaController.Velocity = Vector3.Zero;
            }

            _wasBinding = true;
            SetViewCamera(null, player);
            IsUsingCamera = false;
            return;
        }

        if (_wasBinding)
        {
            _wasBinding = false;
            player.InertiaController.Velocity = Vector3.Zero;
            FaceConsole(player, console);
        }

        player.SetPosition(entity.EntityLocalToWorld(_seatLocal));

        var camera = bindings?.Camera is { } cell ? entity.ShipControl.GetPart<ShipCameraBlock>(cell) : null;
        if (camera != null && camera.IsPowered)
        {
            if (_viewCamera != camera)
            {
                if (!IsUsingCamera) _seatRotationLocal = Quaternion.Invert(entity.Orientation) * player.GetRotation();
                SetViewCamera(camera, player);
                _lastViewOrientation = entity.Orientation;
                _view.SetRotation(ShipAiming.LookRotation(
                    entity.DirectionToWorldSpace(GetCameraLookDirection(entity, bindings, camera)),
                    entity.DirectionToWorldSpace(camera.Frame.Up)));
            }

            IsUsingCamera = true;
            return;
        }

        SetViewCamera(null, player);
        IsUsingCamera = false;
    }

    private static void FaceConsole(LocalAstronaut player, ShipConsoleBlock console)
    {
        var entity = console.Entity;
        player.SetRotation(ShipAiming.LookRotation(entity.DirectionToWorldSpace(console.PilotForward), entity.DirectionToWorldSpace(console.PilotUp)));
    }

    private static Vector3 GetCameraLookDirection(SpaceEntity entity, ConsoleBindings bindings, ShipCameraBlock camera)
    {
        var sum = Vector3.Zero;
        foreach (var pair in bindings.Bound)
        {
            if (entity.ShipControl.GetPart<CannonBlock>(pair.Key) is { } cannon) sum += cannon.AimDirection;
        }

        return ShipAiming.SafeNormalize(sum, camera.Frame.Front);
    }

    private static void SetViewCamera(ShipCameraBlock camera, LocalAstronaut player)
    {
        if (_viewCamera != camera)
        {
            _viewCamera?.SetViewHidden(false);
            _viewCamera = camera;
            _viewCamera?.SetViewHidden(true);
        }

        if (camera != null)
        {
            _view ??= new ShipViewCamera();
            if (player != null && Camera.Main == player) Camera.Main = _view;
        }
        else if (_view != null && Camera.Main == _view)
        {
            Camera.Main = player ?? _player;
        }
    }
}
