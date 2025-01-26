using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.Common.Audio;
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

            if(World.CurrentSector.Raycast(new Common.Physics.Ray(player.Position, player.Front, 1000), out var hit))
            {
                Debug.Log($"Hit: {hit.position}  chunk: {hit.chunk.PositionIndex}"  );
                DestroyBlock(hit);
            }
            else
            {
                Debug.Log($"Not gun hit!");
            }

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