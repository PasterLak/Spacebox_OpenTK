using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Common.Audio;
using Spacebox.Common.Physics;
using Spacebox.Game.Effects;
using Spacebox.Game.Generation;
using Spacebox.Game.Physics;
using Spacebox.GUI;

namespace Spacebox.Game.Player;

public class InteractionDestroyBlockSurvival : InteractionMode
{
    private const byte MaxDestroyDistance = 6;

    private static AudioSource blockDestroy;

    private Block? lastBlock = null;

    private ItemSlot selectedItemSlot;
    private AnimatedItemModel model;
    public static BlockMiningEffect BlockMiningEffect;
    private InteractiveBlock lastInteractiveBlock;
    public InteractionDestroyBlockSurvival(ItemSlot itemslot)
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
        if (blockDestroy == null)
        {
            blockDestroy = new AudioSource(SoundManager.GetClip("blockDestroyDefault"));
            blockDestroy.Volume = 1f;
        }

        if (BlockSelector.Instance != null)
            BlockSelector.Instance.SimpleBlock.Shader.SetVector4("color", Vector4.One);
        BlockSelector.IsVisible = false;
    }

    public override void OnDisable()
    {
        BlockSelector.IsVisible = false;
        lastBlock = null;
        selectedItemSlot = null;
        //BlockMiningEffect.Enabled = false;
        CenteredText.Hide();
    }

    private const float timeToDamage = 0.5f;
    private float _time = timeToDamage;

    private void ProcessDestroying(VoxelPhysics.HitInfo hit, byte power)
    {
        bool isNull = hit.block == null;

        if (isNull) return;

        if (hit.block.Durability == 0) DestroyBlock(hit);

        if (lastBlock == null)
            lastBlock = hit.block;

        lastBlock = hit.block;

        if (lastBlock != hit.block)
        {
            _time = timeToDamage;
        }

        _time -= Time.Delta;

        if (_time <= 0f)
        {
            DamageBlock(hit, power);
            _time = timeToDamage;
        }

    }

    private void DamageBlock(VoxelPhysics.HitInfo hit, byte damage = 1)
    {
        int dam = (hit.block.Durability - damage);
        if (dam < 0) dam = 0;
        hit.block.Durability = (byte)(dam);

        if (hit.block.Durability <= 0) DestroyBlock(hit);
    }

    private void DestroyBlock(VoxelPhysics.HitInfo hit)
    {
        if (hit.block != null && selectedItemSlot != null)
        {
            var b = hit.block as ResourceProcessingBlock;

            if (b != null && selectedItemSlot.Storage != null)
            {
                b.GiveAllResourcesBack(selectedItemSlot.Storage);
            }
        }
        hit.chunk.RemoveBlock(hit.blockPositionIndex, hit.normal);

        if (blockDestroy != null)
        {
            PickDestroySound(hit.block.BlockId);
            blockDestroy.Play();
        }

        else Debug.Error("blockDestroy was null!");

        lastBlock = null;
        _time = timeToDamage;
    }

    public override void Update(Astronaut player)
    {
        if (Input.IsMouseButtonUp(MouseButton.Left))
        {
            model?.SetAnimation(false);
            _time = timeToDamage;
            lastBlock = null;
            BlockMiningEffect.ClearParticles();
            BlockMiningEffect.Enabled = false;
        }

        if (!player.CanMove)
        {
            if (Input.IsKeyDown(Keys.F))
            {
                if (lastInteractiveBlock != null)
                    lastInteractiveBlock.Use(player);
            }
            model?.SetAnimation(false);
            return;
        }


        Ray ray = new Ray(player.Position, player.Front, MaxDestroyDistance);
        VoxelPhysics.HitInfo hit;

        if (World.CurrentSector.Raycast(ray, out hit))
        {
            BlockSelector.IsVisible = true;

            if (hit.block != null)
            {
                if (lastBlock == null)
                    lastBlock = hit.block;
            }

            Vector3 selectorPos = hit.blockPositionIndex + hit.chunk.PositionWorld;
            BlockSelector.Instance.UpdatePosition(selectorPos,
                Block.GetDirectionFromNormal(hit.normal));



            if (Input.IsMouseButton(MouseButton.Left))
            {
                var item = selectedItemSlot.Item as DrillItem;

                var blockData = GameBlocks.GetBlockDataById(hit.block.BlockId);
                BlockMiningEffect.Enabled = true;
                BlockMiningEffect.ParticleSystem.Position = hit.position + new Vector3(hit.normal.X, hit.normal.Y, hit.normal.Z) * 0.05f;
                BlockMiningEffect.Update();

                if (item != null)
                {




                    if (item.Power >= blockData.PowerToDrill)
                    {
                        BlockMiningEffect.SetEmitter(true);


                        ProcessDestroying(hit, item.Power);
                    }
                    else
                    {
                        //BlockMiningEffect.Enabled = false;
                        BlockMiningEffect.SetEmitter(false);
                    }


                }

            }

            if (Input.IsMouseButtonDown(MouseButton.Left))
            {
                model?.SetAnimation(true);
                BlockMiningEffect.Enabled = true;
            }


            lastInteractiveBlock = hit.block as InteractiveBlock;

            if (lastInteractiveBlock == null)
            {
                CenteredText.Hide();
                return;
            }

            if (Vector3.DistanceSquared(hit.position, player.Position) <= 3 * 3)
                UpdateInteractive(lastInteractiveBlock, player, hit.chunk, hit.position);
        }
        else
        {
            BlockMiningEffect.Enabled = false;
            BlockSelector.IsVisible = false;

            if (Input.IsMouseButtonDown(MouseButton.Left))
            {
                model?.SetAnimation(true);

            }

        }
    }

    private void UpdateInteractive(InteractiveBlock block, Astronaut player, Chunk chunk, Vector3 hitPos)
    {
        var disSq = Vector3.DistanceSquared(player.Position, hitPos);

        if (disSq > InteractiveBlock.InteractionDistanceSquared)
        {
            CenteredText.Hide();
        }
        else
        {
            CenteredText.Show();
            if (ToggleManager.OpenedWindowsCount == 0)
            {
                if (Input.IsMouseButtonDown(MouseButton.Right))
                {
                    block.chunk = chunk;
                    block.Use(player);

                }
            }
        }




    }

    private void PickDestroySound(short blockId)
    {
        var clip = GameBlocks.GetBlockAudioClipFromItemID(blockId, BlockInteractionType.Destroy);
        if (clip == null)
        {
            return;
        }

        if (blockDestroy != null && blockDestroy.Clip == clip)
        {
            return;
        }

        if (blockDestroy != null)
        {
            blockDestroy.Stop();
        }

        blockDestroy = new AudioSource(clip);
    }
}