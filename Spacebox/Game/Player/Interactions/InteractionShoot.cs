using OpenTK.Mathematics;

using Engine.Audio;
using Engine.Physics;
using Engine.Light;
using Spacebox.Game.Animations;
using Spacebox.Game.Effects;
using Spacebox.Game.Generation;
using Spacebox.Game.Physics;
using Spacebox.GUI;

using Engine;
using Spacebox.Game;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Player;


namespace Spacebox.Game.Player.Interactions;
public class InteractionShoot : InteractionMode
{

    public static InteractionShoot Instance;
    private AudioSource shotSound;
    private InteractiveBlock lastInteractiveBlock;
    private ItemSlot selectedItemSlot;
    private AnimatedItemModel model;
    public static BlockMiningEffect BlockMiningEffect;// needs dispose

    

    public static ProjectilesPool ProjectilesPool;  // needs dispose
    private ProjectileParameters projectileParameters;
    private WeaponItem weapon;

    private float _time = 0;
    private bool canShoot = false;

    public InteractionShoot(ItemSlot itemslot)
    {
        Instance = this;
        selectedItemSlot = itemslot;
        AllowReload = true;
        if (BlockMiningEffect == null)
        {

            var texture = Resources.Load<Texture2D>("Resources/Textures/blockHit.png");
            texture.FilterMode = FilterMode.Nearest;
            // texture
            BlockMiningEffect = new BlockMiningEffect(Camera.Main, Vector3.Zero, new Vector3(1, 1, 1),
                texture, Resources.Load<Shader>("Resources/Shaders/particle"));
        }

        UpdateItemSlot(itemslot);


        if (ProjectilesPool == null)
        {
            ProjectilesPool = new ProjectilesPool(20);
        }

        var weapone = itemslot.Item as WeaponItem;
        if (weapone != null)
        {
            projectileParameters = GameAssets.Projectiles[weapone.ProjectileID];
            weapon = weapone;
            startPos = model.Position;

            if (shotSound == null)
            {
                var v = GameAssets.Sounds;
                shotSound = new AudioSource(v[weapone.ShotSound]); // 
                shotSound.Volume = 1f;
            }

        }

    }

    public void UpdateItemSlot(ItemSlot itemslot)
    {
        selectedItemSlot = itemslot;
        var mod = GameAssets.ItemModels[itemslot.Item.Id];
        model = mod as AnimatedItemModel;

    }
    public override void OnEnable()
    {
        _time = 0;
        model?.SetAnimation(true);
        //model?.PlayDrawAnimation();
       // light.Enabled = true;
    }

    public override void OnDisable()
    {
       // CenteredText.Hide();
        selectedItemSlot = null;
        //BlockMiningEffect.Enabled = false;
        model.Animator.Clear();
        // model?.SetAnimation(false);
        model.Position = startPos;
        //model?.ResetToEnd();

    }
    private Vector3 startPos;

    private void SetSphere(Projectile p)
    {
        p.OnDespawn -= SetSphere;

        despawnPos = p.Position;

        var sphereRenderer = SpheresPool.Instance.Take();
        sphereRenderer.Activate(despawnPos);

    }

    private Vector3 despawnPos = Vector3.Zero;

    public override void Update(Astronaut player)
    {
        if(player.IsAlive == false) 
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

            if (canShoot == false && Input.IsMouseButton(0) && ToggleManager.OpenedWindowsCount < 1 && !Debug.IsVisible)
            {
                canShoot = true;
                model?.SetAnimation(false);
                model?.SetAnimation(true);
                //model.Position = startPos;
                model.Animator.Clear();
                if (model != null)
                    //model.Animator.speed =  1f ;
                    model.Animator.AddAnimation(new ShootAnimation(startPos, model.Position - new Vector3(0.001f * weapon.Pushback, 0, 0), 0.05f));
                model.Animator.speed = weapon.AnimationSpeed;
                //model?.SetAnimation(true);

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

        if (canShoot && Input.IsMouseButton(0))
        {
            if (player.PowerBar.StatsData.Value < weapon.PowerUsage) return;
            canShoot = false;

            player.PowerBar.StatsData.Decrement(weapon.PowerUsage);
      
            int count = 10;
            int count1 = 0;
           
            var projectile = ProjectilesPool.Take();

            if (weapon.ProjectileID == 3)
                projectile.OnDespawn += SetSphere;

            var projectileSpawnPos = Node3D.LocalToWorld(new Vector3(0, 0, 0), player) + player.Front * 0.05f;

            var shotDir = WeaponItem.CalculateSpreadCone(weapon, player.Front);
            var shotRay = new Ray(projectileSpawnPos, shotDir, 1f);

            projectile.Initialize(shotRay,
                projectileParameters, player);

            ApplyRecoilWithMass(player, weapon, shotRay.Direction, projectileParameters);

            _time = 0;

        }

    }

    public void ApplyRecoilWithMass(Astronaut player, WeaponItem weapon, Vector3 shootDirection, ProjectileParameters projectileParams)
    {
        if (weapon.Pushback <= 0) return;

        float projectileMass = projectileParams.Mass * 0.1f;
        float projectileSpeed = projectileParams.Speed;

        float recoilForce = (weapon.Pushback / 255f) * (projectileMass * projectileParameters.Damage * projectileSpeed) * 0.01f;

        Vector3 recoilDirection = -shootDirection;
        Vector3 recoilImpulse = recoilDirection * recoilForce;

        player.InertiaController.ApplyInput(recoilImpulse);
        player.InertiaController.Velocity += recoilImpulse;
    }

}