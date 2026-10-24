using OpenTK.Mathematics;

using OpenTK.Windowing.GraphicsLibraryFramework;

using Engine.Physics;
using Engine;
using Spacebox.Game.Effects;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI;
using Spacebox.GUI;
using Spacebox.Game.Physics;

namespace Spacebox.Game.Player.Interactions;

public abstract class InteractionAreaTool : InteractionMode
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

    protected InteractionAreaTool() : this(GameScene.Current?.Creative ?? new CreativeTools())
    {
    }

    protected InteractionAreaTool(CreativeTools tools)
    {
        Tools = tools;
    }

    protected CreativeTools Tools { get; }

    protected abstract void Confirm();

    private const float MessageDuration = 2f;

    private string _message;
    private float _messageTimer;

    protected virtual bool HandleToolInput(SpaceEntity entity, Vector3i cell, Vector3i normal, Block aimedBlock) => false;

    protected virtual void OnNoTarget()
    {
    }

    protected void ShowMessage(string message)
    {
        _message = message;
        _messageTimer = MessageDuration;
    }

    private void UpdateMessage()
    {
        if (_messageTimer <= 0f) return;

        _messageTimer -= Time.Delta;
        CenteredText.SetText(_message);
        CenteredText.Show();
    }

    protected virtual bool ShowsSelection => true;

    protected virtual void RenderTool()
    {
    }

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

        Tools.Reset();

        selection?.Dispose();
        cube1?.Dispose();
        cube2?.Dispose();
        pointer?.Dispose();
    }

    public override void Update(LocalAstronaut player)
    {
        UpdateMessage();
        if (!player.CanMove)
        {
            BlockSelector.IsVisible = false;
            pointer.Hide();
            OnNoTarget();
            UpdateSelectionVisuals();
            return;
        }

        Ray ray = new Ray(player.Position, player.Front, MaxBuildDistance);

        if (World.CurrentSector.Raycast(ray, out HitInfo hit))
        {
            AImedBlockElement.AimedBlock = hit.block;
            Logic(hit.chunk.SpaceEntity, GetEntityCell(hit), new Vector3i(hit.normal.X, hit.normal.Y, hit.normal.Z), hit.block);
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

    private void Logic(SpaceEntity entity, Vector3i cell, Vector3i normal, Block aimedBlock)
    {
        if (HandleToolInput(entity, cell, normal, aimedBlock))
        {
            pointer.Hide();
            return;
        }

        bool isPoint1 = IsSelected(Tools.Block1, entity, cell);
        bool isPoint2 = IsSelected(Tools.Block2, entity, cell);

        if (isPoint1 || isPoint2) pointer.Hide();
        else pointer.Show(entity, cell, cell);

        if (Input.IsMouseButtonDown(MouseButton.Middle))
        {
            Tools.Reset();
        }

        if (Input.IsKeyDown(Keys.Enter))
        {
            Confirm();
        }

        if (Input.IsMouseButtonDown(MouseButton.Right))
        {
            Tools.SetPoint2(CreatePointer(entity, cell));
        }

        if (Input.IsMouseButtonDown(MouseButton.Left))
        {
            Tools.SetPoint1(CreatePointer(entity, cell));
        }
    }

    protected static BlockPointer CreatePointer(SpaceEntity entity, Vector3i cell)
    {
        return new BlockPointer(PanelUI.CurrentSlot().Item.Id, entity, cell);
    }

    private static bool IsSelected(BlockPointer point, SpaceEntity entity, Vector3i cell)
    {
        return point != null && point.spaceEntity == entity && point.Cell == cell;
    }

    private void UpdateSelectionVisuals()
    {
        if (!ShowsSelection)
        {
            cube1.Hide();
            cube2.Hide();
            selection.Hide();
            return;
        }

        ShowPoint(cube1, Tools.Block1);
        ShowPoint(cube2, Tools.Block2);

        if (Tools.TryGetSelection(out var entity, out var min, out var max))
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
            OnNoTarget();
            return;
        }

        var local = entity.WorldToEntityLocal(target);
        var cell = new Vector3i((int)MathF.Floor(local.X), (int)MathF.Floor(local.Y), (int)MathF.Floor(local.Z));
        Logic(entity, cell, Vector3i.Zero, null);
    }

    public override void Render(LocalAstronaut player)
    {
        selection.Render();
        cube1.Render();
        cube2.Render();
        pointer.Render();
        RenderTool();
    }
}
