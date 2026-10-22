using Engine;
using Engine.Networking;
using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Network.Messages;
using Spacebox.Network.Players;

namespace Spacebox.Network.Combat;

public static class CombatNetwork
{
    private const float MaxOriginOffset = 4f;
    private const float FireRateTolerance = 0.75f;
    private const float HeadshotMultiplier = 2f;
    private const float MinShotInterval = 0.02f;

    private static readonly Dictionary<int, float> _lastShotTime = new Dictionary<int, float>();
    private static readonly CombatSounds _sounds = new CombatSounds();

    public static void Register(NetworkManager network)
    {
        network.Messages.Register<WeaponFireMessage>(OnWeaponFire);
        network.Messages.Register<ProjectileSpawnMessage>(OnProjectileSpawn);
        network.Messages.Register<PlayerDamageMessage>(OnPlayerDamage);
        network.Messages.Register<HitConfirmMessage>(OnHitConfirm);
    }

    public static void Reset()
    {
        _lastShotTime.Clear();
        _sounds.Clear();
        PlayerHitboxes.Clear();
    }

    public static Projectile FireLocal(LocalAstronaut shooter, WeaponItem weapon, ProjectileParameters parameters, Ray ray)
    {
        var network = NetworkManager.Instance;
        bool isAuthoritative = network.IsServer;

        var projectile = World.Instance.ProjectilesPool.Take();
        projectile.Initialize(ray, ref parameters, shooter, network.LocalPeerId, isAuthoritative, NetworkTransform.InterpolationDelay);
        ProjectileImpactEffects.Attach(projectile);

        if (isAuthoritative)
        {
            network.SendToClients(new ProjectileSpawnMessage
            {
                ShooterPeer = network.LocalPeerId,
                ItemId = weapon.Id,
                Origin = ray.Origin,
                Direction = ray.Direction,
                Age = 0f
            }, DeliveryMode.ReliableUnordered);
        }
        else
        {
            network.SendToServer(new WeaponFireMessage { ItemId = weapon.Id, Origin = ray.Origin, Direction = ray.Direction }, DeliveryMode.ReliableUnordered);
        }

        return projectile;
    }

    public static void ApplyPlayerHit(int victimPeer, int attackerPeer, int damage, bool headshot)
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer) return;
        if (!Spacebox.Game.Rules.GameplayRules.AllowsPlayerHit(Spacebox.Game.Rules.ServerRules.Current, attackerPeer, victimPeer)) return;
        if (!CanBeHurt(victimPeer)) return;

        int finalDamage = (int)MathF.Round(damage * (headshot ? HeadshotMultiplier : 1f));
        var message = new PlayerDamageMessage { AttackerPeer = attackerPeer, Damage = finalDamage, Headshot = headshot };

        if (victimPeer == network.LocalPeerId)
            OnPlayerDamage(network.LocalPeerId, message);
        else
            network.Send(victimPeer, message);

        if (attackerPeer == network.LocalPeerId)
            HitMarkerUI.Show(headshot);
        else if (attackerPeer != victimPeer)
            network.Send(attackerPeer, new HitConfirmMessage { Headshot = headshot });
    }

    private static bool CanBeHurt(int peer)
    {
        if (peer == NetworkManager.Instance.LocalPeerId)
            return AstronautNetwork.Local?.Owner is LocalAstronaut local && local.GameMode == Spacebox.Game.Player.GameModes.GameMode.Survival;
        return PlayerGameModeService.Of(PlayerRegistry.GetName(peer)) == Spacebox.Game.Player.GameModes.GameMode.Survival;
    }

    public static string GetPlayerName(int peer)
    {
        if (peer == NetworkManager.Instance.LocalPeerId) return GameSession.PlayerName;

        var astronaut = PlayerHitboxes.FindPlayer(peer);
        if (astronaut is RemoteAstronaut remote) return remote.PlayerName;
        return PlayerRegistry.GetName(peer);
    }

    private static void OnWeaponFire(int sender, WeaponFireMessage message)
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer || sender == network.LocalPeerId) return;
        if (!GameAssets.TryGetItemById(message.ItemId, out var item) || item is not WeaponItem weapon) return;
        if (!GameAssets.Projectiles.TryGetValue(weapon.ProjectileID, out var parameters)) return;
        if (!IsFireAllowed(sender, weapon, message.Origin)) return;

        var direction = message.Direction.LengthSquared > 1e-6f ? message.Direction.Normalized() : Vector3.UnitZ;
        float latency = network.GetOneWayLatency(sender);
        float rewind = latency + NetworkTransform.InterpolationDelay;

        var projectile = World.Instance.ProjectilesPool.Take();
        projectile.Initialize(new Ray(message.Origin, direction, 1f), ref parameters, null, sender, true, rewind);
        ApplyShooterVisuals(projectile, sender);
        projectile.FastForward(latency);
        ProjectileImpactEffects.Attach(projectile);

        if (GameSession.HasLocalPlayer) _sounds.PlayShot(weapon, message.Origin);
        PlayShotKick(sender);

        network.SendToClients(new ProjectileSpawnMessage
        {
            ShooterPeer = sender,
            ItemId = weapon.Id,
            Origin = message.Origin,
            Direction = direction,
            Age = latency
        }, DeliveryMode.ReliableUnordered, sender);
    }

    private static bool IsFireAllowed(int sender, WeaponItem weapon, Vector3 origin)
    {
        float now = (float)Time.Total;
        float interval = MathF.Max(MinShotInterval, weapon.ReloadTime * 0.05f * FireRateTolerance);
        if (_lastShotTime.TryGetValue(sender, out float last) && now - last < interval) return false;

        var shooter = PlayerHitboxes.FindPlayer(sender);
        if (shooter == null && sender != NetworkManager.Instance.LocalPeerId) return false;
        if (shooter != null && Vector3.DistanceSquared(shooter.Position, origin) > MaxOriginOffset * MaxOriginOffset) return false;

        _lastShotTime[sender] = now;
        return true;
    }

    private static void OnProjectileSpawn(int sender, ProjectileSpawnMessage message)
    {
        var network = NetworkManager.Instance;
        if (network.IsServer || message.ShooterPeer == network.LocalPeerId) return;
        if (!GameAssets.TryGetItemById(message.ItemId, out var item) || item is not WeaponItem weapon) return;
        if (!GameAssets.Projectiles.TryGetValue(weapon.ProjectileID, out var parameters) || World.Instance == null) return;

        var projectile = World.Instance.ProjectilesPool.Take();
        projectile.Initialize(new Ray(message.Origin, message.Direction, 1f), ref parameters, null, message.ShooterPeer, false, 0f);
        ApplyShooterVisuals(projectile, message.ShooterPeer);
        projectile.FastForward(message.Age + network.LatencyToServer);
        ProjectileImpactEffects.Attach(projectile);

        _sounds.PlayShot(weapon, message.Origin);
        PlayShotKick(message.ShooterPeer);
    }

    private static void ApplyShooterVisuals(Projectile projectile, int shooterPeer)
    {
        if (PlayerHitboxes.FindPlayer(shooterPeer) is not RemoteAstronaut shooter) return;

        projectile.SetRollReference(shooter.Up);
        if (shooter.HeldItemVisual != null && shooter.HeldItemVisual.TryGetMuzzleWorld(out var muzzle)) projectile.SetVisualOrigin(muzzle);
    }

    private static void PlayShotKick(int shooterPeer)
    {
        if (PlayerHitboxes.FindPlayer(shooterPeer) is RemoteAstronaut shooter) shooter.HeldItemVisual?.PlayKick();
    }

    private static void OnPlayerDamage(int sender, PlayerDamageMessage message)
    {
        if (!AcceptsDamageFrom(sender)) return;

        var local = AstronautNetwork.Local?.Owner as LocalAstronaut;
        if (local == null || !local.IsAlive || local.GameMode != Spacebox.Game.Player.GameModes.GameMode.Survival) return;

        local.RegisterAttacker(message.AttackerPeer);
        string attacker = GetPlayerName(message.AttackerPeer);
        local.TakeDamage(message.Damage, new DeathCase(message.Headshot ? $"Headshot by {attacker}" : $"Killed by {attacker}"));
    }

    public static bool AcceptsDamageFrom(int sender) => sender == NetworkManager.ServerPeerId;

    private static void OnHitConfirm(int sender, HitConfirmMessage message)
    {
        if (NetworkManager.Instance.IsServer) return;
        HitMarkerUI.Show(message.Headshot);
    }
}
