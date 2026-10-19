using OpenTK.Mathematics;

using OpenTK.Windowing.GraphicsLibraryFramework;

using Engine.Physics;
using Engine;
using Spacebox.Game.Effects;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;

namespace Spacebox.Game.Player.Interactions;

public class InteractionEraser : InteractionMode
{
    private const byte MaxBuildDistance = 6;
    private const float FreePointDistance = 5f;

    public override bool UsesPrimaryAction => true;

    private EntityBoxOutline selection;
    private EntityBoxOutline pointer;
    private EntityBoxOutline cube1;
    private EntityBoxOutline cube2;

    private Color4 leftColor = new Color3Byte(226, 87, 76).ToColor4();
    private Color4 rightColor = new Color3Byte(38, 166, 209).ToColor4();

    public override void OnEnable()
    {
        selection = new EntityBoxOutline(Color4.Red, 0.12f, 0.04f);
        cube1 = new EntityBoxOutline(leftColor, 0.08f, 0.06f);
        cube2 = new EntityBoxOutline(rightColor, 0.08f, 0.06f);
        pointer = new EntityBoxOutline(Color4.White, 0.05f, 0.02f);
    }

    public override void OnDisable()
    {
        BlockSelector.IsVisible = false;

        if (BlockSelector.Instance != null)
        {

            BlockSelector.Instance.SimpleBlock.Scale = new Vector3(1f, 1f, 1f);
        }

        CreativeTools.Reset();

        selection?.Dispose();
        cube1?.Dispose();
        cube2?.Dispose();
        pointer?.Dispose();
    }

    public override void Update(LocalAstronaut player)
    {
        if (!player.CanMove)
        {
            BlockSelector.IsVisible = false;
            pointer.Hide();
            UpdateSelectionVisuals();
            return;
        }

        Ray ray = new Ray(player.Position, player.Front, MaxBuildDistance);

        if (World.CurrentSector.Raycast(ray, out HitInfo hit))
        {
            AImedBlockElement.AimedBlock = hit.block;
            Logic(hit.chunk.SpaceEntity, GetEntityCell(hit));
        }
        else
        {
            OnNoEntityFound(ray);
        }

        UpdateSelectionVisuals();
    }

    private static Vector3i GetEntityCell(HitInfo hit)
    {
        var origin = hit.chunk.PositionIndex * Chunk.Size;
        return new Vector3i(origin.X + hit.blockPositionIndex.X, origin.Y + hit.blockPositionIndex.Y, origin.Z + hit.blockPositionIndex.Z);
    }

    private void Logic(SpaceEntity entity, Vector3i cell)
    {
        bool isPoint1 = IsSelected(CreativeTools.Block1, entity, cell);
        bool isPoint2 = IsSelected(CreativeTools.Block2, entity, cell);

        if (isPoint1 || isPoint2) pointer.Hide();
        else pointer.Show(entity, cell, cell);

        if (Input.IsMouseButtonDown(MouseButton.Middle))
        {
            CreativeTools.Reset();
        }

        if (Input.IsKeyDown(Keys.Enter))
        {
            CreativeTools.DeleteBlocks();
        }

        if (Input.IsMouseButtonDown(MouseButton.Right))
        {
            CreativeTools.SetPoint2(CreatePointer(entity, cell));
        }

        if (Input.IsMouseButtonDown(MouseButton.Left))
        {
            CreativeTools.SetPoint1(CreatePointer(entity, cell));
        }
    }

    private static BlockPointer CreatePointer(SpaceEntity entity, Vector3i cell)
    {
        return new BlockPointer(PanelUI.CurrentSlot().Item.Id, entity, cell);
    }

    private static bool IsSelected(BlockPointer point, SpaceEntity entity, Vector3i cell)
    {
        return point != null && point.spaceEntity == entity && point.Cell == cell;
    }

    private void UpdateSelectionVisuals()
    {
        ShowPoint(cube1, CreativeTools.Block1);
        ShowPoint(cube2, CreativeTools.Block2);

        if (CreativeTools.TryGetSelection(out var entity, out var min, out var max))
            selection.Show(entity, min, max);
        else
            selection.Hide();
    }

    private static void ShowPoint(EntityBoxOutline outline, BlockPointer point)
    {
        if (point?.spaceEntity == null) outline.Hide();
        else outline.Show(point.spaceEntity, point.Cell, point.Cell);
    }

    private void OnNoEntityFound(Ray ray)
    {
        AImedBlockElement.AimedBlock = null;

        var target = ray.Origin + ray.Direction * FreePointDistance;

        if (!World.CurrentSector.TryGetNearestEntity(target, out SpaceEntity entity))
        {
            pointer.Hide();
            return;
        }

        var local = entity.WorldToEntityLocal(target);
        var cell = new Vector3i((int)MathF.Floor(local.X), (int)MathF.Floor(local.Y), (int)MathF.Floor(local.Z));
        Logic(entity, cell);
    }

    public override void Render(LocalAstronaut player)
    {
        selection.Render();
        cube1.Render();
        cube2.Render();
        pointer.Render();
    }
}
