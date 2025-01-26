using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.Common.Audio;
using Spacebox.Common.Physics;
using Spacebox.Game.Effects;
using Spacebox.Game.Generation;
using Spacebox.Game.Physics;

namespace Spacebox.Game.Player;

public class InteractionShoot : InteractionMode
{

    private static AudioSource blockDestroy;

    private ItemSlot selectedItemSlot;
    private AnimatedItemModel model;
    public static BlockMiningEffect BlockMiningEffect;

    public static LineRenderer lineRenderer;
    public InteractionShoot(ItemSlot itemslot)
    {
        selectedItemSlot = itemslot;
        AllowReload = true;
        if (BlockMiningEffect == null)
        {
            var texture = TextureManager.GetTexture("Resources/Textures/blockHit.png", true);
            // texture
            BlockMiningEffect = new BlockMiningEffect(Camera.Main, Vector3.Zero, new Vector3(1, 1, 1),
                texture, ShaderManager.GetShader("Shaders/particle"));
        }

        UpdateItemSlot(itemslot);

        lineRenderer = new LineRenderer();
        lineRenderer.Thickness = 0.02f;
        lineRenderer.Color = Color4.Blue;
        lineRenderer.Enabled = true;
    }

    public void UpdateItemSlot(ItemSlot itemslot)
    {
        selectedItemSlot = itemslot;
        var mod = GameBlocks.ItemModels[itemslot.Item.Id];
        model = mod as AnimatedItemModel;

    }
    public override void OnEnable()
    {

    }

    public override void OnDisable()
    {

        selectedItemSlot = null;
        //BlockMiningEffect.Enabled = false;
        model?.SetAnimation(false);

        lineRenderer.ClearPoints();
    }

    public override void Update(Astronaut player)
    {
        if (!player.CanMove)
        {
            model?.SetAnimation(false);
            return;
        }

        if (Input.IsMouseButtonDown(0))
        {
            model?.SetAnimation(true);
            lineRenderer.ClearPoints();


            lineRenderer.AddPoint(player.Position + player.Front);
            int count = 100;
            int count1 = 0;
            Ray ray = new Common.Physics.Ray(player.Position, player.Front, 1000);
            if (World.CurrentSector.Raycast(ray, out var hit))
            {

                lineRenderer.AddPoint(hit.position);
            
                while (count > 0)
                {
                    ray = ray.CalculateRicochetRay(hit, 1000);

                    if (World.CurrentSector.Raycast(ray, out  hit))
                    {
                        lineRenderer.AddPoint(hit.position);
                        count--;
                        count1++;
                    }
                    else
                    {
                        lineRenderer.AddPoint(ray.GetPoint(1000));
                        count = 0;
                        count1++;
                    }
                }
            }
            else
            {
                lineRenderer.AddPoint(ray.GetPoint(1000));
                // Debug.Log($"Not gun hit!");
            }

            // Debug.Log($"hits "+ count1);

        }
        if (Input.IsMouseButtonUp(0))
        {
            model?.SetAnimation(false);
        }

    }

    private void DestroyBlock(HitInfo hit)
    {

        hit.block.Durability = 0;
        hit.chunk.RemoveBlock(hit.blockPositionIndex, hit.normal);

    }

}