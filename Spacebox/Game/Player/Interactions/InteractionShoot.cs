using Engine;
using Engine.Audio;
using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game.Animations;
using Spacebox.Game.Effects;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Physics;
using Spacebox.GUI;
using System;

namespace Spacebox.Game.Player.Interactions;

public class InteractionShoot : InteractionMode
{
    private AudioSource shotSound;

    private AnimatedItemModel model;

    private ProjectileParameters projectileParameters;
    private WeaponItem weapon;

    private float _time = 0;
    private bool canShoot = false;
    private Vector3 startPos;

    public override bool UsesPrimaryAction => true;

    public InteractionShoot(ItemSlot itemslot)
    {
        AllowReload = true;

        UpdateItemSlot(itemslot);

        var weapone = itemslot.Item as WeaponItem;
        if (weapone != null)
        {
            projectileParameters = GameAssets.Projectiles[weapone.ProjectileID];
            weapon = weapone;
            if (model != null)
                startPos = model.Position;

            if (shotSound == null)
            {
                var v = GameAssets.Sounds;
                shotSound = new AudioSource(v[weapone.ShotSound]);
                shotSound.Volume = 1f;
            }
        }
    }

    public void UpdateItemSlot(ItemSlot itemslot)
    {
        var mod = GameAssets.ItemModels[itemslot.Item.Id];
        model = mod as AnimatedItemModel;
    }

    public override void OnEnable()
    {
        _time = 0;
        model?.SetAnimation(true);
    }

    public override void OnDisable()
    {
        shotSound?.Dispose();
        shotSound = null;
        model.Animator.Clear();
        model.Position = startPos;
    }

    private bool TryConsumeAmmo(LocalAstronaut player)
    {
        if (!weapon.NeedsAmmo || weapon.Ammo == null) return true;

        //if (player.GameMode == GameModes.GameMode.Creative) return true;

        if (player.Panel.TryRemoveItem(weapon.Ammo, 1)) return true;

        if (player.Inventory.TryRemoveItem(weapon.Ammo, 1)) return true;

        return false;
    }

    public override void Update(LocalAstronaut player)
    {
        if (player.IsAlive == false)
        {
            canShoot = false;
            return;
        }

        if (_time < weapon.ReloadTime * 0.05f)
        {
            _time += Time.Delta;
        }
        else
        {
            if (player.PowerBar.StatsData.Value < weapon.PowerUsage) return;

            if (canShoot == false && Input.IsAction("shoot") && !UIManager.IsUIMode)
            {

                if (!TryConsumeAmmo(player))
                {
                    if(weapon.NeedsAmmo)
                    {
                    CenteredText.SetText("No ammo!");
                    CenteredText.Show();

                    }
                    return;
                }

                canShoot = true;
                model?.SetAnimation(false);
                model?.SetAnimation(true);
                model.Animator.Clear();
                if (model != null)
                {
                    model.Animator.AddAnimation(new ShootAnimation(startPos, model.Position - new Vector3(0.001f * weapon.AnimationPushback, 0, 0), 0.05f));
                    model.Animator.speed = weapon.AnimationSpeed;
                }

                Random random = new Random();

                shotSound.Pitch = random.Next(95, 105) * 0.01f;
                shotSound.Play();
                player.PlayerStatistics.ShotsFired++;
            }
        }

        if (!player.CanMove)
        {
            return;
        }

        BlockUseTarget.Update(player);

        if (canShoot && Input.IsAction("shoot"))
        {
            if (player.PowerBar.StatsData.Value < weapon.PowerUsage) return;
            canShoot = false;

            player.PowerBar.StatsData.Decrement(weapon.PowerUsage);

            var projectileSpawnPos = Node3D.LocalToWorld(new Vector3(0, 0, 0), player) + player.Front * 0.25f;

            var shotDir = WeaponItem.CalculateSpreadCone(weapon, player.Front);
            var shotRay = new Ray(projectileSpawnPos, shotDir, 1f);

            var projectile = Spacebox.Network.Combat.CombatNetwork.FireLocal(player, weapon, projectileParameters, shotRay);
            projectile.SetRollReference(player.Up);
            if (TryGetMuzzle(weapon, out var muzzle)) projectile.SetVisualOrigin(muzzle);

            ApplyRecoil(player, weapon, shotRay.Direction, projectileParameters);

            _time = 0;
        }
    }

    private bool TryGetMuzzle(WeaponItem weapon, out Vector3 world)
    {
        world = Vector3.Zero;
        if (model == null || !GameAssets.ItemMuzzles.TryGetValue(weapon.Id, out var muzzle)) return false;
        return model.TryGetMuzzleWorld(muzzle.FirstPersonPoint(muzzle.LowEnd, weapon), muzzle.FirstPersonPoint(muzzle.HighEnd, weapon), out world);
    }

    public static void ApplyRecoil(LocalAstronaut player, WeaponItem weapon, Vector3 shootDirection, ProjectileParameters projectileParams)
    {
        if (weapon.Recoil <= 0) return;

        const float recoilMultiplier = 0.1f;

        float recoilForce = weapon.Recoil * recoilMultiplier;

        Vector3 recoilDirection = -shootDirection;
        Vector3 recoilImpulse = recoilDirection * recoilForce;

        player.InertiaController.ApplyInput(recoilImpulse);
        player.InertiaController.Velocity += recoilImpulse;
    }
}