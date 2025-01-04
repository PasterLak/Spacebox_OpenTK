using OpenTK.Mathematics;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Common.Audio;
using Spacebox.Common.Physics;
using Spacebox.Game.Effects;
using Spacebox.Game.Generation;
using Spacebox.Game.Physics;

namespace Spacebox.Game.Player;

public class InteractionDestroyBlockSurvival : InteractionMode
{
    private const byte MaxDestroyDistance = 6;

    private static AudioSource blockDestroy;

    private Block? lastBlock = null;

    private ItemSlot selectedItemSlot;

    public static BlockMiningEffect BlockMiningEffect;
    public InteractionDestroyBlockSurvival(ItemSlot itemslot)
    {
        selectedItemSlot = itemslot;

        if (BlockMiningEffect == null)
        {
            var texture = TextureManager.GetTexture("Resources/Textures/blockHit.png", true);
           // texture
            BlockMiningEffect = new BlockMiningEffect(Camera.Main, Vector3.Zero, new Vector3(1, 1, 1),
                texture, ShaderManager.GetShader("Shaders/particle"));
        }
            
    }
    public override void OnEnable()
    {
        if (blockDestroy == null)
        {
            blockDestroy = new AudioSource(SoundManager.GetClip("blockDestroy"));
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
        BlockMiningEffect.Enabled = false;
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

        if(lastBlock != hit.block)
        {
            _time = timeToDamage;
        }

        _time -= Time.Delta;

        if(_time <= 0f) 
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
        hit.chunk.RemoveBlock(hit.blockPosition, hit.normal);

        if (blockDestroy != null)
        {

            blockDestroy.Play();
        }

        else Debug.Error("blockDestroy was null!");

        lastBlock = null;
        _time = timeToDamage;
    }

    public override void Update(Astronaut player)
    {
        if (!player.CanMove) return;
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

            Vector3 selectorPos = hit.blockPosition + hit.chunk.PositionWorld;
            BlockSelector.Instance.UpdatePosition(selectorPos,
                Block.GetDirectionFromNormal(hit.normal));

            if (Input.IsMouseButton(MouseButton.Left))
            {
                var item = selectedItemSlot.Item as DrillItem;

                var blockData = GameBlocks.GetBlockDataById(hit.block.BlockId);

                if(item != null)
                {
                    BlockMiningEffect.ParticleSystem.Position = hit.position + new Vector3(hit.normal.X, hit.normal.Y, hit.normal.Z) * 0.05f;
                    BlockMiningEffect.Update();

                   

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
                
                BlockMiningEffect.Enabled = true;
            }
                if (Input.IsMouseButtonUp(MouseButton.Left))
            {
                _time = timeToDamage;
                lastBlock = null;
                BlockMiningEffect.ClearParticles();
                BlockMiningEffect.Enabled = false;
            }

            /*if (Input.IsMouseButtonDown(MouseButton.Left))
            {
                hit.chunk.RemoveBlock(hit.blockPosition, hit.normal);

                if (blockDestroy != null)
                {

                    blockDestroy.Play();
                }

                else Debug.Error("blockDestroy was null!");
            }*/
        }
        else
        {
            BlockMiningEffect.Enabled = false;
            BlockSelector.IsVisible = false;
        }
    }
}