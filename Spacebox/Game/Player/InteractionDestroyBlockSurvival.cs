namespace Spacebox.Game.Player;
using Engine;
using Engine.Animation;
using Engine.Audio;
using Engine.Physics;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation;
using Spacebox.Game.Physics;
using Spacebox.GUI;
using Engine.Light;
using Spacebox.Game.Effects;

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
            var texture = TextureManager.GetTexture("Resources/Textures/blockHit.png", true);
            BlockMiningEffect = new BlockMiningEffect(Camera.Main, Vector3.Zero, new Vector3(1, 1, 1), texture, ShaderManager.GetShader("Shaders/particle"));
        }
        light = PointLightsPool.Instance.Take();
        light.Range = 8;
        light.Ambient = new Color3Byte(100, 116, 255).ToVector3();
        light.Diffuse = new Color3Byte(100, 116, 255).ToVector3();
        light.Specular = new Color3Byte(0, 0, 0).ToVector3();
        light.IsActive = false;
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
        model.Animator.Clear();
        model.Animator.AddAnimation(new MoveAnimation(model.Position, model.Position + new Vector3(0.005f, 0, 0), 0.05f, true));
    }

    public override void OnDisable()
    {
        base.OnDisable();
        lastBlock = null;
        BlockMiningEffect.Enabled = false;
        model.SetAnimation(false);
        BlockMiningEffect.ClearParticles();
        drillAudio.Stop();
        drill0Audio.Stop();
        light.IsActive = false;
        PointLightsPool.Instance.PutBack(light);
    }

    private void ProcessDestroying(HitInfo hit, byte power)
    {
        if (hit.block == null) return;
        if (hit.block.Durability == 0)
        {
            DestroyBlock(hit);
            return;
        }
        if (lastBlock == null || lastBlock != hit.block)
            _time = timeToDamage;
        lastBlock = hit.block;
        _time -= Time.Delta;
        if (_time <= 0f)
        {
            DamageBlock(hit, power);
            _time = timeToDamage;
        }
    }

    private void DamageBlock(HitInfo hit, byte damage = 1)
    {
        int dam = hit.block.Durability - damage;
        hit.block.Durability = (byte)(dam < 0 ? 0 : dam);
        if (hit.block.Durability <= 0)
            DestroyBlock(hit);
    }

    public override void Update(Astronaut player)
    {
        if (Input.IsMouseButtonUp(MouseButton.Left))
        {
            model.SetAnimation(false);
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
            if (Input.IsKeyDown(Keys.F) && lastInteractiveBlock != null)
                lastInteractiveBlock.Use(player);
            model.SetAnimation(false);
            light.IsActive = false;
            return;
        }
        Ray ray = new Ray(player.Position, player.Front, MaxDestroyDistance);
        if (World.CurrentSector.Raycast(ray, out HitInfo hit))
        {
            UpdateBlockSelector(hit);
            if (lastBlock == null && hit.block != null)
                lastBlock = hit.block;
            if (Input.IsMouseButton(MouseButton.Left))
            {
                var item = selectedItemSlot.Item as DrillItem;
                var blockData = GameBlocks.GetBlockDataById(hit.block.BlockId);
                BlockMiningEffect.Enabled = true;
                BlockMiningEffect.ParticleSystem.Position = hit.position + new Vector3(hit.normal.X, hit.normal.Y, hit.normal.Z) * 0.05f;
                BlockMiningEffect.Update();
                if (!model.Animator.IsActive)
                    model.SetAnimation(true);
                if (!light.IsActive)
                    light.IsActive = true;
                light.Position = player.Position;
                if (item != null)
                {
                    if (item.Power >= blockData.PowerToDrill)
                        ProcessDestroying(hit, item.Power);
                    else
                        BlockMiningEffect.SetEmitter(false);
                }
            }
            if (Input.IsMouseButtonDown(MouseButton.Left))
            {
                model.SetAnimation(true);
                BlockMiningEffect.Enabled = true;
                if (!light.IsActive)
                    light.IsActive = true;
            }
            lastInteractiveBlock = hit.block as InteractiveBlock;
            if (lastInteractiveBlock == null)
                CenteredText.Hide();
            else
                InteractiveBlock.UpdateInteractive(lastInteractiveBlock, player, hit.chunk, hit.position);
        }
        else
        {
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
