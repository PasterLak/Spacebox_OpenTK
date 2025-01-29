using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.common.Animation;
using Spacebox.Common;
using Spacebox.Common.Animation;
using Spacebox.Common.Audio;
using Spacebox.Common.Physics;
using Spacebox.FPS;
using Spacebox.Game.Animations;
using Spacebox.Game.Effects;
using Spacebox.Game.Generation;
using Spacebox.Game.Physics;
using Spacebox.GUI;

namespace Spacebox.Game.Player;

public class InteractionShoot : InteractionMode
{

    public static InteractionShoot Instance;
    private static AudioSource shotSound;
    private InteractiveBlock lastInteractiveBlock;
    private ItemSlot selectedItemSlot;
    private AnimatedItemModel model;
    public static BlockMiningEffect BlockMiningEffect;// needs dispose

    public static LineRenderer lineRenderer;// needs dispose

    public static ProjectilesPool ProjectilesPool;  // needs dispose
    private ProjectileParameters projectileParameters;
    private WeaponItem weapon;

    private float _time = 0;
    private bool canShoot = false;

    public SphereRenderer sphereRenderer;
    public InteractionShoot(ItemSlot itemslot)
    {
        Instance = this;
        selectedItemSlot = itemslot;
        AllowReload = true;
        if (BlockMiningEffect == null)
        {
            var texture = TextureManager.GetTexture("Resources/Textures/blockHit.png", true);
            // texture
            BlockMiningEffect = new BlockMiningEffect(Camera.Main, Vector3.Zero, new Vector3(1, 1, 1),
                texture, ShaderManager.GetShader("Shaders/particle"));
        }





        UpdateItemSlot(itemslot);

        lineRenderer = new LineRenderer();
        lineRenderer.Thickness = 0.02f;
        lineRenderer.Color = Color4.Blue;
        lineRenderer.Enabled = true;

        if (ProjectilesPool == null)
        {
            ProjectilesPool = new ProjectilesPool(20);
        }

        var weapone = itemslot.Item as WeaponItem;
        if (weapone != null)
        {
            projectileParameters = GameBlocks.Projectiles[weapone.ProjectileID];
            weapon = weapone;
            startPos = model.Position;

            shotSound = new AudioSource(GameBlocks.Sounds[weapone.ShotSound]);
            shotSound.Volume = 1f;
        }

        if (sphereRenderer == null)
        {
            sphereRenderer = new SphereRenderer(Camera.Main.Position, 0.5f, 8, 8);
            //sphereRenderer.Color = ne;
            sphereRenderer.Color = new Color4(1, 1, 1, 0.1f);
            sphereRenderer.TextureId = TextureManager.GetTexture("Resources/Textures/arSphere.png").Handle;
            sphereRenderer.Scale = new Vector3(1, 1, 1);

        }
        else
        {
            sphereRenderer.Scale = new Vector3(1, 1, 1);

        }


    }

    bool renderSphere = false;

    public void UpdateItemSlot(ItemSlot itemslot)
    {
        selectedItemSlot = itemslot;
        var mod = GameBlocks.ItemModels[itemslot.Item.Id];
        model = mod as AnimatedItemModel;

    }
    public override void OnEnable()
    {
        _time = 0;
        model?.SetAnimation(true);
        sphereRenderer.Enabled = false;

    }

    public override void OnDisable()
    {

        selectedItemSlot = null;
        //BlockMiningEffect.Enabled = false;
        model.Animator.Clear();
        // model?.SetAnimation(false);
        model.Position = startPos;
        lineRenderer.ClearPoints();
        sphereRenderer.Enabled = false;

        sphereRenderer.Dispose();
    }
    private Vector3 startPos;

    private void SetSphere(Projectile p)
    {

        p.OnDespawn -= SetSphere;


        despawnPos = p.Position;

        sphereRenderer.Position = despawnPos;
        sphereRenderer.Scale = new Vector3(1, 1, 1);
        sphereRenderer.Enabled = true;
        alpha = 0.3f;
        renderSphere = true;
    }

    private Vector3 despawnPos = Vector3.Zero;

    float alpha = 1;
    public override void Update(Astronaut player)
    {

        if (renderSphere)

        {

            const int sp = 800;
            sphereRenderer.Scale += new Vector3(sp, sp, sp) * Time.Delta;

            if (alpha > 0)
            {
                alpha -= Time.Delta;

            }
            else
            {
                alpha = 0;
            }
            sphereRenderer.Color = new Color4(1, 1, 1, alpha);

            if (sphereRenderer.Scale.X > 2000 || alpha == 0)
            {
                renderSphere = false;
                sphereRenderer.Enabled = false;
                alpha = 1f;
                sphereRenderer.Scale = new Vector3(1, 1, 1);
                // sphereRenderer.Position = despawnPos;
            }
        }

        if (_time < weapon.ReloadTime * 0.05f)
        {
            _time += Time.Delta;
        }
        else
        {
            if (player.PowerBar.StatsData.Count < weapon.PowerUsage) return;

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

                shotSound.Play();
            }
        }


        if (!player.CanMove)
        {
            //model?.SetAnimation(false);
            return;
        }

        Ray ray2 = new Ray(player.Position, player.Front, InteractiveBlock.InteractionDistance);
        HitInfo hit;

        if (World.CurrentSector.Raycast(ray2, out hit))
        {
            if (hit.block.Is<InteractiveBlock>(out var b))
            {
                lastInteractiveBlock = b;
                UpdateInteractive(lastInteractiveBlock, player, hit.chunk, hit.position);
            }
            else
            {
                CenteredText.Hide();

            }
        }

        if (canShoot && Input.IsMouseButton(0))
        {
            if (player.PowerBar.StatsData.Count < weapon.PowerUsage) return;
            canShoot = false;

            player.PowerBar.StatsData.Decrement(weapon.PowerUsage);
            lineRenderer.ClearPoints();


            //lineRenderer.AddPoint(player.Position + player.Front);
            int count = 10;
            int count1 = 0;
            Ray ray = new Common.Physics.Ray(player.Position, player.Front, 1000);


            var projectile = ProjectilesPool.Take();

            if (weapon.ProjectileID == 3)
                projectile.OnDespawn += SetSphere;

            if (projectileParameters == null) return;


            projectile.Initialize(new Ray(Node3D.LocalToWorld(new Vector3(0, 0, 0), player) + player.Front * 0.05f, player.Front, 1f), projectileParameters);
            alpha = 1f;
            sphereRenderer.Scale = new Vector3(1, 1, 1);
            _time = 0;

            /*if (World.CurrentSector.Raycast(ray, out var hit))
            {

                lineRenderer.AddPoint(hit.position);
            
                while (count > 0)
                {
                    ray = ray.CalculateRicochetRay(hit, 1000);

                    if (World.CurrentSector.Raycast(ray, out  hit))
                    {
                        lineRenderer.AddPoint(hit.position);
                        count--;
                        count1++;
                    }
                    else
                    {
                        lineRenderer.AddPoint(ray.GetPoint(1000));
                        count = 0;
                        count1++;
                    }
                }
            }
            else
            {
                lineRenderer.AddPoint(ray.GetPoint(1000));
                // Debug.Log($"Not gun hit!");
            }*/

            // Debug.Log($"hits "+ count1);

        }
        if (Input.IsMouseButtonUp(0))
        {
            //model?.SetAnimation(false);
            // model.Animator.AddAnimation(new MoveAnimation(startPos, model.Position - new Vector3(0.001f * weapon.Pushback, 0, 0), weapon.AnimationSpeed, false));
        }

    }

    private void UpdateInteractive(InteractiveBlock block, Astronaut player, Chunk chunk, Vector3 hitPos)
    {
        var disSq = Vector3.DistanceSquared(player.Position, hitPos);

        if (disSq > InteractiveBlock.InteractionDistanceSquared)
        {
            CenteredText.Hide();
        }
        else
        {
            CenteredText.Show();
            if (ToggleManager.OpenedWindowsCount == 0)
            {
                if (Input.IsMouseButtonDown(MouseButton.Right))
                {
                    block.chunk = chunk;
                    block.Use(player);

                }
            }
        }
    }

    private void DestroyBlock(HitInfo hit)
    {

        hit.block.Durability = 0;
        hit.chunk.RemoveBlock(hit.blockPositionIndex, hit.normal);

    }

}