using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Player.Respawn;
using Spacebox.Game.Generation.Blocks;

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
    private readonly NetworkVariable<bool> _isUsingItem;
    private readonly NetworkVariable<bool> _isBoosting;
    private readonly NetworkVariable<short>[] _equipment;
    private static readonly EquipmentSlotType[] EquipmentSlots = Enum.GetValues<EquipmentSlotType>();

    public static AstronautNetwork Local { get; private set; }
    public static event Action<int> PlayerDied;

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
        _isUsingItem = CreateVariable(false, NetworkWritePermission.Owner);
        _isBoosting = CreateVariable(false, NetworkWritePermission.Owner);
        _equipment = new NetworkVariable<short>[EquipmentSlots.Length];
        for (int i = 0; i < _equipment.Length; i++) _equipment[i] = CreateVariable(NoItem, NetworkWritePermission.Owner);
        _isAlive.Changed += (previous, alive) =>
        {
            if (alive && !previous) _movement.AllowJump(Time.Total, RespawnGraceSeconds);
        };
    }

    private const double SpawnGraceSeconds = 10.0;
    private const double RespawnGraceSeconds = 10.0;
    private const double TeleportGraceSeconds = 3.0;
    private const float CorrectionInterval = 1f;

    private readonly MovementGuard _movement = new MovementGuard();
    private float _lastCorrection = float.MinValue;

    protected override bool ValidateOwnerState(TransformState previous, TransformState next)
    {
        if (!IsServer || IsOwner) return true;
        if (!NetValidation.IsValidRotation(next.Rotation)) return false;
        if (PlayerGameModeService.Of(PlayerName) != GameMode.Survival)
        {
            _movement.AllowJump(Time.Total, TeleportGraceSeconds);
        }
        return _movement.Accept(next.Position, next.Time, Time.Total, ShipCarrierSpeed.Around(_movement.LastPosition, next.Position));
    }

    protected override void OnOwnerStateRejected(TransformState previous, TransformState next)
    {
        float now = Time.Total;
        if (now - _lastCorrection < CorrectionInterval) return;
        _lastCorrection = now;
        SendRpc(nameof(CorrectPosition), _movement.LastPosition);
    }

    [Rpc(RpcTarget.Owner)]
    private void CorrectPosition(Vector3 position)
    {
        if (Owner is LocalAstronaut local) local.Teleport(position);
    }

    public override void OnUpdate()
    {
        base.OnUpdate();

        if (IsSpawned && IsOwner && Owner is LocalAstronaut local)
        {
            _isVulnerable.Value = local.GameMode == GameMode.Survival;
            _flashlightItem.Value = local.ActiveFlashlightItem?.Id ?? NoItem;
            _hasOxygen.Value = local.HasOxygen;
            _isUsingItem.Value = local.IsUsingItem;
            _isBoosting.Value = local.IsBoosting && local.IsAlive;
            WriteEquipment(local);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _movement.AllowJump(Time.Total, SpawnGraceSeconds);

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
                localAstronaut.Respawner.Reconstructed += OnLocalReconstructed;
                localAstronaut.ItemUsed += OnLocalItemUsed;
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
        _isUsingItem.Changed += (_, value) => SetUsing(astronaut, value);
        _isBoosting.Changed += (_, value) => astronaut.IsBoosting = value;
        astronaut.IsBoosting = _isBoosting.Value;
        SetUsing(astronaut, _isUsingItem.Value);
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
                localAstronaut.Respawner.Reconstructed -= OnLocalReconstructed;
                localAstronaut.ItemUsed -= OnLocalItemUsed;
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

        var local = Owner as LocalAstronaut;
        int killer = local != null ? local.GetRecentAttacker() : -1;
        SendRpc(nameof(ReportDeath), killer, local?.LastDeathCase?.CauseOfDeath ?? "");
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

    private static void SetUsing(Astronaut astronaut, bool isUsing)
    {
        if (astronaut.HeldItemVisual != null) astronaut.HeldItemVisual.IsUsing = isUsing;
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

    private void OnLocalReconstructed(ReconstructionGrant grant)
    {
        SendRpc(nameof(ReportReconstructed), grant.Key.EntityId, grant.Key.Cell);
    }

    [Rpc(RpcTarget.Server)]
    private void ReportReconstructed(long entityId, Vector3i cell)
    {
        if (!DnaReconstructorRegistry.TryFind(entityId, cell, out _)) return;
        SendRpc(nameof(Reconstructed), entityId, cell);
    }

    [Rpc(RpcTarget.ClientsExceptOwner, Delivery = DeliveryMode.ReliableUnordered)]
    private void Reconstructed(long entityId, Vector3i cell)
    {
        if (DnaReconstructorRegistry.TryFind(entityId, cell, out var reconstructor)) DnaReconstructorSounds.PlaySpawn(reconstructor);
    }

    public static AstronautNetwork Find(Astronaut astronaut)
    {
        if (astronaut == null) return null;

        foreach (var networkObject in NetworkManager.Instance.Spawner.Objects)
        {
            if (networkObject.Owner == astronaut) return networkObject.GetBehaviour<AstronautNetwork>();
        }
        return null;
    }

    public static AstronautNetwork Find(int peer)
    {
        foreach (var networkObject in NetworkManager.Instance.Spawner.Objects)
        {
            if (networkObject.OwnerPeer != peer) continue;

            var network = networkObject.GetBehaviour<AstronautNetwork>();
            if (network != null) return network;
        }
        return null;
    }

    public void SetGameModeByServer(GameMode mode)
    {
        SendRpc(nameof(ApplyServerGameMode), (byte)mode);
    }

    public void TeleportByServer(Vector3 position)
    {
        _movement.AllowJump(Time.Total, TeleportGraceSeconds);
        SendRpc(nameof(ApplyServerTeleport), position);
    }

    public void RestoreStatsByServer(PlayerStat stats, int amount)
    {
        SendRpc(nameof(ApplyServerStats), (byte)stats, amount);
    }

    public void GiveItemByServer(string fullItemId, int count)
    {
        int seq = GameAssets.ItemsStr.TryGetValue(fullItemId ?? "", out var item) ? Items.ItemAuthority.Grant(OwnerPeer, item.Id, count) : 0;
        SendRpc(nameof(ApplyServerGiveItem), fullItemId, count, seq);
    }

    [Rpc(RpcTarget.Owner)]
    private void ApplyServerStats(byte stats, int amount)
    {
        if (Owner is not LocalAstronaut local) return;

        var restored = (PlayerStat)stats & PlayerStat.All;
        PlayerStatsAccess.Restore(local, restored, amount);
        Spacebox.Game.GUI.Chat.Write($"The server restored your {PlayerStatsAccess.Describe(restored)}", Color4.Orange);
    }

    [Rpc(RpcTarget.Owner)]
    private void ApplyServerGiveItem(string fullItemId, int count, int seq)
    {
        if (Owner is LocalAstronaut local && count > 0 && GameAssets.ItemsStr.TryGetValue(fullItemId ?? "", out var item))
        {
            PlayerItemGiver.Give(local, item, count);
            Spacebox.Game.GUI.Chat.Write($"The server gave you {count} x {item.Name}", Color4.Orange);
        }
        Items.ItemSync.Applied(seq);
    }

    [Rpc(RpcTarget.Owner)]
    private void ApplyServerGameMode(byte mode)
    {
        if (Owner is not LocalAstronaut local || mode > (byte)GameMode.Spectator) return;

        local.GameMode = (GameMode)mode;
        Spacebox.Game.GUI.Chat.Write($"The server changed your game mode to {local.GameMode}", Color4.Orange);
    }

    [Rpc(RpcTarget.Owner)]
    private void ApplyServerTeleport(Vector3 position)
    {
        if (Owner is not LocalAstronaut local) return;

        local.Teleport(position);
        Spacebox.Game.GUI.Chat.Write("The server teleported you", Color4.Orange);
    }

    private static readonly GiftLedger _gifts = new GiftLedger();
    private static readonly List<GiftLedger.Gift> _finishedGifts = new List<GiftLedger.Gift>();

    public static void UpdateGifts()
    {
        if (!NetworkManager.Instance.IsServer || _gifts.Count == 0) return;

        _finishedGifts.Clear();
        _gifts.TakeExpired(Time.Total, _finishedGifts);
        foreach (var gift in _finishedGifts)
            Find(gift.GiverPeer)?.SendRpc(nameof(GiftResult), gift.RequestId, true, "", 0);
    }

    public static void ForgetGiftsOf(int peer)
    {
        _finishedGifts.Clear();
        _gifts.TakeForPeer(peer, _finishedGifts);
        foreach (var gift in _finishedGifts)
        {
            if (gift.GiverPeer != peer) Find(gift.GiverPeer)?.SendRpc(nameof(GiftResult), gift.RequestId, false, "The player left", RefundGift(gift.GiverPeer, gift.ItemId));
        }
    }

    public static void ClearGifts() => _gifts.Clear();

    public void RequestGift(short itemId, int requestId)
    {
        SendRpc(nameof(GiftRequested), itemId, requestId);
    }

    [Rpc(RpcTarget.Server, RequireOwnership = false)]
    private void GiftRequested(short itemId, int requestId)
    {
        int giverPeer = RpcContext.SenderPeer;
        var giver = Find(giverPeer);
        if (giver == null) return;

        if (!Items.ItemAuthority.TryLose(giverPeer, itemId, 1))
        {
            giver.SendRpc(nameof(GiftResult), requestId, false, "You don't have this item", 0);
            return;
        }

        string failure = ValidateGift(giverPeer, itemId);
        if (failure != null)
        {
            giver.SendRpc(nameof(GiftResult), requestId, false, failure, RefundGift(giverPeer, itemId));
            return;
        }

        int giftId = _gifts.Begin(giverPeer, OwnerPeer, requestId, Time.Total + ItemGiftNetwork.Timeout, itemId);
        SendRpc(nameof(ReceiveGift), itemId, giftId, giverPeer);
    }

    private string ValidateGift(int giverPeer, short itemId)
    {
        if (giverPeer == OwnerPeer) return "You cannot give items to yourself";
        if (!IsAlive) return "The player is dead";
        if (!ItemReceiver.CanReceive(GetItem(itemId))) return "This item cannot be given";

        var giverAstronaut = Spacebox.Network.Combat.PlayerHitboxes.FindPlayer(giverPeer);
        if (giverAstronaut == null) return "Unknown player";

        var targetPosition = IsOwner && Owner is Astronaut astronaut ? astronaut.Position : TargetPosition;
        float maxDistance = ItemGiftNetwork.MaxDistance + ItemGiftNetwork.ServerDistanceTolerance;
        return Vector3.DistanceSquared(giverAstronaut.Position, targetPosition) > maxDistance * maxDistance ? "Too far away" : null;
    }

    [Rpc(RpcTarget.Owner)]
    private void ReceiveGift(short itemId, int giftId, int giverPeer)
    {
        var item = GetItem(itemId);
        string failure = null;
        bool success = Owner is LocalAstronaut local && ItemReceiver.TryReceive(local, item, out failure);
        if (success) Spacebox.Game.GUI.Chat.Write($"{Combat.CombatNetwork.GetPlayerName(giverPeer)} gave you {item.Name}", new Color4(0.6f, 1f, 0.6f, 1f));

        SendRpc(nameof(ReportGiftResult), giftId, success, ItemGiftNetwork.Clamp(failure));
    }

    [Rpc(RpcTarget.Server)]
    private void ReportGiftResult(int giftId, bool success, string failure)
    {
        if (!_gifts.TryComplete(giftId, RpcContext.SenderPeer, out var gift)) return;
        if (success) Items.ItemAuthority.Gain(gift.TargetPeer, gift.ItemId, 1);
        int seq = success ? 0 : RefundGift(gift.GiverPeer, gift.ItemId);
        Find(gift.GiverPeer)?.SendRpc(nameof(GiftResult), gift.RequestId, success, ItemGiftNetwork.Clamp(failure), seq);
    }

    [Rpc(RpcTarget.Owner)]
    private void GiftResult(int requestId, bool success, string failure, int seq)
    {
        ItemGiftNetwork.OnResult(requestId, success, failure);
        Items.ItemSync.Applied(seq);
    }

    private static int RefundGift(int giverPeer, short itemId) => Items.ItemAuthority.Grant(giverPeer, itemId, 1);

    private void OnLocalItemUsed(Item item)
    {
        if (ItemUseCue.IsReplicated(item)) SendRpc(nameof(ReportItemUse), item.Id);
    }

    [Rpc(RpcTarget.Server)]
    private void ReportItemUse(short itemId)
    {
        if (!IsAlive || !ItemUseCue.IsReplicated(GetItem(itemId))) return;
        SendRpc(nameof(ItemUse), itemId);
    }

    [Rpc(RpcTarget.ClientsExceptOwner, Delivery = DeliveryMode.ReliableUnordered)]
    private void ItemUse(short itemId)
    {
        if (Owner is RemoteAstronaut remote) remote.PlayItemUse(GetItem(itemId));
    }

    private void OnLocalRevived()
    {
        _isAlive.Value = true;
    }

    [Rpc(RpcTarget.Server)]
    private void ReportDeath(int killerPeer, string reason)
    {
        PlayerDied?.Invoke(OwnerPeer);

        string victim = Spacebox.Network.Combat.CombatNetwork.GetPlayerName(OwnerPeer);
        string killer = killerPeer > 0 && killerPeer != OwnerPeer ? Spacebox.Network.Combat.CombatNetwork.GetPlayerName(killerPeer) : null;
        string text = DeathMessageFormatter.Format(victim, killer, reason);

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
