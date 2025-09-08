namespace Spacebox.Game.Player.Interactions;
using Engine;
using Engine.Animation;
using Engine.Audio;
using Engine.Physics;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation;
using Spacebox.Game.Physics;
using Engine.Light;
using Spacebox.Game.Effects;
using Spacebox.Game.Generation.Blocks;

public class InteractionDestroyBlockSurvival : InteractionDestroyBlock
{
    private const float timeToDamage = 0.5f;
    private float _time = timeToDamage;
    private Block lastBlock = null;
    private InteractiveBlock lastInteractiveBlock;
    private static AudioSource drillAudio;
    private static AudioSource drill0Audio;
    public static BlockMiningEffect BlockMiningEffect;
    private PointLight light;
    public InteractionDestroyBlockSurvival(ItemSlot itemSlot) : base(itemSlot)
    {
        if (BlockMiningEffect == null)
        {

            var texture = Resources.Load<Texture2D>("Resources/Textures/blockHit.png");
            texture.FilterMode = FilterMode.Nearest;
            BlockMiningEffect = new BlockMiningEffect(Camera.Main, Vector3.Zero, new Vector3(1, 1, 1), texture, Resources.Load<Shader>("Shaders/particle"));
        }
        var pool = PointLightsPool.Instance;

        light = new PointLight();
        light.Range = 8;

        var drill = itemSlot.Item as DrillItem;
        if (drill != null)
        {
            light.Diffuse = drill.DrillColor.ToVector3();
        }
        else
        {
            light.Diffuse = new Color3Byte(100, 116, 255).ToVector3();
        }

        light.Specular = new Color3Byte(0, 0, 0).ToVector3();
        light.Intensity = 1.5f;
        light.Enabled = false;


    }

    public override void OnEnable()
    {
        if (blockDestroy == null)
        {
            blockDestroy = new AudioSource(Resources.Load<AudioClip>("blockDestroyDefault"));
            blockDestroy.Volume = 1f;
        }
        if (drillAudio == null)
        {
            drillAudio = new AudioSource(Resources.Load<AudioClip>("drill1"));
            drillAudio.Volume = 1f;
            drillAudio.IsLooped = true;
        }
        if (drill0Audio == null)
        {
            drill0Audio = new AudioSource(Resources.Load<AudioClip>("drill0"));
            drill0Audio.Volume = 1f;
            drill0Audio.IsLooped = true;
        }
        if (BlockSelector.Instance != null)
            BlockSelector.Instance.SimpleBlock.Material.Shader.SetVector4("color", Vector4.One);
        BlockSelector.IsVisible = false;
        model.Animator.Clear();
        model.Animator.AddAnimation(new MoveAnimation(model.Position, model.Position + new Vector3(0.005f, 0, 0), 0.05f, true));
    }

    public override void OnDisable()
    {
        base.OnDisable();
        lastBlock = null;
        if (BlockMiningEffect != null)
            BlockMiningEffect.Enabled = false;
        model.SetAnimation(false);
        BlockMiningEffect.ClearParticles();
        drillAudio.Stop();
        drill0Audio.Stop();
        light.Enabled = false;
        light.Destroy();
        light = null;
    }

    private void ProcessDestroying(HitInfo hit, byte power, Astronaut astronaut, DrillItem drill)
    {
        if (hit.block == null) return;
        if (hit.block.Durability == 0)
        {
            DestroyBlock(hit);
            return;
        }
        BlockMiningEffect.SetEmitter(true);
        if (lastBlock == null || lastBlock != hit.block)
            _time = timeToDamage;
        lastBlock = hit.block;
        _time -= Time.Delta;
        if (_time <= 0f)
        {
            DamageBlock(hit, astronaut, drill, power);

            _time = timeToDamage;
        }
    }

    private void DamageBlock(HitInfo hit, Astronaut astronaut, DrillItem drill,byte damage = 1)
    {
        int dam = hit.block.Durability - damage;
        hit.block.Durability = (byte)(dam < 0 ? 0 : dam);
        if (hit.block.Durability <= 0)
        {
            DestroyBlock(hit);
            astronaut.PowerBar.StatsData.Decrement(drill.PowerUsage);
        }

    }

    private void StopDrill()
    {
        model.SetAnimation(false);
        _time = timeToDamage;
        lastBlock = null;
        BlockMiningEffect.ClearParticles();
        BlockMiningEffect.Enabled = false;
        drillAudio.Stop();
        drill0Audio.Stop();
        light.Enabled = false;
    }

    public override void Update(Astronaut player)
    {
        if (Input.IsMouseButtonUp(MouseButton.Left))
        {
            
            StopDrill();
        }

        if (!player.CanMove)
        {
            //if (Input.IsKeyDown(Keys.F) && lastInteractiveBlock != null) // Press F to interact  WTF
            //    lastInteractiveBlock.Use(player);
            model.SetAnimation(false);
            light.Enabled = false;
            return;
        }

        Ray ray = new Ray(player.Position, player.Front, MaxDestroyDistance);
        var drill = selectedItemSlot.Item as DrillItem;

        if(drill.PowerUsage > player.PowerBar.StatsData.Value)
        {
            BlockSelector.IsVisible = false;
            StopDrill();
            return;
        }

        if (World.CurrentSector.Raycast(ray, out HitInfo hit))
        {
            UpdateBlockSelector(hit);
            if (lastBlock == null && hit.block != null)
                lastBlock = hit.block;
            if (Input.IsMouseButton(MouseButton.Left))
            {
                
                var blockData = GameAssets.GetBlockDataById(hit.block.Id);
                BlockMiningEffect.Enabled = true;

                var effectPosition = hit.position + new Vector3(hit.normal.X, hit.normal.Y, hit.normal.Z) * 0.05f;
                var toPlayer = Vector3.Normalize(player.Position - effectPosition);

                BlockMiningEffect.ParticleSystem.Position = effectPosition;

                var cone = BlockMiningEffect.ParticleSystem.Emitter as ConeEmitter;

                if (cone != null)
                {
                    cone.Direction = toPlayer;
                }

                BlockMiningEffect.Update();
                if (!model.Animator.IsActive)
                    model.SetAnimation(true);
                if (!light.Enabled)
                    light.Enabled = true;
                light.Position = player.Position;
                if (drill != null)
                {
                    if (drill.Power >= blockData.PowerToDrill)
                        ProcessDestroying(hit, drill.Power, player, drill);
                    else
                        BlockMiningEffect.SetEmitter(false);
                }
            }
            if (Input.IsMouseButtonDown(MouseButton.Left))
            {
                if (drill.PowerUsage > player.PowerBar.StatsData.Value) return;

                model.SetAnimation(true);
                BlockMiningEffect.Enabled = true;
                if (!light.Enabled)
                    light.Enabled = true;
            }
            lastInteractiveBlock = hit.block as InteractiveBlock;
            if (lastInteractiveBlock != null)

            {
                InteractiveBlock.UpdateInteractive(lastInteractiveBlock, player, hit.chunk, hit.position);
                if (hit.block.Is<StorageBlock>(out var storageBlock))
                {
                    storageBlock.SetPositionInChunk(hit.blockPositionIndex);
                }
            }

        }
        else
        {
            BlockMiningEffect.Enabled = false;
            BlockSelector.IsVisible = false;
            // CenteredText.Hide();
            if (Input.IsMouseButtonDown(MouseButton.Left))
                model.SetAnimation(true);
            if (Input.IsMouseButton(MouseButton.Left))
                light.Position = player.Position;
        }
    }
}
