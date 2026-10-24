using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Ships;

namespace Spacebox.Network;

public struct ShipCommandMessage : INetMessage
{
    public long EntityId;
    public Vector3i ConsoleCell;
    public ushort Buttons;
    public ushort Flight;
    public Vector3 AimLocal;
    public Vector3 AimPointLocal;

    public void Serialize(NetWriter writer)
    {
        writer.Write(EntityId);
        writer.Write(ConsoleCell);
        writer.Write(Buttons);
        writer.Write(Flight);
        writer.Write(AimLocal);
        writer.Write(AimPointLocal);
    }

    public void Deserialize(NetReader reader)
    {
        EntityId = reader.ReadLong();
        ConsoleCell = reader.ReadVector3i();
        Buttons = reader.ReadUShort();
        Flight = reader.ReadUShort();
        AimLocal = reader.ReadVector3();
        AimPointLocal = reader.ReadVector3();
    }
}

public struct ShipMountMessage : INetMessage
{
    public long EntityId;
    public Vector3i ConsoleCell;
    public bool Mount;

    public void Serialize(NetWriter writer)
    {
        writer.Write(EntityId);
        writer.Write(ConsoleCell);
        writer.Write(Mount);
    }

    public void Deserialize(NetReader reader)
    {
        EntityId = reader.ReadLong();
        ConsoleCell = reader.ReadVector3i();
        Mount = reader.ReadBool();
    }
}

public struct ShipStateMessage : INetMessage
{
    public long EntityId;
    public Vector3 Position;
    public Quaternion Orientation;
    public Vector3 Velocity;
    public Vector3 AngularVelocity;
    public List<(Vector3i Cell, ConsoleCommand Command)> Commands;

    public void Serialize(NetWriter writer)
    {
        writer.Write(EntityId);
        writer.Write(Position);
        writer.Write(Orientation);
        writer.Write(Velocity);
        writer.Write(AngularVelocity);

        int count = Commands?.Count ?? 0;
        writer.Write((byte)Math.Min(count, byte.MaxValue));
        for (int i = 0; i < count && i < byte.MaxValue; i++)
        {
            writer.Write(Commands[i].Cell);
            writer.Write(Commands[i].Command.Buttons);
            writer.Write((ushort)Commands[i].Command.Flight);
            writer.Write(Commands[i].Command.AimLocal);
            writer.Write(Commands[i].Command.AimPointLocal);
        }
    }

    public void Deserialize(NetReader reader)
    {
        EntityId = reader.ReadLong();
        Position = reader.ReadVector3();
        Orientation = reader.ReadQuaternion();
        Velocity = reader.ReadVector3();
        AngularVelocity = reader.ReadVector3();

        int count = reader.ReadByte();
        Commands = new List<(Vector3i, ConsoleCommand)>(count);
        for (int i = 0; i < count; i++)
        {
            var cell = reader.ReadVector3i();
            var command = new ConsoleCommand
            {
                Buttons = reader.ReadUShort(),
                Flight = (FlightInput)reader.ReadUShort(),
                AimLocal = reader.ReadVector3(),
                AimPointLocal = reader.ReadVector3()
            };
            Commands.Add((cell, command));
        }
    }
}

public struct ShipBindingRequestMessage : INetMessage
{
    public const byte OperationCycle = 0;
    public const byte OperationCamera = 1;
    public const byte OperationKey = 2;
    public const byte OperationCycleBack = 3;

    public long EntityId;
    public Vector3i ConsoleCell;
    public byte Operation;
    public Vector3i TargetCell;
    public byte Group;
    public int Key;

    public void Serialize(NetWriter writer)
    {
        writer.Write(EntityId);
        writer.Write(ConsoleCell);
        writer.Write(Operation);
        writer.Write(TargetCell);
        writer.Write(Group);
        writer.Write(Key);
    }

    public void Deserialize(NetReader reader)
    {
        EntityId = reader.ReadLong();
        ConsoleCell = reader.ReadVector3i();
        Operation = reader.ReadByte();
        TargetCell = reader.ReadVector3i();
        Group = reader.ReadByte();
        Key = reader.ReadInt();
    }
}

public struct ShipBindingsMessage : INetMessage
{
    public long EntityId;
    public Vector3i ConsoleCell;
    public ConsoleBindings Bindings;

    public void Serialize(NetWriter writer)
    {
        writer.Write(EntityId);
        writer.Write(ConsoleCell);
        ConsoleBindingsCodec.Write(writer, Bindings);
    }

    public void Deserialize(NetReader reader)
    {
        EntityId = reader.ReadLong();
        ConsoleCell = reader.ReadVector3i();
        Bindings = ConsoleBindingsCodec.Read(reader);
    }
}

public struct ShipShotMessage : INetMessage
{
    public ShipShot Shot;

    public void Serialize(NetWriter writer)
    {
        writer.Write(Shot.CannonBlockId);
        writer.Write(Shot.EntityId);
        writer.Write(Shot.ShooterPeer);
        writer.Write(Shot.Origin);
        writer.Write(Shot.Direction);
        writer.Write(Shot.CarrierVelocity);
    }

    public void Deserialize(NetReader reader)
    {
        Shot = new ShipShot
        {
            CannonBlockId = reader.ReadShort(),
            EntityId = reader.ReadLong(),
            ShooterPeer = reader.ReadInt(),
            Origin = reader.ReadVector3(),
            Direction = reader.ReadVector3(),
            CarrierVelocity = reader.ReadVector3()
        };
    }
}

public static class ShipNetwork
{
    private const int StateTicksInterval = 1;

    private static readonly ShipPilotSeats _pilots = new ShipPilotSeats();
    private static readonly HashSet<long> _movingLastTick = new HashSet<long>();
    private static readonly HashSet<long> _movingThisTick = new HashSet<long>();
    private static readonly Dictionary<long, int> _restRepeats = new Dictionary<long, int>();
    private const int RestStateRepeats = 5;
    private static readonly List<(Vector3i, ConsoleCommand)> _commandScratch = new List<(Vector3i, ConsoleCommand)>();
    private static readonly List<Vector3i> _cellScratch = new List<Vector3i>();
    private static int _tickCounter;
    private static long _stateTick;
    private static readonly List<int> _peerIds = new List<int>();
    private static readonly List<Vector3> _peerPositions = new List<Vector3>();
    private static readonly List<bool> _peerKnown = new List<bool>();
    private static readonly List<int> _recipients = new List<int>();
    private static PilotSeat? _localSeat;

    public static void Register(NetworkManager network)
    {
        network.Messages.Register<ShipCommandMessage>(OnCommand);
        network.Messages.Register<ShipMountMessage>(OnMount);
        network.Messages.Register<ShipStateMessage>(OnState);
        network.Messages.Register<ShipBindingRequestMessage>(OnBindingRequest);
        network.Messages.Register<ShipBindingsMessage>(OnBindings);
        network.Messages.Register<ShipShotMessage>(OnShot);
        network.PeerDisconnected += OnPeerDisconnected;
        Players.AstronautNetwork.PlayerDied += OnPlayerDied;

        ShipWeapons.Register();
        ShipWeapons.ShotFired += OnShotFired;
        ShipControlSystem.BindingsChanged += OnBindingsChanged;
        ShipControlSystem.ConsoleRemoved += OnConsoleRemoved;
        Time.OnTick += OnTick;
    }

    public static void Reset()
    {
        _pilots.Clear();
        _localSeat = null;
        _movingLastTick.Clear();
        _movingThisTick.Clear();
        _restRepeats.Clear();
        _deferredStates.Clear();
        _commandThrottle.Reset();
    }

    private static readonly Dictionary<long, ShipStateMessage> _deferredStates = new Dictionary<long, ShipStateMessage>();

    private static readonly CommandSendThrottle _commandThrottle = new CommandSendThrottle();

    public static void SubmitCommand(ShipConsoleBlock console, ConsoleCommand command)
    {
        var entity = console?.Entity;
        if (entity == null) return;

        entity.ShipControl.SetCommand(console.LocalCell, command);

        var network = NetworkManager.Instance;
        if (!network.IsRemoteClient || !_commandThrottle.ShouldSend(command, Time.Total)) return;

        network.SendToServer(new ShipCommandMessage
        {
            EntityId = entity.EntityID,
            ConsoleCell = console.LocalCell,
            Buttons = command.Buttons,
            Flight = (ushort)command.Flight,
            AimLocal = command.AimLocal,
            AimPointLocal = command.AimPointLocal
        }, DeliveryMode.Sequenced);
    }

    public static bool TryMount(ShipConsoleBlock console)
    {
        var entity = console?.Entity;
        if (entity == null) return false;

        var seat = new PilotSeat(entity.EntityID, console.LocalCell);
        var network = NetworkManager.Instance;
        if (network.IsServer && !_pilots.TryTake(network.LocalPeerId, seat)) return false;

        _localSeat = seat;
        if (network.IsRemoteClient)
            network.SendToServer(new ShipMountMessage { EntityId = seat.EntityId, ConsoleCell = seat.Cell, Mount = true });
        return true;
    }

    public static void SendUnmount()
    {
        if (_localSeat is not { } seat) return;
        _localSeat = null;

        if (World.Instance != null && World.Instance.TryFindEntity(seat.EntityId, out var entity))
            entity.ShipControl.ClearCommand(seat.Cell);

        var network = NetworkManager.Instance;
        if (network.IsServer)
        {
            if (_pilots.IsPilotOf(network.LocalPeerId, seat)) _pilots.TryRelease(network.LocalPeerId, out _);
            return;
        }

        if (network.IsRemoteClient)
            network.SendToServer(new ShipMountMessage { EntityId = seat.EntityId, ConsoleCell = seat.Cell, Mount = false });
    }

    public static void RequestCycleBinding(SpaceEntity entity, Vector3i consoleCell, Vector3i targetCell, int direction = 1)
    {
        if (!NetworkManager.Instance.IsRemoteClient)
        {
            entity.ShipControl.CycleBinding(consoleCell, targetCell, direction);
            return;
        }

        byte operation = direction < 0 ? ShipBindingRequestMessage.OperationCycleBack : ShipBindingRequestMessage.OperationCycle;
        SendBindingRequest(entity, consoleCell, operation, targetCell, 0, 0);
    }

    public static void RequestToggleCamera(SpaceEntity entity, Vector3i consoleCell, Vector3i cameraCell)
    {
        if (!NetworkManager.Instance.IsRemoteClient)
        {
            entity.ShipControl.ToggleCamera(consoleCell, cameraCell);
            return;
        }

        SendBindingRequest(entity, consoleCell, ShipBindingRequestMessage.OperationCamera, cameraCell, 0, 0);
    }

    public static void RequestSetKey(SpaceEntity entity, Vector3i consoleCell, int group, int key)
    {
        if (!NetworkManager.Instance.IsRemoteClient)
        {
            entity.ShipControl.SetKey(consoleCell, group, key);
            return;
        }

        SendBindingRequest(entity, consoleCell, ShipBindingRequestMessage.OperationKey, Vector3i.Zero, (byte)group, key);
    }

    private static void SendBindingRequest(SpaceEntity entity, Vector3i consoleCell, byte operation, Vector3i target, byte group, int key)
    {
        NetworkManager.Instance.SendToServer(new ShipBindingRequestMessage
        {
            EntityId = entity.EntityID,
            ConsoleCell = consoleCell,
            Operation = operation,
            TargetCell = target,
            Group = group,
            Key = key
        });
    }

    private static void OnCommand(int sender, ShipCommandMessage message)
    {
        if (!NetworkManager.Instance.IsServer) return;
        if (!_pilots.IsPilotOf(sender, new PilotSeat(message.EntityId, message.ConsoleCell))) return;
        if (IsDead(sender))
        {
            ReleasePilot(sender);
            return;
        }
        if (!TryGetConsole(message.EntityId, message.ConsoleCell, out var entity, out _)) return;

        entity.ShipControl.SetCommand(message.ConsoleCell, new ConsoleCommand
        {
            Buttons = message.Buttons,
            Flight = (FlightInput)message.Flight,
            AimLocal = NetValidation.IsFinite(message.AimLocal) ? message.AimLocal : Vector3.Zero,
            AimPointLocal = NetValidation.IsFinite(message.AimPointLocal) ? message.AimPointLocal : Vector3.Zero,
            PilotPeer = sender
        });
    }

    private static void OnMount(int sender, ShipMountMessage message)
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer)
        {
            OnMountRefused(message);
            return;
        }

        ReleasePilot(sender);
        if (!message.Mount) return;
        if (CanMount(sender, message) && _pilots.TryTake(sender, new PilotSeat(message.EntityId, message.ConsoleCell))) return;

        network.Send(sender, new ShipMountMessage { EntityId = message.EntityId, ConsoleCell = message.ConsoleCell, Mount = false });
    }

    private static bool CanMount(int sender, ShipMountMessage message)
    {
        if (IsDead(sender)) return false;
        if (!TryGetConsole(message.EntityId, message.ConsoleCell, out var mountEntity, out var console) || !console.IsPowered) return false;
        if (ProtectionNetwork.BlocksPeerUse(sender, mountEntity)) return false;
        return WorldEditNetwork.IsInReach(sender, mountEntity, message.ConsoleCell);
    }

    private static void OnMountRefused(ShipMountMessage message)
    {
        if (message.Mount) return;

        var console = ShipPilot.Console;
        if (console?.Entity == null || console.Entity.EntityID != message.EntityId || console.LocalCell != message.ConsoleCell) return;

        ShipPilot.OnConsoleUnavailable(console);
    }

    private static bool IsDead(int peer)
    {
        return Players.AstronautNetwork.Find(peer) is { IsAlive: false };
    }

    private static void OnPlayerDied(int peer)
    {
        if (NetworkManager.Instance.IsServer) ReleasePilot(peer);
    }

    private static void OnPeerDisconnected(int peer, string reason)
    {
        if (NetworkManager.Instance.IsServer) ReleasePilot(peer);
    }

    private static void ReleasePilot(int peer)
    {
        if (!_pilots.TryRelease(peer, out var pilot)) return;

        if (World.Instance != null && World.Instance.TryFindEntity(pilot.EntityId, out var entity))
            entity.ShipControl.ClearCommand(pilot.Cell);
    }

    private static void OnConsoleRemoved(ShipControlSystem control, Vector3i consoleCell)
    {
        var network = NetworkManager.Instance;
        if (network == null || !network.IsServer) return;

        var seat = new PilotSeat(control.Entity.EntityID, consoleCell);
        while (_pilots.TryReleaseSeat(seat, out int peer))
        {
            if (peer == network.LocalPeerId || network.Mode == NetworkMode.Offline) continue;
            network.Send(peer, new ShipMountMessage { EntityId = seat.EntityId, ConsoleCell = seat.Cell, Mount = false });
        }
    }

    private static void OnBindingRequest(int sender, ShipBindingRequestMessage message)
    {
        if (!NetworkManager.Instance.IsServer) return;
        if (!TryGetConsole(message.EntityId, message.ConsoleCell, out var entity, out _)) return;
        if (ProtectionNetwork.BlocksPeerUse(sender, entity)) return;
        if (!_pilots.IsPilotOf(sender, new PilotSeat(message.EntityId, message.ConsoleCell)) && !WorldEditNetwork.IsInReach(sender, entity, message.ConsoleCell)) return;

        var control = entity.ShipControl;
        switch (message.Operation)
        {
            case ShipBindingRequestMessage.OperationCycle:
                control.CycleBinding(message.ConsoleCell, message.TargetCell);
                break;
            case ShipBindingRequestMessage.OperationCycleBack:
                control.CycleBinding(message.ConsoleCell, message.TargetCell, -1);
                break;
            case ShipBindingRequestMessage.OperationCamera:
                control.ToggleCamera(message.ConsoleCell, message.TargetCell);
                break;
            case ShipBindingRequestMessage.OperationKey:
                if (ShipKeys.IsValid(message.Key)) control.SetKey(message.ConsoleCell, message.Group, message.Key);
                break;
        }
    }

    private static void OnBindingsChanged(ShipControlSystem control, Vector3i consoleCell)
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer || network.Mode == NetworkMode.Offline) return;

        var bindings = control.GetBindings(consoleCell);
        if (bindings == null) return;

        network.SendToClients(new ShipBindingsMessage { EntityId = control.Entity.EntityID, ConsoleCell = consoleCell, Bindings = bindings });
    }

    private static void OnBindings(int sender, ShipBindingsMessage message)
    {
        if (NetworkManager.Instance.IsServer || message.Bindings == null) return;
        if (RemoteEntityBacklog.TryDefer(message.EntityId, () => OnBindings(sender, message))) return;
        if (World.Instance == null || !World.Instance.TryFindEntity(message.EntityId, out var entity)) return;

        entity.ShipControl.SetBindings(message.ConsoleCell, message.Bindings, false);
    }

    private static void OnShotFired(ShipShot shot)
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer || network.Mode == NetworkMode.Offline) return;

        network.SendToClients(new ShipShotMessage { Shot = shot }, DeliveryMode.ReliableUnordered);
    }

    private static void OnShot(int sender, ShipShotMessage message)
    {
        var network = NetworkManager.Instance;
        if (network.IsServer) return;

        ShipWeapons.Spawn(message.Shot, false, network.LatencyToServer);
    }

    private static void OnTick()
    {
        var network = NetworkManager.Instance;
        var world = World.Instance;
        if (network == null || world == null || !network.IsServer || network.Mode == NetworkMode.Offline) return;
        if (++_tickCounter < StateTicksInterval) return;
        _tickCounter = 0;
        _stateTick++;
        CollectPeerPositions(network);

        _movingThisTick.Clear();
        foreach (var sector in world.LoadedSectors)
        {
            var entities = sector.Entities;
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                var ship = entity.Ship;
                if (ship == null) continue;

                bool active = ship.Motion.IsMoving || (entity.HasShipControl && entity.ShipControl.Commands.Count > 0);
                long id = entity.EntityID;
                if (active)
                {
                    _movingThisTick.Add(id);
                    _restRepeats.Remove(id);
                    SendToRelevantPeers(network, entity);
                    continue;
                }
                else
                {
                    if (_movingLastTick.Contains(id)) _restRepeats[id] = RestStateRepeats;
                    if (!_restRepeats.TryGetValue(id, out int repeats)) continue;
                    if (repeats <= 1) _restRepeats.Remove(id);
                    else _restRepeats[id] = repeats - 1;
                }

                network.SendToClients(BuildState(entity), DeliveryMode.Sequenced);
            }
        }

        _movingLastTick.Clear();
        _movingLastTick.UnionWith(_movingThisTick);
    }

    private static void CollectPeerPositions(NetworkManager network)
    {
        network.CopyAdmittedPeers(_peerIds);
        _peerPositions.Clear();
        _peerKnown.Clear();
        for (int i = 0; i < _peerIds.Count; i++)
        {
            bool known = Players.PlayerLocator.TryGetPose(_peerIds[i], out var position, out _);
            _peerPositions.Add(position);
            _peerKnown.Add(known);
        }
    }

    private static void SendToRelevantPeers(NetworkManager network, SpaceEntity entity)
    {
        _recipients.Clear();
        var center = entity.CenterOfMassWorld;
        for (int i = 0; i < _peerIds.Count; i++)
        {
            int interval = _peerKnown[i]
                ? Ships.ShipStateRelevance.IntervalFor(Vector3.DistanceSquared(center, _peerPositions[i]))
                : Ships.ShipStateRelevance.NearInterval;
            if (Ships.ShipStateRelevance.IsDue(_stateTick, entity.EntityID, interval)) _recipients.Add(_peerIds[i]);
        }

        if (_recipients.Count > 0) network.SendToPeers(BuildState(entity), _recipients, DeliveryMode.Sequenced);
    }

    private static ShipStateMessage BuildState(SpaceEntity entity)
    {
        _commandScratch.Clear();
        if (entity.HasShipControl)
        {
            foreach (var pair in entity.ShipControl.Commands)
                _commandScratch.Add((pair.Key, pair.Value));
        }

        return new ShipStateMessage
        {
            EntityId = entity.EntityID,
            Position = entity.PositionWorld,
            Orientation = entity.Orientation,
            Velocity = entity.Ship.Motion.Velocity,
            AngularVelocity = entity.Ship.Motion.AngularVelocity,
            Commands = _commandScratch
        };
    }

    private static void OnState(int sender, ShipStateMessage message)
    {
        if (NetworkManager.Instance.IsServer) return;

        long id = message.EntityId;
        if (_deferredStates.ContainsKey(id))
        {
            var world = World.Instance;
            if (world == null || !world.TryFindEntity(id, out _))
            {
                if (world != null && world.IsEntityLoading(id)) _deferredStates[id] = message;
                else _deferredStates.Remove(id);
                return;
            }
            _deferredStates.Remove(id);
        }
        else if (RemoteEntityBacklog.TryDefer(id, () => ApplyDeferredState(id)))
        {
            _deferredStates[id] = message;
            return;
        }

        ApplyState(message);
    }

    private static void ApplyDeferredState(long entityId)
    {
        if (_deferredStates.Remove(entityId, out var latest)) ApplyState(latest);
    }

    private static void ApplyState(ShipStateMessage message)
    {
        if (World.Instance == null || !World.Instance.TryFindEntity(message.EntityId, out var entity) || entity.Ship == null) return;

        entity.Ship.ApplySnapshot(message.Position, message.Orientation, message.Velocity, message.AngularVelocity);

        var control = entity.ShipControl;
        var localConsole = ShipPilot.Console;
        _cellScratch.Clear();
        foreach (var pair in control.Commands) _cellScratch.Add(pair.Key);
        for (int i = 0; i < _cellScratch.Count; i++)
        {
            if (localConsole != null && localConsole.Entity == entity && localConsole.LocalCell == _cellScratch[i]) continue;
            control.ClearCommand(_cellScratch[i]);
        }

        if (message.Commands == null) return;
        for (int i = 0; i < message.Commands.Count; i++)
        {
            var (cell, command) = message.Commands[i];
            if (localConsole != null && localConsole.Entity == entity && localConsole.LocalCell == cell) continue;
            control.SetCommand(cell, command);
        }
    }

    private static bool TryGetConsole(long entityId, Vector3i cell, out SpaceEntity entity, out ShipConsoleBlock console)
    {
        console = null;
        entity = null;
        if (World.Instance == null || !World.Instance.TryFindEntity(entityId, out entity)) return false;

        console = entity.ShipControl.GetPart<ShipConsoleBlock>(cell);
        return console != null;
    }
}
