using Engine;
using Engine.Audio;
using Engine.Physics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Player.GameModes;
using Spacebox.Network;

namespace Spacebox.Game.Player.Interactions;

public class InteractionRepair : InteractionMode
{
    private const float SoundInterval = 0.35f;

    private readonly ItemSlot _slot;
    private readonly RepairTarget _target = new RepairTarget();
    private AudioSource _sound;
    private float _repairProgress;
    private float _powerProgress;
    private float _soundTimer;

    public override bool UsesPrimaryAction => true;

    public InteractionRepair(ItemSlot slot)
    {
        _slot = slot;
    }

    public override void OnEnable()
    {
        var clip = Resources.Load<AudioClip>("placeMetal");
        if (clip != null) _sound = new AudioSource(clip) { Volume = 0.35f };
    }

    public override void OnDisable()
    {
        RepairTargetUI.Hide();
        _sound?.Stop();
    }

    public override void Update(LocalAstronaut player)
    {
        if (_slot?.Item is not RepairToolItem tool || !player.CanMove)
        {
            RepairTargetUI.Hide();
            return;
        }

        BlockUseTarget.Update(player);

        var ray = new Ray(player.Position, player.Front, tool.Range);
        if (World.CurrentSector == null || !World.CurrentSector.Raycast(ray, out HitInfo hit) || hit.block == null || hit.block.IsAir)
        {
            RepairTargetUI.Hide();
            ResetProgress();
            return;
        }

        var data = GameAssets.GetBlockDataById(hit.block.Id);
        int max = data?.Durability ?? 0;
        _target.Measure(hit.chunk, hit.blockPositionIndex, hit.block, (byte)max);
        var tally = _target.Tally;
        bool holding = Input.IsAction("block_destroy");
        bool repairing = holding && max > 0 && tally.NeedsRepair && HasPower(player, tool);

        if (repairing) Repair(player, tool, (byte)max, tally.Max);
        else ResetProgress();

        RepairTargetUI.Show(data?.Name ?? "Block", tally.Current, tally.Max, repairing);
    }

    private void Repair(LocalAstronaut player, RepairToolItem tool, byte max, int totalMax)
    {
        float delta = Time.Delta;
        _repairProgress += tool.GetRepairRate(totalMax) * delta;
        int amount = (int)_repairProgress;
        if (amount > 0)
        {
            _repairProgress -= amount;
            var block = _target.WeakestBlock;
            byte repaired = (byte)Math.Min(amount, max - block.Durability);
            byte before = block.Durability;
            block.Durability = BlockDamage.Repair(block.Durability, repaired, max);
            if (_target.WeakestChunk != null) _target.WeakestChunk.IsModified = true;
            Spacebox.Game.Generation.Blocks.BlockDamageVisuals.OnDurabilityChanged(_target.WeakestChunk, block, before);
            WorldEditNetwork.NotifyRepaired(_target.WeakestChunk, _target.WeakestIndex, block.Id, repaired);
        }

        if (player.GameModeRules.ConsumeItems)
        {
            _powerProgress += tool.PowerPerSecond * delta;
            int power = (int)_powerProgress;
            if (power > 0)
            {
                _powerProgress -= power;
                player.PowerBar.StatsData.Decrement(power);
            }
        }

        _soundTimer -= delta;
        if (_soundTimer <= 0f)
        {
            _soundTimer = SoundInterval;
            _sound?.Play();
        }
    }

    private static bool HasPower(LocalAstronaut player, RepairToolItem tool)
    {
        return player.GameMode != GameMode.Survival || tool.PowerPerSecond <= 0f || player.PowerBar.StatsData.Value > 0;
    }

    private void ResetProgress()
    {
        _repairProgress = 0f;
        _soundTimer = 0f;
    }
}
