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

    public static ShipConsoleBlock Console { get; private set; }
    public static bool IsPiloting => Console != null;
    public static bool IsUsingCamera { get; private set; }
    public static bool AllowsMovement => IsPiloting && ShipBindingTool.IsActive;

    public static event Action<ShipConsoleBlock> Mounted;
    public static event Action<ShipConsoleBlock> Unmounted;

    public static void Mount(ShipConsoleBlock console, LocalAstronaut player)
    {
        if (console?.Entity == null || player == null) return;
        if (IsPiloting) Unmount(player);

        Console = console;
        _player = player;
        _skipUseThisFrame = true;
        _wasBinding = false;
        IsUsingCamera = false;

        ShipRider.Board(player, console.Entity);
        _seatLocal = console.Entity.WorldToEntityLocal(player.Position);
        player.InertiaController.Velocity = Vector3.Zero;

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

        SetViewCamera(null);
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
        Console = null;
        _player = null;
        _viewCamera = null;
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
            Unmount(player);
            return;
        }

        Spacebox.Game.GUI.ShipConsoleUI.HandleInput();
        ShipBindingTool.Update(player);

        var bindings = entity.ShipControl.GetBindings(console.LocalCell);
        var command = new ConsoleCommand
        {
            AimLocal = entity.DirectionToEntitySpace(player.Front).Normalized(),
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

    private static Vector3 FindAimPoint(LocalAstronaut player, SpaceEntity entity)
    {
        var ray = new Ray(player.Position, player.Front, AimDistance);
        var sector = World.CurrentSector;
        if (sector != null && sector.Raycast(ray, out var hit, entity)) return hit.hitPosition;
        return player.Position + player.Front * AimDistance;
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
        if (Input.IsAction("rollLeft")) player.Roll(-roll);
        if (Input.IsAction("rollRight")) player.Roll(roll);
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
            SetViewCamera(null);
            IsUsingCamera = false;
            return;
        }

        if (_wasBinding)
        {
            _wasBinding = false;
            player.InertiaController.Velocity = Vector3.Zero;
        }

        var camera = bindings?.Camera is { } cell ? entity.ShipControl.GetPart<ShipCameraBlock>(cell) : null;
        if (camera != null && camera.IsPowered)
        {
            if (_viewCamera != camera)
            {
                SetViewCamera(camera);
                player.SetRotation(ShipAiming.LookRotation(
                    entity.DirectionToWorldSpace(GetCameraLookDirection(entity, bindings, camera)),
                    entity.DirectionToWorldSpace(camera.Frame.Up)));
            }

            player.SetPosition(entity.EntityLocalToWorld(camera.ViewPointLocal));
            IsUsingCamera = true;
            return;
        }

        SetViewCamera(null);
        IsUsingCamera = false;
        player.SetPosition(entity.EntityLocalToWorld(_seatLocal));
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

    private static void SetViewCamera(ShipCameraBlock camera)
    {
        if (_viewCamera == camera) return;
        _viewCamera?.SetViewHidden(false);
        _viewCamera = camera;
        _viewCamera?.SetViewHidden(true);
    }
}
