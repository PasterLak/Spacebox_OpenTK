
using OpenTK.Mathematics;

using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Common.Audio;
using Spacebox.Common.Physics;

using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;

namespace Spacebox.Game.Player;

public class InteractionEraser : InteractionMode
{
    private const byte MaxBuildDistance = 6;

    private BoundingBoxRenderer boxRender;

    private CubeRenderer pointer;
    private CubeRenderer cube1;
    private CubeRenderer cube2;

    private Color4 leftColor = new Color3Byte(226, 87, 76).ToColor4();
    private Color4 rightColor = new Color3Byte(38, 166, 209).ToColor4();
    public override void OnEnable()
    {


        boxRender = new BoundingBoxRenderer(null);
        boxRender.Color = Color4.Red;
        boxRender.Thickness = 0.2f;
        boxRender.Enabled = false;

        cube1 = new CubeRenderer(new Vector3(0, 0, 0));
        cube1.Color = leftColor;
        cube1.Scale = Vector3.One * 1.05f;
        cube1.Enabled = false;

        cube2 = new CubeRenderer(new Vector3(0, 0, 0));
        cube2.Color = rightColor;
        cube2.Scale = Vector3.One * 1.05f;
        cube2.Enabled = false;


        pointer = new CubeRenderer(new Vector3(0, 0, 0));
        pointer.Color = Color4.White;
        pointer.Scale = Vector3.One * 1.05f;
        pointer.Enabled = true;
    }

    public override void OnDisable()
    {
        BlockSelector.IsVisible = false;

        if (BlockSelector.Instance != null)
        {

            BlockSelector.Instance.SimpleBlock.Scale = new Vector3(1f, 1f, 1f);
        }

        CreativeTools.Reset();
    }


    public override void Update(Astronaut player)
    {
        if (!player.CanMove)
        {
            BlockSelector.IsVisible = false;

            return;

        }
        Ray ray = new Ray(player.Position, player.Front, MaxBuildDistance);

        HitInfo hit;

        if (World.CurrentSector.Raycast(ray, out hit))
        {

            AImedBlockElement.AimedBlock = hit.block;

            Vector3 selectorPos = hit.blockPositionIndex + hit.chunk.PositionWorld;

            Logic(hit.chunk.SpaceEntity, selectorPos);
        }
        else

        {
            OnNoEntityFound(ray, player);
        }



    }


    private void Logic(SpaceEntity entity, Vector3 selectorPosition)
    {



        if (CreativeTools.Block2 != null)
        {
            pointer.Enabled = selectorPosition != CreativeTools.Block2.worldPosition;
        }
        else
        {
            pointer.Color = rightColor;
        }
        if (CreativeTools.Block1 != null)
        {

            pointer.Enabled = selectorPosition != CreativeTools.Block1.worldPosition;
        }
        else
        {

            pointer.Color = leftColor;

        }

        pointer.Position = selectorPosition + Vector3.One * 0.5f;



        if (Input.IsMouseButtonDown(MouseButton.Middle))
        {
            boxRender.Enabled = false;
            CreativeTools.Reset();

            cube1.Enabled = false;
            cube2.Enabled = false;
        }

        if (Input.IsKeyDown(Keys.Enter))
        {
            CreativeTools.DeleteBlocks();
            boxRender.Enabled = false;

            cube1.Enabled = false;
            cube2.Enabled = false;
        }
        if (Input.IsMouseButtonDown(MouseButton.Right))
        {
            var id = PanelUI.CurrentSlot().Item.Id;
            BlockPointer p = new BlockPointer(id, entity, (Vector3)entity.WorldPositionToLocal(selectorPosition));

            CreativeTools.SetPoint2(p);

            if (CreativeTools.Block2 != null)
            {
                cube2.Enabled = true;
                cube2.Position = selectorPosition + Vector3.One * 0.5f;
            }
            else
            {
                cube2.Enabled = false;
            }
            boxRender.BoundingBox = CreativeTools.GetBoundingBox();

            if (boxRender.BoundingBox != null) boxRender.Enabled = true;
            else boxRender.Enabled = false;

        }

        if (Input.IsMouseButtonDown(MouseButton.Left))
        {
            var id = PanelUI.CurrentSlot().Item.Id;
            BlockPointer p = new BlockPointer(id, entity, (Vector3)entity.WorldPositionToLocal(selectorPosition));

            CreativeTools.SetPoint1(p);

            if (CreativeTools.Block1 != null)
            {
                cube1.Enabled = true;
                cube1.Position = selectorPosition + Vector3.One * 0.5f;
            }
            else
            {
                cube1.Enabled = false;
            }



            boxRender.BoundingBox = CreativeTools.GetBoundingBox();

            if (boxRender.BoundingBox != null) boxRender.Enabled = true;
            else boxRender.Enabled = false;
        }
    }

    private void OnNoEntityFound(Ray ray, Astronaut player)
    {
        AImedBlockElement.AimedBlock = null;

        const float placeDistance = 5f;

        var selectorPosition = ray.Origin + ray.Direction * placeDistance;

        var localPos = selectorPosition;

        SpaceEntity entity = null;

        if (World.CurrentSector.TryGetNearestEntity(selectorPosition, out entity))
        {
            localPos = entity.WorldPositionToLocal(selectorPosition);

            localPos.X = (int)MathF.Floor(localPos.X);
            localPos.Y = (int)MathF.Floor(localPos.Y);
            localPos.Z = (int)MathF.Floor(localPos.Z);

            //pos += Vector3.One * 0.5f;

            selectorPosition = entity.LocalPositionToWorld(localPos);
        }


        var norm = (player.Position - selectorPosition).Normalized();

        norm = Block.RoundVector3(norm);

        var direction = Block.GetDirectionFromNormal(norm);


        if (entity != null)
        {

            Logic(entity, selectorPosition);

        }
    }

    public override void Render(Astronaut player)
    {
        boxRender.Render();
        cube1.Render();
        cube2.Render();
        pointer.Render();
    }
}