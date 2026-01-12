using Client;
using Engine;
using Engine.Audio;
using Engine.Physics;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Resource;
using SpaceNetwork;

namespace Spacebox.Game.Player.Interactions;

public class InteractionPlaceBlock : InteractionMode
{
    private const byte MaxBuildDistance = 6;

    private static AudioSource blockPlace;

    private LineRenderer lineRenderer;

    Random r = new Random();
    public override void OnEnable()
    {
        if (blockPlace == null)
            blockPlace = new AudioSource(Resources.Load<AudioClip>("blockPlaceDefault"));

        if (BlockSelector.Instance != null)
            BlockSelector.Instance.SimpleBlock.Material.Shader.SetVector4("color", new Vector4(1, 1, 1, 0.5f));


        BlockSelector.IsVisible = false;

        if (lineRenderer == null)
        {
            if (World.Instance != null)
                lineRenderer = World.Instance.LineRenderer;


        }
        else
        {
            lineRenderer.Enabled = true;
        }
    }
    public override void OnDisable()
    {
        BlockSelector.IsVisible = false;

        if (lineRenderer != null)
            lineRenderer.Enabled = false;
    }

    private Vector3 UpdateBlockPreview(HitInfo hit, LocalAstronaut player)
    {
        BlockSelector.IsVisible = true;
        var selectorPositionWorld = new Vector3(hit.blockPositionIndex.X + hit.normal.X,
            hit.blockPositionIndex.Y + hit.normal.Y,
            hit.blockPositionIndex.Z + hit.normal.Z) + hit.chunk.PositionWorld;

        BlockPlacementHelper.CalculateOrientation(player.PositionWorld, selectorPositionWorld, player.Up,
                     hit.normal.ToVector3(), BlockSelector.Instance.EnableMagnet, BlockSelector.Instance.EnableMagnet,BlockSelector.Instance.CurrentBlockData, BlockSelector.Instance.Rotation, out var finalDir, out var finalRot);
        BlockSelector.Instance.UpdatePosition(selectorPositionWorld, finalDir, finalRot, true);
        lineRenderer.Enabled = false;
        /*
        lineRenderer.Points[0] = selectorPositionWorld + new Vector3(0.5f, 0.5f, 0.5f);
        lineRenderer.Points[1] = hit.chunk.SpaceEntity.CenterOfMass;
        lineRenderer.SetNeedsRebuild();
        lineRenderer.Enabled = true;
        */

        return selectorPositionWorld;
    }

    const float MinDistanceToBlock = 1.25f * 1.25f; // 1.37
    public override void Update(LocalAstronaut player)
    {
        if (!player.CanMove)
        {
            BlockSelector.IsVisible = false;
            lineRenderer.Enabled = false;
            return;

        }
        Ray ray = new Ray(player.Position, player.Front, MaxBuildDistance);
        HitInfo hit;

        if (World.CurrentSector != null && World.CurrentSector.Raycast(ray, out hit))
        {

            var pos = hit.blockPositionIndex + hit.chunk.PositionWorld + hit.normal + new Vector3(0.5f, 0.5f, 0.5f);
            var disSqrt = Vector3.DistanceSquared(pos, player.Position);

            /*if (Input.IsKeyDown(Keys.KeyPad1))
            {
                BlockPointer p = new BlockPointer(hit);

                CreativeTools.AddBlock(p);

                player.PlayerStatistics.BlocksPlaced++;
            }

            if (Input.IsKeyDown(Keys.KeyPad3))
            {
                CreativeTools.DeleteBlocks();

                player.PlayerStatistics.BlocksDestroyed++;
            }*/

            if (disSqrt > MinDistanceToBlock)
                OnEntityFound(hit, player);
            else
            {
                BlockSelector.IsVisible = false;
                lineRenderer.Enabled = false;
            }
        }
        else
        {
            OnNoEntityFound(ray, player);
        }
    }

    private void OnEntityFound(HitInfo hit, LocalAstronaut player)
    {
        var selectorPos = UpdateBlockPreview(hit, player);

        if (Input.IsActionDown("block_place"))
        {
            Chunk chunk = hit.chunk;

            if (chunk != null)
            {
                var cachedBlockRotation = BlockSelector.Instance.Rotation;

                if (PanelUI.TryPlaceItem(out var id, GameMode))
                {
                    Block newBlock = GameAssets.CreateBlockFromId(id);

                    if (newBlock.Is<StorageBlock>(out var storageBlock))
                    {


                        storageBlock.SetPositionInChunk(hit.blockPositionIndex);
                    }


                    // int x = hit.blockPositionIndex.X + hit.normal.X;
                    // int y = hit.blockPositionIndex.Y + hit.normal.Y;
                    // int z = hit.blockPositionIndex.Z + hit.normal.Z;

                    //chunk.PlaceBlock(x, y, z, newBlock);


                    BlockPlacementHelper.CalculateOrientation(player.PositionWorld, selectorPos, player.Up,
                        hit.normal.ToVector3(), BlockSelector.Instance.EnableMagnet, BlockSelector.Instance.EnableMagnet,
                        BlockSelector.Instance.CurrentBlockData, cachedBlockRotation, out var finalDir, out var finalRot);

                    newBlock.Direction = finalDir;
                    newBlock.Rotation = finalRot;

                    if (chunk.SpaceEntity.TryPlaceBlock(selectorPos, newBlock))
                    {
                        if (ClientNetwork.Instance != null)
                        {
                            var loc = chunk.SpaceEntity.WorldPositionToLocal(selectorPos);
                            ClientNetwork.Instance.SendBlockPlaced(newBlock, (short)loc.X, (short)loc.Y, (short)loc.Z);

                        }
                        player.PlayerStatistics.BlocksPlaced++;
                    }

                    if (blockPlace != null)
                    {
                        PickPlaceSound(newBlock.Id);
                        blockPlace.Pitch = r.Next(9, 12) * 0.1f;
                        blockPlace.Play();
                    }
                }
            }

        }
    }

    private void PickPlaceSound(short blockId)
    {
        var clip = GameAssets.GetBlockAudioClipFromItemID(blockId, BlockInteractionType.Place);
        if (clip == null)
        {
            return;
        }

        if (blockPlace != null && blockPlace.Clip == clip)
        {
            return;
        }

        if (blockPlace != null)
        {
            blockPlace.Stop();
        }

        blockPlace = new AudioSource(clip);
    }

    private void OnNoEntityFound(Ray ray, LocalAstronaut player)
    {
        AImedBlockElement.AimedBlock = null;
        BlockSelector.IsVisible = true;
        const float placeDistance = 5f;
        lineRenderer.Enabled = false;
        var cachedBlockRotation = BlockSelector.Instance.Rotation;

        var selectorPosition = ray.Origin + ray.Direction * placeDistance;

        var localPos = selectorPosition;

        SpaceEntity entity = null;

        if (World.CurrentSector != null && World.CurrentSector.TryGetNearestEntity(selectorPosition, out entity))
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

        BlockPlacementHelper.CalculateOrientation(player.PositionWorld, selectorPosition, player.Up,
                  new Vector3(0, 0, 0), false,false, BlockSelector.Instance.CurrentBlockData, cachedBlockRotation, out var finalDir, out var finalRot);

        if (BlockSelector.Instance != null)
            BlockSelector.Instance.UpdatePosition(selectorPosition, finalDir, finalRot, false);

        if (entity != null)
        {
            var dis = Vector3.DistanceSquared(selectorPosition, entity.CenterOfMass);

            if (dis <= entity.GravityRadius * entity.GravityRadius)
            {
                lineRenderer.Points[0] = selectorPosition + new Vector3(0.5f, 0.5f, 0.5f);
                lineRenderer.Points[1] = entity.CenterOfMass;
                lineRenderer.SetNeedsRebuild();
                lineRenderer.Enabled = true;
            }
            else
            {
                lineRenderer.Enabled = false;
            }

            /*if (Input.IsKeyDown(Keys.KeyPad1))
            {
                var id = PanelUI.CurrentSlot().Item.Id;
                BlockPointer p = new BlockPointer(id, entity, entity.WorldPositionToLocal(selectorPosition));

                CreativeTools.AddBlock(p);
                player.PlayerStatistics.BlocksPlaced++;
            }

            if (Input.IsKeyDown(Keys.KeyPad3))
            {
                CreativeTools.DeleteBlocks();
                player.PlayerStatistics.BlocksDestroyed++;
            }*/
        }
        //VisualDebug.DrawBoundingBox(
        //    new BoundingBox(selectorPosition, Vector3.One * 1.01f), Color4.Gray);

        if (Input.IsActionDown("block_place"))
        {
            if (entity != null)
            {


                if (PanelUI.TryPlaceItem(out var id, GameMode))
                {
                    Block newBlock = GameAssets.CreateBlockFromId(id);

                    if (newBlock.Is<StorageBlock>(out var storageBlock))
                    {
                        //storageBlock.SetPositionInEntity(entity.); 
                    }


                    newBlock.Direction = finalDir;
                    newBlock.Rotation = finalRot;


                    if (entity.TryPlaceBlock(selectorPosition, newBlock))
                    {
                        if (ClientNetwork.Instance != null)
                        {
                            var loc = entity.WorldPositionToLocal(selectorPosition);
                            ClientNetwork.Instance.SendBlockPlaced(newBlock, (short)loc.X, (short)loc.Y, (short)loc.Z);

                        }
                        player.PlayerStatistics.BlocksPlaced++;
                    }
                    else
                    {
                        var newEntity = World.CurrentSector.CreateNewEntity(selectorPosition);

                        newEntity.CreateFirstBlock(newBlock);
                        player.PlayerStatistics.BlocksPlaced++;
                    }


                    if (blockPlace != null)
                    {
                        PickPlaceSound(newBlock.Id);
                        blockPlace.Pitch = r.Next(9, 12) * 0.1f;
                        blockPlace.Play();
                    }
                }
            }
        }
    }
}