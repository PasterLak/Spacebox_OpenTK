using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Common.Physics;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.GUI;
using Spacebox.Scenes;

namespace Spacebox.Game.Player;

public class InteractionPlaceBlock : InteractionMode
{
    private const byte MaxBuildDistance = 6;

    public override void Update(Astronaut player)
    {
        //if (!Input.IsMouseButtonDown(MouseButton.Right)) return;

        Vector3 rayOrigin = player.Position;
        Vector3 rayDirection = player.Front;

        Ray ray = new Ray(rayOrigin, rayDirection, MaxBuildDistance);
        VoxelPhysics.HitInfo hit;

        if (World.CurrentSector.Raycast(ray, out hit))
        {
            //hit.chunk.RemoveBlock(hit.blockPosition, Vector3SByte.CreateFrom(hit.normal));
        }
        else
        {
            if (BlockSelector.IsVisible)
                BlockSelector.IsVisible = false;

            AImedBlockElement.AimedBlock = null;

            const float placeDistance = 5f;
            Vector3 placePosition = rayOrigin + rayDirection * placeDistance;
            
            BlockSelector.IsVisible = true;
            BlockSelector.Instance.UpdatePosition(placePosition, Direction.Up);
            /*
            Vector3 localPosition = placePosition - PositionWorld;

            int x = (int)MathF.Floor(localPosition.X);
            int y = (int)MathF.Floor(localPosition.Y);
            int z = (int)MathF.Floor(localPosition.Z);

            VisualDebug.DrawBoundingBox(
                new BoundingBox(worldBlockPosition + new Vector3(0.5f), Vector3.One * 1.01f), Color4.Gray);

            float dis = Vector3.DistanceSquared(worldBlockPosition, player.Position);

            if (PanelUI.IsHoldingBlock() && dis > Block.DiagonalSquared)
            {
                //Debug.Log("holding");
                var norm = (player.Position - worldBlockPosition).Normalized();

                norm = Block.RoundVector3(norm);

                BlockSelector.IsVisible = true;

                var direction = Block.GetDirectionFromNormal(norm);
                BlockSelector.Instance.UpdatePosition(worldBlockPosition, direction);


                if (Input.IsMouseButtonDown(MouseButton.Right) &&
                    IsInRange(x, y, z) &&
                    Blocks[x, y, z].IsAir())
                {
                    if (PanelUI.TryPlaceItem(out var blockId))
                    {
                        Block newBlock = GameBlocks.CreateBlockFromId(blockId);

                        bool hasSameSides = GameBlocks.GetBlockDataById(blockId).AllSidesAreSame;

                        if (!hasSameSides)
                            newBlock.Direction = direction;

                        SetBlock(x, y, z, newBlock);

                        SpaceScene.blockPlace.Play();
                    }
                    else
                    {
                    }
                }
            }
            else
            {
                BlockSelector.IsVisible = false;
            }

            if (CenteredText.IsVisible)
            {
                CenteredText.Hide();
            }*/
        }
    }
}