
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Common.Audio;
using Spacebox.Common.Physics;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;

namespace Spacebox.Game.Player;

public class InteractionPlaceBlock : InteractionMode
{
    private const byte MaxBuildDistance = 6;

    private static AudioSource blockPlace;
    private string lastBlockPlaceSound = "blockPlaceDefault";

    public override void OnEnable()
    {
        if (blockPlace == null)
            blockPlace = new AudioSource(SoundManager.GetClip("blockPlaceDefault"));

        if (BlockSelector.Instance != null)
            BlockSelector.Instance.SimpleBlock.Shader.SetVector4("color", new Vector4(1, 1, 1, 0.5f));
        BlockSelector.IsVisible = false;
    }

    public override void OnDisable()
    {
        BlockSelector.IsVisible = false;
    }

    private Vector3 UpdateBlockPreview(VoxelPhysics.HitInfo hit)
    {

        

        BlockSelector.IsVisible = true;
        var selectorPositionWorld = new Vector3(hit.blockPositionIndex.X + hit.normal.X,
            hit.blockPositionIndex.Y + hit.normal.Y,
            hit.blockPositionIndex.Z + hit.normal.Z) + hit.chunk.PositionWorld;
        BlockSelector.Instance.UpdatePosition(selectorPositionWorld, Block.GetDirectionFromNormal(hit.normal));

        return selectorPositionWorld;
    }

    const float MinDistanceToBlock = 1.37f * 1.37f;
    public override void Update(Astronaut player)
    {
        if (!player.CanMove)
        {
            BlockSelector.IsVisible = false;
            return;

        }
        Ray ray = new Ray(player.Position, player.Front, MaxBuildDistance);
        VoxelPhysics.HitInfo hit;

        if (World.CurrentSector.Raycast(ray, out hit))
        {
            

            var pos = hit.blockPositionIndex + hit.chunk.PositionWorld + hit.normal + new Vector3(0.5f,0.5f,0.5f);
            var disSqrt = Vector3.DistanceSquared(pos, player.Position);

            if (disSqrt > MinDistanceToBlock)
                OnEntityFound(hit);
            else BlockSelector.IsVisible = false;
        }
        else
        {
            OnNoEntityFound(ray);
        }
    }

    private void OnEntityFound(VoxelPhysics.HitInfo hit)
    {
        UpdateBlockPreview(hit);

        if (Input.IsMouseButtonDown(MouseButton.Right))
        {
            Chunk chunk = hit.chunk;

            if (chunk != null)
            {
                if (PanelUI.TryPlaceItem(out var id, GameMode))
                {
                    Block newBlock = GameBlocks.CreateBlockFromId(id);

                    bool hasSameSides = GameBlocks.GetBlockDataById(id).AllSidesAreSame;

                    if (!hasSameSides)
                        newBlock.SetDirectionFromNormal(hit.normal);

                    int x = hit.blockPositionIndex.X + hit.normal.X;
                    int y = hit.blockPositionIndex.Y + hit.normal.Y;
                    int z = hit.blockPositionIndex.Z + hit.normal.Z;

                    chunk.PlaceBlock(x, y, z, newBlock);


                    if (blockPlace != null)
                    {
                        PickPlaceSound(newBlock.BlockId);
                        blockPlace.Play();
                    }
                }
            }



        }
    }

    private void PickPlaceSound(short blockId)
    {
        var clip = GameBlocks.GetBlockAudioClipFromItemID(blockId, BlockInteractionType.Place);
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

    private void OnNoEntityFound(Ray ray)
    {
        AImedBlockElement.AimedBlock = null;
        BlockSelector.IsVisible = true;
        const float placeDistance = 5f;

        var selectorPosition = ray.Origin + ray.Direction * placeDistance;

        selectorPosition.X = (int)MathF.Floor(selectorPosition.X);
        selectorPosition.Y = (int)MathF.Floor(selectorPosition.Y);
        selectorPosition.Z = (int)MathF.Floor(selectorPosition.Z);

        BlockSelector.Instance.UpdatePosition(selectorPosition, Direction.Up);

        VisualDebug.DrawBoundingBox(
            new BoundingBox(selectorPosition, Vector3.One * 1.01f), Color4.Gray);
    }
}