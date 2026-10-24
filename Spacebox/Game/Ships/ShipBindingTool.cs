using Engine;
using Engine.Physics;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Doors;
using Spacebox.Game.Player.Interactions;
using Spacebox.Game.Player;
using Spacebox.Network;

namespace Spacebox.Game.Ships;

public sealed class ShipBindingTool
{
    private const float Reach = 48f;
    private const float Thin = 0.035f;
    private const float Thick = 0.05f;
    private const float HoverGrow = 0.25f;

    private static readonly Color4 ConsoleColor = new Color4(0.2f, 0.6f, 1f, 1f);
    private static readonly Color4 CameraColor = new Color4(1f, 1f, 1f, 1f);
    private static readonly Color4 HoverColor = new Color4(1f, 1f, 0.6f, 1f);

    private ShipOutlineRenderer _outlines;
    private IShipPart _hovered;

    public bool IsActive { get; private set; }
    public IShipPart Hovered => _hovered;

    public void Toggle(ShipConsoleBlock console)
    {
        if (IsActive) Exit();
        else Enter(console);
    }

    public void Enter(ShipConsoleBlock console)
    {
        if (console == null) return;
        IsActive = true;
        _hovered = null;
    }

    public void Exit()
    {
        IsActive = false;
        _hovered = null;
    }

    public void Reset()
    {
        Exit();
        _outlines?.Dispose();
        _outlines = null;
    }

    public void Update(LocalAstronaut player, ShipConsoleBlock console)
    {
        if (!IsActive || console?.Entity == null)
        {
            Exit();
            return;
        }

        if (UIManager.IsUIMode) return;

        if (Input.IsActionDown("use"))
        {
            if (!TryUseDoor(player)) Exit();
            return;
        }

        _hovered = FindTarget(player, console.Entity);
        if (_hovered == null) return;

        int direction = ReadCycleDirection();
        if (direction == 0) return;

        if (_hovered is ShipCameraBlock)
            ShipNetwork.RequestToggleCamera(console.Entity, console.LocalCell, _hovered.LocalCell);
        else
            ShipNetwork.RequestCycleBinding(console.Entity, console.LocalCell, _hovered.LocalCell, direction);

        PlayerActivity.Raise(PlayerActivityKind.ShipPartBound);
    }

    public readonly record struct Label(Vector2 Screen, string Text, Color4 Color);

    public void CollectLabels(List<Label> labels, ShipConsoleBlock console)
    {
        labels.Clear();
        var entity = console?.Entity;
        var camera = Camera.Main;
        if (!IsActive || entity == null || camera == null) return;

        var bindings = entity.ShipControl.GetBindings(console.LocalCell);
        if (bindings == null) return;

        foreach (var pair in bindings.Bound)
        {
            var part = entity.ShipControl.GetPart<IShipPart>(pair.Key);
            var color = ConsoleBindings.GroupColors[pair.Value % ConsoleBindings.GroupColors.Length];
            AddLabel(labels, camera, part, ShipKeys.GetName(bindings.GetKey(pair.Value)), color);
        }

        if (bindings.Camera is { } cameraCell)
            AddLabel(labels, camera, entity.ShipControl.GetPart<IShipPart>(cameraCell), "CAM", CameraColor);
    }

    private static void AddLabel(List<Label> labels, Camera camera, IShipPart part, string text, Color4 color)
    {
        if (part?.Entity == null) return;

        var screen = camera.WorldToScreenPoint(part.Entity.EntityLocalToWorld(part.Frame.Center));
        if (screen == Vector2.Zero) return;
        labels.Add(new Label(screen, text, new Color4(color.R, color.G, color.B, 1f)));
    }

    private static bool TryUseDoor(LocalAstronaut player)
    {
        if (!BlockUseTarget.TryFind(player, out var block, out var hit) || block is not DoorBlock) return false;

        InteractiveBlock.UpdateInteractive(block, player, ref hit);
        return true;
    }

    private static int ReadCycleDirection()
    {
        if (Input.IsMouseButtonDown(MouseButton.Left)) return 1;

        float scroll = Input.MouseScrollDelta.Y;
        if (scroll > 0f) return 1;
        if (scroll < 0f) return -1;
        return 0;
    }

    public void Render(ShipConsoleBlock console)
    {
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

        if (_hovered != null) AddOutline(_hovered, HoverColor, Thick, HoverGrow);
        _outlines.Render();
    }

    public string DescribeHovered(ShipConsoleBlock console)
    {
        var bindings = console?.Entity?.ShipControl.GetBindings(console.LocalCell);
        if (_hovered == null || bindings == null) return null;

        if (_hovered is ShipCameraBlock)
            return bindings.Camera == _hovered.LocalCell ? "Camera: bound" : "Camera: not bound";

        if (!console.Entity.ShipControl.CanBind(console.LocalCell, _hovered)) return "Can't be bound to this console";

        int group = bindings.GetGroup(_hovered.LocalCell);
        return group < 0 ? "Not bound" : $"Group {group + 1} ({ShipKeys.GetName(bindings.GetKey(group))})";
    }

    private void AddOutline(IShipPart part, Color4 color, float thickness, float grow = 0f)
    {
        if (part?.Entity == null) return;
        _outlines.Add(part.Entity, part.Frame.Center, part.Frame.Size + new Vector3(grow), color, thickness);
    }

    private static IShipPart FindTarget(LocalAstronaut player, SpaceEntity entity)
    {
        var ray = new Ray(player.Position, player.Front, Reach);
        if (World.CurrentSector == null || !World.CurrentSector.Raycast(ray, out var hit) || hit.chunk == null || hit.chunk.SpaceEntity != entity)
            return null;

        return entity.GetStructureOrigin(hit.chunk, hit.blockPositionIndex) as IShipPart;
    }
}
