using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Ships;

public sealed class ShipControlSystem
{
    private readonly SpaceEntity _entity;
    private readonly Dictionary<Vector3i, IShipPart> _parts = new Dictionary<Vector3i, IShipPart>();
    private readonly List<ThrusterBlock> _thrusters = new List<ThrusterBlock>();
    private readonly List<CannonBlock> _cannons = new List<CannonBlock>();
    private readonly Dictionary<Vector3i, ConsoleBindings> _bindings = new Dictionary<Vector3i, ConsoleBindings>();
    private readonly Dictionary<Vector3i, ConsoleCommand> _commands = new Dictionary<Vector3i, ConsoleCommand>();
    private readonly List<Vector3i> _commandKeys = new List<Vector3i>();

    public SpaceEntity Entity => _entity;
    public IReadOnlyList<ThrusterBlock> Thrusters => _thrusters;
    public IReadOnlyList<CannonBlock> Cannons => _cannons;
    public IReadOnlyDictionary<Vector3i, ConsoleCommand> Commands => _commands;
    public bool HasParts => _parts.Count > 0;

    public static event Action<ShipControlSystem, CannonBlock> CannonFired;
    public static event Action<ShipControlSystem, Vector3i> BindingsChanged;

    public ShipControlSystem(SpaceEntity entity)
    {
        _entity = entity;
    }

    public void Register(IShipPart part)
    {
        _parts[part.LocalCell] = part;

        switch (part)
        {
            case ThrusterBlock thruster:
                if (!_thrusters.Contains(thruster)) _thrusters.Add(thruster);
                break;
            case CannonBlock cannon:
                if (!_cannons.Contains(cannon)) _cannons.Add(cannon);
                break;
            case ShipConsoleBlock console:
                if (!_bindings.ContainsKey(console.LocalCell))
                    _bindings[console.LocalCell] = new ConsoleBindings(console.Data.Buttons, console.Data.Weapons);
                break;
        }
    }

    public void Unregister(IShipPart part)
    {
        if (_parts.TryGetValue(part.LocalCell, out var registered) && registered == part)
            _parts.Remove(part.LocalCell);

        switch (part)
        {
            case ThrusterBlock thruster:
                _thrusters.Remove(thruster);
                break;
            case CannonBlock cannon:
                _cannons.Remove(cannon);
                break;
            case ShipConsoleBlock console:
                _bindings.Remove(console.LocalCell);
                _commands.Remove(console.LocalCell);
                break;
        }

        foreach (var pair in _bindings)
        {
            if (pair.Value.Unbind(part.LocalCell)) NotifyBindingsChanged(pair.Key);
        }
    }

    public T GetPart<T>(Vector3i cell) where T : class, IShipPart
    {
        return _parts.TryGetValue(cell, out var part) ? part as T : null;
    }

    public ConsoleBindings GetBindings(Vector3i consoleCell)
    {
        return _bindings.TryGetValue(consoleCell, out var bindings) ? bindings : null;
    }

    public void SetBindings(Vector3i consoleCell, ConsoleBindings bindings, bool notify = true)
    {
        if (bindings == null) return;
        _bindings[consoleCell] = bindings;
        if (notify) NotifyBindingsChanged(consoleCell);
    }

    public bool CanBind(Vector3i consoleCell, IShipPart target)
    {
        var bindings = GetBindings(consoleCell);
        if (bindings == null || target == null || target.Entity != _entity) return false;
        if (target is ShipCameraBlock || target is ShipConsoleBlock) return false;
        return bindings.Weapons ? target is CannonBlock : target is ThrusterBlock || target is CannonBlock;
    }

    public int CycleBinding(Vector3i consoleCell, Vector3i targetCell)
    {
        var bindings = GetBindings(consoleCell);
        if (bindings == null || !CanBind(consoleCell, GetPart<IShipPart>(targetCell))) return -2;

        ReleaseFromOtherConsoles(consoleCell, targetCell);
        int group = bindings.Cycle(targetCell);
        NotifyBindingsChanged(consoleCell);
        return group;
    }

    public bool ToggleCamera(Vector3i consoleCell, Vector3i cameraCell)
    {
        var bindings = GetBindings(consoleCell);
        if (bindings == null || GetPart<ShipCameraBlock>(cameraCell) == null) return false;

        bindings.ToggleCamera(cameraCell);
        NotifyBindingsChanged(consoleCell);
        return true;
    }

    public void SetKey(Vector3i consoleCell, int group, int key)
    {
        var bindings = GetBindings(consoleCell);
        if (bindings == null) return;

        bindings.SetKey(group, key);
        NotifyBindingsChanged(consoleCell);
    }

    public void SetCommand(Vector3i consoleCell, ConsoleCommand command)
    {
        if (!_bindings.ContainsKey(consoleCell)) return;
        _commands[consoleCell] = command;
    }

    public void ClearCommand(Vector3i consoleCell)
    {
        _commands.Remove(consoleCell);
    }

    public void Update(float deltaTime)
    {
        if (_parts.Count == 0) return;

        for (int i = 0; i < _thrusters.Count; i++) _thrusters[i].IsFiring = false;
        for (int i = 0; i < _cannons.Count; i++) _cannons[i].TriggerHeld = false;

        _commandKeys.Clear();
        _commandKeys.AddRange(_commands.Keys);
        for (int i = 0; i < _commandKeys.Count; i++)
            ApplyCommand(_commandKeys[i], _commands[_commandKeys[i]]);

        ApplyThrust();
        UpdateCannons(deltaTime);
    }

    private void ApplyCommand(Vector3i consoleCell, ConsoleCommand command)
    {
        var console = GetPart<ShipConsoleBlock>(consoleCell);
        var bindings = GetBindings(consoleCell);
        if (console == null || bindings == null || !console.IsPowered) return;

        foreach (var pair in bindings.Bound)
        {
            var part = GetPart<IShipPart>(pair.Key);
            if (part is CannonBlock cannon)
            {
                if (command.AimLocal != Vector3.Zero) cannon.SetAimTarget(command.AimLocal);
                if (bindings.IsGroupPressed(pair.Value, command.Buttons))
                {
                    cannon.TriggerHeld = true;
                    cannon.ShooterPeer = command.PilotPeer;
                }
            }
            else if (part is ThrusterBlock thruster && bindings.IsGroupPressed(pair.Value, command.Buttons))
            {
                thruster.IsFiring = true;
            }
        }

        if (console.Data.Flight) ApplyFlight(console, command);
    }

    private void ApplyFlight(ShipConsoleBlock console, ConsoleCommand command)
    {
        var flight = command.Flight;
        var frame = new ConsoleFrame(console.PilotForward, console.PilotUp);
        var ship = _entity.Ship;

        var angularVelocity = ship?.Motion.AngularVelocity ?? Vector3.Zero;
        var velocityLocal = ship != null ? _entity.DirectionToEntitySpace(ship.Motion.Velocity) : Vector3.Zero;

        if ((flight & FlightInput.Steer) != 0 && command.AimLocal != Vector3.Zero)
            flight |= AutoFlight.Steer(command.AimLocal, angularVelocity, frame);

        if (flight == FlightInput.None) return;

        var com = ship?.CenterOfMassLocal ?? Vector3.Zero;
        var brake = AutoFlight.BrakeDirection(velocityLocal);

        for (int i = 0; i < _thrusters.Count; i++)
        {
            var thruster = _thrusters[i];
            var flags = AutoFlight.Classify(thruster.ThrustDirection, thruster.Frame.Center, com, frame);
            if (AutoFlight.ShouldFire(flags, flight, thruster.ThrustDirection, brake)) thruster.IsFiring = true;
        }
    }

    private void ApplyThrust()
    {
        var ship = _entity.Ship;
        if (ship == null) return;

        for (int i = 0; i < _thrusters.Count; i++)
        {
            var thruster = _thrusters[i];
            if (!thruster.IsFiring || !thruster.IsPowered) continue;
            ship.ApplyForceAtLocal(thruster.Frame.Center, thruster.ThrustDirection * thruster.Force);
        }
    }

    private void UpdateCannons(float deltaTime)
    {
        bool authoritative = !Spacebox.Network.GameSession.IsRemoteClient;

        for (int i = 0; i < _cannons.Count; i++)
        {
            var cannon = _cannons[i];
            if (cannon.UpdateCannon(deltaTime) && authoritative) CannonFired?.Invoke(this, cannon);
        }
    }

    private void ReleaseFromOtherConsoles(Vector3i consoleCell, Vector3i targetCell)
    {
        foreach (var pair in _bindings)
        {
            if (pair.Key != consoleCell && pair.Value.Unbind(targetCell)) NotifyBindingsChanged(pair.Key);
        }
    }

    private void NotifyBindingsChanged(Vector3i consoleCell)
    {
        if (_entity.TryGetBlockLocal(consoleCell, out var chunk, out _)) chunk.IsModified = true;
        BindingsChanged?.Invoke(this, consoleCell);
    }
}
