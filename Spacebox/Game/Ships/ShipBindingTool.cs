using Engine;
using Engine.Physics;
using ImGuiNET;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Doors;
using Spacebox.Game.Player.Interactions;
using Spacebox.Game.Player;
using Spacebox.Network;

namespace Spacebox.Game.Ships;

public static class ShipBindingTool
{
    private const float Reach = 48f;
    private const float Thin = 0.035f;
    private const float Thick = 0.05f;
    private const float HoverGrow = 0.25f;

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
        if (_hovered == null) return;

        int direction = ReadCycleDirection();
        if (direction == 0) return;

        if (_hovered is ShipCameraBlock)
            ShipNetwork.RequestToggleCamera(console.Entity, console.LocalCell, _hovered.LocalCell);
        else
            ShipNetwork.RequestCycleBinding(console.Entity, console.LocalCell, _hovered.LocalCell, direction);

        PlayerActivity.Raise(PlayerActivityKind.ShipPartBound);
    }

    public static void DrawLabels()
    {
        var console = ShipPilot.Console;
        var entity = console?.Entity;
        var camera = Camera.Main;
        if (!IsActive || entity == null || camera == null) return;

        var bindings = entity.ShipControl.GetBindings(console.LocalCell);
        if (bindings == null) return;

        var drawList = ImGui.GetBackgroundDrawList();
        foreach (var pair in bindings.Bound)
        {
            var part = entity.ShipControl.GetPart<IShipPart>(pair.Key);
            var color = ConsoleBindings.GroupColors[pair.Value % ConsoleBindings.GroupColors.Length];
            DrawLabel(drawList, camera, part, ShipKeys.GetName(bindings.GetKey(pair.Value)), color);
        }

        if (bindings.Camera is { } cameraCell)
            DrawLabel(drawList, camera, entity.ShipControl.GetPart<IShipPart>(cameraCell), "CAM", CameraColor);
    }

    private static void DrawLabel(ImDrawListPtr drawList, Camera camera, IShipPart part, string text, Color4 color)
    {
        if (part?.Entity == null) return;

        var screen = camera.WorldToScreenPoint(part.Entity.EntityLocalToWorld(part.Frame.Center));
        if (screen == Vector2.Zero) return;

        var size = ImGui.CalcTextSize(text);
        var min = new System.Numerics.Vector2(screen.X - size.X * 0.5f - 4f, screen.Y - size.Y * 0.5f - 2f);
        var max = new System.Numerics.Vector2(screen.X + size.X * 0.5f + 4f, screen.Y + size.Y * 0.5f + 2f);
        drawList.AddRectFilled(min, max, ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0f, 0f, 0f, 0.6f)), 3f);
        drawList.AddText(new System.Numerics.Vector2(min.X + 4f, min.Y + 2f), ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(color.R, color.G, color.B, 1f)), text);
    }

    public static bool TryUseDoor(LocalAstronaut player)
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

        if (_hovered != null) AddOutline(_hovered, HoverColor, Thick, HoverGrow);
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

    private static void AddOutline(IShipPart part, Color4 color, float thickness, float grow = 0f)
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
