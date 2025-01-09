using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.Common.Audio;

using Spacebox.Game.Effects;


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
        if (!player.CanMove) return;

        if(Input.IsMouseButtonDown(0))
        {
            model?.SetAnimation(true);
        }
        if (Input.IsMouseButtonUp(0))
        {
            model?.SetAnimation(false);
        }

    }
}