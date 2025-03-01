using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

using Engine.Audio;
using Engine.Physics;
using Engine.Light;
using Spacebox.Game.Animations;
using Spacebox.Game.Effects;
using Spacebox.Game.Generation;
using Spacebox.Game.Physics;
using Spacebox.GUI;

namespace Spacebox.Game.Player;
using Engine;
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

    private PointLight light;
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
            projectileParameters = GameAssets.Projectiles[weapone.ProjectileID];
            weapon = weapone;
            startPos = model.Position;

           
                shotSound = new AudioSource(GameAssets.Sounds[weapone.ShotSound]); // 
                shotSound.Volume = 1f;
            

        }

        light = PointLightsPool.Instance.Take();

        light.Range = 4;
        light.Ambient = new Color3Byte(100, 116, 255).ToVector3();
        light.Diffuse = new Color3Byte(100, 116, 255).ToVector3();
        light.Specular = new Color3Byte(0, 0, 0).ToVector3();
        light.IsActive = false;
    }

    bool renderSphere = false;
    Vector3 dir = Vector3.Zero;
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

        light.IsActive = false;
    }

    public override void OnDisable()
    {
        CenteredText.Hide();
        selectedItemSlot = null;
        //BlockMiningEffect.Enabled = false;
        model.Animator.Clear();
        // model?.SetAnimation(false);
        model.Position = startPos;
        lineRenderer.ClearPoints();
        //  sphereRenderer.Enabled = false;

        // sphereRenderer.Dispose();
        //  sphereRenderer = null;
        if (PointLightsPool.Instance != null)
            PointLightsPool.Instance.PutBack(light);
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

    float alpha = 1;
    float lightTime = 0;
    public override void Update(Astronaut player)
    {
        if (lightTime > 0)
        {
            if (light.Range > 1)
            {
                //light.Range = light.Range - Time.Delta * 2;
            }
            //light.Position += dir * Time.Delta * projectileParameters.Speed;
            lightTime -= Time.Delta;
        }
        else
        {
            lightTime = 0;
            light.IsActive = false;
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
                InteractiveBlock.UpdateInteractive(lastInteractiveBlock, player, hit.chunk, hit.position);
            }
            else
            {
                CenteredText.Hide();

            }
        }
        else
        {
            CenteredText.Hide();
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
            Ray ray = new Engine.Physics.Ray(player.Position, player.Front, 1000);


            var projectile = ProjectilesPool.Take();

            if (weapon.ProjectileID == 3)
                projectile.OnDespawn += SetSphere;

            if (projectileParameters == null) return;


            light.Ambient = projectileParameters.Color3;
            light.IsActive = false;
            lightTime = 1f;
            dir = player.Front;
            light.Position = player.Position;
            light.Range = 4;
            projectile.Initialize(new Ray(Node3D.LocalToWorld(new Vector3(0, 0, 0), player) + player.Front * 0.05f, player.Front, 1f),
                projectileParameters);
            alpha = 0.3f;

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



    private void DestroyBlock(HitInfo hit)
    {

        hit.block.Durability = 0;
        hit.chunk.RemoveBlock(hit.blockPositionIndex, hit.normal);

    }

}