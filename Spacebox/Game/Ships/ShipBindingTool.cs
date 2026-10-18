using Engine;
using Engine.Physics;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Player;
using Spacebox.Network;

namespace Spacebox.Game.Ships;

public static class ShipBindingTool
{
    private const float Reach = 48f;
    private const float Thin = 0.035f;
    private const float Thick = 0.07f;

    private static readonly Color4 ConsoleColor = new Color4(0.2f, 0.6f, 1f, 1f);
    private static readonly Color4 CameraColor = new Color4(1f, 1f, 1f, 1f);
    private static readonly Color4 HoverColor = new Color4(1f, 1f, 0.6f, 1f);

    private static ShipOutlineRenderer _outlines;
    private static IShipPart _hovered;

    public static bool IsActive { get; private set; }
    public static IShipPart Hovered => _hovered;

    public static void Toggle()
    {
        if (IsActive) Exit();
        else Enter();
    }

    public static void Enter()
    {
        if (!ShipPilot.IsPiloting) return;
        IsActive = true;
        _hovered = null;
    }

    public static void Exit()
    {
        IsActive = false;
        _hovered = null;
    }

    public static void Reset()
    {
        Exit();
        _outlines?.Dispose();
        _outlines = null;
    }

    public static void Update(LocalAstronaut player)
    {
        var console = ShipPilot.Console;
        if (!IsActive || console?.Entity == null)
        {
            Exit();
            return;
        }

        if (UIManager.IsUIMode) return;

        if (Input.IsMouseButtonDown(MouseButton.Right))
        {
            Exit();
            return;
        }

        _hovered = FindTarget(player, console.Entity);
        if (_hovered == null || !Input.IsMouseButtonDown(MouseButton.Left)) return;

        if (_hovered is ShipCameraBlock)
            ShipNetwork.RequestToggleCamera(console.Entity, console.LocalCell, _hovered.LocalCell);
        else
            ShipNetwork.RequestCycleBinding(console.Entity, console.LocalCell, _hovered.LocalCell);
    }

    public static void Render()
    {
        var console = ShipPilot.Console;
        var entity = console?.Entity;
        if (!IsActive || entity == null) return;

        _outlines ??= new ShipOutlineRenderer();
        _outlines.Begin();

        var control = entity.ShipControl;
        var bindings = control.GetBindings(console.LocalCell);
        AddOutline(console, ConsoleColor, Thin);

        if (bindings != null)
        {
            foreach (var pair in bindings.Bound)
                AddOutline(control.GetPart<IShipPart>(pair.Key), ConsoleBindings.GroupColors[pair.Value % ConsoleBindings.GroupColors.Length], Thin);

            if (bindings.Camera is { } cameraCell)
                AddOutline(control.GetPart<IShipPart>(cameraCell), CameraColor, Thin);
        }

        if (_hovered != null) AddOutline(_hovered, HoverColor, Thick);
        _outlines.Render();
    }

    public static string DescribeHovered()
    {
        var console = ShipPilot.Console;
        var bindings = console?.Entity?.ShipControl.GetBindings(console.LocalCell);
        if (_hovered == null || bindings == null) return null;

        if (_hovered is ShipCameraBlock)
            return bindings.Camera == _hovered.LocalCell ? "Camera: bound" : "Camera: not bound";

        if (!console.Entity.ShipControl.CanBind(console.LocalCell, _hovered)) return "Can't be bound to this console";

        int group = bindings.GetGroup(_hovered.LocalCell);
        return group < 0 ? "Not bound" : $"Group {group + 1} ({ShipKeys.GetName(bindings.GetKey(group))})";
    }

    private static void AddOutline(IShipPart part, Color4 color, float thickness)
    {
        if (part?.Entity == null) return;
        _outlines.Add(part.Entity, part.Frame.Center, part.Frame.Size, color, thickness);
    }

    private static IShipPart FindTarget(LocalAstronaut player, SpaceEntity entity)
    {
        var ray = new Ray(player.Position, player.Front, Reach);
        if (World.CurrentSector == null || !World.CurrentSector.Raycast(ray, out var hit) || hit.chunk == null || hit.chunk.SpaceEntity != entity)
            return null;

        return entity.GetStructureOrigin(hit.chunk, hit.blockPositionIndex) as IShipPart;
    }
}
