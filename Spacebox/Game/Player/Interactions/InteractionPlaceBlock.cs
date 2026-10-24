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
using Spacebox.Network;

namespace Spacebox.Game.Player.Interactions;

public class InteractionPlaceBlock : InteractionMode
{
    private const byte MaxBuildDistance = 6;

    private static AudioSource blockPlace;

    private LineRenderer lineRenderer;
    private bool _previewPlaceable = true;

    Random r = new Random();
    public override void OnEnable()
    {
        if (blockPlace == null || blockPlace.IsDisposed)
            blockPlace = new AudioSource(Resources.Load<AudioClip>("blockPlaceDefault"));

        if (BlockSelector.Instance != null)
            BlockSelector.Instance.SimpleBlock.Material.Shader.SetVector4("color", new Vector4(1, 1, 1, 0.5f));

        if(BlockSelector.Instance != null)
        BlockSelector.IsVisible = false;

        if (lineRenderer == null)
        {
            if (BlockSelector.Instance != null)
                lineRenderer = BlockSelector.Instance.LineRenderer;


        }
        else
        {
            lineRenderer.Enabled = true;
        }
    }
    public override void OnDisable()
    {
        if(BlockSelector.Instance!=null)
        BlockSelector.IsVisible = false;

        BlockSelector.Instance?.SetPlaceable(true);
        BlockSelector.Instance?.HideReservedArea();

        if (lineRenderer != null)
            lineRenderer.Enabled = false;
    }

    private static Vector3i TargetCell(HitInfo hit)
    {
        return new Vector3i(hit.blockPositionEntity.X + hit.normal.X,
            hit.blockPositionEntity.Y + hit.normal.Y,
            hit.blockPositionEntity.Z + hit.normal.Z);
    }

    private Vector3 UpdateBlockPreview(HitInfo hit, LocalAstronaut player)
    {

        BlockSelector.IsVisible = true;
        var entity = hit.chunk.SpaceEntity;
        var selectorPositionWorld = entity.CellCorner(TargetCell(hit));
        if (BlockSelector.Instance.CurrentBlockData == null) return selectorPositionWorld;

        BlockPlacementHelper.CalculateOrientation(entity.ToEntitySpace(player.PositionWorld), selectorPositionWorld, entity.DirectionToEntitySpace(player.Up),
                     hit.normal.ToVector3(), BlockSelector.Instance.EnableMagnet, BlockSelector.Instance.EnableMagnet, BlockSelector.Instance.CurrentBlockData, BlockSelector.Instance.Rotation, out var finalDir, out var finalRot);
        BlockSelector.Instance.SetSurfaceNormal(new Vector3i(hit.normal.X, hit.normal.Y, hit.normal.Z));
        BlockSelector.Instance.UpdatePosition(selectorPositionWorld, finalDir, finalRot, true, entity);

        var previewData = BlockSelector.Instance.CurrentBlockData;
        _previewPlaceable = entity.CanPlaceBlockAtCell(TargetCell(hit), previewData, finalDir, finalRot);
        BlockSelector.Instance.SetPlaceable(_previewPlaceable);

        UpdateReservedArea(entity, selectorPositionWorld, previewData, finalDir, finalRot, _previewPlaceable);
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
            var disSqrt = Vector3.DistanceSquared(pos, hit.chunk.SpaceEntity.ToEntitySpace(player.Position));

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
                var blockData = BlockSelector.Instance.CurrentBlockData;

                if (blockData != null && !_previewPlaceable)
                    return;

                if (PanelUI.TryPlaceItem(out var id))
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


                    BlockPlacementHelper.CalculateOrientation(chunk.SpaceEntity.ToEntitySpace(player.PositionWorld), selectorPos, chunk.SpaceEntity.DirectionToEntitySpace(player.Up),
                        hit.normal.ToVector3(), BlockSelector.Instance.EnableMagnet, BlockSelector.Instance.EnableMagnet,
                       blockData, cachedBlockRotation, out var finalDir, out var finalRot);

                    newBlock.Direction = finalDir;
                    newBlock.Rotation = finalRot;

                    var targetCell = TargetCell(hit);
                    if (Spacebox.Game.Protection.EntityProtection.CheckLocalEdit(chunk.SpaceEntity, targetCell) &&
                        Spacebox.Game.Protection.ClaimPlacement.CheckLocal(chunk.SpaceEntity, targetCell, newBlock) &&
                        !chunk.SpaceEntity.IsBlockAreaOccupiedByBodyAtCell(targetCell, newBlock) &&
                        chunk.SpaceEntity.TryPlaceBlockAtCell(targetCell, newBlock))
                    {
                        Spacebox.Game.Protection.ClaimPlacement.OnPlacedLocally(newBlock);
                        Spacebox.Game.Ships.ShipKeyMemory.OnPlacedLocally(newBlock);
                        WorldEditNetwork.NotifyPlacedAtCell(chunk.SpaceEntity, targetCell, newBlock);
                        player.PlayerStatistics.BlocksPlaced++;
                        PlayerActivity.RaiseBlock(PlayerActivityKind.BlockPlaced, newBlock.Id);
                    }
                    else
                    {
                        PlacementRefund.Refund(player, id);
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

    private static void UpdateReservedArea(SpaceEntity entity, Vector3 originPosition, BlockData blockData, Direction direction, Rotation rotation, bool placeable)
    {
        if (entity != null && blockData != null &&
            entity.TryGetReservedArea(originPosition, blockData, direction, rotation, out var min, out var size))
        {
            BlockSelector.Instance.ShowReservedArea(min, size, entity, placeable);
        }
        else
        {
            BlockSelector.Instance.HideReservedArea();
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
            blockPlace.Dispose();
        }

        blockPlace = new AudioSource(clip);
    }

    private void OnFreeSpace(Ray ray, LocalAstronaut player, Rotation cachedBlockRotation, float placeDistance)
    {
        var orientation = Spacebox.Game.Player.Building.PlayerAlignedFrame.FromView(player.Front, player.Up);
        var corner = Spacebox.Game.Player.Building.PlayerAlignedFrame.CornerForCenter(ray.Origin + ray.Direction * placeDistance, orientation);
        var playerLocal = Vector3.Transform(player.Position - corner, Quaternion.Invert(orientation));
        var blockData = BlockSelector.Instance.CurrentBlockData;

        BlockPlacementHelper.CalculateOrientation(playerLocal, Vector3.Zero, Vector3.UnitY, Vector3.Zero, false, false, blockData, cachedBlockRotation, out var finalDir, out var finalRot);

        BlockSelector.Instance.UpdateFreePosition(corner, orientation, finalDir, finalRot);
        bool placeable = blockData?.Shape?.RequiresSupport != true && !EntityCreationNetwork.IsBlockedByBody(corner, orientation);
        BlockSelector.Instance.SetPlaceable(placeable);
        BlockSelector.Instance.HideReservedArea();
        lineRenderer.Enabled = false;

        if (!placeable || !Input.IsActionDown("block_place") || !PanelUI.TryPlaceItem(out var id)) return;

        Block newBlock = GameAssets.CreateBlockFromId(id);
        newBlock.Direction = finalDir;
        newBlock.Rotation = finalRot;
        EntityCreationNetwork.PlaceNewEntity(World.CurrentSector, corner, newBlock, orientation);
        player.PlayerStatistics.BlocksPlaced++;
        PlayerActivity.RaiseBlock(PlayerActivityKind.BlockPlaced, newBlock.Id);

        if (blockPlace != null)
        {
            PickPlaceSound(newBlock.Id);
            blockPlace.Pitch = r.Next(9, 12) * 0.1f;
            blockPlace.Play();
        }
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
            localPos = entity.WorldPositionToLocal(entity.ToEntitySpace(selectorPosition));

            localPos.X = (int)MathF.Floor(localPos.X);
            localPos.Y = (int)MathF.Floor(localPos.Y);
            localPos.Z = (int)MathF.Floor(localPos.Z);

            //pos += Vector3.One * 0.5f;

            selectorPosition = entity.LocalPositionToWorld(localPos);
        }

        if (entity == null || !entity.IsPositionWithinEntitySize(selectorPosition) || !entity.IsWithinBuildRange(selectorPosition))
        {
            OnFreeSpace(ray, player, cachedBlockRotation, placeDistance);
            return;
        }


        var playerPosition = entity != null ? entity.ToEntitySpace(player.Position) : player.Position;
        var playerUp = entity != null ? entity.DirectionToEntitySpace(player.Up) : player.Up;

        var norm = (playerPosition - selectorPosition).Normalized();

        norm = Block.RoundVector3(norm);

        var direction = Block.GetDirectionFromNormal(norm);

        BlockPlacementHelper.CalculateOrientation(playerPosition, selectorPosition, playerUp,
                  new Vector3(0, 0, 0), false, false, BlockSelector.Instance.CurrentBlockData, cachedBlockRotation, out var finalDir, out var finalRot);

        if (BlockSelector.Instance != null)
            BlockSelector.Instance.UpdatePosition(selectorPosition, finalDir, finalRot, false, entity);

        var currentData = BlockSelector.Instance.CurrentBlockData;
        bool insideEntityRange = entity != null &&
                                 entity.IsPositionWithinEntitySize(selectorPosition) &&
                                 entity.IsWithinBuildRange(selectorPosition);
        bool structureBlocked = currentData != null && insideEntityRange &&
                                !entity.CanPlaceBlock(selectorPosition, currentData, finalDir, finalRot);
        BlockSelector.Instance.SetPlaceable(!structureBlocked);
        UpdateReservedArea(entity, selectorPosition, currentData, finalDir, finalRot, !structureBlocked);

        if (structureBlocked)
        {
            lineRenderer.Enabled = false;
            return;
        }

        if (entity != null)
        {
            if (insideEntityRange)
            {
                lineRenderer.Points[0] = entity.ToWorldSpace(selectorPosition + new Vector3(0.5f, 0.5f, 0.5f));
                lineRenderer.Points[1] = entity.CenterOfMassWorld;
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


                if (PanelUI.TryPlaceItem(out var id))
                {
                    Block newBlock = GameAssets.CreateBlockFromId(id);

                    if (newBlock.Is<StorageBlock>(out var storageBlock))
                    {
                        //storageBlock.SetPositionInEntity(entity.); 
                    }


                    newBlock.Direction = finalDir;
                    newBlock.Rotation = finalRot;


                    var cell = new Vector3i((int)localPos.X, (int)localPos.Y, (int)localPos.Z);
                    if (insideEntityRange &&
                        (!Spacebox.Game.Protection.EntityProtection.CheckLocalEdit(entity, cell) ||
                         !Spacebox.Game.Protection.ClaimPlacement.CheckLocal(entity, cell, newBlock)))
                    {
                        PlacementRefund.Refund(player, newBlock.Id);
                        return;
                    }

                    if (insideEntityRange && !entity.IsBlockAreaOccupiedByBodyAtCell(cell, newBlock) && entity.TryPlaceBlockAtCell(cell, newBlock))
                    {
                        Spacebox.Game.Protection.ClaimPlacement.OnPlacedLocally(newBlock);
                        Spacebox.Game.Ships.ShipKeyMemory.OnPlacedLocally(newBlock);
                        WorldEditNetwork.NotifyPlacedAtCell(entity, cell, newBlock);
                        player.PlayerStatistics.BlocksPlaced++;
                        PlayerActivity.RaiseBlock(PlayerActivityKind.BlockPlaced, newBlock.Id);
                    }
                    else if (GameAssets.GetBlockDataById(newBlock.Id)?.Shape?.RequiresSupport == true)
                    {
                        PlacementRefund.Refund(player, newBlock.Id);
                        return;
                    }
                    else
                    {
                        EntityCreationNetwork.PlaceNewEntity(World.CurrentSector, entity.ToWorldSpace(selectorPosition), newBlock, entity.Orientation);
                        player.PlayerStatistics.BlocksPlaced++;
                        PlayerActivity.RaiseBlock(PlayerActivityKind.BlockPlaced, newBlock.Id);
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