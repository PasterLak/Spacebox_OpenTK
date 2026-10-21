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
using Spacebox.GUI;
using Spacebox.Network;

public class InteractionDestroyBlockSurvival : InteractionDestroyBlock
{
    private const float timeToDamage = 0.5f;
    private float _time = timeToDamage;
    private Block lastBlock = null;
    private static AudioSource drillAudio;
    private static AudioSource drill0Audio;

    private PointLight light;
    private BlockMiningEffect BlockMiningEffect;
    private const float TargetPowerInterval = 1f;
    private float _targetPowerTimer;
    public InteractionDestroyBlockSurvival(ItemSlot itemSlot) : base(itemSlot)
    {


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

       

        if (drillAudio == null || drillAudio.IsDisposed)
        {
            drillAudio = new AudioSource(Resources.Load<AudioClip>("drill1"));
            drillAudio.Volume = 1f;
            drillAudio.IsLooped = true;
        }
        if (drill0Audio == null || drill0Audio.IsDisposed)
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
        {
            BlockMiningEffect.Enabled = false;
            BlockMiningEffect.ClearParticles();
        }
        model.SetAnimation(false);

        drillAudio.Stop();
        drill0Audio.Stop();
        light.Enabled = false;
        light.Destroy();
        BlockSelector.IsVisible = false;
        light = null;
    }

    private void ProcessDestroying(HitInfo hit, byte power, LocalAstronaut astronaut, DrillItem drill)
    {
        if (hit.block == null) return;
        if (hit.block.Durability == 0)
        {
            DestroyBlock(hit, astronaut);
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

    private void DamageBlock(HitInfo hit, LocalAstronaut astronaut, DrillItem drill, byte damage = 1)
    {
        if (hit.chunk != null && !Spacebox.Game.Protection.EntityProtection.CheckLocalEdit(hit.chunk.SpaceEntity, Spacebox.Game.Protection.ProtectionRules.CellOf(hit.chunk, hit.blockPositionIndex))) return;
        hit.block.Durability = BlockDamage.Apply(hit.block.Durability, damage);
        if (hit.chunk != null) hit.chunk.IsModified = true;
        astronaut.PlayerStatistics.BlockDamageDealt += damage;
        if (hit.block.Durability <= 0)
        {
            DestroyBlock(hit, astronaut);
            astronaut.PowerBar.StatsData.Decrement(drill.PowerUsage);
        }
        else
        {
            WorldEditNetwork.NotifyDamaged(hit.chunk, hit.blockPositionIndex, hit.block.Id, damage);
        }

    }

    private void UpdateTargetDrilling(LocalAstronaut player)
    {
        lastBlock = null;
        if (!Input.IsAction("block_destroy")) return;

        ConsumeTargetPower(player);

        if (!model.Animator.IsActive)
            model.SetAnimation(true);
        light.Enabled = true;
        light.Position = player.Position;
    }

    private void ConsumeTargetPower(LocalAstronaut player)
    {
        _targetPowerTimer += Time.Delta;
        if (_targetPowerTimer < TargetPowerInterval) return;

        _targetPowerTimer -= TargetPowerInterval;
        player.PowerBar.StatsData.Decrement(drillItem.PowerUsage);
    }

    private void StopDrill()
    {
        model.SetAnimation(false);
        _time = timeToDamage;
        lastBlock = null;
        if (BlockMiningEffect != null)
        {
            BlockMiningEffect.ClearParticles();
            BlockMiningEffect.Enabled = false;
        }

        drillAudio.Stop();
        drill0Audio.Stop();
        light.Enabled = false;
    }

    public override void Update(LocalAstronaut player)
    {
        if(BlockMiningEffect==null)
        {
            BlockMiningEffect = World.Instance.BlockMiningEffect;
        }
        if (Input.IsMouseButtonUp(MouseButton.Left))
        {

            StopDrill();
        }

        if (!player.CanMove)
        {
            //if (Input.IsKeyDown(Keys.F) && lastInteractiveBlock != null) // Press F to interact  WTF
            //    lastInteractiveBlock.Use(player);
            model.SetAnimation(false);
            if (light != null)
                light.Enabled = false;
            return;
        }

        BlockUseTarget.Update(player);
        Ray ray = new Ray(player.Position, player.Front, drillItem.Range);


        if (drillItem.PowerUsage > player.PowerBar.StatsData.Value)
        {
            BlockSelector.IsVisible = false;
            StopDrill();
            return;
        }

        bool hasVoxelHit = World.CurrentSector.Raycast(ray, out HitInfo hit);

        if (TryDrillTarget(ray, player, hasVoxelHit, hit))
        {
            UpdateTargetDrilling(player);
            return;
        }

        if (hasVoxelHit)
        {
            UpdateBlockSelector(hit);
            if (lastBlock == null && hit.block != null)
                lastBlock = hit.block;
            if (Input.IsMouseButton(MouseButton.Left))
            {

                var blockData = GameAssets.GetBlockDataById(hit.block.Id);
                if (BlockMiningEffect != null)
                    BlockMiningEffect.Enabled = true;

                var effectPosition = hit.hitPosition + hit.normalWorld * 0.05f;
                var toPlayer = Vector3.Normalize(player.Position - effectPosition);

                if (BlockMiningEffect != null)
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
                if (drillItem != null)
                {
                    if (drillItem.Tier >= blockData.PowerToDrill)
                        ProcessDestroying(hit, drillItem.Tier, player, drillItem);
                    else
                        BlockMiningEffect.SetEmitter(false);
                }
            }
            if (Input.IsMouseButtonDown(MouseButton.Left))
            {
                if (drillItem.PowerUsage > player.PowerBar.StatsData.Value) return;

                model.SetAnimation(true);
                BlockMiningEffect.Enabled = true;
                if (!light.Enabled)
                    light.Enabled = true;
            }

        }
        else
        {
            if (BlockMiningEffect != null)
                BlockMiningEffect.Enabled = false;
            BlockSelector.IsVisible = false;
             CenteredText.Hide();
            if (Input.IsMouseButtonDown(MouseButton.Left))
                model.SetAnimation(true);
            if (Input.IsMouseButton(MouseButton.Left))
                light.Position = player.Position;
        }
    }
}
