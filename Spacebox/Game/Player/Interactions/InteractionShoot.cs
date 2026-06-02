using OpenTK.Mathematics;
using Engine.Audio;
using Engine.Physics;
using Spacebox.Game.Animations;
using Spacebox.Game.Effects;
using Spacebox.Game.Generation;
using Spacebox.Game.Physics;
using Engine;
using Spacebox.Game.Generation.Blocks;
using System;

namespace Spacebox.Game.Player.Interactions;

public class InteractionShoot : InteractionMode
{
    private AudioSource shotSound;
    private InteractiveBlock lastInteractiveBlock;

    private AnimatedItemModel model;

    private ProjectileParameters projectileParameters;
    private WeaponItem weapon;

    private float _time = 0;
    private bool canShoot = false;
    private Vector3 startPos;
    private Vector3 despawnPos = Vector3.Zero;

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
        model.Animator.Clear();
        model.Position = startPos;
    }

    private void SetSphere(Projectile p)
    {
        p.OnDespawn -= SetSphere;

        despawnPos = p.Position;

        var sphereRenderer = SpheresPool.Instance.Take();
        sphereRenderer.Activate(despawnPos);
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

            if (canShoot == false && Input.IsAction("shoot") && ToggleManager.OpenedWindowsCount < 1 && !Debug.IsVisible)
            {
                // Проверяем наличие патронов ПЕРЕД тем, как проигрывать анимацию и звук
                if (!TryConsumeAmmo(player))
                {
                    // Патронов нет - можно добавить звук осечки (щелчок) здесь
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

        Ray rayNormal = new Ray(player.Position, player.Front, InteractiveBlock.InteractionDistance);
        HitInfo hit;

        if (World.CurrentSector.Raycast(rayNormal, out hit))
        {
            if (hit.block.Is<InteractiveBlock>(out var b))
            {
                lastInteractiveBlock = b;
                InteractiveBlock.UpdateInteractive(lastInteractiveBlock, player, ref hit);

                if (hit.block.Is<StorageBlock>(out var storageBlock))
                {
                    storageBlock.SetPositionInChunk(hit.blockPositionIndex);
                }
            }
        }

        if (canShoot && Input.IsAction("shoot"))
        {
            if (player.PowerBar.StatsData.Value < weapon.PowerUsage) return;
            canShoot = false;

            player.PowerBar.StatsData.Decrement(weapon.PowerUsage);

            var projectile = World.Instance.ProjectilesPool.Take();

            if (projectileParameters.Name == "p_ar")
            {
                projectile.OnDespawn += SetSphere;
            }

            var projectileSpawnPos = Node3D.LocalToWorld(new Vector3(0, 0, 0), player) + player.Front * 0.25f;

            var shotDir = WeaponItem.CalculateSpreadCone(weapon, player.Front);
            var shotRay = new Ray(projectileSpawnPos, shotDir, 1f);

            projectile.Initialize(shotRay, ref projectileParameters, player);

            ApplyRecoil(player, weapon, shotRay.Direction, projectileParameters);

            _time = 0;
        }
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