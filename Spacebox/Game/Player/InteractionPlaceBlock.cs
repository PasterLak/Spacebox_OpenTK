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

    private AudioSource blockPlace;

    public override void OnEnable()
    {
        if (blockPlace == null)
            blockPlace = new AudioSource(SoundManager.GetClip("blockPlace3"));
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
        var selectorPositionWorld = new Vector3(hit.blockPosition.X + hit.normal.X,
            hit.blockPosition.Y + hit.normal.Y,
            hit.blockPosition.Z + hit.normal.Z) + hit.chunk.PositionWorld;
        BlockSelector.Instance.UpdatePosition(selectorPositionWorld, Block.GetDirectionFromNormal(hit.normal));

        return selectorPositionWorld;
    }

    public override void Update(Astronaut player)
    {
        Ray ray = new Ray(player.Position, player.Front, MaxBuildDistance);
        VoxelPhysics.HitInfo hit;

        if (World.CurrentSector.Raycast(ray, out hit))
        {
            OnEntityFound(hit);
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

                    int x = hit.blockPosition.X + hit.normal.X;
                    int y = hit.blockPosition.Y + hit.normal.Y;
                    int z = hit.blockPosition.Z + hit.normal.Z;

                    chunk.PlaceBlock(x, y, z, newBlock);
                }
            }
            blockPlace?.Play();

        }
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