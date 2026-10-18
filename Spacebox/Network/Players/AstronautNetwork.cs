using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;

namespace Spacebox.Network.Players;

public sealed class AstronautNetwork : NetworkTransform
{
    private const short NoItem = -1;
    private const int MaxFallDamage = 255;
    private const float MaxDrillDistance = 8f;
    private const int MaxDrillDamagePerSecond = 20;
    private const float DrillWindow = 1f;

    private readonly Dictionary<int, DrillBudget> _drillBudgets = new Dictionary<int, DrillBudget>();

    private readonly NetworkVariable<bool> _flashlight;
    private readonly NetworkVariable<short> _heldItem;
    private readonly NetworkVariable<bool> _isAlive;
    private readonly NetworkVariable<bool> _isVulnerable;
    private readonly NetworkVariable<short> _flashlightItem;
    private readonly NetworkVariable<bool> _hasOxygen;
    private readonly NetworkVariable<short>[] _equipment;
    private static readonly EquipmentSlotType[] EquipmentSlots = Enum.GetValues<EquipmentSlotType>();

    public static AstronautNetwork Local { get; private set; }

    private FallImpactSound? _fallSound;

    public string PlayerName { get; set; }
    public bool IsAlive => _isAlive.Value;
    public bool IsVulnerable => _isAlive.Value && _isVulnerable.Value;

    public AstronautNetwork()
    {
        _flashlight = CreateVariable(false, NetworkWritePermission.Owner);
        _heldItem = CreateVariable(NoItem, NetworkWritePermission.Owner);
        _isAlive = CreateVariable(true, NetworkWritePermission.Owner);
        _isVulnerable = CreateVariable(false, NetworkWritePermission.Owner);
        _flashlightItem = CreateVariable(NoItem, NetworkWritePermission.Owner);
        _hasOxygen = CreateVariable(true, NetworkWritePermission.Owner);
        _equipment = new NetworkVariable<short>[EquipmentSlots.Length];
        for (int i = 0; i < _equipment.Length; i++) _equipment[i] = CreateVariable(NoItem, NetworkWritePermission.Owner);
    }

    public override void OnUpdate()
    {
        base.OnUpdate();

        if (IsSpawned && IsOwner && Owner is LocalAstronaut local)
        {
            _isVulnerable.Value = local.GameMode == GameMode.Survival;
            _flashlightItem.Value = local.ActiveFlashlightItem?.Id ?? NoItem;
            _hasOxygen.Value = local.HasOxygen;
            WriteEquipment(local);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (Owner is not Astronaut astronaut) return;

        if (IsOwner)
        {
            Local = this;
            _flashlight.Value = astronaut.Flashlight.Enabled;
            if (astronaut is LocalAstronaut localAstronaut)
            {
                _isAlive.Value = localAstronaut.IsAlive;
                localAstronaut.OnDeath += OnLocalDeath;
                localAstronaut.OnRevived += OnLocalRevived;
                localAstronaut.FallDamage.Impact += OnLocalFallImpact;
            }
            SetHeldItem(Spacebox.Game.GUI.PanelUI.CurrentSlot());
            astronaut.Flashlight.OnEnabledChanged += OnLocalFlashlightChanged;
            return;
        }

        _flashlight.Changed += (_, value) => astronaut.Flashlight.Enabled = value;
        _heldItem.Changed += (_, value) => astronaut.ChangeItemInHand(GetItem(value));
        _isAlive.Changed += (_, value) => ApplyAliveVisual(astronaut, value);
        _flashlightItem.Changed += (_, value) => ApplyFlashlight(astronaut, value);
        if (astronaut is RemoteAstronaut remote) remote.DrillDamaged += OnDrilledLocally;
        _hasOxygen.Changed += (_, value) => astronaut.HasOxygen = value;
        astronaut.HasOxygen = _hasOxygen.Value;
        for (int i = 0; i < _equipment.Length; i++)
        {
            var slot = EquipmentSlots[i];
            _equipment[i].Changed += (_, value) => ApplyEquipment(astronaut, slot, value);
            ApplyEquipment(astronaut, slot, _equipment[i].Value);
        }
        ApplyFlashlight(astronaut, _flashlightItem.Value);
        ApplyAliveVisual(astronaut, _isAlive.Value);
        astronaut.Flashlight.Enabled = _flashlight.Value;
        astronaut.ChangeItemInHand(GetItem(_heldItem.Value));
    }

    public override void OnNetworkDespawn()
    {
        _fallSound?.Dispose();
        _fallSound = null;
        _drillBudgets.Clear();
        if (Owner is RemoteAstronaut remote) remote.DrillDamaged -= OnDrilledLocally;

        if (Local == this)
        {
            Local = null;
            if (Owner is Astronaut astronaut) astronaut.Flashlight.OnEnabledChanged -= OnLocalFlashlightChanged;
            if (Owner is LocalAstronaut localAstronaut)
            {
                localAstronaut.OnDeath -= OnLocalDeath;
                localAstronaut.OnRevived -= OnLocalRevived;
                localAstronaut.FallDamage.Impact -= OnLocalFallImpact;
            }
        }
    }

    public void SetHeldItem(ItemSlot slot)
    {
        if (IsOwner) _heldItem.Value = slot != null && slot.HasItem ? slot.Item.Id : NoItem;
    }

    protected override Quaternion ReadRotation()
    {
        return Owner is Camera360Base camera ? camera.GetRotation() : base.ReadRotation();
    }

    protected override void ApplyRotation(Quaternion rotation)
    {
        if (Owner is Camera360Base camera) camera.SetRotation(rotation);
        if (Owner is RemoteAstronaut) base.ApplyRotation(rotation);
    }

    private void OnLocalDeath()
    {
        _isAlive.Value = false;

        int killer = Owner is LocalAstronaut local ? local.GetRecentAttacker() : -1;
        SendRpc(nameof(ReportDeath), killer);
    }

    private static void ApplyFlashlight(Astronaut astronaut, short itemId)
    {
        var settings = GetItem(itemId) is EquipmentItem equipment ? equipment.Flashlight : null;
        astronaut.Flashlight.Apply(settings ?? FlashlightSettings.Default);
    }

    private void WriteEquipment(LocalAstronaut local)
    {
        for (int i = 0; i < _equipment.Length; i++)
        {
            var slot = local.Equipment.GetSlot(EquipmentSlots[i]);
            _equipment[i].Value = slot.HasItem ? slot.Item.Id : NoItem;
        }
    }

    private static void ApplyEquipment(Astronaut astronaut, EquipmentSlotType slot, short itemId)
    {
        astronaut.ArmorVisual?.Set(slot, GetItem(itemId) as EquipmentItem);
    }

    private void OnDrilledLocally(int damage)
    {
        SendRpc(nameof(DrillDamage), damage);
    }

    [Rpc(RpcTarget.Server, RequireOwnership = false)]
    private void DrillDamage(int damage)
    {
        int attacker = RpcContext.SenderPeer;
        if (attacker == OwnerPeer || damage <= 0 || !IsAlive) return;

        var attackerAstronaut = Spacebox.Network.Combat.PlayerHitboxes.FindPlayer(attacker);
        if (attackerAstronaut == null) return;

        var victimPosition = IsOwner && Owner is Astronaut astronaut ? astronaut.Position : TargetPosition;
        if (Vector3.DistanceSquared(attackerAstronaut.Position, victimPosition) > MaxDrillDistance * MaxDrillDistance) return;

        int allowed = ConsumeDrillBudget(attacker, damage);
        if (allowed > 0) Spacebox.Network.Combat.CombatNetwork.ApplyPlayerHit(OwnerPeer, attacker, allowed, false);
    }

    private int ConsumeDrillBudget(int attacker, int damage)
    {
        float now = Time.Total;
        if (!_drillBudgets.TryGetValue(attacker, out var budget) || now - budget.Start >= DrillWindow || now < budget.Start)
            budget = new DrillBudget(now, 0);

        int allowed = Math.Min(damage, MaxDrillDamagePerSecond - budget.Damage);
        if (allowed <= 0) return 0;

        _drillBudgets[attacker] = new DrillBudget(budget.Start, budget.Damage + allowed);
        return allowed;
    }

    private readonly struct DrillBudget
    {
        public readonly float Start;
        public readonly int Damage;

        public DrillBudget(float start, int damage)
        {
            Start = start;
            Damage = damage;
        }
    }

    private void OnLocalFallImpact(int damage)
    {
        SendRpc(nameof(ReportFallImpact), damage);
    }

    [Rpc(RpcTarget.Server)]
    private void ReportFallImpact(int damage)
    {
        SendRpc(nameof(FallImpact), Math.Clamp(damage, 1, MaxFallDamage));
    }

    [Rpc(RpcTarget.ClientsExceptOwner, Delivery = DeliveryMode.ReliableUnordered)]
    private void FallImpact(int damage)
    {
        if (Owner is not Astronaut astronaut) return;

        _fallSound ??= new FallImpactSound(true);
        _fallSound.Play(damage, astronaut.Position);
    }

    private void OnLocalRevived()
    {
        _isAlive.Value = true;
    }

    [Rpc(RpcTarget.Server)]
    private void ReportDeath(int killerPeer)
    {
        string victim = Spacebox.Network.Combat.CombatNetwork.GetPlayerName(OwnerPeer);
        string text = killerPeer > 0 && killerPeer != OwnerPeer
            ? $"{victim} was killed by {Spacebox.Network.Combat.CombatNetwork.GetPlayerName(killerPeer)}"
            : $"{victim} died";

        ChatService.Broadcast(text, new Color4(1f, 0.35f, 0.35f, 1f));
    }

    private static void ApplyAliveVisual(Astronaut astronaut, bool isAlive)
    {
        if (astronaut is RemoteAstronaut remote) remote.SetAliveVisual(isAlive);
    }

    private void OnLocalFlashlightChanged(bool enabled)
    {
        _flashlight.Value = enabled;
    }

    private static Item GetItem(short id)
    {
        return id != NoItem && GameAssets.Items.TryGetValue(id, out var item) ? item : null;
    }
}
