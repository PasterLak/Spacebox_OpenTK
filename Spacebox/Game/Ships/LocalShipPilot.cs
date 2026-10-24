using Engine;
using Engine.Networking;
using Engine.Physics;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Player;

namespace Spacebox.Game.Ships;

public sealed class LocalShipPilot
{
    private const float AimDistance = 300f;
    private const float RollSpeed = 1000f;
    private const string SeatTakenMessage = "This console is already in use.";

    private readonly ShipRider _rider;
    private readonly ShipBindingTool _binding;
    private LocalAstronaut _player;
    private Vector3 _seatLocal;
    private bool _skipUseThisFrame;
    private bool _wasBinding;
    private ShipCameraBlock _viewCamera;
    private ShipViewCamera _view;
    private Quaternion _seatRotationLocal = Quaternion.Identity;
    private Quaternion _lastViewOrientation = Quaternion.Identity;

    public LocalShipPilot(ShipRider rider, ShipBindingTool binding)
    {
        _rider = rider;
        _binding = binding;
    }

    public ShipConsoleBlock Console { get; private set; }
    public bool IsPiloting => Console != null;
    public bool IsUsingCamera { get; private set; }
    public bool AllowsMovement => IsPiloting && _binding.IsActive;
    public bool IsSeated => IsPiloting && !_binding.IsActive;

    public event Action<ShipConsoleBlock> Mounted;
    public event Action<ShipConsoleBlock> Unmounted;

    public void Mount(ShipConsoleBlock console, LocalAstronaut player)
    {
        if (console?.Entity == null || player == null || !player.IsAlive) return;
        if (!Spacebox.Game.Protection.EntityProtection.CheckLocalUse(console.Entity)) return;
        if (IsPiloting) Unmount(player);
        if (!Spacebox.Network.ShipNetwork.TryMount(console))
        {
            Spacebox.Game.GUI.Chat.Write(SeatTakenMessage, Color4.Red);
            return;
        }

        Console = console;
        _player = player;
        _skipUseThisFrame = true;
        _wasBinding = false;
        IsUsingCamera = false;

        _rider.Board(player, console.Entity);
        _seatLocal = console.Entity.WorldToEntityLocal(player.Position);
        player.InertiaController.Velocity = Vector3.Zero;

        Spacebox.Game.GUI.PanelUI.BlockHands();
        Mounted?.Invoke(console);
    }

    public void Unmount(LocalAstronaut player)
    {
        var console = Console;
        if (console == null) return;

        if ((IsUsingCamera || _wasBinding) && console.Entity != null && player != null)
        {
            player.SetPosition(console.Entity.EntityLocalToWorld(_seatLocal));
            player.InertiaController.Velocity = Vector3.Zero;
        }

        SetViewCamera(null, player);
        Spacebox.Network.ShipNetwork.SendUnmount();
        _binding.Exit();
        Spacebox.Game.GUI.PanelUI.UnblockHands();
        Console = null;
        IsUsingCamera = false;
        _wasBinding = false;
        Unmounted?.Invoke(console);
    }

    public void OnConsoleUnavailable(ShipConsoleBlock console)
    {
        if (Console == console) Unmount(_player);
    }

    public void Reset()
    {
        _binding.Reset();
        Spacebox.Game.GUI.PanelUI.ResetHandsBlock();
        if (_view != null && Camera.Main == _view) Camera.Main = _player;
        Console = null;
        _player = null;
        _viewCamera = null;
        _view = null;
        _wasBinding = false;
        IsUsingCamera = false;
    }

    public void Update(LocalAstronaut player)
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
            if (!_binding.IsActive)
            {
                Unmount(player);
                return;
            }
        }

        Spacebox.Game.GUI.GameUi.Current?.ShipConsole.HandleInput();
        _binding.Update(player, console);

        var bindings = entity.ShipControl.GetBindings(console.LocalCell);
        var command = new ConsoleCommand
        {
            AimLocal = entity.DirectionToEntitySpace(ViewFront(player)).Normalized(),
            AimPointLocal = entity.WorldToEntityLocal(FindAimPoint(player, entity)),
            PilotPeer = NetworkManager.Instance.LocalPeerId
        };

        if (!UIManager.IsUIMode && !_binding.IsActive)
        {
            if (bindings != null) command.Buttons = bindings.ReadButtons(ShipKeys.IsHeld);
            if (console.Data.Flight) command.Flight = ReadFlight();
            else HandleViewRoll(player);
        }

        Spacebox.Network.ShipNetwork.SubmitCommand(console, command);
        UpdateView(player, console, bindings);
    }

    public void UpdateViewCamera(LocalAstronaut player)
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

    private Vector3 ViewFront(LocalAstronaut player)
    {
        return IsUsingCamera && _view != null ? _view.Front : player.Front;
    }

    private Vector3 FindAimPoint(LocalAstronaut player, SpaceEntity entity)
    {
        var origin = IsUsingCamera && _view != null ? _view.Position : player.Position;
        var front = ViewFront(player);
        var ray = new Ray(origin, front, AimDistance);
        var sector = World.CurrentSector;
        if (sector != null && sector.Raycast(ray, out var hit, entity)) return hit.hitPosition;
        return origin + front * AimDistance;
    }

    private FlightInput ReadFlight()
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

    private void HandleViewRoll(LocalAstronaut player)
    {
        float roll = RollSpeed * Time.Delta;
        Camera360Base view = IsUsingCamera && _view != null ? _view : player;
        if (Input.IsAction("rollLeft")) view.Roll(-roll);
        if (Input.IsAction("rollRight")) view.Roll(roll);
    }

    private void UpdateView(LocalAstronaut player, ShipConsoleBlock console, ConsoleBindings bindings)
    {
        var entity = console.Entity;

        if (_binding.IsActive)
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

    private Vector3 GetCameraLookDirection(SpaceEntity entity, ConsoleBindings bindings, ShipCameraBlock camera)
    {
        var sum = Vector3.Zero;
        foreach (var pair in bindings.Bound)
        {
            if (entity.ShipControl.GetPart<CannonBlock>(pair.Key) is { } cannon) sum += cannon.AimDirection;
        }

        return ShipAiming.SafeNormalize(sum, camera.Frame.Front);
    }

    private void SetViewCamera(ShipCameraBlock camera, LocalAstronaut player)
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
