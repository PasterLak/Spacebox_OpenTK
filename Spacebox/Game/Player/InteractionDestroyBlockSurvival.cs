using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

using Engine.Animation;
using Engine.Audio;
using Engine.Physics;
using Engine.Light;
using Engine;
using Spacebox.Game.Effects;
using Spacebox.Game.Generation;
using Spacebox.Game.Physics;
using Spacebox.GUI;
using Client;


namespace Spacebox.Game.Player;

public class InteractionDestroyBlockSurvival : InteractionMode
{
    private const byte MaxDestroyDistance = 6;

    private static AudioSource blockDestroy;
    private static AudioSource drillAudio;
    private static AudioSource drill0Audio;

    private Block? lastBlock = null;

    private ItemSlot selectedItemSlot;
    private AnimatedItemModel model;
    public static BlockMiningEffect BlockMiningEffect;
    private InteractiveBlock lastInteractiveBlock;
    private PointLight light;
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

        light = PointLightsPool.Instance.Take();

        light.Range = 8;
        light.Ambient = new Color3Byte(100, 116, 255).ToVector3();
        light.Diffuse = new Color3Byte(100, 116, 255).ToVector3();
        light.Specular = new Color3Byte(0, 0, 0).ToVector3();
        light.IsActive = false;
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

        if (drillAudio == null)
        {
            drillAudio = new AudioSource(SoundManager.GetClip("drill1"));
            drillAudio.Volume = 1f;
            drillAudio.IsLooped = true;
        }
        if (drill0Audio == null)
        {
            drill0Audio = new AudioSource(SoundManager.GetClip("drill0"));
            drill0Audio.Volume = 1f;
            drill0Audio.IsLooped = true;
        }

        if (BlockSelector.Instance != null)
            BlockSelector.Instance.SimpleBlock.Shader.SetVector4("color", Vector4.One);
        BlockSelector.IsVisible = false;

        model.Animator.AddAnimation(new MoveAnimation(model.Position, model.Position + new Vector3(0.005f, 0, 0), 0.05f, true));
    }

    public override void OnDisable()
    {
        BlockSelector.IsVisible = false;
        lastBlock = null;
        selectedItemSlot = null;
        //BlockMiningEffect.Enabled = false;
        CenteredText.Hide();
        model.Animator.Clear();

        PointLightsPool.Instance.PutBack(light);
    }

    private const float timeToDamage = 0.5f;
    private float _time = timeToDamage;

    private void ProcessDestroying(HitInfo hit, byte power)
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

    private void DamageBlock(HitInfo hit, byte damage = 1)
    {
        int dam = (hit.block.Durability - damage);
        if (dam < 0) dam = 0;
        hit.block.Durability = (byte)(dam);

        if (hit.block.Durability <= 0) DestroyBlock(hit);
    }

    private void DestroyBlock(HitInfo hit)
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

        var x = (hit.chunk.PositionIndex * Chunk.Size);
        var localPos = new Vector3(x.X + hit.blockPositionIndex.X, x.Y + hit.blockPositionIndex.Y, x.Z + hit.blockPositionIndex.Z);
       

        if (ClientNetwork.Instance != null)
        {
            ClientNetwork.Instance.SendBlockDestroyed((short)localPos.X, (short)localPos.Y, (short)localPos.Z);
        }

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
            drillAudio.Stop();
            drill0Audio.Stop();
            light.IsActive = false;

        }

        if (!player.CanMove)
        {
            if (Input.IsKeyDown(Keys.F))
            {
                if (lastInteractiveBlock != null)
                    lastInteractiveBlock.Use(player);
            }
            model?.SetAnimation(false);

            light.IsActive = false;
            return;
        }


        Ray ray = new Ray(player.Position, player.Front, MaxDestroyDistance);
        HitInfo hit;

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

                if(!light.IsActive)
                {
                    light.IsActive=true;
                }
                light.Position = player.Position;
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
                if (!light.IsActive)
                {
                    light.IsActive = true;
                }

                //   drillAudio.Play();

            }


            lastInteractiveBlock = hit.block as InteractiveBlock;

            if (lastInteractiveBlock == null)
            {
                CenteredText.Hide();
                return;
            }

            if (Vector3.DistanceSquared(hit.position, player.Position) <= 3 * 3)
                InteractiveBlock.UpdateInteractive(lastInteractiveBlock, player, hit.chunk, hit.position);
        }
        else
        {
            BlockMiningEffect.Enabled = false;
            BlockSelector.IsVisible = false;

            CenteredText.Hide();
            if (Input.IsMouseButtonDown(MouseButton.Left))
            {
                model?.SetAnimation(true);

                light.IsActive = true;

                // drill0Audio.Play();

            }

            if (Input.IsMouseButton(MouseButton.Left))
            {
                light.Position = player.Position;
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