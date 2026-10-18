using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Player;

namespace Spacebox.Game.Ships;

public static class ShipPilot
{
    private static LocalAstronaut _player;
    private static Vector3 _seatLocal;
    private static bool _skipUseThisFrame;

    public static ShipConsoleBlock Console { get; private set; }
    public static bool IsPiloting => Console != null;
    public static bool IsUsingCamera { get; private set; }

    public static event Action<ShipConsoleBlock> Mounted;
    public static event Action<ShipConsoleBlock> Unmounted;

    public static void Mount(ShipConsoleBlock console, LocalAstronaut player)
    {
        if (console?.Entity == null || player == null) return;
        if (IsPiloting) Unmount(player);

        Console = console;
        _player = player;
        _skipUseThisFrame = true;
        IsUsingCamera = false;

        ShipRider.Board(player, console.Entity);
        _seatLocal = console.Entity.WorldToEntityLocal(player.Position);
        player.InertiaController.Velocity = Vector3.Zero;

        Spacebox.Network.ShipNetwork.SendMount(console, true);
        Mounted?.Invoke(console);
    }

    public static void Unmount(LocalAstronaut player)
    {
        var console = Console;
        if (console == null) return;

        if (IsUsingCamera && console.Entity != null && player != null)
            player.SetPosition(console.Entity.EntityLocalToWorld(_seatLocal));

        Spacebox.Network.ShipNetwork.SendMount(console, false);
        ShipBindingTool.Exit();
        Console = null;
        IsUsingCamera = false;
        Unmounted?.Invoke(console);
    }

    public static void OnConsoleUnavailable(ShipConsoleBlock console)
    {
        if (Console == console) Unmount(_player);
    }

    public static void Reset()
    {
        ShipBindingTool.Reset();
        Console = null;
        _player = null;
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
            PilotPeer = NetworkManager.Instance.LocalPeerId
        };

        if (!UIManager.IsUIMode && !ShipBindingTool.IsActive)
        {
            if (bindings != null) command.Buttons = bindings.ReadButtons(ShipKeys.IsHeld);
            if (console.Data.Flight) command.Flight = ReadFlight();
        }

        Spacebox.Network.ShipNetwork.SubmitCommand(console, command);
        UpdateView(player, console, bindings);
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

    private static void UpdateView(LocalAstronaut player, ShipConsoleBlock console, ConsoleBindings bindings)
    {
        var entity = console.Entity;
        var camera = bindings?.Camera is { } cell ? entity.ShipControl.GetPart<ShipCameraBlock>(cell) : null;

        if (camera != null && camera.IsPowered)
        {
            player.SetPosition(entity.EntityLocalToWorld(camera.ViewPointLocal));
            IsUsingCamera = true;
            return;
        }

        IsUsingCamera = false;
        player.SetPosition(entity.EntityLocalToWorld(_seatLocal));
    }
}
